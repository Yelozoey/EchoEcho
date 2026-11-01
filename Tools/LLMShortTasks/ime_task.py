"""Small evidence helpers for the single authorized IME task."""
import argparse, hashlib, json, sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/MCP'))
from client import Client
from preflight import payload
EVIDENCE=ROOT/'Docs/Tests/SalvagePrototype/Evidence/LLMShortTasks/IME01'
EVIDENCE.mkdir(parents=True,exist_ok=True)
p=argparse.ArgumentParser();p.add_argument('action',choices=['execute','protect','verify','budget','console']);p.add_argument('--label',required=True);p.add_argument('--code');a=p.parse_args()
if not a.label or any(c not in 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_-' for c in a.label):raise SystemExit('Safe label required')
target=EVIDENCE/(a.label+'.json')
if target.exists():raise SystemExit('Evidence must be fresh')
if a.action=='execute':
 code=(ROOT/a.code).read_text(encoding='utf-8-sig')
 raw=Client().request('tools/call',{'name':'execute_code','arguments':{'action':'execute','compiler':'codedom','code':code}})
 target.write_text(json.dumps(raw,ensure_ascii=False,indent=2),encoding='utf-8');r=payload(raw);print(json.dumps(r,ensure_ascii=False));assert r.get('success'),r
elif a.action=='console':
 raw=Client().request('tools/call',{'name':'read_console','arguments':{'action':'get','types':['error'],'count':20,'format':'detailed'}});target.write_text(json.dumps(raw,ensure_ascii=False,indent=2),encoding='utf-8');r=payload(raw);assert r.get('success') and not r.get('data'),r;print('Final Console: zero errors')
elif a.action in ['protect','verify']:
 if a.action=='protect':
  paths=[f for base in ['Assets','Packages','ProjectSettings','Builds'] for f in (ROOT/base).rglob('*') if f.is_file() and (base=='Builds' or f.suffix.lower() in {'.cs','.meta','.asset','.unity','.prefab','.asmdef','.shader','.hlsl','.json','.onnx','.onnx_data'})]
 else:paths=[ROOT/s for s in json.loads((EVIDENCE/'Protection_Before.json').read_text(encoding='utf-8'))['files']]
 def sha(f):
  h=hashlib.sha256()
  with f.open('rb') as stream:
   while b:=stream.read(4*1024*1024):h.update(b)
  return h.hexdigest()
 files={f.relative_to(ROOT).as_posix():sha(f) if f.exists() else None for f in paths};r={'files':files}
 if a.action=='verify':
  before=json.loads((EVIDENCE/'Protection_Before.json').read_text(encoding='utf-8'))['files'];r['changed']=[s for s in before if files[s]!=before[s]];r['intentionalAllowed']=['Assets/EchoStation/Player/Runtime/GameTextInput.cs','Assets/EchoStation/Player/Runtime/GameTextBaseInput.cs'];r['unexpected']=[s for s in r['changed'] if s not in r['intentionalAllowed']];r['passed']=not r['unexpected']
 target.write_text(json.dumps(r,ensure_ascii=False,indent=2),encoding='utf-8');print(json.dumps({'files':len(files),**{k:v for k,v in r.items() if k!='files'}},ensure_ascii=False));assert r.get('passed',True)
else:
 baseline=json.loads((ROOT/'Docs/Tests/SalvagePrototype/Evidence/Expansion/V01/Budget_Final_A.json').read_text(encoding='utf-8-sig'))
 paths=[ROOT/s['path'] for s in baseline['parts']]+[ROOT/s for s in ['Assets/EchoStation/LLMShortTasks','Tools/LLMShortTasks','Builds/LLMShortTasks','Library/EchoStation/LLMShortTasks','Docs/Tests/SalvagePrototype/Evidence/LLMShortTasks']]
 selected=[]
 for path in sorted(set(s.resolve() for s in paths),key=lambda s:(len(s.parts),str(s))):
  if not any(parent==path or parent in path.parents for parent in selected):selected.append(path)
 parts=[{'path':s.relative_to(ROOT).as_posix(),'bytes':s.stat().st_size if s.is_file() else sum(f.stat().st_size for f in s.rglob('*') if f.is_file()) if s.exists() else 0} for s in selected];total=sum(s['bytes'] for s in parts)
 r={'parts':parts,'combinedBytes':total,'combinedGiB':total/1024**3,'underLimit':total<=20*1024**3};target.write_text(json.dumps(r,indent=2),encoding='utf-8');print(json.dumps({k:v for k,v in r.items() if k!='parts'}));assert r['underLimit']
