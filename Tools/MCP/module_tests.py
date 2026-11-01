"""Guarded test start/poll: never let the MCP runner auto-save user's dirty scenes."""
import argparse
import json
import re
import time
from pathlib import Path
from client import Client
from preflight import payload,require_idle

ROOT=Path(__file__).resolve().parents[2]
parser=argparse.ArgumentParser()
parser.add_argument('action',choices=['start','poll'])
parser.add_argument('--evidence',required=True)
parser.add_argument('--label',required=True)
parser.add_argument('--assembly',action='append')
parser.add_argument('--group',action='append')
parser.add_argument('--output')
args=parser.parse_args()
if not re.fullmatch(r'[A-Za-z0-9_-]+',args.label):parser.error('Invalid artifact label')
evidence=(ROOT/args.evidence).resolve()
if not evidence.is_relative_to(ROOT/'Docs'/'Tests'):parser.error('Evidence must stay within Docs/Tests')
evidence.mkdir(parents=True,exist_ok=True)
def save(name,value):(evidence/name).write_text(json.dumps(value,indent=2,ensure_ascii=False),encoding='utf-8')
c=Client()
def tool(name,arguments):return c.request('tools/call',{'name':name,'arguments':arguments})
if args.action=='start':
    state=json.loads(c.request('resources/read',{'uri':'mcpforunity://editor/state'})['contents'][0]['text'])['data']
    try:require_idle(c,state,lambda raw:save(args.label+'-NativePreflight.json',raw))
    except RuntimeError as error:raise SystemExit(str(error))
    if (state.get('assets') or {}).get('external_changes_dirty'):
        save(args.label+'-ExternalChangesPending.json',state)
        raise SystemExit('External changes pending: refresh without requesting another compile, verify current DLLs/readiness, then retry; no test started')
    started=time.time();tool('execute_menu_item',{'menu_path':'Tools/EchoStation/Checks/Export Scene Safety'})
    safety_path=ROOT/'Library/EchoStation/SceneSafety.json'
    if not safety_path.exists() or safety_path.stat().st_mtime<started-.05:raise SystemExit('Fresh scene safety preflight missing; no test started')
    safety=json.loads(safety_path.read_text(encoding='utf-8-sig'));save(args.label+'-SceneSafety.json',safety)
    if not safety['allowed']:raise SystemExit('Scene safety rejected: no save/discard/test performed; inspect preflight')
    render_started=time.time();tool('execute_menu_item',{'menu_path':'Tools/EchoStation/Checks/Export Render Safety'})
    render_path=ROOT/'Library/EchoStation/RenderSafety.json'
    if not render_path.exists() or render_path.stat().st_mtime<render_started-.05:raise SystemExit('Fresh render safety preflight missing; no test started')
    render=json.loads(render_path.read_text(encoding='utf-8-sig'));save(args.label+'-RenderSafety.json',render)
    if not render['allowed']:raise SystemExit('Render bindings/API/HDR/Forward baseline rejected: no test started; inspect render preflight')
    errors=payload(tool('read_console',{'action':'get','types':['error'],'count':10,'format':'plain'}))
    if errors.get('data'):save(args.label+'-ConsoleErrors.json',errors);raise SystemExit('Console errors present; no test started')
    request={'mode':'EditMode','include_details':True}
    if args.assembly:request['assembly_names']=args.assembly
    if args.group:request['group_names']=args.group
    raw=tool('run_tests',request);save(args.label+'-Job.json',raw);receipt=payload(raw);print(json.dumps(receipt,ensure_ascii=False))
    if not receipt.get('success') or not (receipt.get('data') or {}).get('job_id'):
        raise SystemExit('No actual test job started; preserve receipt and retry when Unity is ready')
else:
    job=payload(json.loads((evidence/(args.label+'-Job.json')).read_text(encoding='utf-8-sig')))['data']['job_id']
    raw=tool('get_test_job',{'job_id':job,'include_details':True,'include_failed_tests':True})
    name=args.output or args.label+'.json'
    if Path(name).name!=name:parser.error('Output must be a single filename')
    save(name,raw);data=payload(raw)['data']
    print(json.dumps({'status':data['status'],'summary':(data.get('result') or {}).get('summary'),
                      'failures':(data.get('progress') or {}).get('failures_so_far')},ensure_ascii=False))
    summary=(data.get('result') or {}).get('summary')
    if summary is not None and (summary.get('total',0)<=0 or summary.get('failed',0)>0 or summary.get('resultState')!='Passed'):
        raise SystemExit('No positive passing test suite; preserved raw result requires review')
