"""Confirm that model import/loop edits never changed any selected original data bytes."""
import hashlib,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
E=ROOT/'Docs/Tests/InfiltrationPrototype/Evidence/A00'
out=E/'SelectedSourceVerification_Final.json'
if out.exists():raise SystemExit('Preserve verification')
before=json.loads((E/'SelectedSources_A.json').read_text(encoding='utf-8'))['files']
rows=[]
for row in before:
    path=ROOT/row['path'];digest=hashlib.sha256(path.read_bytes()).hexdigest()
    rows.append({'path':row['path'],'sha256':digest,'sameBytes':digest==row['sha256'] and path.stat().st_size==row['bytes']})
result={'files':rows,'passed':all(r['sameBytes'] for r in rows)}
out.write_text(json.dumps(result,indent=2),encoding='utf-8');print(json.dumps({'selectedFiles':len(rows),'allOriginalDataBytesUnchanged':result['passed']}));assert result['passed']
