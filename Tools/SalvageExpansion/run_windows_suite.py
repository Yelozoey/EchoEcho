"""Sequential Windows checks. Every process retains its own raw result and input restoration proof."""
import argparse,json,subprocess,sys,time
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('--suffix',required=True);a=p.parse_args()
if not a.suffix.isascii() or not a.suffix.isalnum():raise SystemExit('Simple fresh suffix required')
evidence=ROOT/'Docs/Tests/SalvagePrototype/Evidence/Expansion/V01';target=evidence/('WindowsSuite_'+a.suffix+'.json')
if target.exists():raise SystemExit('Fresh suite label required')
steps=[('startup','Startup'),('fan','Fan'),('valve','Valve'),('write','Cross'),('read','Cross'),('failure','Failure'),('display','Display'),('performance','Performance')];rows=[];start=time.monotonic()
try:
 for mode,label in steps:
  label+='_'+a.suffix;result=subprocess.run([sys.executable,'-X','utf8',str(ROOT/'Tools/SalvageExpansion/run_windows.py'),'--mode',mode,'--label',label],cwd=ROOT)
  rows.append({'mode':mode,'label':label,'exitCode':result.returncode})
  if result.returncode:raise RuntimeError('Stopped at actual failed '+mode)
finally:
 record={'passed':len(rows)==len(steps) and all(r['exitCode']==0 for r in rows),'steps':rows,'seconds':time.monotonic()-start};target.write_text(json.dumps(record,indent=2),encoding='utf-8');print(json.dumps(record),flush=True)
