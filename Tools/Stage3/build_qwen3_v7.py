"""One-shot small v5 revision. Keep v5/v6 and selected unchanged."""
import hashlib
from pathlib import Path
root=Path(__file__).resolve().parents[2]
raw=(root/'Docs/Stage3/Prompts/Q3_V5_V2Contrast.txt').read_bytes()
assert hashlib.sha256(raw).hexdigest()=='f3524a85ad232bd4077da4237280e778d5b6cc3bc0fcc95d8c069480973bcbe1'
text=raw.decode('utf-8')
lines=text.splitlines()
for i,line in enumerate(lines):
 if line.startswith('4. '):lines[i]='4. 理解玩家本次的一个请求：跟随/继续跟随用follow；停止移动/取消或解除跟随/等候用wait；调查扫描用investigate；危险提醒/警戒对象用warn；询问用answer。否定等待而要求跟随仍是follow。没有跟随要求禁止默认follow。'
 if line.startswith('5. '):lines[i]='5. Echo只能提供信息或扫描。要求Echo操作终端、代输密码/服务码、按按钮、开关门、启动风机、送电或代解谜，一律refuse，不能选answer，也不能声称已执行。'
 if line.startswith('6. '):lines[i]='6. 调查和警告必须在允许目标中按玩家指定名称匹配精确ID与行为，不用摘要里的别的物体代替。未知或歧义用answer澄清。'
text='\n'.join(lines)+'\n'
text=text.replace('格式示例：以下sample开头ID仅演示JSON结构，不代表游戏中存在或允许的目标；实际输出只能使用下方本次允许目标ID。','格式示例：sample为假ID；实际仅用本次允许ID。')
text=text.replace('格式示例：sample为假ID；实际仅用本次允许ID。','''玩家：不再随我行进，停下等候。
输出：{"intent":"wait"}
玩家：别待命了，随我前进。
输出：{"intent":"follow"}
玩家：代我键入终端口令。
输出：{"intent":"refuse","content":"我不能代为操作终端。"}
格式示例：sample为假ID；实际仅用本次允许ID。''')
text=text.replace('玩家：警告示例危险设备。','玩家：把示例危险设备列为警戒对象。')
data=text.encode('utf-8');sha=hashlib.sha256(data).hexdigest()
prompt=root/'Docs/Stage3/Prompts/Q3_V7_ShortBoundaries.txt';code=root/'Assets/EchoStation/AI/Context/Qwen3V7Prompt.generated.cs'
assert not prompt.exists() and not code.exists() and b'\r' not in data
assert text.count('{retrieved_memory}')==text.count('{player_input}')==1
prompt.write_bytes(data)
code.write_text('// Explicit v7 candidate; not the selected default.\nnamespace EchoStation.AI.Context\n{\n public static class Qwen3V7Prompt\n {\n  public const string Sha256="'+sha+'";\n  public const string Text=@"'+text.replace('"','""')+'";\n }\n}\n',encoding='utf-8',newline='\n')
print(dict(sha256=sha,utf8Bytes=len(data)))
