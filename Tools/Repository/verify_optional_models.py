"""Read-only optional-weight verification; never downloads or converts anything."""
import argparse, hashlib, json
from pathlib import Path

p=argparse.ArgumentParser()
p.add_argument('--root',type=Path,default=Path(__file__).resolve().parents[2])
p.add_argument('--verify-present',action='store_true')
p.add_argument('--require-all',action='store_true')
a=p.parse_args();rows=[];bad=False
for name in ['Qwen3_06B_Q4','Qwen25_05B_W8A8']:
    folder=a.root/'Assets/StreamingAssets/EchoLLM'/name
    manifest=json.loads((folder/'manifest.json').read_text(encoding='utf-8-sig'))
    expected=next(f for f in manifest['files'] if f['name']=='model.onnx')
    weight=folder/'model.onnx';row={'model':name,'expectedBytes':expected['bytes'],'expectedSha256':expected['sha256']}
    if not weight.exists():
        row['status']='missing-optional';bad|=a.require_all
    elif weight.stat().st_size!=expected['bytes']:
        row['status']='size-mismatch';bad=True
    elif a.verify_present:
        with weight.open('rb') as stream:actual=hashlib.file_digest(stream,'sha256').hexdigest()
        row['status']='verified' if actual==expected['sha256'] else 'sha-mismatch';bad|=row['status']!='verified'
    else:row['status']='present-size-ok-hash-not-run'
    rows.append(row)
print(json.dumps({'passed':not bad,'downloads':False,'models':rows},ensure_ascii=False,indent=2))
raise SystemExit(1 if bad else 0)
