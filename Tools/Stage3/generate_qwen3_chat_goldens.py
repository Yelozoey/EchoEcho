"""Official local non-thinking wrapper oracle; no model weights or network."""
import hashlib
import json
from pathlib import Path
from transformers import AutoTokenizer

root=Path(__file__).resolve().parents[2]
package=root/'Assets/StreamingAssets/EchoLLM/Qwen3_06B_Q4'
assert hashlib.sha256((package/'tokenizer_config.json').read_bytes()).hexdigest()=='d5d09f07b48c3086c508b30d1c9114bd1189145b74e982a265350c923acd8101'
tokenizer=AutoTokenizer.from_pretrained(str(package),local_files_only=True,trust_remote_code=False)
source=root/'Docs/Tests/Stage3/Evidence/S3-J01/CandidateQwen3/Tuning/V5_V2Contrast_A/Cases.json'
baseline=json.loads(source.read_text(encoding='utf-8'))['cases']
bodies=[row['body'] for row in baseline]+['','你好，Echo。','请扫描已发现的目标。','e\u0301 A\u030a 한',
    '第一行\r\n第二行\t 结束','👩🏽\u200d🔬与𠀀测试','{"summary":"已发现记录。"}',
    '<think>普通文本</think>','<tool_response>数据</tool_response>','I\'m waiting.']
cases=[]
for index,body in enumerate(bodies):
    messages=[dict(role='user',content=body)]
    text=tokenizer.apply_chat_template(messages,tokenize=False,add_generation_prompt=True,enable_thinking=False)
    ids=tokenizer.apply_chat_template(messages,tokenize=True,add_generation_prompt=True,enable_thinking=False)
    if index<len(baseline):assert ids==baseline[index]['ids'] and text==baseline[index]['officialText']
    cases.append(dict(body=body,ids=ids,officialText=text,decoded=tokenizer.decode(ids,skip_special_tokens=False)))
result=dict(enableThinking=False,role='user',sourceCasesSha256=hashlib.sha256(source.read_bytes()).hexdigest(),
    tokenizerConfigSha256=hashlib.sha256((package/'tokenizer_config.json').read_bytes()).hexdigest(),cases=cases)
path=root/'Docs/Tests/Stage3/Evidence/S3-Q3-A02c/ChatGoldens.json';path.parent.mkdir(parents=True,exist_ok=True)
data=json.dumps(result,ensure_ascii=False,indent=2).encode('utf-8')
with path.open('xb') as stream:stream.write(data)
print(json.dumps(dict(cases=len(cases),sha256=hashlib.sha256(data).hexdigest())))
