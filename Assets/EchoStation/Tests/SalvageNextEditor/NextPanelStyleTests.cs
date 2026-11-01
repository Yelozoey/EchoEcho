using System;
using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.Player;
using EchoStation.Salvage;
using EchoStation.SalvageNext.Editor;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace EchoStation.SalvageNext.Tests
{
    public sealed class NextPanelStyleTests
    {
        sealed class PresentationDisplay:IDisplayBackend
        {public DisplaySelection Capture()=>DisplaySelection.Default();public bool TryApply(DisplaySelection value,out string error){error="Presentation fixture does not change native display";return false;}}
        const string Evidence="Docs/Tests/SalvagePrototype/Evidence/Feedback02/U02";
        Scene previous,scene;bool background;NextRoomStreaming stream;PlayerInputReader input;NextCommandPanel commands;NextDisplayPanel display;SalvageMission mission;string run;
        static IEnumerator Wait(Func<bool> condition){var timer=System.Diagnostics.Stopwatch.StartNew();while(!condition()){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(20));yield return null;}}
        [UnityTest,Timeout(180000)]public IEnumerator AllFourStyledPanelsRenderInTwoSizesAndTaskBoardKeepsConfirmationFlow()
        {
            yield return new EnterPlayMode();background=Application.runInBackground;Application.runInBackground=true;previous=SceneManager.GetActiveScene();scene=EditorSceneManager.LoadSceneInPlayMode(NextArtV2Stream.Main,new LoadSceneParameters(LoadSceneMode.Additive));while(!scene.isLoaded)yield return null;SceneManager.SetActiveScene(scene);stream=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<NextRoomStreaming>()).Single();yield return Wait(()=>!stream.Blocking);yield return null;input=stream.GetComponent<PlayerInputReader>();input.SetApplicationFocus(true);input.SetPointerCaptured(true);stream.GetComponent<SalvageMotor>().enabled=false;mission=stream.GetComponent<SalvageMission>();commands=stream.GetComponent<NextCommandPanel>();display=stream.GetComponent<NextDisplayPanel>();run=Guid.NewGuid().ToString("N");Directory.CreateDirectory(Evidence);
            display.ConfigureForTests(new PresentationDisplay(),Path.GetFullPath("Library/EchoStation/SalvageNext/PanelStyle_"+run));
            Assert.That(commands.Open(true),Is.True);commands.InputField.text="电量";commands.Submit();yield return null;yield return Capture("EchoCommands");commands.Close();Assert.That(mission.OpenBoard(),Is.True);yield return null;Assert.That(stream.transform.Find("TaskBoard/Panel/Text").GetComponent<Text>().enabled,Is.False);Assert.That(mission.RequestCompletion(),Is.False);Assert.That(display.Open(),Is.False);yield return Capture("TaskBoard");mission.CloseBoard();
            Assert.That(display.Open(),Is.True);yield return Capture("NextDisplay");display.RequestClose();yield return null;
            var session=stream.GetComponent<NextSession>();var seed=session.Capture();seed.World.Cargo.Single(c=>c.Id=="cargo_a").State=1;Assert.That(session.TryRestore(seed,out var error),Is.True,error);yield return Wait(()=>!stream.Blocking);var delivery=stream.GetComponent<SalvageDelivery>();Assert.That(delivery.Request(),Is.True);yield return new WaitForSecondsRealtime(.2f);yield return Capture("DeliveryConfirmation");Assert.That(commands.Open(false),Is.False);delivery.Cancel();Assert.That(input.CanPlay,Is.True);Assert.That(stream.GetComponent<SalvageCarry>().HeldId,Is.EqualTo("cargo_a"));
            seed=session.Capture();foreach(var item in seed.World.Cargo.Where(c=>new[]{"cargo_b","cargo_d","cargo_e"}.Contains(c.Id)))item.State=2;Assert.That(session.TryRestore(seed,out error),Is.True,error);yield return Wait(()=>!stream.Blocking);Assert.That(mission.OpenBoard(),Is.True);Assert.That(mission.RequestCompletion(),Is.True);stream.GetComponent<NextPanelStyle>().RefreshBoard();Assert.That(stream.transform.Find("TaskBoard/Panel/BoardStatus").GetComponent<Text>().text,Does.Contain("确认结束"));Assert.That(mission.Completed,Is.False);Assert.That(mission.ConfirmCompletion(),Is.True);stream.GetComponent<NextPanelStyle>().RefreshBoard();Assert.That(stream.transform.Find("TaskBoard/Panel/BoardStatus").GetComponent<Text>().text,Does.Contain("已完成"));
            File.WriteAllText(Evidence+"/Panels_"+run+".json",new JObject{{"panels",4},{"actualUguiSizes",new JArray("1920x1080","1280x720")},{"confirmationStillRequired",true},{"cancelPreservesHeld",true},{"modalOwnership",true}}.ToString());
        }
        IEnumerator Capture(string name)
        {
            var canvas=stream.transform.Find(name).GetComponent<Canvas>();Assert.That(canvas,Is.Not.Null);Assert.That(canvas.transform.Find("ModalShade"),Is.Not.Null);var scaler=canvas.GetComponent<CanvasScaler>();var nodes=canvas.GetComponentsInChildren<Transform>(true);var layers=nodes.Select(t=>t.gameObject.layer).ToArray();
            foreach(int width in new[]{1920,1280})
            {
                int height=width*9/16;var go=new GameObject("OwnedPanelCapture",typeof(Camera));SceneManager.MoveGameObjectToScene(go,scene);var camera=go.GetComponent<Camera>();camera.enabled=false;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.4f,.4f,.4f);var rt=new RenderTexture(width,height,24);rt.Create();camera.targetTexture=rt;camera.aspect=16f/9;var old=RenderTexture.active;Texture2D pixels=null;
                try
                {
                    foreach(var node in nodes)node.gameObject.layer=31;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;scaler.enabled=false;canvas.scaleFactor=width/1920f;yield return null;Canvas.ForceUpdateCanvases();
                    foreach(var button in canvas.GetComponentsInChildren<Button>()){var text=button.GetComponentInChildren<Text>();if(!text)continue;Assert.That(text.preferredWidth,Is.LessThanOrEqualTo(text.rectTransform.rect.width+2),name+"/"+button.name);Assert.That(text.preferredHeight,Is.LessThanOrEqualTo(text.rectTransform.rect.height+2),name+"/"+button.name);}
                    RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=rt});RenderTexture.active=rt;pixels=new Texture2D(width,height,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();File.WriteAllBytes(Evidence+"/"+name+"_"+width+"_"+run+".png",pixels.EncodeToPNG());Assert.That(pixels.GetPixels32().Select(p=>p.r+256*p.g+65536*p.b).Distinct().Count(),Is.GreaterThan(100));
                }
                finally{canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;scaler.enabled=true;for(int i=0;i<nodes.Length;i++)if(nodes[i])nodes[i].gameObject.layer=layers[i];RenderTexture.active=old;camera.targetTexture=null;rt.Release();Object.Destroy(rt);if(pixels)Object.Destroy(pixels);Object.Destroy(go);}
            }
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {if(!Application.isPlaying)yield break;if(commands)commands.Close();if(display)display.RequestClose();var owner=stream?stream.Owner:null;if(previous.IsValid())SceneManager.SetActiveScene(previous);if(scene.IsValid()&&scene.isLoaded){var op=SceneManager.UnloadSceneAsync(scene);while(op!=null&&!op.isDone)yield return null;}if(owner!=null)yield return Wait(()=>owner.IsClosed);Application.runInBackground=background;yield return new ExitPlayMode();}
    }
}
