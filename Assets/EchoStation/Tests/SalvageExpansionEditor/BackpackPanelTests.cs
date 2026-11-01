using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EchoStation.Player;
using EchoStation.Tests.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EchoStation.SalvageExpansion.Tests
{
    public sealed class B03MovementProbe : MonoBehaviour
    {
        public int Frames; public float Seconds;
        void Update() { Frames++; Seconds += Time.deltaTime; }
    }
    public sealed class BackpackPanelTests
    {
        static Scene old, scene; static GameObject root; static BackpackPanel panel; static ExpansionPause pause;
        static PlayerInputReader input; static InventoryStore store; static B03MovementProbe moving, disabled;
        static bool background; static float scale; static SyntheticInputScope devices;
        static NavMeshData data; static NavMeshDataInstance nav; static NavMeshAgent agent;
        static IEnumerator Frame()
        {
            int start = Time.frameCount; var timer = System.Diagnostics.Stopwatch.StartNew();
            while (Time.frameCount <= start) { if (timer.Elapsed.TotalSeconds > 5) Assert.Fail("Actual frame timeout"); yield return null; }
        }
        static InventoryFootprint Rect(int w, int h) => new InventoryFootprint(Enumerable.Range(0,h).SelectMany(y => Enumerable.Range(0,w).Select(x => new InventoryCell(x,y))));
        static IEnumerator Boot()
        {
            background=Application.runInBackground;Application.runInBackground=true;scale=Time.timeScale;old=SceneManager.GetActiveScene();
            scene=SceneManager.CreateScene("Expansion_B03_"+Guid.NewGuid().ToString("N"));SceneManager.SetActiveScene(scene);
            devices=new SyntheticInputScope();root=new GameObject("B03_OwnedRoot");input=root.AddComponent<PlayerInputReader>();input.SetApplicationFocus(true);input.SetPointerCaptured(true);
            moving=root.AddComponent<B03MovementProbe>();var disabledObject=new GameObject("AlreadyDisabled");disabledObject.transform.SetParent(root.transform);disabled=disabledObject.AddComponent<B03MovementProbe>();disabled.enabled=false;
            var sources=new List<NavMeshBuildSource>{new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Box,size=new Vector3(10,.1f,10),transform=Matrix4x4.TRS(new Vector3(0,-.05f,0),Quaternion.identity,Vector3.one),area=0}};
            data=NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByIndex(0),sources,new Bounds(new Vector3(0,1,0),new Vector3(12,4,12)),Vector3.zero,Quaternion.identity);
            Assert.That(data,Is.Not.Null);nav=NavMesh.AddNavMeshData(data);var robot=new GameObject("PauseNavigationProbe");robot.transform.SetParent(root.transform);robot.transform.position=new Vector3(1,0,1);agent=robot.AddComponent<NavMeshAgent>();yield return Frame();Assert.That(agent.isOnNavMesh,Is.True);
            store=new InventoryStore(new[]{new InventoryItemDefinition("wrench","维修扳手",InventoryItemKind.Tool,Rect(1,3),2),new InventoryItemDefinition("cell","便携电池",InventoryItemKind.Battery,Rect(1,2),2),new InventoryItemDefinition("record","记录匣",InventoryItemKind.Salvage,new InventoryFootprint(new[]{new InventoryCell(0,0),new InventoryCell(0,1),new InventoryCell(1,1)}),1)});
            store.TryStow("wrench",0,0,0,out _);store.TryStow("cell",4,1,0,out _);store.TryStow("record",2,2,0,out _);
            pause=root.AddComponent<ExpansionPause>();pause.Configure(input,new Behaviour[]{moving,disabled},new[]{agent});panel=root.AddComponent<BackpackPanel>();panel.Configure(store,pause,input);yield return Frame();input.SetApplicationFocus(true);input.SetPointerCaptured(true);
        }
        static IEnumerator KeyPress(Key key)
        {
            InputSystem.QueueStateEvent(devices.Keyboard,new KeyboardState());yield return Frame();
            InputSystem.QueueStateEvent(devices.Keyboard,new KeyboardState(key));yield return Frame();
            InputSystem.QueueStateEvent(devices.Keyboard,new KeyboardState());yield return Frame();
        }
        [UnityTest] public IEnumerator NestedPauseStopsActualNavigationAndRestoresOriginalTimeAndFlags()
        {
            yield return new EnterPlayMode();yield return Boot();Time.timeScale=.75f;Assert.That(agent.SetDestination(new Vector3(3,0,1)),Is.True);yield return Frame();
            Assert.That(panel.Open(),Is.True);var extra=pause.Acquire();int frames=moving.Frames;float seconds=moving.Seconds;Vector3 position=agent.transform.position;
            yield return Frame();yield return Frame();Assert.That(Time.timeScale,Is.Zero);Assert.That(moving.Frames,Is.EqualTo(frames));Assert.That(moving.Seconds,Is.EqualTo(seconds));Assert.That(agent.transform.position,Is.EqualTo(position));Assert.That(agent.isStopped,Is.True);Assert.That(input.CanPlay,Is.False);
            panel.Close();Assert.That(Time.timeScale,Is.Zero);Assert.That(input.UiBlocked,Is.True);extra.Dispose();Assert.That(Time.timeScale,Is.EqualTo(.75f));Assert.That(moving.enabled,Is.True);Assert.That(disabled.enabled,Is.False);Assert.That(agent.isStopped,Is.False);Assert.That(input.UiBlocked,Is.False);
            yield return Frame();Assert.That(moving.Frames,Is.GreaterThan(frames));
        }
        [UnityTest] public IEnumerator ActualUguiPointerDragAndRotationCommitOnlyAtRelease()
        {
            yield return new EnterPlayMode();yield return Boot();Assert.That(panel.Open(),Is.True);Canvas.ForceUpdateCanvases();long version=store.Revision;
            var pointer=panel.Grid.Find("Cell_0_0").gameObject;var eventData=new PointerEventData(EventSystem.current){position=panel.ScreenCell(0,0)};
            ExecuteEvents.Execute(pointer,eventData,ExecuteEvents.beginDragHandler);Assert.That(panel.Drag.Active,Is.True);
            panel.Rotate();Assert.That(panel.Drag.GrabX,Is.EqualTo(2));eventData.position=panel.ScreenCell(3,0);ExecuteEvents.Execute(pointer,eventData,ExecuteEvents.dragHandler);
            Assert.That(store.Revision,Is.EqualTo(version));ExecuteEvents.Execute(pointer,eventData,ExecuteEvents.endDragHandler);
            Assert.That(store.Get("wrench").X,Is.EqualTo(1));Assert.That(store.Get("wrench").Y,Is.Zero);Assert.That(store.Get("wrench").Rotation,Is.EqualTo(1));Assert.That(store.Revision,Is.EqualTo(version+1));
        }
        [UnityTest] public IEnumerator KeyboardRotationAndEscapeDoNotReachWorldAndInvalidDropDoesNotLoseItem()
        {
            yield return new EnterPlayMode();yield return Boot();yield return KeyPress(Key.I);Assert.That(panel.IsOpen,Is.True);panel.Select(0,0);
            yield return KeyPress(Key.R);Assert.That(store.Get("wrench").Rotation,Is.EqualTo(1));Assert.That(input.CanPlay,Is.False);
            panel.BeginDrag(0,0);long version=store.Revision;Assert.That(panel.CommitDrag(-1,-1),Is.False);Assert.That(store.Revision,Is.EqualTo(version));Assert.That(store.Get("wrench").Location,Is.EqualTo(InventoryItemLocation.Backpack));
            panel.BeginDrag(0,0);yield return KeyPress(Key.Escape);Assert.That(panel.IsOpen,Is.True);Assert.That(panel.Drag.Active,Is.False);yield return KeyPress(Key.Escape);Assert.That(panel.IsOpen,Is.False);Assert.That(pause.Paused,Is.False);Assert.That(input.PointerCaptured,Is.True);
        }
        [UnityTest] public IEnumerator DisableAndFocusLossReleasePauseWithoutCommittingDrag()
        {
            yield return new EnterPlayMode();yield return Boot();panel.Open();panel.BeginDrag(0,0);long version=store.Revision;panel.enabled=false;yield return Frame();
            Assert.That(Time.timeScale,Is.EqualTo(scale));Assert.That(store.Revision,Is.EqualTo(version));Assert.That(input.UiBlocked,Is.False);
            panel.enabled=true;Assert.That(panel.Open(),Is.True);input.SetApplicationFocus(false);yield return Frame();Assert.That(panel.IsOpen,Is.False);Assert.That(Time.timeScale,Is.EqualTo(scale));Assert.That(input.PointerCaptured,Is.False);
        }
        [UnityTest] public IEnumerator ProductUguiRendersAt1080And720WithReadableTextAndCorrectCellCount()
        {
            yield return new EnterPlayMode();yield return Boot();panel.Open();panel.Select(0,0);string evidence="Docs/Tests/SalvagePrototype/Evidence/Expansion/B03";Directory.CreateDirectory(evidence);string run=Guid.NewGuid().ToString("N");var bounds=new List<string>();
            foreach(int width in new[]{1920,1280})
            {
                int height=width*9/16;var cameraObject=new GameObject("OwnedUiCapture",typeof(Camera));var camera=cameraObject.GetComponent<Camera>();camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.015f,.02f,.03f);camera.enabled=false;
                var target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);target.Create();camera.targetTexture=target;camera.aspect=16f/9;
                var canvas=panel.Canvas;var scaler=canvas.GetComponent<CanvasScaler>();var nodes=canvas.GetComponentsInChildren<Transform>(true);var layers=nodes.Select(t=>t.gameObject.layer).ToArray();var previous=RenderTexture.active;Texture2D texture=null;
                try
                {
                    foreach(var node in nodes)node.gameObject.layer=31;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;scaler.enabled=false;canvas.scaleFactor=width/1920f;yield return Frame();Canvas.ForceUpdateCanvases();
                    RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});RenderTexture.active=target;texture=new Texture2D(width,height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();File.WriteAllBytes(evidence+"/Backpack_"+width+"_"+run+".png",texture.EncodeToPNG());
                    Assert.That(texture.GetPixels32().Select(p=>p.r+256*p.g+65536*p.b).Distinct().Count(),Is.GreaterThan(100),"Actual product UI must render");Assert.That(panel.Grid.childCount,Is.EqualTo(24));
                    foreach(var text in canvas.GetComponentsInChildren<Text>())
                    {
                        bounds.Add(width+"\t"+text.name+"\t"+text.preferredHeight+"/"+text.rectTransform.rect.height+"\t"+text.text.Replace("\n"," / "));
                        Assert.That(text.preferredHeight,Is.LessThanOrEqualTo(text.rectTransform.rect.height+2),text.name+" vertical clipping");
                        if(text.horizontalOverflow==HorizontalWrapMode.Overflow)Assert.That(text.preferredWidth,Is.LessThanOrEqualTo(text.rectTransform.rect.width+2),text.name+" horizontal clipping");
                    }
                }
                finally
                {
                    canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;scaler.enabled=true;for(int i=0;i<nodes.Length;i++)nodes[i].gameObject.layer=layers[i];RenderTexture.active=previous;camera.targetTexture=null;target.Release();Object.Destroy(target);if(texture)Object.Destroy(texture);Object.Destroy(cameraObject);
                    File.WriteAllLines(evidence+"/Bounds_"+run+".txt",bounds);
                }
            }
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(!Application.isPlaying)yield break;if(panel)panel.Close();if(pause)pause.enabled=false;if(agent)agent.enabled=false;if(nav.valid)nav.Remove();if(data)Object.Destroy(data);
            if(old.IsValid())SceneManager.SetActiveScene(old);if(scene.IsValid()&&scene.isLoaded){var op=SceneManager.UnloadSceneAsync(scene);while(op!=null&&!op.isDone)yield return null;}
            if(devices!=null){devices.Dispose();Assert.That(devices.IsRestored,Is.True,"Actual input settings restored");devices=null;}Time.timeScale=scale;Application.runInBackground=background;yield return new ExitPlayMode();
        }
    }
}
