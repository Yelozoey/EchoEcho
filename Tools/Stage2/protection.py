"""Read-only hashes of S1 assets/delivery; excludes S2 and C# integration edits."""
import argparse, hashlib, json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('action',choices=['capture','verify']);p.add_argument('output');a=p.parse_args()
output=(ROOT/a.output).resolve()
if not output.is_relative_to(ROOT/'Docs'/'Tests'):raise SystemExit('Evidence path required')
def sha(path):
    h=hashlib.sha256()
    with path.open('rb') as f:
        for block in iter(lambda:f.read(1024*1024),b''):h.update(block)
    return h.hexdigest()
if a.action=='capture':
    if output.exists():raise SystemExit('Never overwrite a protection baseline')
    files=[]
    for folder in ['World','Rendering','Echo','Player']:
        files.extend(f for f in (ROOT/'Assets/EchoStation'/folder).rglob('*') if f.is_file() and f.suffix in {'.unity','.asset','.mat','.prefab','.shader'})
    files.extend(f for f in (ROOT/'Builds/S1').rglob('*') if f.is_file())
    data={str(f.relative_to(ROOT)).replace('\\','/'):sha(f) for f in sorted(set(files))};output.parent.mkdir(parents=True,exist_ok=True);output.write_text(json.dumps(data,indent=2),encoding='utf-8');print(json.dumps({'captured':len(data)}))
else:
    baseline=json.loads(output.read_text(encoding='utf-8'));diff=[k for k,v in baseline.items() if not (ROOT/k).is_file() or sha(ROOT/k)!=v]
    result={'checked':len(baseline),'unchanged':not diff,'changed':diff};output.with_name(output.stem+'-Verified.json').write_text(json.dumps(result,indent=2),encoding='utf-8');print(json.dumps(result));raise SystemExit(bool(diff))
