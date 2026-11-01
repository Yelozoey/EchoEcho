"""Read-only numerical diagnosis at the first intent token; not semantic acceptance."""
from pathlib import Path
import argparse,json,threading,time,os
import numpy as np
import onnxruntime as ort
from tokenizers import Tokenizer
root=Path(__file__).resolve().parents[2]
parser=argparse.ArgumentParser();parser.add_argument('--model',required=True);parser.add_argument('--tag',required=True);args=parser.parse_args()
folder=root/'Docs/Tests/Stage3/Evidence/S3-J01'/args.tag;folder.mkdir(parents=True,exist_ok=True)
output=folder/'IntentReference.json'
if output.exists():raise SystemExit('Fresh evidence tag required')
source=max((root/'Docs/Tests/Stage3/Evidence/S3-J01').glob('Ablation_*.json'),key=lambda p:p.stat().st_mtime)
rows=[r for r in json.loads(source.read_text(encoding='utf-8'))['requests'] if r['mode']=='A'];assert len(rows)==6
tokenizer=Tokenizer.from_file(str(root/'Assets/StreamingAssets/EchoLLM/Qwen25_05B_W8A8/tokenizer.json'));tokenizer.encode_special_tokens=True
def encode(text):return tokenizer.encode(text,add_special_tokens=False).ids
def wrap(body):return [151644]+encode('system\nYou are Qwen, created by Alibaba Cloud. You are a helpful assistant.')+[151645]+encode('\n')+[151644]+encode('user\n'+body)+[151645]+encode('\n')+[151644]+encode('assistant\n')
prefix=encode('{"intent":"');words={w:encode(w)[0] for w in ['follow','investigate','wait','warn','answer','refuse']};errors=[]
def run():
 ort.disable_telemetry_events();opts=ort.SessionOptions();opts.intra_op_num_threads=2;opts.inter_op_num_threads=1;opts.execution_mode=ort.ExecutionMode.ORT_SEQUENTIAL;opts.add_session_config_entry('session.intra_op.allow_spinning','0')
 model=root/'Library/EchoStation/S3Cache/models/qwen-int4/onnx'/args.model;started=time.perf_counter();session=ort.InferenceSession(str(model),sess_options=opts,providers=['CPUExecutionProvider']);report={'model':args.model,'loadSeconds':time.perf_counter()-started,'ownerThread':threading.get_ident(),'mainThread':threading.main_thread().ident,'pid':os.getpid(),'prefixIds':prefix,'wordFirstIds':words,'cases':[],'scope':'teacher-forced intent prefix logits only; no schema/quality acceptance'}
 for index,row in enumerate(rows):
  ids=wrap(row['fixedBody'])+prefix;length=len(ids);feed={'input_ids':np.array([ids],np.int64),'attention_mask':np.ones((1,length),np.int64),'position_ids':np.arange(length,dtype=np.int64)[None,:]}
  feed.update({f'past_key_values.{i}.{kind}':np.zeros((1,2,0,64),np.float32) for i in range(24) for kind in ['key','value']})
  started=time.perf_counter();result=session.run(['logits'],feed)[0];logits=result[0,-1];assert np.isfinite(logits).all();top=np.argsort(logits)[-20:][::-1]
  logits.astype('<f4').tofile(folder/f'Intent{index}Logits.bin');case={'input':row['input'],'expected':row['expected'],'tokens':length,'seconds':time.perf_counter()-started,'wordFirstScores':{w:float(logits[token]) for w,token in words.items()},'top20':[{'id':int(token),'text':tokenizer.decode([int(token)],skip_special_tokens=False),'score':float(logits[token])} for token in top]};report['cases'].append(case);output.write_text(json.dumps(report,ensure_ascii=True,indent=2),encoding='utf-8');print(json.dumps({'input':row['input'],'expected':row['expected'],'scores':case['wordFirstScores']},ensure_ascii=True),flush=True);del result
 report['completed']=True;output.write_text(json.dumps(report,ensure_ascii=True,indent=2),encoding='utf-8')
def checked():
 try:run()
 except BaseException as error:errors.append(error)
t=threading.Thread(target=checked,name='S3-Intent-Reference');t.start();t.join()
if errors:raise errors[0]
