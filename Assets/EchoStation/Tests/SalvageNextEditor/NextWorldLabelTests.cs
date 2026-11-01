using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using EchoStation.Echo;
using EchoStation.Player;
using EchoStation.Salvage;
using EchoStation.Salvage.Editor;
using EchoStation.SalvageNext.Editor;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace EchoStation.SalvageNext.Tests
{
    public sealed class NextWorldLabelTests
    {
        GameObject context;Camera camera;TextMesh text;NextWorldLabels labels;Material wallMaterial;bool background;Scene previous,main;RoomSceneOwner owner;Material owned;Font font;string fontPath,fontHash;
        static string Hash(string path){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
        static IEnumerator Wait(Func<bool> condition,string stage){var timer=System.Diagnostics.Stopwatch.StartNew();while(!condition()){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(15),stage);yield return null;}}
        void Begin()
        {
            background=Application.runInBackground;Application.runInBackground=true;Directory.CreateDirectory(NextWorldLabelSetup.Evidence);font=RuntimeUiFont.Get();fontPath=AssetDatabase.GetAssetPath(font);fontHash=Hash(fontPath);context=new GameObject("WorldFontTest");camera=new GameObject("View").AddComponent<Camera>();camera.transform.SetParent(context.transform);camera.transform.position=new Vector3(0,0,-2);camera.transform.rotation=Quaternion.identity;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.allowHDR=true;camera.GetUniversalAdditionalCameraData().allowHDROutput=false;camera.scene=context.scene;
            var light=new GameObject("TestSun").AddComponent<Light>();light.transform.SetParent(context.transform);light.type=LightType.Directional;light.intensity=1;light.transform.rotation=Quaternion.Euler(45,-30,0);var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.SetParent(context.transform);wall.transform.position=new Vector3(0,0,2);wall.transform.localScale=new Vector3(6,4,.4f);wallMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit"));wallMaterial.SetColor("_BaseColor",new Color(.08f,.1f,.12f));wall.GetComponent<Renderer>().sharedMaterial=wallMaterial;text=new GameObject("WorldLabel").AddComponent<TextMesh>();text.transform.SetParent(context.transform);text.transform.position=new Vector3(0,0,4);text.font=font;text.fontSize=32;text.characterSize=.025f;text.text="测试铭牌";text.color=Color.cyan;text.GetComponent<Renderer>().sharedMaterial=font.material;labels=context.AddComponent<NextWorldLabels>();labels.Configure(camera,AssetDatabase.LoadAssetAtPath<Shader>(NextWorldLabelSetup.ShaderPath),new[]{text});owned=labels.RenderingMaterial;
        }
        [UnityTest]public IEnumerator DepthShaderOccludesTextThroughOpaqueWallAndShowsLegibleForeground()
        {
            yield return new EnterPlayMode();Begin();yield return null;labels.EvaluateNow();var renderer=text.GetComponent<Renderer>();string id=Guid.NewGuid().ToString("N");Color[] warm,depth,none,old,front;
            using(var image=new SalvageFrameCapture(camera)){warm=image.Capture(NextWorldLabelSetup.Evidence+"/Warm_"+id+".png");depth=image.Capture(NextWorldLabelSetup.Evidence+"/Depth_"+id+".png");renderer.enabled=false;none=image.Capture(NextWorldLabelSetup.Evidence+"/WallOnly_"+id+".png");renderer.enabled=true;renderer.sharedMaterial=font.material;old=image.Capture(NextWorldLabelSetup.Evidence+"/Legacy_"+id+".png");renderer.sharedMaterial=owned;text.transform.position=Vector3.zero;labels.EvaluateNow();front=image.Capture(NextWorldLabelSetup.Evidence+"/Foreground_"+id+".png");}
            float hiddenDifference=SalvageFrameCapture.Difference(depth,none),oldDifference=SalvageFrameCapture.Difference(old,none),frontDifference=SalvageFrameCapture.Difference(front,none);Assert.That(hiddenDifference,Is.LessThan(.000001f));Assert.That(oldDifference,Is.GreaterThan(.00001f));Assert.That(frontDifference,Is.GreaterThan(.00001f));Assert.That(renderer.bounds.size.y,Is.InRange(.05f,.5f));camera.transform.rotation=Quaternion.Euler(0,30,0);labels.EvaluateNow();Assert.That(Quaternion.Angle(text.transform.rotation,camera.transform.rotation),Is.LessThan(.01f));text.transform.position=new Vector3(0,0,20);labels.EvaluateNow();Assert.That(renderer.enabled,Is.False);text.transform.position=Vector3.zero;labels.EvaluateNow();Assert.That(renderer.enabled,Is.True);Assert.That(Hash(fontPath),Is.EqualTo(fontHash));File.WriteAllText(NextWorldLabelSetup.Evidence+"/Depth_"+id+".json",new JObject{{"depthVsNoText",hiddenDifference},{"legacyVsNoText",oldDifference},{"foregroundVsNoText",frontDifference},{"glyphWorldHeight",renderer.bounds.size.y},{"billboard",true},{"rangeLimit",true},{"fontAssetShaUnchanged",true}}.ToString());
        }
        [UnityTest]public IEnumerator FontAtlasTracksNativeRebuildAndWarmCacheWithoutChangingFontAsset()
        {
            yield return new EnterPlayMode();Begin();text.transform.position=Vector3.zero;labels.EvaluateNow();yield return null;string id=Guid.NewGuid().ToString("N");Color[] before,after;using(var image=new SalvageFrameCapture(camera))before=image.Capture(NextWorldLabelSetup.Evidence+"/AtlasBefore_"+id+".png");int initial=labels.Rebuilds;var atlasBefore=font.material.mainTexture;int widthBefore=atlasBefore.width;font.RequestCharactersInTexture(new string(Enumerable.Range(0x4e00,512).Select(i=>(char)i).ToArray()),53,FontStyle.BoldAndItalic);yield return null;labels.EvaluateNow();using(var image=new SalvageFrameCapture(camera))after=image.Capture(NextWorldLabelSetup.Evidence+"/AtlasAfter_"+id+".png");if(atlasBefore!=font.material.mainTexture||widthBefore!=font.material.mainTexture.width)Assert.That(labels.Rebuilds,Is.GreaterThan(initial),"Changed native atlas must notify the material scope");Assert.That(owned.mainTexture,Is.SameAs(font.material.mainTexture));Assert.That(Hash(fontPath),Is.EqualTo(fontHash));float difference=SalvageFrameCapture.Difference(before,after);Assert.That(difference,Is.LessThan(.0001f));File.WriteAllText(NextWorldLabelSetup.Evidence+"/Atlas_"+id+".json",new JObject{{"nativeRebuildEvents",labels.Rebuilds-initial},{"widthBefore",widthBefore},{"widthAfter",font.material.mainTexture.width},{"difference",difference},{"atlasMatchesFont",true},{"fontAssetShaUnchanged",true},{"nativeRebuildOccurred",labels.Rebuilds>initial}}.ToString());
        }
        [UnityTest]public IEnumerator MainLabelsMergeKnownMarkerHideHeldCargoAndResetOnRestore()
        {
            yield return new EnterPlayMode();background=Application.runInBackground;Application.runInBackground=true;previous=SceneManager.GetActiveScene();main=EditorSceneManager.LoadSceneInPlayMode(NextRoomPartition.Main,new LoadSceneParameters(LoadSceneMode.Additive));while(!main.isLoaded)yield return null;SceneManager.SetActiveScene(main);var root=main.GetRootGameObjects().Single(r=>r.name=="ES_SalvageNext_World");var stream=root.GetComponentInChildren<NextRoomStreaming>();owner=stream.Owner;labels=stream.GetComponent<NextWorldLabels>();Assert.That(labels.LabelCount,Is.EqualTo(13));Assert.That(stream.GetComponent<SalvageDiscoveryMarkers>(),Is.Null);owned=labels.RenderingMaterial;var motor=stream.GetComponent<SalvageMotor>();motor.enabled=false;var echo=root.GetComponentInChildren<SalvageEcho>();var brain=echo.GetComponent<SalvageEchoCoordinator>();brain.enabled=false;brain.AutomaticWarnings=false;Assert.That(brain.Initialize(),Is.True);brain.ResetBase(IntentKind.Wait);yield return Wait(()=>owner.IsReady(0)&&owner.IsReady(1)&&!stream.Blocking,"Label main ready");var session=stream.GetComponent<NextSession>();var original=session.Capture();var discovered=NextSaveStore.Decode(NextSaveStore.Encode(original));discovered.World.Collaboration.Discovered=new[]{"cargo_a"};Assert.That(session.TryRestore(discovered,out var error),Is.True,error);yield return Wait(()=>owner.IsReady(0)&&!stream.Blocking,"Known label world ready");motor.RestorePose(new Vector3(7,.1f,19),90,35);labels.EvaluateNow();var carry=stream.GetComponent<SalvageCarry>();var label=root.GetComponentsInChildren<CargoActor>(true).Single(a=>a.Id=="cargo_a").GetComponentInChildren<TextMesh>();Assert.That(label.text,Does.StartWith("◇ 已扫描\n"));Assert.That(label.GetComponent<Renderer>().enabled,Is.True);var input=stream.GetComponent<PlayerInputReader>();input.SetApplicationFocus(true);input.SetPointerCaptured(true);using(var image=new SalvageFrameCapture(motor.View.GetComponent<Camera>()))image.Capture(NextWorldLabelSetup.Evidence+"/ActualCargoLabel_"+Guid.NewGuid().ToString("N")+".png");Assert.That(carry.TryPickFromView(),Is.True,carry.LastMessage);labels.EvaluateNow();Assert.That(label.GetComponent<Renderer>().enabled,Is.False);Assert.That(session.TryRestore(original,out error),Is.True,error);yield return Wait(()=>owner.IsReady(0)&&!stream.Blocking,"Original world ready");labels.EvaluateNow();Assert.That(label.text,Does.Not.StartWith("◇"));Assert.That(carry.HeldId,Is.Null);Assert.That(root.GetComponentsInChildren<TextMesh>(true).Length,Is.EqualTo(13));
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {
            if(!Application.isPlaying)yield break;if(labels)labels.enabled=false;if(context)Object.Destroy(context);if(wallMaterial)Object.Destroy(wallMaterial);if(previous.IsValid())SceneManager.SetActiveScene(previous);if(main.IsValid()&&main.isLoaded){var unload=SceneManager.UnloadSceneAsync(main);while(unload!=null&&!unload.isDone)yield return null;}if(owner!=null)yield return Wait(()=>owner.IsClosed&&Resources.FindObjectsOfTypeAll<RoomScenePump>().Length==0,"Labels exit cleanup");yield return null;Assert.That(owned==null,Is.True,"Owned runtime text material released");Application.runInBackground=background;yield return new ExitPlayMode();
        }
    }
}
