"""Prepare versioned local official Qwen3 inputs, without model inference."""
from pathlib import Path
import argparse,hashlib,json,re
from transformers import AutoTokenizer
from qwen3_tuning_inputs import prepare_case,validate_suite

root=Path(__file__).resolve().parents[2]
parser=argparse.ArgumentParser();parser.add_argument('--label',required=True)
parser.add_argument('--mode',choices=['baseline_v2','smoke18','validation100'],required=True)
parser.add_argument('--prompt');parser.add_argument('--role',choices=['user','system'],default='user')
parser.add_argument('--cases')
parser.add_argument('--cpu-threads',type=int,choices=[2,4],default=2)
args=parser.parse_args();assert re.fullmatch('[A-Za-z0-9_-]{1,64}',args.label)
ev=root/'Docs/Tests/Stage3/Evidence/S3-J01/CandidateQwen3'
folder=ev/'Tuning'/args.label;assert not folder.exists(),'Fresh experiment label required'
base_bytes=(ev/'CandidateCases.json').read_bytes();base=json.loads(base_bytes)
v2=(root/'Docs/Stage3/PromptTemplate.txt').read_bytes()
assert hashlib.sha256(v2).hexdigest()=='5b47baccb0048d5ed970c74dfd5f69a62443fd155112788e3eb2b5ca89074bb3'
prompt=(root/args.prompt).read_bytes() if args.prompt else v2
assert b'\r' not in prompt,'Canonical LF prompt required'
text=prompt.decode('utf-8');assert text.count('{retrieved_memory}')==text.count('{player_input}')==1
assert args.mode!='baseline_v2' or (prompt==v2 and args.role=='user' and args.cpu_threads==2)
package=root/'Library/EchoStation/S3Cache/models/qwen3-candidate/original'
tokenizer=AutoTokenizer.from_pretrained(str(package),local_files_only=True,trust_remote_code=False)
rows=[]
if args.mode=='validation100':
    assert args.cases,'Frozen validation suite required'
    suite_bytes=(root/args.cases).read_bytes();suite=json.loads(suite_bytes)
    validate_suite(suite['cases'],{row['input'] for row in base['cases']},text)
    rows=[prepare_case(text,row,tokenizer,args.role) for row in suite['cases']]
else:
  assert args.cases is None,'Alternate cases forbidden for fixed 18-case modes'
  for row in base['cases']:
    before,after=v2.decode().split('{retrieved_memory}');middle,tail=after.split('{player_input}')
    assert row['body'].startswith(before) and row['body'].endswith(middle+row['input']+tail)
    context=row['body'][len(before):len(row['body'])-len(middle+row['input']+tail)]
    start,rest=text.split('{retrieved_memory}');between,end=rest.split('{player_input}')
    body=start+context+between+row['input']+end
    messages=[{'role':args.role,'content':body}]
    if args.role=='system':messages.append({'role':'user','content':row['input']})
    rendered=tokenizer.apply_chat_template(messages,tokenize=False,add_generation_prompt=True,enable_thinking=False)
    ids=tokenizer.apply_chat_template(messages,tokenize=True,add_generation_prompt=True,enable_thinking=False)
    assert len(ids)<=1536 and len(ids)+256<=2048
    if args.mode=='baseline_v2':assert body==row['body'] and rendered==row['officialText'] and ids==row['ids']
    rows.append(dict(row,body=body,officialText=rendered,ids=ids))
if args.mode=='baseline_v2':case_bytes=base_bytes
else:
    updated=dict(base,promptSha256=hashlib.sha256(prompt).hexdigest(),cases=rows)
    updated['scope']='isolated prompt experiment, not production model/tokenizer acceptance'
    updated['messageRole']=args.role
    if args.mode=='validation100':updated['heldOutSuiteSha256']=hashlib.sha256(suite_bytes).hexdigest()
    case_bytes=json.dumps(updated,ensure_ascii=False,indent=2).encode('utf-8')
folder.mkdir(parents=True)
(folder/'Prompt.txt').write_bytes(prompt);(folder/'Cases.json').write_bytes(case_bytes)
record={'label':args.label,'mode':args.mode,'caseFile':(folder/'Cases.json').relative_to(root).as_posix(),
        'cpuThreads':args.cpu_threads,
        'promptFile':(folder/'Prompt.txt').relative_to(root).as_posix(),'promptSha256':hashlib.sha256(prompt).hexdigest(),
        'caseSha256':hashlib.sha256(case_bytes).hexdigest(),'enableThinking':False,'role':args.role,
        'officialTokenizerSha256':hashlib.sha256((package/'tokenizer.json').read_bytes()).hexdigest(),
        'inputLengths':[len(row['ids']) for row in rows]}
encoded=json.dumps(record,ensure_ascii=False,indent=2).encode('utf-8')
(folder/'Run.json').write_bytes(encoded)
(ev/'Tuning/ActiveRun.json').write_bytes(encoded)
print(json.dumps(record,ensure_ascii=True))
