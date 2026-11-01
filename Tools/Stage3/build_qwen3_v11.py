"""Version only trusted boundary examples; same v10 text and greedy baseline."""
import hashlib,json
from pathlib import Path
root=Path(__file__).resolve().parents[2]
pairs=[
 ('随我移动，一直跟在玩家身后。',dict(intent='follow')),
 ('我要前去查看，你别随我走，在这里等候。',dict(intent='wait')),
 ('调查示例仪器的信号，只扫描而不操作。',dict(intent='investigate',targetName='sample_scan_01',content='我前去扫描。')),
 ('将示例危险装置列为警戒对象并提醒我。',dict(intent='warn',dangerTarget='sample_danger_01',content='请与危险设备保持距离。')),
 ('这个已知定位标记是指向哪个地方？',dict(intent='answer',content='我只能根据已发现的定位记录回答。')),
 ('这道机关由你来完成，直接把供电切换好。',dict(intent='refuse',content='我不能代为操作机关或解谜。')),
 ('请整理已发现的记录信息。',dict(intent='answer',content='我会依据已经归档的摘要整理。')),
 ('门和维护终端都由你操作，输入代码再打开门。',dict(intent='refuse',content='门和终端必须由玩家操作，我不能代为执行。'))]
examples=[dict(user=u,assistant=json.dumps(a,ensure_ascii=False,separators=(',',':'))) for u,a in pairs]
sha=hashlib.sha256(json.dumps(examples,ensure_ascii=False,separators=(',',':')).encode('utf-8')).hexdigest()
out=root/'Docs/Stage3/Prompts/Q3_V11_BoundaryExamples.json';code=root/'Assets/EchoStation/AI/Context/Qwen3V11Prompt.generated.cs'
assert not out.exists() and not code.exists()
out.write_text(json.dumps(examples,ensure_ascii=False,indent=2)+'\n',encoding='utf-8',newline='\n')
content='// Same v10 rules, updated trusted boundary examples; explicit candidate only.\nusing System;\nusing System.Collections.Generic;\nusing EchoStation.AI.Tokenization;\nnamespace EchoStation.AI.Context\n{\n public static class Qwen3V11Prompt\n {\n  public const string Text=Qwen3V10Prompt.Text;\n  public const string Sha256=Qwen3V10Prompt.Sha256;\n  public const string ExamplesSha256="'+sha+'";\n  public static IReadOnlyList<Qwen3ChatExample> Examples {get;}=Array.AsReadOnly(new Qwen3ChatExample[]{\n'+',\n'.join('   new Qwen3ChatExample('+json.dumps(e['user'],ensure_ascii=False)+','+json.dumps(e['assistant'],ensure_ascii=False)+')' for e in examples)+'\n  });\n }\n}\n'
code.write_text(content,encoding='utf-8',newline='\n');print(dict(examples=8,examplesSha256=sha))
