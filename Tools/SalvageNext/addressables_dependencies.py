"""Official registry metadata and pinned dependency audit; no package installation."""
import argparse
import hashlib
import json
from pathlib import Path
from urllib.request import urlopen

ROOT = Path(__file__).resolve().parents[2]
EVIDENCE = ROOT / 'Docs/Tests/SalvagePrototype/Evidence/R4-R03a-1'
NEW = {'com.unity.addressables': '2.11.2', 'com.unity.scriptablebuildpipeline': '3.0.3'}

def version(value):
    return tuple(int(part) for part in value.split('-')[0].split('.'))

def inspect():
    EVIDENCE.mkdir(parents=True, exist_ok=True)
    destination = EVIDENCE / 'DependencyReview_A.json'
    if destination.exists():
        raise RuntimeError('Keep prior review; do not overwrite evidence')
    manifest = ROOT / 'Packages/manifest.json'
    lock = ROOT / 'Packages/packages-lock.json'
    original = json.loads(lock.read_text(encoding='utf-8'))['dependencies']
    metadata = {}
    for name, pinned in NEW.items():
        with urlopen('https://packages.unity.com/' + name, timeout=30) as response:
            data = json.load(response)['versions'][pinned]
        if data['name'] != name or data['version'] != pinned or data['unity'] != '6000.0':
            raise RuntimeError('Unexpected package identity/engine requirement')
        if not data['dist']['tarball'].startswith('https://download.packages.unity.com/'):
            raise RuntimeError('Unexpected non-official distribution')
        metadata[name] = data
        (EVIDENCE / (name + '.json')).write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding='utf-8')
    rows = []
    for name, data in metadata.items():
        for dependency, minimum in data['dependencies'].items():
            installed = original.get(dependency, {}).get('version')
            selected = NEW.get(dependency, installed)
            if selected is None or version(selected) < version(minimum):
                raise RuntimeError(f'Unresolved or incompatible {dependency}: {selected} < {minimum}')
            rows.append(dict(parent=name, dependency=dependency, minimum=minimum, installed=installed, selected=selected))
    for source in (manifest, lock):
        target = EVIDENCE / (source.name + '.before')
        if target.exists():
            raise RuntimeError('Keep existing backup')
        target.write_bytes(source.read_bytes())
    result = dict(passed=True, registry='https://packages.unity.com', newPackages=NEW, requiredRows=rows,
                  oldVersions={name: data['version'] for name, data in original.items()},
                  hashes={source.name: hashlib.sha256(source.read_bytes()).hexdigest() for source in (manifest, lock)})
    destination.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps(dict(passed=True, newPackages=NEW, oldVersionCount=len(original), dependencyRows=len(rows))))

def verify():
    review = json.loads((EVIDENCE / 'DependencyReview_A.json').read_text(encoding='utf-8'))
    manifest = json.loads((ROOT / 'Packages/manifest.json').read_text(encoding='utf-8'))
    lock = json.loads((ROOT / 'Packages/packages-lock.json').read_text(encoding='utf-8'))['dependencies']
    old = json.loads((EVIDENCE / 'manifest.json.before').read_text(encoding='utf-8'))
    expected = dict(old)
    expected['dependencies'] = dict(old['dependencies'], **{'com.unity.addressables': '2.11.2'})
    errors = []
    if manifest != expected:
        errors.append('Manifest contains unapproved dependency changes')
    original_lock = json.loads((EVIDENCE / 'packages-lock.json.before').read_text(encoding='utf-8'))['dependencies']
    for name, original in review['oldVersions'].items():
        if lock.get(name, {}).get('version') != original:
            errors.append(f'Changed old resolved version: {name}')
        before = {key: value for key, value in original_lock[name].items() if key != 'depth'}
        after = {key: value for key, value in lock.get(name, {}).items() if key != 'depth'}
        if before != after:
            errors.append(f'Changed old package source/hash/dependencies: {name}')
    for name, pinned in NEW.items():
        if lock.get(name, {}).get('version') != pinned:
            errors.append(f'Wrong new package: {name}')
    result = dict(passed=not errors, errors=errors, preservedOldVersionCount=len(review['oldVersions']), addedPackages=NEW)
    (EVIDENCE / 'InstalledVersionAudit_A.json').write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps(result))
    if errors:
        raise SystemExit(1)

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('action', choices=['inspect', 'verify'])
    args = parser.parse_args()
    (inspect if args.action == 'inspect' else verify)()
