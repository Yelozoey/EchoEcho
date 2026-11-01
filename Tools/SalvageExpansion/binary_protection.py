"""Freeze original weights and old Windows deliveries before building the independent output."""
import argparse,hashlib,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('action',choices=('before','after'));p.add_argument('--evidence',required=True);a=p.parse_args();folder=(ROOT/a.evidence).resolve()
if not folder.is_relative_to(ROOT/'Docs/Tests'):raise SystemExit('Owned evidence required')
roots=[ROOT/'Assets/StreamingAssets/EchoLLM']+[path for path in (ROOT/'Builds').iterdir() if path.is_dir() and path.name!='SalvageExpansion']
files={}
for root in roots:
 for file in sorted(root.rglob('*')):
  if not file.is_file():continue
  h=hashlib.sha256()
  with file.open('rb') as stream:
   while block:=stream.read(4*1024*1024):h.update(block)
  files[file.relative_to(ROOT).as_posix()]={'bytes':file.stat().st_size,'sha256':h.hexdigest()}
record={'roots':[r.relative_to(ROOT).as_posix() for r in roots],'files':files}
if a.action=='after':
 previous=json.loads((folder/'Binary_before_A.json').read_text(encoding='utf-8'))['files'];record['changed']=[path for path in previous.keys()|files.keys() if previous.get(path)!=files.get(path)];record['passed']=not record['changed']
target=folder/('Binary_'+a.action+'_A.json')
if target.exists():raise SystemExit('Fresh evidence required')
folder.mkdir(parents=True,exist_ok=True);target.write_text(json.dumps(record,ensure_ascii=False,indent=2),encoding='utf-8');print(json.dumps({'files':len(files),'bytes':sum(f['bytes'] for f in files.values()),'passed':record.get('passed'),'changed':record.get('changed')},ensure_ascii=False))
if a.action=='after' and not record['passed']:raise SystemExit('Protected binary differences require review')
