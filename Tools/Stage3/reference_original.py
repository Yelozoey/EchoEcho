"""Teacher-forced original FP32 reference, kept separate from game's quantized runtime."""
import json,time,threading,ctypes,os,argparse
os.environ['USE_TF']='0'
from pathlib import Path
import numpy as np
import torch
from transformers import AutoModelForCausalLM,AutoTokenizer
root=Path(__file__).resolve().parents[2];base=root/'Docs/Tests/Stage3/Evidence/S3-G01';cache=root/'Library/EchoStation/S3Cache/models'
parser=argparse.ArgumentParser();parser.add_argument('--reference-tag',default='');args=parser.parse_args();ev=base/args.reference_tag
if (ev/'OriginalReference.json').exists():raise SystemExit('Fresh reference evidence required')
torch.set_num_threads(2);torch.set_num_interop_threads(1)
fixture=json.loads((base/'PrefillFixture.json').read_text(encoding='utf-8'));expected=json.loads((ev/'PythonOrtReference.json').read_text(encoding='utf-8'))
t=time.perf_counter();model=AutoModelForCausalLM.from_pretrained(str(cache/'original'),local_files_only=True,trust_remote_code=False,dtype=torch.float32,low_cpu_mem_usage=True,attn_implementation='eager').eval();load=time.perf_counter()-t
ids=torch.tensor([fixture['inputIds']]);past=None;steps=[];own=[]
with torch.inference_mode():
 for index in range(3):
  t=time.perf_counter();out=model(input_ids=ids,past_key_values=past,use_cache=True);elapsed=time.perf_counter()-t
  logits=out.logits[0,-1].cpu().numpy();q=np.load(ev/f'PythonStep{index}Logits.npy');top=np.argsort(logits)[-10:][::-1];qt=np.argsort(q)[-10:][::-1]
  cosine=float(np.dot(logits,q)/(np.linalg.norm(logits)*np.linalg.norm(q)));rms=float(np.sqrt(np.mean((logits-q)**2)));overlap=len(set(top)&set(qt))
  token=int(np.argmax(logits));own.append(token);steps.append({'teacherForcedOnInt4Prefix':index>0,'token':token,'int4Token':expected['generated'][index],'cosine':cosine,'rms':rms,'top10Overlap':overlap,'withinPredeclaredTolerance':cosine>=.98 and rms<=1 and overlap>=5,'seconds':elapsed})
  past=out.past_key_values;ids=torch.tensor([[expected['generated'][index]]]);np.save(ev/f'OriginalStep{index}Logits.npy',logits);print(steps[-1],flush=True)
tok=AutoTokenizer.from_pretrained(str(cache/'original'),local_files_only=True,trust_remote_code=False)
result={'dtype':'float32','referenceOnlyNotGameRuntime':True,'loadSeconds':load,'steps':steps,'teacherForcedArgmaxTokens':own,'decodedTeacherForcedArgmax':tok.decode(own),'allWithinTolerance':all(s['withinPredeclaredTolerance'] for s in steps)}
(ev/'OriginalReference.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8');print(json.dumps(result,ensure_ascii=False,indent=2),flush=True)
