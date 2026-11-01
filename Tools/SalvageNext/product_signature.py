"""Compare tested gameplay with later validation-only builds; no inferred equivalence."""
import argparse
import hashlib
import json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
parser=argparse.ArgumentParser()
parser.add_argument('action',choices=['capture','verify'])
parser.add_argument('--label',required=True)
parser.add_argument('--baseline',type=Path)
args=parser.parse_args()
assert args.label.replace('_','').replace('-','').isalnum()
target=ROOT/'Docs/Tests/SalvagePrototype/Evidence/R4-P01c'/('Product_'+args.label+'.json')
assert not target.exists(),'Fresh evidence only'
def sha(path):
    digest=hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda:stream.read(1024*1024),b''):digest.update(block)
    return digest.hexdigest()
paths=[]
base=ROOT/'Assets/EchoStation/SalvageNext'
for folder in ['Runtime','Domain','Scenes','Generated','Prefabs','Shaders','ThirdParty','ArtV2']:
    paths.extend(p for p in (base/folder).rglob('*') if p.is_file())
managed=ROOT/'Builds/SalvageNext/EchoStationSalvageNext_Data/Managed'
paths.extend(p for p in managed.glob('EchoStation.*.dll') if 'Validation' not in p.name and 'Fixtures' not in p.name)
paths.extend((ROOT/'Builds/SalvageNext/EchoStationSalvageNext_Data/StreamingAssets/aa').rglob('*.bundle'))
files={p.relative_to(ROOT).as_posix():sha(p) for p in sorted(set(paths))}
record={'files':files,'scope':'New product source/assets, all EchoStation product DLLs, and local bundles; validation DLLs/build metadata excluded explicitly.'}
if args.action=='verify':
    assert args.baseline
    old=json.loads(args.baseline.read_text(encoding='utf-8'))['files']
    changed=sorted(k for k in old.keys()|files.keys() if old.get(k)!=files.get(k))
    record['changed']=changed;record['passed']=not changed;record['baseline']=str(args.baseline.resolve())
target.parent.mkdir(parents=True,exist_ok=True)
target.write_text(json.dumps(record,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'files':len(files),'changed':record.get('changed'),'passed':record.get('passed')},ensure_ascii=False))
if args.action=='verify' and record['changed']:raise SystemExit(1)
