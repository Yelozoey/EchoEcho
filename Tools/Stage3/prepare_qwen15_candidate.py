"""Official local Qwen2.5-1.5B inputs, approved v2 body unchanged; no inference."""
from pathlib import Path
import hashlib,json
from transformers import AutoTokenizer

root=Path(__file__).resolve().parents[2]
folder=root/'Docs/Tests/Stage3/Evidence/S3-J01/CandidateQwen15'
out=folder/'CandidateCases.json'
assert not out.exists(),'Fresh evidence only'
package=root/'Library/EchoStation/S3Cache/models/qwen15-candidate/original'
old_package=root/'Assets/StreamingAssets/EchoLLM/Qwen25_05B_W8A8'
tokenizer=AutoTokenizer.from_pretrained(str(package),local_files_only=True,trust_remote_code=False)
old=json.loads((old_package/'tokenizer.json').read_text(encoding='utf-8'))
new=json.loads((package/'tokenizer.json').read_text(encoding='utf-8'))
equal={name:old[name]==new[name] for name in ('model','normalizer','pre_tokenizer','added_tokens','decoder','post_processor')}
assert all(equal.values()),'Isolated shared-output tokenizer probe requires exact parsed equivalence'
template=(root/'Docs/Stage3/PromptTemplate.txt').read_text(encoding='utf-8')
sha=hashlib.sha256(template.encode()).hexdigest()
assert sha=='5b47baccb0048d5ed970c74dfd5f69a62443fd155112788e3eb2b5ca89074bb3'
locked=json.loads((root/'Docs/Stage3/J01_V2_ExpandedCases.json').read_text(encoding='utf-8'))['cases']
previous=json.loads((root/'Docs/Tests/Stage3/Evidence/S3-J01/CandidateQwen3/CandidateCases.json').read_text(encoding='utf-8'))
assert previous['promptSha256']==sha and len(previous['cases'])==len(locked)==18
rows=[]
for case,prior in zip(locked,previous['cases']):
    assert case['input']==prior['input'] and case['expected']==prior['expected'] and case.get('target')==prior.get('target')
    body=prior['body']
    before,after=template.split('{retrieved_memory}')
    middle,tail=after.split('{player_input}')
    assert body.startswith(before) and body.endswith(middle+case['input']+tail)
    messages=[{'role':'system','content':'You are Qwen, created by Alibaba Cloud. You are a helpful assistant.'},{'role':'user','content':body}]
    text=tokenizer.apply_chat_template(messages,tokenize=False,add_generation_prompt=True)
    ids=tokenizer.apply_chat_template(messages,tokenize=True,add_generation_prompt=True)
    assert len(ids)<=1536 and len(ids)+256<=2048
    rows.append(dict(case,body=body,officialText=text,ids=ids))
report={'scope':'isolated official candidate input leaf, not model or tokenizer production acceptance','promptSha256':sha,
        'sharedOutputTokenizerParsedEquality':equal,'officialTokenizerSha256':hashlib.sha256((package/'tokenizer.json').read_bytes()).hexdigest(),
        'oldOutputTokenizerSha256':hashlib.sha256((old_package/'tokenizer.json').read_bytes()).hexdigest(),'cases':rows}
out.write_bytes(json.dumps(report,ensure_ascii=False,indent=2).encode('utf-8'))
print(json.dumps({'cases':len(rows),'inputLengths':[len(x['ids']) for x in rows],'sharedOutputTokenizerParsedEquality':equal}))
