"""G01 real CPU ONNX autoregression. Stores tokens/logits, never substitutes scripted output."""
import hashlib,json,time,threading,os,argparse
from pathlib import Path
import numpy as np
import onnxruntime as ort
from transformers import AutoTokenizer
root=Path(__file__).resolve().parents[2];ev=root/'Docs/Tests/Stage3/Evidence/S3-G01';cache=root/'Library/EchoStation/S3Cache/models'
fixture=json.loads((ev/'PrefillFixture.json').read_text(encoding='utf-8'))
parser=argparse.ArgumentParser();parser.add_argument('--model',default='model_q4.onnx');parser.add_argument('--tag',default='');parser.add_argument('--length',type=int,default=0);args=parser.parse_args()
if args.length:fixture['inputIds']=[fixture['inputIds'][i%len(fixture['inputIds'])] for i in range(args.length)]
if args.tag:ev=ev/args.tag;ev.mkdir(parents=True,exist_ok=True)
if (ev/'PythonOrtReference.json').exists():raise SystemExit('Fresh evidence tag required')
def run():
 opts=ort.SessionOptions();opts.intra_op_num_threads=2;opts.inter_op_num_threads=1;opts.execution_mode=ort.ExecutionMode.ORT_SEQUENTIAL;opts.add_session_config_entry('session.intra_op.allow_spinning','0')
 started=time.perf_counter();session=ort.InferenceSession(str(cache/'qwen-int4/onnx'/args.model),sess_options=opts,providers=['CPUExecutionProvider']);load=time.perf_counter()-started
 ids=np.array([fixture['inputIds']],dtype=np.int64);past={f'past_key_values.{i}.{k}':np.zeros((1,2,0,64),np.float32) for i in range(24) for k in ('key','value')};generated=[];steps=[]
 for index in range(3):
  length=len(fixture['inputIds'])+index;feed={'input_ids':ids,'attention_mask':np.ones((1,length),np.int64),'position_ids':np.arange(length-ids.shape[1],length,dtype=np.int64)[None,:],**past}
  started=time.perf_counter();out=session.run(None,feed);elapsed=time.perf_counter()-started
  logits=out[0][0,-1];assert np.isfinite(logits).all();token=int(np.argmax(logits));top=np.argsort(logits)[-10:][::-1]
  generated.append(token);steps.append({'seconds':elapsed,'token':token,'top10':[{'id':int(t),'logit':float(logits[t])} for t in top],'logitsShape':list(out[0].shape),'kvShape':list(out[1].shape)})
  np.save(ev/f'PythonStep{index}Logits.npy',logits)
  logits.astype('<f4').tofile(ev/f'PythonStep{index}Logits.bin')
  past={name.replace('present.','past_key_values.'):value for name,value in zip([o.name for o in session.get_outputs()][1:],out[1:])};ids=np.array([[token]],np.int64)
  print('step',index,token,round(elapsed,3),flush=True)
 tok=AutoTokenizer.from_pretrained(str(cache/'original'),local_files_only=True,trust_remote_code=False)
 result={'model':args.model,'backend':ort.__version__,'providers':session.get_providers(),'mainThread':threading.main_thread().ident,'ownerThread':threading.get_ident(),'pid':os.getpid(),'loadSeconds':load,'inputTokens':len(fixture['inputIds']),'generated':generated,'decoded':tok.decode(generated),'steps':steps,'note':'Short G01 compatibility spike; not full generation/quality acceptance.'}
 (ev/'PythonOrtReference.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8');print(json.dumps(result,ensure_ascii=False,indent=2),flush=True)
errors=[]
def checked_run():
 try:run()
 except BaseException as error:errors.append(error)
thread=threading.Thread(target=checked_run,name='S3-G01-ORT-Owner');thread.start();thread.join()
if errors:raise errors[0]
