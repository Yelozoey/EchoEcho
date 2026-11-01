"""Observe only the approved build interval; final accounting still uses the 20 GiB tool."""
import argparse,json,time
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('--baseline',required=True);p.add_argument('--receipt',required=True);p.add_argument('--output',required=True);a=p.parse_args()
baseline=json.loads((ROOT/a.baseline).read_text(encoding='utf-8-sig'));receipt=(ROOT/a.receipt).resolve();output=(ROOT/a.output).resolve()
if not receipt.is_relative_to(ROOT/'Docs/Tests/SalvagePrototype/Evidence/Expansion/V01') or not output.is_relative_to(ROOT/'Docs/Tests') or output.exists():raise SystemExit('Owned fresh evidence required')
roots=[(ROOT/part['path']).resolve() for part in baseline['parts']]
if any(not root.is_relative_to(ROOT) for root in roots):raise SystemExit('Budget root escaped workspace')
def size(root):
 if root.is_file():return root.stat().st_size
 total=0
 if root.exists():
  for file in root.rglob('*'):
   try:
    if file.is_file():total+=file.stat().st_size
   except FileNotFoundError:pass
 return total
start=time.monotonic();peak=0;rows=[];limit=22*1024**3;violation=False;build=None
while True:
 current=sum(size(root) for root in roots);peak=max(peak,current);rows.append({'seconds':round(time.monotonic()-start,3),'bytes':current});violation|=current>limit
 if receipt.exists():
  try:build=json.loads(receipt.read_text(encoding='utf-8-sig'));break
  except json.JSONDecodeError:pass
 if time.monotonic()-start>900:break
 time.sleep(2)
# The pre-strip native receipt captures the brief peak between observer samples.
floor=None
if build and 'outputBytesBeforeStrip' in build:
 build_root=ROOT/'Builds/SalvageExpansion';outside=sum(size(root) for root in roots if root!=build_root.resolve());floor=outside+build['outputBytesBeforeStrip'];peak=max(peak,floor);violation|=floor>limit
record={'temporaryBuildLimitGiB':22,'peakObservedBytes':peak,'peakObservedGiB':peak/1024**3,'receiptPreStripConservativeCombinedBytes':floor,'violation':violation,'buildReceipt':a.receipt,'buildCompleted':build is not None,'rows':rows,'final20GiBNotCheckedHere':True}
output.parent.mkdir(parents=True,exist_ok=True);output.write_text(json.dumps(record,indent=2),encoding='utf-8');print(json.dumps({k:v for k,v in record.items() if k!='rows'}),flush=True)
if violation or build is None:raise SystemExit('Build interval budget/completion requires review')
