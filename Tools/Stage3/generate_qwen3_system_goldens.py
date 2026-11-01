"""Pinned official system/user non-thinking oracle, local tokenizer only."""
import hashlib,json
from pathlib import Path
from transformers import AutoTokenizer
root=Path(__file__).resolve().parents[2]
package=root/'Assets/StreamingAssets/EchoLLM/Qwen3_06B_Q4'
config=package/'tokenizer_config.json'
assert hashlib.sha256(config.read_bytes()).hexdigest()=='d5d09f07b48c3086c508b30d1c9114bd1189145b74e982a265350c923acd8101'
tokenizer=AutoTokenizer.from_pretrained(str(package),local_files_only=True,trust_remote_code=False)
pairs=[('只输出JSON。','请跟随我。'),('系统规则：不可操作设备。','相关摘要：已发现信号。\n玩家：请扫描。'),('',''),('规则e\u0301 A\u030a','👩🏽\u200d🔬与𠀀'),('第一行\r\n第二行','用户\t多空格  结束'),('Reply with one JSON object.','Please wait here.'),('冷静、简短。','{"summary":"已发现记录"}'),('规则。'*100,'这是有界用户数据。'*100)]
rows=[]
for system,user in pairs:
 messages=[dict(role='system',content=system),dict(role='user',content=user)]
 rows.append(dict(system=system,user=user,ids=tokenizer.apply_chat_template(messages,tokenize=True,add_generation_prompt=True,enable_thinking=False)))
out=root/'Docs/Tests/Stage3/Evidence/S3-T01w1/SystemGoldens.json';out.parent.mkdir(parents=True,exist_ok=True)
data=(json.dumps(dict(configSha256=hashlib.sha256(config.read_bytes()).hexdigest(),enableThinking=False,cases=rows),ensure_ascii=False,indent=2)+'\n').encode('utf-8')
with out.open('xb') as stream:stream.write(data)
print(dict(cases=len(rows),sha256=hashlib.sha256(data).hexdigest()))
