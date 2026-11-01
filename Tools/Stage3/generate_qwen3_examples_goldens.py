"""Official local multi-turn wrapper oracle. No inference or network."""
import hashlib,json
from pathlib import Path
from transformers import AutoTokenizer
root=Path(__file__).resolve().parents[2];package=root/'Assets/StreamingAssets/EchoLLM/Qwen3_06B_Q4'
assert hashlib.sha256((package/'tokenizer_config.json').read_bytes()).hexdigest()=='d5d09f07b48c3086c508b30d1c9114bd1189145b74e982a265350c923acd8101'
tokenizer=AutoTokenizer.from_pretrained(str(package),local_files_only=True,trust_remote_code=False)
rows=[]
for n in [1,2,6,8]:
 examples=[dict(user=f'示例请求{i} e\u0301',assistant='{"intent":"answer","content":"有界示例。"}') for i in range(n)]
 system='输出一个JSON，中文简短。';user='当前请求：𠀀与👩🏽\u200d🔬。'
 messages=[dict(role='system',content=system)]
 for item in examples:messages.extend([dict(role='user',content=item['user']),dict(role='assistant',content=item['assistant'])])
 messages.append(dict(role='user',content=user))
 rows.append(dict(system=system,user=user,examples=examples,ids=tokenizer.apply_chat_template(messages,tokenize=True,add_generation_prompt=True,enable_thinking=False)))
out=root/'Docs/Tests/Stage3/Evidence/S3-T01w3/ExamplesGoldens.json';out.parent.mkdir(parents=True,exist_ok=True)
data=(json.dumps(dict(enableThinking=False,configSha256=hashlib.sha256((package/'tokenizer_config.json').read_bytes()).hexdigest(),cases=rows),ensure_ascii=False,indent=2)+'\n').encode('utf-8')
with out.open('xb') as stream:stream.write(data)
print(dict(cases=4,sha256=hashlib.sha256(data).hexdigest()))
