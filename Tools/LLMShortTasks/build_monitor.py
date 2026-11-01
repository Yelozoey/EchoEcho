"""Read-only sampling for the authorized Windows-only 22 GiB temporary peak."""
import argparse,json,time
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];p=argparse.ArgumentParser();p.add_argument('--label',required=True);a=p.parse_args();folder=ROOT/'Docs/Tests/SalvagePrototype/Evidence/LLMShortTasks/IME01';baseline=json.loads((folder/'Budget_Before.json').read_text(encoding='utf-8'));receipt=folder/('Build_'+a.label+'.json');output=folder/('BuildPeak_'+a.label+'.json')
if output.exists() or receipt.exists():raise SystemExit('Fresh build labels only')
roots=[ROOT/r['path'] for r in baseline['parts']]
def size(path):
 total=0
 if path.is_file():return path.stat().st_size
 if path.exists():
  for f in path.rglob('*'):
   try:
    if f.is_file():total+=f.stat().st_size
   except FileNotFoundError:pass
 return total
start=time.monotonic();rows=[];peak=0;build=None
while time.monotonic()-start<900:
 current=sum(size(r) for r in roots);peak=max(peak,current);rows.append({'seconds':time.monotonic()-start,'bytes':current})
 if receipt.exists():
  try:build=json.loads(receipt.read_text(encoding='utf-8-sig'));break
  except json.JSONDecodeError:pass
 time.sleep(2)
if build and 'outputBytesBeforeStrip' in build:peak=max(peak,sum(size(r) for r in roots)-size(ROOT/'Builds/LLMShortTasks/IME01'/a.label)+build['outputBytesBeforeStrip'])
r={'peakGiB':peak/1024**3,'limitGiB':22,'violation':peak>22*1024**3,'buildCompleted':build is not None,'rows':rows};output.write_text(json.dumps(r,indent=2),encoding='utf-8');print(json.dumps({k:v for k,v in r.items() if k!='rows'}));assert not r['violation'] and r['buildCompleted']
