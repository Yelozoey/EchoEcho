"""Read-only verification of the delivered folder; write a fresh evidence manifest."""
import argparse
import hashlib
import json
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
EVIDENCE = ROOT / 'Docs/Tests/SalvagePrototype/Evidence'
parser = argparse.ArgumentParser()
parser.add_argument('--label', required=True)
args = parser.parse_args()
assert args.label.replace('_', '').replace('-', '').isalnum()
target = EVIDENCE / 'R4-Final' / ('DeliveryManifest_' + args.label + '.json')
assert not target.exists(), 'Fresh evidence only'

def read(relative):
    return json.loads((EVIDENCE / relative).read_text(encoding='utf-8'))

def sha(path):
    digest = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            digest.update(block)
    return digest.hexdigest()

product = read('R4-P01c/Product_Final_F.json')
assert product['passed'] and not product['changed'] and len(product['files']) == 646
for relative, expected in product['files'].items():
    assert sha(ROOT / relative) == expected, 'Product changed: ' + relative
job = read('R4-Final/FullNext_C.json')
result = json.loads(next(block['text'] for block in job['content'] if block['type'] == 'text'))
summary = result['data']['result']['summary']
assert result['success'] and summary['passed'] == summary['total'] == 188
assert summary['failed'] == summary['skipped'] == 0
build = read('R4-P01b-2/Build_9e6d0080a96146a98ac1963712c26d40.json')
assert build['passed'] and build['result'] == 'Succeeded' and build['errors'] == 0
restored = read('R4-Final/OriginalSettingsCandidates/Restored_F.json')
for item in restored:
    assert item['exactTaskOriginal'] and sha(ROOT / item['path']) == item['afterSHA256']
state = read('R4-Final/FinalWorkspace_G.json')
assert not state['sceneDirty'] and not state['playing'] and not state['editorSettingsDirty']
assert state['ordinaryForward'] and state['internalHDR'] and not state['displayHDR']
assert state['api'] == 'Direct3D11' and state['quality'] == 1 and state['aa'] == 4

folder = ROOT / 'Builds/SalvageNext'
assert (folder / 'EchoStationSalvageNext.exe').is_file()
assert (folder / 'UnityPlayer.dll').is_file()
assert (folder / 'EchoStationSalvageNext_Data').is_dir()
assert not (folder / 'Validation').exists()
assert not (folder / 'EchoStationSalvageNext_Data/StreamingAssets/EchoLLM').exists()
assert (folder / 'README_zh-CN.md').read_bytes() == (ROOT / 'Docs/SalvagePrototype/WindowsPlayGuide.md').read_bytes()
notices = sorted(p.relative_to(folder / 'ThirdPartyNotices').as_posix()
                 for p in (folder / 'ThirdPartyNotices').rglob('*') if p.is_file())
assert len(notices) >= 10
files = {p.relative_to(folder).as_posix(): {'bytes': p.stat().st_size, 'sha256': sha(p)}
         for p in sorted(folder.rglob('*')) if p.is_file()}
total = sum(item['bytes'] for item in files.values())
record = {
    'utc': datetime.now(timezone.utc).isoformat(), 'folder': str(folder),
    'entry': 'EchoStationSalvageNext.exe', 'build': '9e6d0080a96146a98ac1963712c26d40',
    'bytes': total, 'MiB': total / 1024**2, 'fileCount': len(files),
    'files': files, 'notices': notices, 'productSignatureFilesVerifiedNow': 646,
    'regression': summary, 'originalSettingsSHARechecked': True,
    'useLLM': False, 'modelQuality': 'No-Go; R3 incomplete',
    'limitations': ['Target hardware and Windows10 not tested',
                    'Development build; external first-player experience not tested'],
}
target.write_text(json.dumps(record, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps({key: record[key] for key in ['fileCount', 'bytes', 'MiB', 'productSignatureFilesVerifiedNow']}, ensure_ascii=False))
