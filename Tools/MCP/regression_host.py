"""Temporarily isolate a full regression in an empty saved scene; never save user scenes."""
import argparse
import json
import re
from pathlib import Path
from client import Client
from preflight import payload, require_idle

ROOT = Path(__file__).resolve().parents[2]
HOST = 'Assets/EchoStation/Tests/Scenes/S1_P09_RegressionHost.unity'
parser = argparse.ArgumentParser()
parser.add_argument('action', choices=['enter', 'leave'])
parser.add_argument('--evidence', required=True)
parser.add_argument('--label', default='FullRegression')
args = parser.parse_args()
if not re.fullmatch(r'[A-Za-z0-9_-]+', args.label):
    parser.error('Invalid evidence label')
evidence = (ROOT / args.evidence).resolve()
if not evidence.is_relative_to(ROOT / 'Docs' / 'Tests'):
    parser.error('Evidence must stay inside project Docs/Tests')
evidence.mkdir(parents=True, exist_ok=True)
before = evidence / (args.label + '-WorkspaceBefore.json')
c = Client()

def save(name, raw):
    (evidence / name).write_text(json.dumps(raw, ensure_ascii=False, indent=2), encoding='utf-8')

def execute(code):
    raw = c.request('tools/call', {'name': 'execute_code', 'arguments': {'action': 'execute', 'code': code}})
    result = payload(raw)
    if not result.get('success'):
        raise RuntimeError(json.dumps(result, ensure_ascii=True))
    return raw, result['data']['result']

state = json.loads(c.request('resources/read', {'uri': 'mcpforunity://editor/state'})['contents'][0]['text'])['data']
require_idle(c, state, lambda raw: save(args.label + '-WorkspaceNativePreflight.json', raw))
guard = '''EchoStation.Rendering.Editor.SceneSafetyGuard.RequireClean();
if(UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage()!=null)throw new System.InvalidOperationException("Preserving open Prefab Stage.");
var api=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UnityEditor.TestTools.TestRunner.Api.TestRunnerApi")).FirstOrDefault(t=>t!=null);
if(api==null||(bool)api.GetMethod("IsRunActive",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).Invoke(null,null))throw new System.InvalidOperationException("Test runner active or unavailable.");
'''

if args.action == 'enter':
    if before.exists():
        raise SystemExit('Snapshot already exists; use a new label instead of overwriting it')
    errors = payload(c.request('tools/call', {'name': 'read_console', 'arguments': {'action': 'get', 'types': ['error'], 'count': 10, 'format': 'plain'}}))
    if errors.get('data'):
        raise SystemExit('Console errors present; workspace unchanged')
    raw, snapshot = execute(guard + '''var setup=UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();
if(setup.Any(s=>s.isLoaded&&string.IsNullOrEmpty(s.path)))throw new System.InvalidOperationException("Cannot restore an unnamed user scene; preserving it.");
return new {scenes=setup.Select(s=>new {path=s.path,loaded=s.isLoaded,active=s.isActive}).ToArray(),selection=UnityEditor.Selection.objects.Select(UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow).Select(i=>i.ToString()).ToArray(),optionsEnabled=UnityEditor.EditorSettings.enterPlayModeOptionsEnabled,options=(int)UnityEditor.EditorSettings.enterPlayModeOptions,defaultPipelinePath=UnityEditor.AssetDatabase.GetAssetPath(UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline),qualityPipelinePath=UnityEditor.AssetDatabase.GetAssetPath(UnityEngine.QualitySettings.renderPipeline)};''')
    save(before.name, raw)
    expected = [s['path'] for s in snapshot['scenes']]
    code = guard + 'var expected=new string[]{' + ','.join(json.dumps(p) for p in expected) + '};'
    code += '''if(!UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup().Select(s=>s.path).SequenceEqual(expected))throw new System.InvalidOperationException("User scene layout changed; preserving it.");
UnityEditor.Selection.objects=new UnityEngine.Object[0];UnityEditor.ActiveEditorTracker.sharedTracker.ForceRebuild();
'''
    code += 'const string host=' + json.dumps(HOST) + ';'
    code += '''if(!UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>(host)){var scene=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,host))throw new System.InvalidOperationException("Owned host save failed.");}else UnityEditor.SceneManagement.EditorSceneManager.OpenScene(host,UnityEditor.SceneManagement.OpenSceneMode.Single);
return new {host=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,sceneCount=UnityEngine.SceneManagement.SceneManager.sceneCount,dirty=UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty};'''
    raw, result = execute(code)
    save(args.label + '-WorkspaceEntered.json', raw)
    if result['host'] != HOST or result['sceneCount'] != 1 or result['dirty']:
        raise SystemExit('Unexpected host state; no test started')
    print('Empty saved regression host ready; original scene layout recorded without saving user scenes')
