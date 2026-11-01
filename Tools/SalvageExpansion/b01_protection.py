"""Hash existing project code/configuration; ignore only this module's new paths."""
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
folder = ROOT / 'Docs/Tests/SalvagePrototype/Evidence/Expansion/B01'
parser = argparse.ArgumentParser()
parser.add_argument('action', choices=['before', 'after'])
args = parser.parse_args()
extensions = {'.cs', '.asmdef', '.asset', '.unity', '.prefab', '.shader', '.hlsl', '.mat', '.meta', '.json'}
excluded = ('Assets/EchoStation/SalvageExpansion', 'Assets/EchoStation/Tests/SalvageExpansionEditor')
files = {}
for root in ('Assets', 'ProjectSettings', 'Packages'):
    for path in (ROOT / root).rglob('*'):
        if not path.is_file() or path.suffix.lower() not in extensions:
            continue
        relative = path.relative_to(ROOT).as_posix()
        if any(relative == prefix + '.meta' or relative == prefix or relative.startswith(prefix + '/') for prefix in excluded):
            continue
        files[relative] = hashlib.sha256(path.read_bytes()).hexdigest()
record = {'scope': 'Existing Assets code/YAML/meta and ProjectSettings/Packages; excludes new Expansion domain/tests only. Binary art/model weights are not audited here.', 'files': files}
if args.action == 'after':
    previous = json.loads((folder / 'Protection_before_A.json').read_text(encoding='utf-8'))['files']
    record['changed'] = sorted(p for p in previous.keys() | files.keys() if previous.get(p) != files.get(p))
    record['passed'] = not record['changed']
target = folder / ('Protection_' + args.action + '_A.json')
if target.exists():
    raise SystemExit('Fresh evidence only')
target.write_text(json.dumps(record, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps({'files': len(files), 'changed': record.get('changed'), 'passed': record.get('passed')}))
if args.action == 'after' and not record['passed']:
    raise SystemExit('Protection differences require review')
