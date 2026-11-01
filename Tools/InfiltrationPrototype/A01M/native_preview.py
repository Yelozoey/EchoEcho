"""One owned editor Play session, real Game screenshot, then return to the saved test host."""
import json, struct, sys, time
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/'Tools/MCP'))
from client import Client
from preflight import payload,require_idle
E=ROOT/'Docs/Tests/InfiltrationPrototype/Evidence/A01M'
version=sys.argv[1]
if not version.isalnum():raise SystemExit('Safe version required')
prefix='NativePreview_'+version
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
UnityEditor.SceneManagement.EditorSceneManager.OpenScene(EchoStation.InfiltrationMetal.Editor.MetalScene.ScenePath,UnityEditor.SceneManagement.OpenSceneMode.Single);
var type=typeof(UnityEditor.EditorWindow).Assembly.GetType("UnityEditor.GameView");UnityEditor.EditorWindow.GetWindow(type).Show();UnityEditor.EditorWindow.GetWindow(type).Focus();
bool oldBackground=UnityEngine.Application.runInBackground;UnityEngine.Application.runInBackground=true;
return new {background=oldBackground,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path};
''','Open')
started=False
try:
    tool('manage_editor',{'action':'play'},'Play');started=True;wait_mode(True)
    time.sleep(1)
    image_path=E/('GameView_'+version+'.png')
    if image_path.exists():raise RuntimeError('Preserve screenshot')
    result=execute('''
if(!UnityEditor.EditorApplication.isPlaying||UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=EchoStation.InfiltrationMetal.Editor.MetalScene.ScenePath)throw new System.InvalidOperationException("Preview ownership changed");
var camera=UnityEngine.Object.FindObjectsByType<UnityEngine.Camera>().Single(x=>x.name=="IL_Camera");
UnityEngine.ScreenCapture.CaptureScreenshot("'''+image_path.as_posix()+'''");
return new {scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,focus=UnityEditor.EditorWindow.focusedWindow?UnityEditor.EditorWindow.focusedWindow.GetType().FullName:null,frame=UnityEngine.Time.frameCount,hdr=camera.allowHDR,sdr=!camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().allowHDROutput,pipeline=UnityEditor.AssetDatabase.GetAssetPath(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline),listeners=UnityEngine.Object.FindObjectsByType<UnityEngine.AudioListener>().Count(a=>a.enabled),aiConnected=false};
''','Capture')
    end=time.monotonic()+20
    while time.monotonic()<end and (not image_path.exists() or image_path.stat().st_size<100):time.sleep(.3)
    if not image_path.exists():raise RuntimeError('Native Game screenshot not saved')
    data=image_path.read_bytes();assert data[:8]==b'\x89PNG\r\n\x1a\n'
    result.update(width=struct.unpack('>I',data[16:20])[0],height=struct.unpack('>I',data[20:24])[0],bytes=len(data),image=image_path.relative_to(ROOT).as_posix())
    (E/(prefix+'_Image.json')).write_text(json.dumps(result,indent=2),encoding='utf-8')
    print(json.dumps(result),flush=True)
finally:
    current=execute('''return new {playing=UnityEditor.EditorApplication.isPlaying,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path};''','FinalState')
    if current['playing']:
        if current['scene']!=opened['scene']:raise RuntimeError('Preserve user-changed Play session')
        tool('manage_editor',{'action':'stop'},'Stop');wait_mode(False)
    execute('''
EchoStation.Rendering.Editor.SceneSafetyGuard.RequireClean();
if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=EchoStation.InfiltrationMetal.Editor.MetalScene.ScenePath)throw new System.InvalidOperationException("Preserve user-changed scene");
UnityEngine.Application.runInBackground='''+str(opened['background']).lower()+''';
UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/EchoStation/Tests/Scenes/S1_P09_RegressionHost.unity",UnityEditor.SceneManagement.OpenSceneMode.Single);
return new {restoredHost=true,playing=UnityEditor.EditorApplication.isPlaying,background=UnityEngine.Application.runInBackground};
''','Closed')
