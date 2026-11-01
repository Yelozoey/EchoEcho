"""Fresh compile gate and the repository's mandatory module-test wrapper."""
import argparse,json,subprocess,sys,time
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT/'Tools/MCP'))
from client import Client
from preflight import payload
p=argparse.ArgumentParser();p.add_argument('--label',required=True);p.add_argument('--ready-only',action='store_true');p.add_argument('--assembly',action='append',default=[]);p.add_argument('--group',action='append',default=[]);a=p.parse_args()
rel='Docs/Tests/SalvagePrototype/Evidence/LLMShortTasks/L02Q01';folder=ROOT/rel;c=Client();deadline=time.monotonic()+240;last=None
while True:
 raw=c.request('resources/read',{'uri':'mcpforunity://editor/state'});r=json.loads(raw['contents'][0]['text']);d=r.get('data') or {};busy=(d.get('compilation') or {}).get('is_compiling') or (d.get('assets') or {}).get('is_updating');status=(r.get('success'),busy,(d.get('assets') or {}).get('external_changes_dirty'))
 if status!=last:print('Readiness',status,flush=True);last=status
 fresh=True
 for assembly,relative in {'EchoStation.L02Q01.Editor.Tests':'Assets/EchoStation/LLMShortTasks/L02Q01/Tests','EchoStation.AI.Context':'Assets/EchoStation/AI/Context','EchoStation.L02.Service':'Assets/EchoStation/LLMShortTasks/L02/Service','EchoStation.L02.Service.Editor.Tests':'Assets/EchoStation/LLMShortTasks/L02/Tests/Service','EchoStation.L02.Domain':'Assets/EchoStation/LLMShortTasks/L02/Domain','EchoStation.L02.Domain.Editor.Tests':'Assets/EchoStation/LLMShortTasks/L02/Tests/Domain','EchoStation.L02.Runtime':'Assets/EchoStation/LLMShortTasks/L02/Runtime','EchoStation.L02.Runtime.Editor.Tests':'Assets/EchoStation/LLMShortTasks/L02/Tests/Runtime','EchoStation.L01.Domain':'Assets/EchoStation/LLMShortTasks/L01/Domain','EchoStation.L01.Domain.Editor.Tests':'Assets/EchoStation/LLMShortTasks/L01/Tests/Domain','EchoStation.L01.Runtime':'Assets/EchoStation/LLMShortTasks/L01/Runtime','EchoStation.L01.Runtime.Editor.Tests':'Assets/EchoStation/LLMShortTasks/L01/Tests/Runtime','EchoStation.Player.Runtime':'Assets/EchoStation/Player/Runtime'}.items():
  if not (ROOT/relative).exists():continue
  source=ROOT/relative;dll=ROOT/('Library/ScriptAssemblies/'+assembly+'.dll');newest=max(f.stat().st_mtime for f in source.rglob('*') if f.suffix in ['.cs','.asmdef'])
  fresh&=dll.exists() and dll.stat().st_mtime>=newest
 if r.get('success') and d.get('advice') and not busy and not (d.get('assets') or {}).get('external_changes_dirty') and fresh:break
 if time.monotonic()>deadline:raise SystemExit('Compilation/import gate timed out; do not run stale assemblies')
 time.sleep(2)
raw=c.request('tools/call',{'name':'read_console','arguments':{'action':'get','types':['error'],'count':20,'format':'detailed'}});(folder/(a.label+'-Console.json')).write_text(json.dumps(raw,ensure_ascii=False,indent=2),encoding='utf-8');errors=payload(raw)
if not errors.get('success') or errors.get('data'):raise SystemExit('Compile/Console gate rejected; no test/build started')
if a.ready_only:print('Fresh compile and clean Console only; no tests executed',flush=True);raise SystemExit(0)
if not a.group:raise SystemExit('Explicit actual test groups required')
command=[sys.executable,'-X','utf8',str(ROOT/'Tools/MCP/module_tests.py')];base=['--evidence',rel,'--label',a.label];start=command+['start']+base
for assembly in a.assembly:start+=['--assembly',assembly]
for group in a.group:start+=['--group',group]
subprocess.run(start,cwd=ROOT,check=True);job=payload(json.loads((folder/(a.label+'-Job.json')).read_text(encoding='utf-8-sig')))
if not job.get('success') or not (job.get('data') or {}).get('job_id'):raise SystemExit('No actual Job; do not poll')
deadline=time.monotonic()+600
while True:
 subprocess.run(command+['poll']+base,cwd=ROOT,check=True);result=payload(json.loads((folder/(a.label+'.json')).read_text(encoding='utf-8-sig')))['data']
 if result['status'] in ['succeeded','failed','cancelled']:break
 if time.monotonic()>deadline:raise SystemExit('Still running; inspect current Job without launching another')
 time.sleep(3)
summary=(result.get('result') or {}).get('summary') or {};names=[t.get('fullName','') for t in (result.get('result') or {}).get('results',[])];missing=[g for g in a.group if not any(s.startswith(g+'.') or s==g for s in names)]
print(json.dumps({'summary':summary,'missingGroups':missing},ensure_ascii=False));assert result['status']=='succeeded' and summary.get('total',0)>0 and not summary.get('failed') and not missing
