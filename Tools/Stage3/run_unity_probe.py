"""Start one actual Owner probe; do not wait on Unity's main thread."""
import argparse,json,sys
from pathlib import Path
root=Path(__file__).resolve().parents[2];sys.path.insert(0,str(root/'Tools/MCP'))
from client import Client
from preflight import payload
p=argparse.ArgumentParser();p.add_argument('--tag',required=True);p.add_argument('--model',default='model_q4.onnx');p.add_argument('--reference',default='');p.add_argument('--length',type=int,default=0);p.add_argument('--repeats',type=int,default=5);a=p.parse_args()
ev=root/'Docs/Tests/Stage3/Evidence/S3-G01';output=ev/(a.tag+'.json')
if output.exists() or output.with_suffix('.Start.json').exists():raise SystemExit('Fresh evidence required')
model=root/'Library/EchoStation/S3Cache/models/qwen-int4/onnx'/a.model
args=[str(model),str(ev/'PrefillFixture.json'),str(ev/a.reference),str(output)]
code='EchoStation.Rendering.Editor.SceneSafetyGuard.RequireClean();\nvar t=EchoStation.AI.Probe.OrtCompatibilityProbe.Start('+','.join(json.dumps(s) for s in args)+f',System.Threading.Thread.CurrentThread.ManagedThreadId,{a.repeats},{a.length});\nreturn new {{started=true,owner=t.ManagedThreadId,main=System.Threading.Thread.CurrentThread.ManagedThreadId}};'
codefile=ev/(a.tag+'.Start.cs.txt');codefile.write_text(code,encoding='utf-8')
raw=Client().request('tools/call',{'name':'execute_code','arguments':{'action':'execute','compiler':'codedom','code':code}})
output.with_suffix('.Start.json').write_text(json.dumps(raw,indent=2),encoding='utf-8');v=payload(raw);print(json.dumps(v,indent=2));raise SystemExit(0 if v.get('success') else 1)
