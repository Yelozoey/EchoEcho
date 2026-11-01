"""Evidence and resource checks for the authorized A01 level preview."""
import argparse, hashlib, json, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / 'Tools/MCP'))
from client import Client
from preflight import payload, require_idle

EVIDENCE = ROOT / 'Docs/Tests/InfiltrationPrototype/Evidence/A01D'
EVIDENCE.mkdir(parents=True, exist_ok=True)
p = argparse.ArgumentParser()
p.add_argument('action', choices=['status', 'execute', 'budget', 'protect', 'verify', 'console'])
p.add_argument('--label', required=True)
p.add_argument('--code')
a = p.parse_args()
if not a.label or any(c not in 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_-' for c in a.label):
    raise SystemExit('Safe evidence label required')
target = EVIDENCE / (a.label + '.json')
if target.exists():
    raise SystemExit('Preserve existing evidence')

def save(value):
    target.write_text(json.dumps(value, ensure_ascii=False, indent=2), encoding='utf-8')

def sha(path):
    h = hashlib.sha256()
    with path.open('rb') as f:
        while block := f.read(4 * 1024**2):
            h.update(block)
    return h.hexdigest()

if a.action in ['protect', 'verify']:
    if a.action == 'protect':
        paths = [f for folder in ['Assets', 'Packages', 'ProjectSettings', 'Builds', 'Tools']
                 for f in (ROOT / folder).rglob('*') if f.is_file() and '__pycache__' not in f.parts and not f.is_relative_to(ROOT/'Assets/EchoStation/InfiltrationRefinement') and not f.is_relative_to(ROOT/'Tools/InfiltrationPrototype/A01D')]
    else:
        before = json.loads((EVIDENCE / 'Protection_Before.json').read_text(encoding='utf-8'))['files']
        paths = [ROOT / s for s in before]
    native=json.loads((EVIDENCE/'NativeBefore_A.json').read_text(encoding='utf-8')) if a.action=='protect' else {}
    import base64
    files={}
    from concurrent.futures import ThreadPoolExecutor
    def read_one(f):
        key=f.relative_to(ROOT).as_posix()
        return key,hashlib.sha256(base64.b64decode(native[key]['disk'])).hexdigest() if key in native else sha(f) if f.is_file() else None
    with ThreadPoolExecutor(max_workers=4) as pool:
        for i,(key,value) in enumerate(pool.map(read_one,paths)):
            files[key]=value
            if i%2000==0: print(json.dumps({'hashed':i,'total':len(paths),'readers':4}),flush=True)
    result = {'files': files}
    if a.action == 'verify':
        result['changed'] = [s for s in before if files[s] != before[s]]
        result['passed'] = not result['changed']
    save(result)
    print(json.dumps({'files': len(files), **{k:v for k,v in result.items() if k != 'files'}}))
    if not result.get('passed', True):
        raise SystemExit('Old file protection differs; inspect before continuing')
elif a.action == 'budget':
    baseline = json.loads((ROOT / 'Docs/Tests/SalvagePrototype/Evidence/Expansion/V01/Budget_Final_A.json').read_text(encoding='utf-8-sig'))
    paths = [ROOT / s['path'] for s in baseline['parts']]
    paths += [ROOT / s for s in ['Assets/EchoStation/LLMShortTasks', 'Tools/LLMShortTasks', 'Builds/LLMShortTasks',
        'Library/EchoStation/LLMShortTasks', 'Docs/Tests/SalvagePrototype/Evidence/LLMShortTasks',
        'Assets/EchoStation/InfiltrationArt', 'Tools/InfiltrationPrototype', 'Library/EchoStation/InfiltrationArt',
        'Assets/EchoStation/InfiltrationLevel', 'Library/EchoStation/InfiltrationLevel', 'Assets/EchoStation/InfiltrationRefinement', 'Library/EchoStation/InfiltrationRefinement',
        'Docs/Assets/Sources/InfiltrationPrototype', 'Docs/Tests/InfiltrationPrototype', 'Docs/InfiltrationPrototype']]
    selected = []
    for path in sorted({s.resolve() for s in paths}, key=lambda s: (len(s.parts), str(s).lower())):
        if not path.is_relative_to(ROOT):
            raise SystemExit('Budget path outside workspace')
        if not any(parent == path or parent in path.parents for parent in selected):
            selected.append(path)
    parts = [{'path':s.relative_to(ROOT).as_posix(), 'bytes':s.stat().st_size if s.is_file() else
              sum(f.stat().st_size for f in s.rglob('*') if f.is_file()) if s.exists() else 0} for s in selected]
    total = sum(s['bytes'] for s in parts)
    result = {'parts':parts, 'combinedBytes':total, 'combinedGiB':total/1024**3,
              'limitBytes':20*1024**3, 'underLimit':total <= 20*1024**3}
    save(result)
    print(json.dumps({k:v for k,v in result.items() if k != 'parts'}))
    if not result['underLimit']:
        raise SystemExit('Resource budget exceeded')
else:
    c = Client()
    if a.action == 'status':
        raw = c.request('resources/read', {'uri':'mcpforunity://editor/state'})
        save(raw)
        print(raw['contents'][0]['text'])
    elif a.action == 'console':
        raw = c.request('tools/call', {'name':'read_console', 'arguments':{'action':'get', 'types':['error'], 'count':20, 'format':'detailed'}})
        save(raw)
        result = payload(raw)
        print(json.dumps(result, ensure_ascii=True))
        if not result.get('success') or result.get('data'):
            raise SystemExit('Console not clear; preserve errors')
    else:
        code_path = (ROOT / a.code).resolve()
        if not code_path.is_relative_to(ROOT):
            raise SystemExit('Code outside workspace')
        state = json.loads(c.request('resources/read', {'uri':'mcpforunity://editor/state'})['contents'][0]['text'])['data']
        require_idle(c, state)
        raw = c.request('tools/call', {'name':'execute_code', 'arguments':{'action':'execute', 'compiler':'codedom', 'code':code_path.read_text(encoding='utf-8-sig')}})
        save(raw)
        result = payload(raw)
        print(json.dumps(result, ensure_ascii=True))
        if not result.get('success'):
            raise SystemExit('Unknown/failed Unity result; inspect state before retry')
