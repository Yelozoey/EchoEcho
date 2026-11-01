"""Create one-use bounded probe admission after the independent artifact gate."""
import argparse, hashlib, json, re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
BASE=ROOT/'Docs/Tests/SalvagePrototype/Evidence/R3-L01'
parser=argparse.ArgumentParser()
parser.add_argument('--profile',required=True,choices=['candidate1','candidate2','candidate3'])
parser.add_argument('--mode',required=True,choices=['diagnostic','heldOut'])
parser.add_argument('--run',required=True)
args=parser.parse_args()
if not re.fullmatch('[A-Za-z0-9_]+',args.run): raise ValueError('Invalid run ID')
active=BASE/'ActiveRun.json'
if active.exists() or (BASE/'Runs'/args.run).exists(): raise FileExistsError('Never overwrite admitted run')
freeze=json.loads((BASE/'Inputs/Freeze.json').read_text(encoding='utf-8'))
for key in [args.profile,args.mode]:
    item=freeze[key]; path=(ROOT/item['path']).resolve()
    if not path.is_relative_to((BASE/'Inputs').resolve()) or hashlib.sha256(path.read_bytes()).hexdigest()!=item['sha256']: raise ValueError('Frozen artifact mismatch')
prior=[json.loads(p.read_text(encoding='utf-8')) for p in (BASE/'Runs').rglob('Configuration.json')] if (BASE/'Runs').exists() else []
if args.mode=='diagnostic' and (sum(p['mode']=='diagnostic' for p in prior)>=3 or any(p['mode']=='heldOut' for p in prior)): raise ValueError('Diagnostic limit or held-out boundary reached')
if args.mode=='heldOut' and any(p['mode']=='heldOut' for p in prior): raise ValueError('One held-out round only')
if any(p['mode']==args.mode and p['profileKey']==args.profile for p in prior): raise ValueError('Candidate already admitted')
record=dict(mode=args.mode,profileKey=args.profile,run=args.run,inputs=freeze[args.mode],profile=freeze[args.profile],scope='one-use configuration; raw quality before C# presentation; no game dispatch')
active.write_text(json.dumps(record,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(dict(run=args.run,mode=args.mode,profile=args.profile)))
