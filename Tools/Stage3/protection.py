"""Capture/verify S1/S2 delivery and owned scene assets before S3 changes."""
import hashlib,json,sys
from pathlib import Path
root=Path(__file__).resolve().parents[2]
out=root/'Docs/Tests/Stage3/Evidence/ProtectedS1S2.json'
def digest(path):
 h=hashlib.sha256()
 with path.open('rb') as stream:
  for block in iter(lambda:stream.read(1048576),b''):h.update(block)
 return h.hexdigest()
if sys.argv[1]=='capture':
 assert not out.exists()
 paths=set()
 for name in ['Builds/S1','Builds/S2','Assets/EchoStation/Station']:
  for p in (root/name).rglob('*'):
   if p.is_file() and (name.startswith('Builds/') or p.suffix in {'.unity','.asset','.mat','.prefab','.shader'}):paths.add(p)
 baseline=json.loads((root/'Docs/Tests/Stage2/Evidence/S2-W01/S1-Protected.json').read_text(encoding='utf-8'))
 paths.update(root/k for k in baseline)
 out.parent.mkdir(parents=True,exist_ok=True)
 out.write_text(json.dumps({p.relative_to(root).as_posix():digest(p) for p in sorted(paths)},indent=2),encoding='utf-8')
 print(json.dumps({'captured':len(paths)}))
else:
 data=json.loads(out.read_text(encoding='utf-8'));bad=[k for k,v in data.items() if not (root/k).is_file() or digest(root/k)!=v]
 record={'checked':len(data),'unchanged':not bad,'changed':bad};out.with_name('ProtectedS1S2-Verified.json').write_text(json.dumps(record,indent=2),encoding='utf-8');print(json.dumps(record));assert not bad