else:
    if not before.exists():
        raise SystemExit('No restoration snapshot; workspace unchanged')
    snapshot = payload(json.loads(before.read_text(encoding='utf-8-sig')))['data']['result']
    # Unity's runner can already have restored the exact captured layout. Verify it
    # without changing scenes or the user's current selection; other layouts keep the old guard.
    raw, current = execute(guard + '''return new {scenes=UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup().Select(s=>new {path=s.path,loaded=s.isLoaded,active=s.isActive}).ToArray(),optionsEnabled=UnityEditor.EditorSettings.enterPlayModeOptionsEnabled,options=(int)UnityEditor.EditorSettings.enterPlayModeOptions,defaultPipelinePath=UnityEditor.AssetDatabase.GetAssetPath(UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline),qualityPipelinePath=UnityEditor.AssetDatabase.GetAssetPath(UnityEngine.QualitySettings.renderPipeline)};''')
    save(args.label + '-WorkspaceObservedBeforeLeave.json', raw)
    observed_keys = ['scenes', 'optionsEnabled', 'options'] + [k for k in ['defaultPipelinePath', 'qualityPipelinePath'] if k in snapshot]
    if {k: current[k] for k in observed_keys} == {k: snapshot[k] for k in observed_keys}:
        save(args.label + '-WorkspaceAlreadyOriginal.json', raw)
        print('Original layout already present; no scene/selection changes; options and bindings verified')
        raise SystemExit(0)
    rows = ['new UnityEditor.SceneManagement.SceneSetup{path=' + json.dumps(s['path']) + ',isLoaded=' + str(s['loaded']).lower() + ',isActive=' + str(s['active']).lower() + '}' for s in snapshot['scenes']]
    code = guard + 'const string host=' + json.dumps(HOST) + ';'
    code += '''if(UnityEngine.SceneManagement.SceneManager.sceneCount!=1||UnityEngine.SceneManagement.SceneManager.GetSceneAt(0).path!=host)throw new System.InvalidOperationException("Scene layout changed outside regression host; preserving current user state.");
'''
    code += 'var setup=new UnityEditor.SceneManagement.SceneSetup[]{' + ','.join(rows) + '};'
    code += '''foreach(var item in setup)if(!UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>(item.path))throw new System.InvalidOperationException("Original scene no longer available; not closing current state.");
UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(setup);
var selection=new System.Collections.Generic.List<UnityEngine.Object>();
'''
    for item in snapshot['selection']:
        code += '{UnityEditor.GlobalObjectId id;if(UnityEditor.GlobalObjectId.TryParse(' + json.dumps(item) + ',out id)){var value=UnityEditor.GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id);if(value)selection.Add(value);}}'
    code += '''UnityEditor.Selection.objects=selection.ToArray();UnityEditor.ActiveEditorTracker.sharedTracker.ForceRebuild();
return new {scenes=UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup().Select(s=>new {path=s.path,loaded=s.isLoaded,active=s.isActive}).ToArray(),optionsEnabled=UnityEditor.EditorSettings.enterPlayModeOptionsEnabled,options=(int)UnityEditor.EditorSettings.enterPlayModeOptions,defaultPipelinePath=UnityEditor.AssetDatabase.GetAssetPath(UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline),qualityPipelinePath=UnityEditor.AssetDatabase.GetAssetPath(UnityEngine.QualitySettings.renderPipeline)};'''
    raw, result = execute(code)
    save(args.label + '-WorkspaceRestored.json', raw)
    keys = ['scenes', 'optionsEnabled', 'options'] + [k for k in ['defaultPipelinePath', 'qualityPipelinePath'] if k in snapshot]
    if {k: result[k] for k in keys} != {k: snapshot[k] for k in keys}:
        raise SystemExit('Restoration/check mismatch; inspect evidence, no user data was saved/discarded')
    print('Original scene layout restored; play-mode options verified unchanged')
