"""Read-only old-file protection and combined owned-resource accounting."""
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
parser = argparse.ArgumentParser()
parser.add_argument('--output', required=True)
parser.add_argument('--s3-budget', required=True)
parser.add_argument('--require-restored', action='store_true')
args = parser.parse_args()
output = (ROOT / args.output).resolve()
assert output.is_relative_to(ROOT / 'Docs/Tests') and not output.exists(), 'Fresh owned evidence required'
baseline = json.loads((ROOT / 'Docs/Tests/SalvagePrototype/Evidence/R0-A02/ProtectedFiles_Before_A.json').read_text(encoding='utf-8'))
changed = [p for p, h in baseline.items() if not (ROOT / p).is_file() or hashlib.sha256((ROOT / p).read_bytes()).hexdigest() != h]
s3 = json.loads((ROOT / args.s3_budget).read_text(encoding='utf-8'))
paths = [ROOT / p['path'] for p in s3['parts']]
paths += [ROOT / p for p in ['Assets/EchoStation/Salvage', 'Assets/EchoStation/Tests/SalvageEditor',
          'Assets/EchoStation/Tests/SalvageFixtures', 'Docs/SalvagePrototype', 'Docs/Tests/SalvagePrototype',
          'Docs/Assets/Sources/SalvagePrototype', 'Tools/Salvage', 'Builds/Salvage',
          'Library/EchoStation/SalvageSaveTests', 'Library/EchoStation/R2SaveTests',
          'Library/EchoStation/R2SceneSaveTests', 'Library/EchoStation/R2Walkthrough', 'Docs/Assets/ThirdPartyLicenses.md',
          'Tools/Assets/receive_salvage_sources.py', 'Tools/Assets/test_salvage_intake.py']]
selected = []
for path in sorted(set(p.resolve() for p in paths), key=lambda p: (len(p.parts), str(p).casefold())):
    assert path.is_relative_to(ROOT)
    if not any(parent == path or parent in path.parents for parent in selected):
        selected.append(path)
parts = []
for path in selected:
    size = path.stat().st_size if path.is_file() else sum(p.stat().st_size for p in path.rglob('*') if p.is_file()) if path.exists() else 0
    parts.append({'path': path.relative_to(ROOT).as_posix(), 'bytes': size})
total = sum(p['bytes'] for p in parts)
record = {'protectedFiles': len(baseline), 'changedProtectedFiles': changed, 'parts': parts,
          'combinedBytes': total, 'combinedGiB': total / 1024**3, 'limitBytes': 20 * 1024**3,
          'underLimit': total <= 20 * 1024**3,
          'scope': 'S3 owned roots plus disjoint salvage sources/assets/builds/evidence/tools; shared Unity import/compiler cache not attributed; read-only stat and SHA, no deletion'}
output.parent.mkdir(parents=True, exist_ok=True)
output.write_text(json.dumps(record, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps({k: record[k] for k in ['protectedFiles', 'changedProtectedFiles', 'combinedGiB', 'underLimit']}, ensure_ascii=False))
assert record['underLimit'], 'Combined owned-resource budget exceeded'
if args.require_restored:
    assert not changed, 'Old project files differ; do not claim restoration'
