"""Real isolated CPU reference for the pinned Qwen1.5 candidate; not acceptance."""
from pathlib import Path
import argparse,hashlib,json,os,threading,time

ROOT=Path(__file__).resolve().parents[2]
def sha(path):
    with Path(path).open('rb') as stream:return hashlib.file_digest(stream,'sha256').hexdigest()
def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--receipt',required=True)
    parser.add_argument('--tag',required=True)
    parser.add_argument('--mode',choices=['reference','intent'],default='reference')
    args=parser.parse_args()
    evidence=ROOT/'Docs/Tests/Stage3/Evidence/S3-J01/CandidateQwen15'
    output=evidence/(args.tag+'.json')
    assert output.parent==evidence and not output.exists(),'Fresh local evidence tag required'
    receipt_path=(ROOT/args.receipt).resolve()
    assert receipt_path.is_relative_to(evidence),'Candidate receipt must be local evidence'
    receipt=json.loads(receipt_path.read_text(encoding='utf-8'))
    assert receipt.get('schemaVersion')==1 and receipt.get('completed') is True,'Conversion must complete first'
    assert receipt['sourceGraph']['sha256']=='8f7da5d0bb5e5b6eba0ba0e9c006fcbc8b670134405ef5e02aaaf738361e2074'
    assert receipt['sourceExternal']['sha256']=='7111acf0e546fe192035357890402832315b4c243e56b01b47ce9358285fc82e'
    assert receipt['transform']['lastPositionGather'] is True and receipt['transform']['constantHead'] is True
    assert receipt['quantizedInventory']=={'MatMulNBits':197,'GatherBlockQuantized':1}
    assert receipt['config']['bits']==4 and receipt['config']['blockSize'] in (64,128,256)
    assert receipt['config']['isSymmetric'] is True and receipt['config']['format']=='QOperator'
    model=(ROOT/receipt['output']['path']).resolve()
    assert model.is_relative_to(ROOT/'Library/EchoStation/S3Cache/models/qwen15-candidate')
    actual=sha(model)
    assert actual==receipt['output']['sha256'] and model.stat().st_size==receipt['output']['bytes']
    assert model.stat().st_size<1024**3,'Model alone exceeds package cap'
    case_path=evidence/'CandidateCases.json'
    cases=json.loads(case_path.read_text(encoding='utf-8'))
    official=ROOT/'Library/EchoStation/S3Cache/models/qwen15-candidate/original'
    old=ROOT/'Assets/StreamingAssets/EchoLLM/Qwen25_05B_W8A8/tokenizer.json'
    old_json=json.loads(old.read_text(encoding='utf-8'))
    new_json=json.loads((official/'tokenizer.json').read_text(encoding='utf-8'))
    fields=('model','normalizer','pre_tokenizer','added_tokens','decoder','post_processor')
    assert len(cases['cases'])==18 and set(cases['sharedOutputTokenizerParsedEquality'])==set(fields)
    assert all(cases['sharedOutputTokenizerParsedEquality'].values()) and all(old_json[key]==new_json[key] for key in fields)
    assert sha(official/'tokenizer.json')==cases['officialTokenizerSha256'] and sha(old)==cases['oldOutputTokenizerSha256']
    def capture_bindings():
        return {'modelSha256':sha(model),'receiptSha256':sha(receipt_path),'casesSha256':sha(case_path),
                'officialFiles':{name:sha(official/name) for name in ('tokenizer.json','tokenizer_config.json','config.json','generation_config.json')},
                'oldOutputTokenizerSha256':sha(old)}
    bindings=capture_bindings()
    report={'scope':'isolated candidate numerical/teacher-forced reference; not full semantic, Unity, Windows or performance acceptance',
            'mode':args.mode,'bindingsStart':bindings,'mainThread':threading.get_ident(),'pid':os.getpid(),'cpuThreads':2,
            'completed':False,'passed':False,'requests':[],
            'passScope':'execution/shape/finite checks only; semanticAcceptanceEvaluated=false; equivalenceCompared=false',
            'semanticAcceptanceEvaluated':False,'equivalenceCompared':False,
            'bindingPolicy':'SHA checks before/after without immutable lease; changes restored between checks are not excluded',
            'timeoutPolicy':'diagnostic observes elapsed after native returns; no live 20s/30s cancellation; not production timeout acceptance'}
    def save():output.write_bytes(json.dumps(report,ensure_ascii=False,indent=2).encode('utf-8'))
    save()
    def worker():
        # No native imports or native objects are transferred to the control thread.
        session=values=past=feed=logits=options=tokenizer=None
        try:
            import numpy as np
            import onnxruntime as ort
            from tokenizers import Tokenizer
            report['ownerThread']=threading.get_ident()
            ort.disable_telemetry_events();options=ort.SessionOptions()
            options.intra_op_num_threads=2;options.inter_op_num_threads=1
            options.execution_mode=ort.ExecutionMode.ORT_SEQUENTIAL
            options.add_session_config_entry('session.intra_op.allow_spinning','0')
            options.add_session_config_entry('session.inter_op.allow_spinning','0')
            started=time.perf_counter()
            session=ort.InferenceSession(str(model),sess_options=options,providers=['CPUExecutionProvider'])
            report['loadSeconds']=time.perf_counter()-started
            assert report['loadSeconds']<30,'Cold native load exceeded logical budget'
            input_names={i.name for i in session.get_inputs()}
            expected={'input_ids','attention_mask','position_ids'}|{f'past_key_values.{i}.{kind}' for i in range(28) for kind in ('key','value')}
            names=['logits']+[f'present.{i}.{kind}' for i in range(28) for kind in ('key','value')]
            assert input_names==expected and {i.name for i in session.get_outputs()}==set(names),'Unexpected tensor contract'
            tokenizer=Tokenizer.from_file(str(official/'tokenizer.json'))
            tokenizer.encode_special_tokens=True
            prefix=tokenizer.encode('{"intent":"',add_special_tokens=False).ids
            words={word:tokenizer.encode(word,add_special_tokens=False).ids[0] for word in ('follow','investigate','wait','warn','answer','refuse')}
            report['prefixIds']=prefix;report['wordFirstIds']=words
            requests=cases['cases'] if args.mode=='intent' else [cases['cases'][0],cases['cases'][0]]
            past=None;consumed=0
            for index,case in enumerate(requests):
                if args.mode=='intent':ids=case['ids']+prefix;past=None;consumed=0
                elif index==0:ids=case['ids'][:64]
                else:ids=[int(np.argmax(values[0][0,-1]))]
                feed={'input_ids':np.array([ids],np.int64),'attention_mask':np.ones((1,consumed+len(ids)),np.int64),
                      'position_ids':np.arange(consumed,consumed+len(ids),dtype=np.int64)[None,:]}
                for layer in range(28):
                    for kind_index,kind in enumerate(('key','value')):
                        feed[f'past_key_values.{layer}.{kind}']=np.zeros((1,2,0,128),np.float32) if past is None else past[layer*2+kind_index]
                started=time.perf_counter();values=session.run(names,feed);seconds=time.perf_counter()-started
                assert seconds<20,'Single native request exceeded logical budget'
                total=consumed+len(ids)
                assert values[0].shape==(1,1,151936) and values[0].dtype==np.float32 and np.isfinite(values[0]).all()
                assert all(value.shape==(1,2,total,128) and value.dtype==np.float32 and np.isfinite(value).all() for value in values[1:])
                row={'index':index,'input':case['input'],'expected':case['expected'],'inputIds':ids,'consumedBefore':consumed,
                     'seconds':seconds,'logitsShape':list(values[0].shape),'kvShape':list(values[1].shape)}
                logits=values[0][0,-1]
                if args.mode=='intent':
                    row['wordFirstScores']={word:float(logits[token]) for word,token in words.items()}
                    row['bestIntentFirstToken']=max(words,key=lambda word:row['wordFirstScores'][word])
                else:
                    binary=evidence/f'{args.tag}_Step{index}_Logits.bin'
                    assert not binary.exists();logits.astype('<f4').tofile(binary)
                    row['logitsFile']=binary.name;row['logitsSha256']=sha(binary)
                    row['argmax']=int(np.argmax(logits))
                    past=values[1:];consumed=total
                report['requests'].append(row);save()
                print(json.dumps({key:row[key] for key in ('index','input','seconds')},ensure_ascii=True),flush=True)
                if args.mode=='intent':values=None;feed=None
                time.sleep(.3)
            report['completed']=True;report['passed']=True
        except BaseException as error:
            report['errorType']=type(error).__name__;report['error']=str(error)
            error.__traceback__=error.__context__=error.__cause__=None
        finally:
            session=values=past=feed=logits=options=tokenizer=None
            import gc;gc.collect()
            report['ownerCleanupComplete']=True
            save()
    thread=threading.Thread(target=worker,name='S3-Qwen15-Reference-Owner',daemon=True)
    thread.start();thread.join()
    report['bindingsEnd']=capture_bindings()
    report['bindingsStable']=report['bindingsStart']==report['bindingsEnd'];save()
    assert report['completed'] and report['passed'] and report['bindingsStable'],report.get('error','Reference incomplete')
    print(json.dumps({'completed':True,'requests':len(report['requests']),'output':str(output)}))
if __name__=='__main__':main()
