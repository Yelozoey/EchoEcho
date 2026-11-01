"""Archive one fully drained experiment; never changes its test or quality result."""
import argparse
import hashlib
import json
import re
from pathlib import Path
from qwen3_tuning_metrics import timing

parser = argparse.ArgumentParser()
parser.add_argument('--label', required=True)
args = parser.parse_args()
assert re.fullmatch(r'[A-Za-z0-9_-]{1,64}', args.label)
root = Path(__file__).resolve().parents[2]
tuning = root / 'Docs/Tests/Stage3/Evidence/S3-J01/CandidateQwen3/Tuning'
matches = []
for path in tuning.glob('UnityTuning_*.json'):
    record = json.loads(path.read_text(encoding='utf-8'))
    if record.get('configuration', {}).get('label') == args.label:
        matches.append((path, record))
assert len(matches) == 1, 'Exactly one raw run per fresh experiment label required'
path, record = matches[0]
assert record.get('completed') and record.get('ownerCleanupComplete'), 'Owner still draining or cleanup failed'
rows = record['requests']
summary = {
    'label': args.label, 'rawFile': path.name,
    'rawSha256': hashlib.sha256(path.read_bytes()).hexdigest(),
    'completed': True, 'ownerCleanupComplete': True,
    'rawPassed': record['passed'],
    'requests': len(rows), 'strictJsonCorrect': record.get('strictJsonCorrect', 0),
    'semanticCorrect': record.get('semanticCorrect', 0),
    'coldSeconds': record.get('coldSeconds'),
    **timing(rows),
    'mainThreadNativeCalls': record.get('mainThreadNativeCalls'),
    'error': record.get('error'),
    'semanticFailures': [
        {key: row.get(key) for key in ['index','input','expected','expectedTarget','actual','actualTarget','raw','error']}
        for row in rows if not row.get('semanticCorrect')
    ],
    'limitation': 'Raw candidate diagnostic only; does not override NUnit, safety, content or game acceptance.'
}
folder = tuning / args.label
archived = folder / path.name
if archived.exists():
    assert archived.read_bytes() == path.read_bytes(), 'Archived raw differs'
else:
    with archived.open('xb') as stream:
        stream.write(path.read_bytes())
with (folder/'Summary.json').open('xb') as stream:
    stream.write(json.dumps(summary, ensure_ascii=False, indent=2).encode('utf-8'))
print(json.dumps(summary, ensure_ascii=False, indent=2))
