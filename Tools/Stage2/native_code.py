"""Execute a saved, reviewable C# diagnostic through the local Unity connector."""
import json
import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'MCP'))
from client import Client
from preflight import payload
source, evidence = map(Path, sys.argv[1:3])
if evidence.exists():
    raise SystemExit('Fresh evidence path required')
raw = Client().request('tools/call', {'name': 'execute_code', 'arguments': {
    'action': 'execute', 'compiler': 'codedom', 'code': source.read_text(encoding='utf-8')}})
evidence.parent.mkdir(parents=True, exist_ok=True)
evidence.write_text(json.dumps(raw, ensure_ascii=False, indent=2), encoding='utf-8')
value = payload(raw)
print(json.dumps(value, ensure_ascii=False, indent=2))
if not value.get('success'):
    raise SystemExit(1)
