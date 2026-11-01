"""One owned editor Play session, real Game screenshot, then return to the saved test host."""
import json, struct, sys, time
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/'Tools/MCP'))
from client import Client
from preflight import payload,require_idle
E=ROOT/'Docs/Tests/InfiltrationPrototype/Evidence/A01'
version=sys.argv[1]
if not version.isalnum():raise SystemExit('Safe version required')
prefix='ShadowDiagnostic_'+version
if (E/(prefix+'_Open.json')).exists():raise SystemExit('Preserve native preview')
c=Client()
def state():return json.loads(c.request('resources/read',{'uri':'mcpforunity://editor/state'})['contents'][0]['text'])['data']
def tool(name,args,label):
    raw=c.request('tools/call',{'name':name,'arguments':args})
    (E/(prefix+'_'+label+'.json')).write_text(json.dumps(raw,indent=2),encoding='utf-8')
    value=payload(raw)
    if not value.get('success'):raise RuntimeError('Unity request did not succeed: '+label)
    return value
def execute(code,label):return tool('execute_code',{'action':'execute','compiler':'codedom','code':code},label)['data']['result']
def wait_mode(playing):
    end=time.monotonic()+70
    attempt=0
    while time.monotonic()<end:
        attempt+=1
        s=execute('''return new {playing=UnityEditor.EditorApplication.isPlaying,requested=UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode,compiling=UnityEditor.EditorApplication.isCompiling,frame=UnityEngine.Time.frameCount};''',('WaitPlay_' if playing else 'WaitStop_')+str(attempt))
        if s['playing']==playing and s['requested']==playing and not s['compiling'] and (not playing or s['frame']>2):return s
        time.sleep(.7)
    raise RuntimeError('Editor transition did not finish')
require_idle(c,state())
opened=execute('''
EchoStation.Rendering.Editor.SceneSafetyGuard.RequireClean();
if(UnityEngine.SceneManagement.SceneManager.sceneCount!=1||UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!="Assets/EchoStation/Tests/Scenes/S1_P09_RegressionHost.unity")throw new System.InvalidOperationException("Preserve unexpected layout");
UnityEditor.SceneManagement.EditorSceneManager.OpenScene(EchoStation.InfiltrationLevel.Editor.LevelBuilder.ScenePath,UnityEditor.SceneManagement.OpenSceneMode.Single);
var type=typeof(UnityEditor.EditorWindow).Assembly.GetType("UnityEditor.GameView");UnityEditor.EditorWindow.GetWindow(type).Show();UnityEditor.EditorWindow.GetWindow(type).Focus();
bool oldBackground=UnityEngine.Application.runInBackground;UnityEngine.Application.runInBackground=true;
return new {background=oldBackground,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path};
''','Open')
started=False
try:
    tool('manage_editor',{'action':'play'},'Play');started=True;wait_mode(True)
    time.sleep(1)
    execute('''var root=EchoStation.InfiltrationLevel.Editor.LevelBuilder.World(UnityEngine.SceneManagement.SceneManager.GetActiveScene());root.GetComponentInChildren<EchoStation.Salvage.SalvageMotor>().enabled=false;root.GetComponentInChildren<EchoStation.Player.PlayerInputReader>().SetUiBlocked(true);var camera=root.GetComponentInChildren<UnityEngine.Camera>();camera.transform.position=new UnityEngine.Vector3(0,1.6f,-14);camera.transform.LookAt(new UnityEngine.Vector3(0,1.8f,-7));var fill=new UnityEngine.GameObject("DiagnosticOnlyFill").AddComponent<UnityEngine.Light>();fill.transform.SetParent(camera.transform,false);fill.type=UnityEngine.LightType.Point;fill.range=18;fill.intensity=5;fill.shadows=UnityEngine.LightShadows.None;return new {ready=true};''','Setup')
    for enabled in [True,False]:
        execute('''var sun=UnityEngine.Object.FindObjectsByType<UnityEngine.Light>().Single(l=>l.type==UnityEngine.LightType.Directional);sun.shadows=UnityEngine.LightShadows.'''+('Soft' if enabled else 'None')+''';return new {shadows=sun.shadows.ToString()};''','Toggle_'+str(enabled))
        time.sleep(.5)
        path=E/('ShadowDiagnostic_'+version+'_'+str(enabled)+'.png')
        execute('''var camera=UnityEngine.Object.FindObjectsByType<UnityEngine.Camera>().Single(c=>c.name=="IL_Camera");camera.scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();using(var capture=new EchoStation.Tests.Fixtures.SceneFrameCapture(camera)){var pixels=capture.Capture("'''+path.as_posix()+'''");return new {mean=pixels.Average(p=>(double)p.r),image="'''+path.name+'''"};}''','Image_'+str(enabled))
    print('Shadow comparison captured; actual owned Play session only',flush=True)

finally:
    current=execute('''return new {playing=UnityEditor.EditorApplication.isPlaying,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path};''','FinalState')
    if current['playing']:
        if current['scene']!=opened['scene']:raise RuntimeError('Preserve user-changed Play session')
        tool('manage_editor',{'action':'stop'},'Stop');wait_mode(False)
    execute('''
EchoStation.Rendering.Editor.SceneSafetyGuard.RequireClean();
if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=EchoStation.InfiltrationLevel.Editor.LevelBuilder.ScenePath)throw new System.InvalidOperationException("Preserve user-changed scene");
UnityEngine.Application.runInBackground='''+str(opened['background']).lower()+''';
UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/EchoStation/Tests/Scenes/S1_P09_RegressionHost.unity",UnityEditor.SceneManagement.OpenSceneMode.Single);
return new {restoredHost=true,playing=UnityEditor.EditorApplication.isPlaying,background=UnityEngine.Application.runInBackground};
''','Closed')
