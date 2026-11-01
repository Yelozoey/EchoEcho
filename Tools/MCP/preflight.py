"""Fresh native fallback for a stale MCP editor-status cache; never save scenes."""
import json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
def payload(raw):
    value=raw.get('structuredContent')
    if value is None:value=json.loads(next(c['text'] for c in raw['content'] if c['type']=='text'))
    while 'result' in value and 'success' not in value:value=value['result']
    return value

def require_idle(client,state,record=None):
    if not isinstance(state,dict) or 'advice' not in state or 'tests' not in state:
        raise RuntimeError('Editor status incomplete during reload; no action dispatched. Retry after compilation.')
    if state['advice']['ready_for_tools']:
        if state['tests']['is_running']:raise RuntimeError('Unity tests already running; no action dispatched')
        return
    if state['advice']['blocking_reasons']!=['stale_status']:raise RuntimeError('Unity not idle/ready; no action dispatched')
    code='''var api = System.AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("UnityEditor.TestTools.TestRunner.Api.TestRunnerApi")).FirstOrDefault(t => t != null);
var method = api == null ? null : api.GetMethod("IsRunActive", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
return new { playing=UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode, compiling=UnityEditor.EditorApplication.isCompiling, updating=UnityEditor.EditorApplication.isUpdating, testsActive=method == null || (bool)method.Invoke(null,null), assetsPath=UnityEngine.Application.dataPath, utc=System.DateTime.UtcNow.ToString("O") };'''
    raw=client.request('tools/call',{'name':'execute_code','arguments':{'action':'execute','code':code,'compiler':'codedom'}})
    if record:record(raw)
    native=payload(raw)
    if not native.get('success'):raise RuntimeError('Fresh native preflight failed; no action dispatched')
    native=native['data']['result']
    if any(native[k] for k in ['playing','compiling','updating','testsActive']) or Path(native['assetsPath']).resolve()!=ROOT/'Assets':raise RuntimeError('Fresh native Editor busy/wrong project; no action dispatched')
    print('Status cache stale; fresh native idle/test-runner checks passed. Continuing safety/error gates.')
