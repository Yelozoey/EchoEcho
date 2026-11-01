"""Wait out imports, then invoke the required guarded module runner exactly once."""
import argparse,json,subprocess,sys,time
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/MCP'))
from client import Client
from preflight import payload
parser=argparse.ArgumentParser();parser.add_argument('--module',required=True);parser.add_argument('--label',required=True);parser.add_argument('--group',action='append',default=[]);parser.add_argument('--ready-only',action='store_true')
args=parser.parse_args()
if not args.group and not args.ready_only:raise SystemExit('At least one actual test group required')
if not args.module.replace('_','').replace('-','').isalnum() or not args.label.replace('_','').replace('-','').isalnum():raise SystemExit('Invalid evidence label')
relative='Docs/Tests/SalvagePrototype/Evidence/Expansion/'+args.module
folder=ROOT/relative;folder.mkdir(parents=True,exist_ok=True)
if (folder/(args.label+'-Job.json')).exists():raise SystemExit('Fresh job label required; inspect previous receipt')
c=Client();deadline=time.monotonic()+240;refreshed=False;last=None
refresh=c.request('tools/call',{'name':'refresh_unity','arguments':{'mode':'force','scope':'all','compile':'request','wait_for_ready':False}})
(folder/(args.label+'-InitialRefresh.json')).write_text(json.dumps(refresh,ensure_ascii=False,indent=2),encoding='utf-8')
time.sleep(2)
source_roots={'Runtime':'Assets/EchoStation/SalvageExpansion/Runtime','Domain':'Assets/EchoStation/SalvageExpansion/Domain','Editor':'Assets/EchoStation/SalvageExpansion/Editor','Validation':'Assets/EchoStation/SalvageExpansion/Validation','Editor.Tests':'Assets/EchoStation/Tests/SalvageExpansionEditor'}
def fresh_assemblies():
 for suffix,relative_source in source_roots.items():
  source=ROOT/relative_source
  if not source.exists():continue
  newest=max((p.stat().st_mtime for p in source.rglob('*') if p.suffix in ('.cs','.asmdef')),default=0)
  assembly=ROOT/'Library/ScriptAssemblies'/('EchoStation.SalvageExpansion.'+suffix+'.dll')
  if not assembly.exists() or assembly.stat().st_mtime<newest:return False
 return True
while True:
 raw=c.request('resources/read',{'uri':'mcpforunity://editor/state'});state=json.loads(raw['contents'][0]['text']);data=state.get('data') or {}
 phase=(data.get('activity') or {}).get('phase');busy=(data.get('compilation') or {}).get('is_compiling') or (data.get('assets') or {}).get('is_updating')
 status=(state.get('success'),phase,busy,(data.get('assets') or {}).get('external_changes_dirty'))
 if status!=last:print('Editor readiness:',status,flush=True);last=status
 if state.get('success') and data.get('advice') and data.get('tests') and not busy:
  if data['tests'].get('is_running'):raise SystemExit('Another test run is active')
  if not (data.get('assets') or {}).get('external_changes_dirty') and fresh_assemblies():break
  if not refreshed:
   response=c.request('tools/call',{'name':'refresh_unity','arguments':{'mode':'if_dirty','scope':'all','compile':'none','wait_for_ready':False}})
   (folder/(args.label+'-Refresh.json')).write_text(json.dumps(response,ensure_ascii=False,indent=2),encoding='utf-8');refreshed=True
 if time.monotonic()>deadline:raise SystemExit('Editor import/readiness timed out; no test dispatched')
 time.sleep(2)
if args.ready_only:
 raw=c.request('tools/call',{'name':'read_console','arguments':{'action':'get','types':['error'],'count':20,'format':'detailed'}})
 (folder/(args.label+'-Console.json')).write_text(json.dumps(raw,ensure_ascii=False,indent=2),encoding='utf-8')
 console=payload(raw)
 if not console.get('success') or console.get('data'):raise SystemExit('Compiler/Console gate not clean; no mutation dispatched')
 print('Fresh owned assemblies and clean Console; readiness only, no tests executed',flush=True);raise SystemExit(0)
command=[sys.executable,'-X','utf8',str(ROOT/'Tools/MCP/module_tests.py')]
base=['--evidence',relative,'--label',args.label]
start=command+['start']+base+['--assembly','EchoStation.SalvageExpansion.Editor.Tests']
for group in args.group:start+=['--group',group]
subprocess.run(start,cwd=ROOT,check=True)
job=payload(json.loads((folder/(args.label+'-Job.json')).read_text(encoding='utf-8-sig')))
if not job.get('success') or not (job.get('data') or {}).get('job_id'):raise SystemExit('No actual Job; no poll allowed')
deadline=time.monotonic()+600
while True:
 subprocess.run(command+['poll']+base,cwd=ROOT,check=True)
 result=payload(json.loads((folder/(args.label+'.json')).read_text(encoding='utf-8-sig')))['data']
 if result['status'] in ('succeeded','failed','cancelled'):break
 if time.monotonic()>deadline:raise SystemExit('Test still running; preserve Job and inspect it, do not launch another')
 time.sleep(2)
summary=(result.get('result') or {}).get('summary') or {}
if result['status']!='succeeded' or summary.get('failed') or summary.get('total',0)<1:raise SystemExit('No positive passing suite')
names=[test.get('fullName','') for test in (result.get('result') or {}).get('results',[])]
missing=[group for group in args.group if not any(name==group or name.startswith(group+'.') for name in names)]
if missing:raise SystemExit('Requested groups did not actually execute: '+', '.join(missing))
print(json.dumps({'module':args.module,'label':args.label,'job':job['data']['job_id'],'summary':summary},ensure_ascii=False),flush=True)
