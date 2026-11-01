"""Only dispatch a known Unity menu after a ready-state and empty-error check."""
import json,sys
from client import Client
from preflight import payload,require_idle,ROOT
c=Client()
state=json.loads(c.request('resources/read',{'uri':'mcpforunity://editor/state'})['contents'][0]['text'])['data']
def record(raw):
    directory=ROOT/'Library/EchoStation';directory.mkdir(parents=True,exist_ok=True)
    (directory/'SafeMenuNativePreflight.json').write_text(json.dumps({'menu':sys.argv[1],'native':raw},indent=2),encoding='utf-8')
try:require_idle(c,state,record)
except RuntimeError as error:raise SystemExit(str(error))
errors=payload(c.request('tools/call',{'name':'read_console','arguments':{'action':'get','types':['error'],'count':10,'format':'plain'}}))
if errors.get('data'):raise SystemExit('Console errors: menu not dispatched\n'+json.dumps(errors,ensure_ascii=False))
result=payload(c.request('tools/call',{'name':'execute_menu_item','arguments':{'menu_path':sys.argv[1]}}))
print(json.dumps(result,ensure_ascii=False))
raise SystemExit(0 if result.get('success') else 1)
