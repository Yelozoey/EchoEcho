"""Archive and clear only the identified fixture errors; reject unrelated messages."""
import json,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT/'Tools/MCP'))
from client import Client
from preflight import payload
c=Client();folder=ROOT/'Docs/Tests/SalvagePrototype/Evidence/LLMShortTasks/IME01';target=folder/'Fix_B_FixtureConsole.json'
if target.exists():raise SystemExit('Preserve recorded errors')
raw=c.request('tools/call',{'name':'read_console','arguments':{'action':'get','types':['error'],'count':50,'format':'detailed'}});target.write_text(json.dumps(raw,ensure_ascii=False,indent=2),encoding='utf-8');rows=payload(raw).get('data')
if not rows or any(not (r.get('message','').startswith("NullReferenceException while executing 'InputSystem.onDeviceCommand'") or r.get('message','').startswith('NullReferenceException: Object reference not set')) for r in rows):raise SystemExit('Unknown errors; do not clear')
if any(r.get('message','').startswith('NullReferenceException: Object reference not set') and not r.get('file','').replace('\\','/').endswith('LLMShortTasks/Tests/ImeLifecycleTests.cs') for r in rows):raise SystemExit('Unknown null reference owner; preserve Console')
r=c.request('tools/call',{'name':'read_console','arguments':{'action':'clear'}});(folder/'Fix_B_FixtureConsole_Cleared.json').write_text(json.dumps(r,ensure_ascii=False,indent=2),encoding='utf-8');print('Known fixture errors archived and cleared; require fresh compile/empty Console before rerun')
