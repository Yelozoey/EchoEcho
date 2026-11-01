using System;
using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.Echo;
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
    public sealed class NextHudTests
    {
        Scene previous,scene;bool background;NextHud hud;NextRoomStreaming stream;PlayerInputReader input;SalvageEcho echo;SalvageMission mission;
        static IEnumerator Wait(Func<bool> condition){var timer=System.Diagnostics.Stopwatch.StartNew();while(!condition()){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(20));yield return null;}}
        IEnumerator Boot()
        {
            previous=SceneManager.GetActiveScene();background=Application.runInBackground;Application.runInBackground=true;scene=EditorSceneManager.LoadSceneInPlayMode(NextArtV2Stream.Main,new LoadSceneParameters(LoadSceneMode.Additive));while(!scene.isLoaded)yield return null;SceneManager.SetActiveScene(scene);stream=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<NextRoomStreaming>()).Single();yield return Wait(()=>!stream.Blocking);yield return null;
            hud=stream.GetComponent<NextHud>();Assert.That(hud,Is.Not.Null);input=stream.GetComponent<PlayerInputReader>();input.SetApplicationFocus(true);input.SetPointerCaptured(true);stream.GetComponent<SalvageMotor>().enabled=false;mission=stream.GetComponent<SalvageMission>();echo=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvageEcho>()).Single();echo.GetComponent<SalvageEchoCoordinator>().enabled=false;echo.enabled=false;hud.RefreshNow();Directory.CreateDirectory(NextArtV2Ui.Evidence);
        }
        [UnityTest]public IEnumerator HudReadsActualRestoredCargoAndBatteryAndPreservesModalInput()
        {
            yield return new EnterPlayMode();yield return Boot();Assert.That(hud.Visible,Is.True);Assert.That(hud.HeldText,Is.EqualTo("空手"));Assert.That(stream.transform.Find("SalvageHUD"),Is.Null);Assert.That(stream.transform.Find("Crosshair"),Is.Null);Assert.That(mission.enabled,Is.True);
            var session=stream.GetComponent<NextSession>();var snapshot=session.Capture();snapshot.World.Cargo.Single(c=>c.Id=="cargo_a").State=1;foreach(var item in snapshot.World.Cargo.Where(c=>new[]{"cargo_b","cargo_d","cargo_e"}.Contains(c.Id)))item.State=2;
            Assert.That(session.TryRestore(snapshot,out var error),Is.True,error);yield return Wait(()=>!stream.Blocking);echo.Battery.Restore(18,true);hud.RefreshNow();Assert.That(hud.HeldText,Is.EqualTo("A 工具箱"));Assert.That(hud.Progress,Is.EqualTo("3 / 3 件    18 / 12 分"));Assert.That(hud.BatteryText,Does.StartWith("18%"));Assert.That(mission.CanComplete,Is.True);Assert.That(GameRuntimeOptions.UseLLM,Is.False);
            Assert.That(mission.OpenBoard(),Is.True);hud.RefreshNow();Assert.That(hud.Visible,Is.False);Assert.That(input.UiBlocked,Is.True);Assert.That(input.PointerCaptured,Is.False);mission.CloseBoard();hud.RefreshNow();Assert.That(hud.Visible,Is.True);Assert.That(input.CanPlay,Is.True);
            File.WriteAllText(NextArtV2Ui.Evidence+"/State_"+Guid.NewGuid().ToString("N")+".json",new JObject{{"actualRestoreHeld",hud.HeldText},{"progress",hud.Progress},{"battery",hud.BatteryText},{"missionEnabled",mission.enabled},{"modalOwnershipPreserved",true},{"UseLLM",false}}.ToString());
        }
        [UnityTest]public IEnumerator ActualUguiRendersAt1080And720AgainstLightAndDarkWithReadableTextBounds()
        {
            yield return new EnterPlayMode();yield return Boot();var rows=new JArray();string id=Guid.NewGuid().ToString("N");
            foreach(int width in new[]{1920,1280})foreach(bool bright in new[]{false,true})
            {
                int height=width*9/16;var go=new GameObject("OwnedHudCapture",typeof(Camera));SceneManager.MoveGameObjectToScene(go,scene);var camera=go.GetComponent<Camera>();camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=bright?new Color(.8f,.8f,.8f):new Color(.015f,.02f,.03f);camera.enabled=false;var target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);target.Create();camera.targetTexture=target;camera.aspect=16f/9;var canvas=hud.Canvas;var scaler=canvas.GetComponent<CanvasScaler>();var nodes=canvas.GetComponentsInChildren<Transform>(true);var layers=nodes.Select(t=>t.gameObject.layer).ToArray();var oldTarget=RenderTexture.active;Texture2D texture=null;
                try
                {
                    foreach(var node in nodes)node.gameObject.layer=31;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;scaler.enabled=false;canvas.scaleFactor=width/1920f;yield return null;Canvas.ForceUpdateCanvases();
                    var bounds=new JArray();foreach(var text in canvas.GetComponentsInChildren<Text>()){Assert.That(text.preferredWidth,Is.LessThanOrEqualTo(text.rectTransform.rect.width+2),text.name+" horizontal clipping");Assert.That(text.preferredHeight,Is.LessThanOrEqualTo(text.rectTransform.rect.height+2),text.name+" vertical clipping");bounds.Add(new JObject{{"text",text.text},{"fontPixels",text.fontSize*canvas.scaleFactor},{"width",text.preferredWidth},{"height",text.preferredHeight}});}
                    RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});RenderTexture.active=target;texture=new Texture2D(width,height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();string file=NextArtV2Ui.Evidence+"/HUD_"+width+"_"+(bright?"Light":"Dark")+"_"+id+".png";File.WriteAllBytes(file,texture.EncodeToPNG());var pixels=texture.GetPixels32();var center=pixels[(height/2)*width+width/2];Assert.That((int)center.r,Is.GreaterThan(bright?210:120),"Visible central reticle");Assert.That(pixels.Select(p=>p.r+256*p.g+65536*p.b).Distinct().Count(),Is.GreaterThan(100),"UI must actually render");rows.Add(new JObject{{"file",file},{"width",width},{"height",height},{"bounds",bounds}});
                }
                finally{canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;scaler.enabled=true;for(int i=0;i<nodes.Length;i++)nodes[i].gameObject.layer=layers[i];RenderTexture.active=oldTarget;camera.targetTexture=null;target.Release();Object.Destroy(target);if(texture)Object.Destroy(texture);Object.Destroy(go);}
            }
            File.WriteAllText(NextArtV2Ui.Evidence+"/Readability_"+id+".json",rows.ToString());
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {if(!Application.isPlaying)yield break;var owner=stream?stream.Owner:null;if(previous.IsValid())SceneManager.SetActiveScene(previous);if(scene.IsValid()&&scene.isLoaded){var op=SceneManager.UnloadSceneAsync(scene);while(op!=null&&!op.isDone)yield return null;}if(owner!=null)yield return Wait(()=>owner.IsClosed);Application.runInBackground=background;yield return new ExitPlayMode();}
    }
}
