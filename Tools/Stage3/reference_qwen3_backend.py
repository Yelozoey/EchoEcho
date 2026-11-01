"""Independent Qwen3 CPU oracle, all native resources on one background owner."""
import gc
import hashlib
import json
import os
import threading
import time
from pathlib import Path
import numpy as np
import onnxruntime as ort

ROOT=Path(__file__).resolve().parents[2]
EVIDENCE=ROOT/'Docs/Tests/Stage3/Evidence/S3-Q3-A03r'
DATA=ROOT/'Library/EchoStation/S3Cache/qwen3-native-reference-A'
MODEL=ROOT/'Assets/StreamingAssets/EchoLLM/Qwen3_06B_Q4/model.onnx'
SHA='3133e1a38a41588d5122e96f89d6ccdd06ce641c4432f547e99ea8a690ead303'

def digest(path):
    with path.open('rb') as stream:return hashlib.file_digest(stream,'sha256').hexdigest()

def save(record):
    temporary=EVIDENCE/'Reference.json.tmp'
    temporary.write_bytes(json.dumps(record,ensure_ascii=False,indent=2).encode('utf-8'))
    temporary.replace(EVIDENCE/'Reference.json')

def binary(name,values):
    path=DATA/name
    with path.open('xb') as stream:
        for value in values:stream.write(np.ascontiguousarray(value,dtype='<f4').tobytes())
    return dict(path=path.relative_to(ROOT).as_posix(),bytes=path.stat().st_size,sha256=digest(path))

def metadata(nodes):
    return [dict(name=node.name,type=node.type,dimensions=[value if isinstance(value,int) else -1 for value in node.shape]) for node in nodes]

def run_owner(main,stop,ready,done):
    session=None;values=None;feeds=None;past=None
    record=dict(scope='independent same-optimized-INT4 reference; not original FP32, game or performance acceptance',
        mainThread=main,ownerThread=threading.get_ident(),pid=os.getpid(),cpuThreads=4,interOpThreads=1,
        modelSha256=SHA,completed=False,passed=False,cases=[],nativeCalls=0,ort=ort.__version__)
    started=time.monotonic()
    try:
        assert threading.get_ident()!=main and ort.__version__.startswith('1.31.')
        assert MODEL.stat().st_size==919096845 and digest(MODEL)==SHA
        ort.disable_telemetry_events();options=ort.SessionOptions()
        options.intra_op_num_threads=4;options.inter_op_num_threads=1;options.execution_mode=ort.ExecutionMode.ORT_SEQUENTIAL
        options.graph_optimization_level=ort.GraphOptimizationLevel.ORT_ENABLE_ALL
        options.add_session_config_entry('session.intra_op.allow_spinning','0');options.add_session_config_entry('session.inter_op.allow_spinning','0')
        session=ort.InferenceSession(str(MODEL),sess_options=options,providers=['CPUExecutionProvider'])
        record['metadata']=dict(inputs=metadata(session.get_inputs()),outputs=metadata(session.get_outputs()))
        assert len(session.get_inputs())==59 and len(session.get_outputs())==57
        record['coldSeconds']=time.monotonic()-started
        assert record['coldSeconds']<30 and not stop.is_set()
        ready.set();save(record)
        source=ROOT/'Docs/Tests/Stage3/Evidence/S3-J01/CandidateQwen3/Tuning/V5_V2Contrast_A/Cases.json'
        raw=source.read_bytes();record['sourceCasesSha256']=hashlib.sha256(raw).hexdigest();base=json.loads(raw)['cases'][0]['ids']
        assert len(base)==669
        for length in [1,16,64,669,1536]:
            if stop.is_set():raise RuntimeError('Reference cancelled')
            ids=np.resize(np.asarray(base,dtype=np.int64),length).reshape(1,-1)
            names=[f'past_key_values.{layer}.{kind}' for layer in range(28) for kind in ['key','value']]
            past={name:np.empty((1,8,0,128),dtype=np.float32) for name in names}
            item=dict(id=f'len{length}',inputIds=ids[0].tolist(),steps=[]);record['cases'].append(item);consumed=0
            for step in range(3):
                if stop.is_set():raise RuntimeError('Reference cancelled')
                total=consumed+ids.shape[1]
                feeds=dict(input_ids=ids,attention_mask=np.ones((1,total),dtype=np.int64),position_ids=np.arange(consumed,total,dtype=np.int64).reshape(1,-1),**past)
                stamp=time.monotonic();record['nativeCalls']+=1;values=session.run(None,feeds);elapsed=time.monotonic()-stamp
                assert elapsed<20 and len(values)==57 and values[0].shape==(1,1,151936)
                for index,value in enumerate(values):
                    assert value.dtype==np.float32 and np.isfinite(value).all()
                    assert value.shape==((1,1,151936) if index==0 else (1,8,total,128))
                token=int(np.argmax(values[0][0,-1]));row=dict(step=step,consumed=total,token=token,nativeSeconds=elapsed,allOutputsFinite=True,
                    logits=binary(f'len{length}_step{step}_logits.bin',[values[0]]))
                if length<=64:row['kv']=binary(f'len{length}_step{step}_kv.bin',values[1:])
                else:row['kvScope']='shape and all finite checked; long KV binary not stored'
                item['steps'].append(row);past=dict(zip(names,values[1:]));consumed=total;ids=np.asarray([[token]],dtype=np.int64);save(record)
            past=None;values=None;feeds=None;gc.collect()
        assert digest(MODEL)==SHA
        record['passed']=True
    except Exception as error:
        # Strings only: no traceback frames or native objects escape the Owner.
        record['error']=dict(type=type(error).__name__,message=str(error))
    finally:
        past=None;values=None;feeds=None;session=None;gc.collect()
        record['releaseThread']=threading.get_ident();record['ownerReferencesCleared']=True
        record['completed']=True;record['seconds']=time.monotonic()-started
        try:save(record)
        finally:done.set()

if __name__=='__main__':
    assert not DATA.exists() and not (EVIDENCE/'Reference.json').exists(),'Fresh reference paths required'
    budget=json.loads((ROOT/'Docs/Tests/Stage3/Evidence/S3-Q3-A03r/BudgetBefore.json').read_text(encoding='utf-8'))
    assert budget['underLimit'] and budget['totalBytes']+128*1024*1024<=20*1024**3
    EVIDENCE.mkdir(parents=True,exist_ok=True);DATA.mkdir(parents=True)
    stop=threading.Event();ready=threading.Event();done=threading.Event();main=threading.get_ident()
    owner=threading.Thread(target=run_owner,args=(main,stop,ready,done),daemon=True,name='Q3-Reference-Owner');owner.start();clock=time.monotonic()
    while not done.wait(.1):
        if not ready.is_set() and time.monotonic()-clock>=30:stop.set()
        if time.monotonic()-clock>=500:stop.set()
    result=json.loads((EVIDENCE/'Reference.json').read_text(encoding='utf-8'))
    print(json.dumps({key:result.get(key) for key in ['passed','nativeCalls','coldSeconds','seconds','error','ownerReferencesCleared']}))
    raise SystemExit(0 if result['passed'] else 1)
