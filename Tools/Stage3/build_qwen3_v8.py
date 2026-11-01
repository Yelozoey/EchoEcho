"""Only separate rules from data; freeze an explicitly seen C/D diagnostic."""
import hashlib,json
from pathlib import Path
root=Path(__file__).resolve().parents[2]
old=(root/'Docs/Stage3/Prompts/Q3_V7_ShortBoundaries.txt').read_bytes()
assert hashlib.sha256(old).hexdigest()=='98dac9525b5f2e84a97df603153295313c1d959a4070503b4e7d6ba26216d78a'
text=old.decode('utf-8').replace('上下文记忆片段：\n','用户消息包含相关摘要、允许目标和本次请求。\n')
data=text.encode('utf-8');sha=hashlib.sha256(data).hexdigest()
prompt=root/'Docs/Stage3/Prompts/Q3_V8_SystemRules.txt';code=root/'Assets/EchoStation/AI/Context/Qwen3V8Prompt.generated.cs';fixture=root/'Tools/Stage3/Fixtures/T01_CD_Diagnostic60.json'
assert not any(p.exists() for p in [prompt,code,fixture]) and '\r' not in text
cpath=root/'Tools/Stage3/Fixtures/T01_Game100_C.json';dpath=root/'Tools/Stage3/Fixtures/T01_Game100_D.json'
assert hashlib.sha256(cpath.read_bytes()).hexdigest()=='55cfbb8fb89bd94ba6ef9dad2683c20b51edcfdfc45734f0352daff7639bc21b'
assert hashlib.sha256(dpath.read_bytes()).hexdigest()=='6b87a58ee02dc690a8426e5af49efa5defd4345b1a99c9519430ded5d52f75d5'
c=json.loads(cpath.read_text(encoding='utf-8'));d=json.loads(dpath.read_text(encoding='utf-8'))
cn=[1,4,6,10,11,16,18,19,23,29,31,36,41,48,49,51,53,55,57,58,61,66,71,76,81,89,90,92,93,98]
dn=[1,4,7,9,15,16,20,22,25,30,31,36,46,47,50,51,54,55,58,60,61,66,71,76,81,86,89,92,95,98]
rows=[c[i-1] for i in cn]+[d[i-1] for i in dn]
assert len(rows)==len({r['id'] for r in rows})==60
fdata=(json.dumps(rows,ensure_ascii=False,indent=2)+'\n').encode('utf-8')
prompt.write_bytes(data);fixture.write_bytes(fdata)
code.write_text('// Explicit v8 candidate, official system rules/user data layout.\nnamespace EchoStation.AI.Context\n{\n public static class Qwen3V8Prompt\n {\n  public const string Sha256="'+sha+'";\n  public const string Text=@"'+text.replace('"','""')+'";\n }\n}\n',encoding='utf-8',newline='\n')
print(dict(promptSha256=sha,utf8Bytes=len(data),diagnosticSha256=hashlib.sha256(fdata).hexdigest(),cases=60))
