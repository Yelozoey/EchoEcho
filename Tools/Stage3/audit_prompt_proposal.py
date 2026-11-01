"""Draft text/token count only. Does not run any model or change the active prompt."""
from pathlib import Path
import argparse,json,hashlib
import numpy as np
from tokenizers import Tokenizer
root=Path(__file__).resolve().parents[2];ev=root/'Docs/Tests/Stage3/Evidence/S3-J01'
parser=argparse.ArgumentParser();parser.add_argument('--output',required=True);args=parser.parse_args();outdir=(root/args.output).resolve();assert outdir.is_relative_to(root/'Docs/Tests') and not outdir.exists(),'Fresh output directory required';outdir.mkdir(parents=True)
tok=Tokenizer.from_file(str(root/'Assets/StreamingAssets/EchoLLM/Qwen25_05B_W8A8/tokenizer.json'));tok.encode_special_tokens=True
def encode(text):return tok.encode(text,add_special_tokens=False).ids
def wrapped(body):return [151644]+encode('system\nYou are Qwen, created by Alibaba Cloud. You are a helpful assistant.')+[151645]+encode('\n')+[151644]+encode('user\n'+body)+[151645]+encode('\n')+[151644]+encode('assistant\n')
original=(root/'Docs/Stage3/Archive/PromptV1.txt').read_text(encoding='utf-8');draft=(root/'Docs/Stage3/PromptV2_Proposed.txt').read_text(encoding='utf-8')
def fill(template,memory,player):
 before,after=template.split('{retrieved_memory}');middle,tail=after.split('{player_input}');return before+memory+middle+player+tail
out={'scope':'archived v1/v2 token count only; no model inference or active prompt mutation','originalSha256':hashlib.sha256(original.encode()).hexdigest(),'draftSha256':hashlib.sha256(draft.encode()).hexdigest(),'originalEmptyContextFollowTokens':len(wrapped(fill(original,'','跟随我。'))),'draftEmptyContextFollowTokens':len(wrapped(fill(draft,'','跟随我。'))),'inputCap':1536,'generationCap':256,'contextCap':2048}
(outdir/'PromptV2_DraftAudit.json').write_text(json.dumps(out,indent=2),encoding='utf-8')
reference=json.loads((ev/'IntentFp32_A/IntentReference.json').read_text(encoding='utf-8'));comparisons=[]
for tag in ['IntentAcc4_A','IntentAcc0_A']:
 actual=json.loads((ev/tag/'IntentReference.json').read_text(encoding='utf-8'));rows=[]
 for i,(a,r) in enumerate(zip(actual['cases'],reference['cases'])):
  x=np.fromfile(ev/tag/f'Intent{i}Logits.bin',dtype='<f4').astype(np.float64);y=np.fromfile(ev/'IntentFp32_A'/f'Intent{i}Logits.bin',dtype='<f4').astype(np.float64)
  rows.append({'input':a['input'],'top1Same':a['top20'][0]['id']==r['top20'][0]['id'],'fp32Top1':r['top20'][0]['text'],'cosine':float(np.dot(x,y)/(np.linalg.norm(x)*np.linalg.norm(y))),'rms':float(np.sqrt(np.mean((x-y)**2)))})
 comparisons.append({'tag':tag,'cases':rows})
(outdir/'IntentReferenceSummary.json').write_text(json.dumps({'scope':'historical v1 first intent token only, not full FP32 semantic acceptance','comparisons':comparisons},ensure_ascii=True,indent=2),encoding='utf-8');print(json.dumps(out));print(json.dumps({'allTop1SameAsFp32':all(row['top1Same'] for c in comparisons for row in c['cases'])}))
