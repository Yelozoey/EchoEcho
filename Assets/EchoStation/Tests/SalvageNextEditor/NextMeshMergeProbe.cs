using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EchoStation.Echo;
using EchoStation.Salvage;
using EchoStation.Salvage.Editor;
using EchoStation.SalvageNext.Editor;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace EchoStation.SalvageNext.Tests
{
    public sealed class NextMeshMergeProbe
    {
        Scene old,scene;bool background;readonly List<Mesh> owned=new List<Mesh>();VolumeProfile profile;Volume volume;
        sealed class Corner{public string Key;public Vector3 Position;public Vector2 Uv;}
        static Corner[] Corners(RoomVisual room)
        {
            var result=new List<Corner>();foreach(var r in room.GetComponentsInChildren<MeshRenderer>()){var mesh=r.GetComponent<MeshFilter>().sharedMesh;var vertices=mesh.vertices;var uv=mesh.uv2;var m=room.transform.worldToLocalMatrix*r.localToWorldMatrix;var so=r.lightmapScaleOffset;for(int sub=0;sub<mesh.subMeshCount;sub++)foreach(int i in mesh.GetTriangles(sub)){var p=m.MultiplyPoint3x4(vertices[i]);var t=new Vector2(uv[i].x*so.x+so.z,uv[i].y*so.y+so.w);result.Add(new Corner{Key=r.sharedMaterials[sub].name+"|"+r.lightmapIndex,Position=p,Uv=t});}}return result.ToArray();
        }
        static void EqualCorners(Corner[] before,Corner[] after)
        {
            Assert.That(after.Length,Is.EqualTo(before.Length));var buckets=before.GroupBy(c=>c.Key).ToDictionary(g=>g.Key,g=>g.ToList());foreach(var corner in after){Assert.That(buckets.ContainsKey(corner.Key),Is.True);var list=buckets[corner.Key];int found=list.FindIndex(c=>(c.Position-corner.Position).sqrMagnitude<2.5e-9f&&(c.Uv-corner.Uv).sqrMagnitude<2.5e-11f);Assert.That(found,Is.GreaterThanOrEqualTo(0),corner.Key+" "+corner.Position+" "+corner.Uv);list.RemoveAt(found);}Assert.That(buckets.Values.All(v=>v.Count==0),Is.True);
        }
        [Test]public void EverySubmeshTriangleCornerAndAtlasUvSurvivesAndSourceComponentsAreRemoved()
        {
            Directory.CreateDirectory(NextMeshMerge.Evidence);old=SceneManager.GetActiveScene();scene=EditorSceneManager.OpenScene(NextLayout.ScenePath,OpenSceneMode.Additive);try{SceneManager.SetActiveScene(scene);var rows=new JArray();foreach(var room in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RoomVisual>())){var before=Corners(room);int count=room.GetComponentsInChildren<MeshRenderer>().Length;var outputs=NextMeshMerge.Merge(room,false);owned.AddRange(outputs.Select(r=>r.GetComponent<MeshFilter>().sharedMesh));EqualCorners(before,Corners(room));Assert.That(outputs.Length,Is.LessThan(count));Assert.That(room.GetComponentsInChildren<MeshFilter>().Length,Is.EqualTo(outputs.Length));Assert.That(outputs.All(r=>r.lightmapScaleOffset==new Vector4(1,1,0,0)),Is.True);rows.Add(new JObject{{"room",room.Index},{"beforeRenderers",count},{"afterRenderers",outputs.Length},{"triangleCorners",before.Length},{"geometryUvEqual",true}});}File.WriteAllText(NextMeshMerge.Evidence+"/Geometry_A.json",rows.ToString());}finally{SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(scene,true);scene=default;foreach(var mesh in owned)if(mesh)Object.DestroyImmediate(mesh);owned.Clear();}
        }
        static IEnumerator Frame(){int frame=Time.frameCount;while(Time.frameCount==frame)yield return null;}
        [UnityTest,Timeout(180000)]public IEnumerator ActualBakedImagesMatchAfterRuntimeMergeAcrossAllRooms()
        {
            yield return new EnterPlayMode();background=Application.runInBackground;Application.runInBackground=true;old=SceneManager.GetActiveScene();scene=EditorSceneManager.LoadSceneInPlayMode(NextLayout.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));while(!scene.isLoaded)yield return null;SceneManager.SetActiveScene(scene);yield return null;var root=scene.GetRootGameObjects().Single(r=>r.name=="ES_SalvageNext_World");root.GetComponentInChildren<SalvageMotor>().enabled=false;var echo=root.GetComponentInChildren<SalvageEcho>();echo.GetComponent<SalvageEchoCoordinator>().enabled=false;echo.enabled=false;root.GetComponentInChildren<NextFocus>().enabled=false;root.GetComponent<NextFogField>().enabled=false;foreach(var light in root.GetComponentsInChildren<Light>())light.enabled=false;volume=root.GetComponentsInChildren<Volume>().Single(v=>v.name=="ES_Next_GlobalVolume");profile=volume.profile;profile.TryGet<FilmGrain>(out var grain);grain.intensity.value=0;profile.TryGet<ChromaticAberration>(out var chromatic);chromatic.intensity.value=0;var camera=root.GetComponentInChildren<Camera>();camera.scene=scene;var rows=new JArray();var positions=new[]{new Vector3(5,1.6f,8),new Vector3(15,1.6f,8),new Vector3(28,1.6f,8)};var aims=new[]{new Vector3(6,.8f,20),new Vector3(18,.8f,20),new Vector3(30,.8f,20)};var before=new Color[3][];string id=Guid.NewGuid().ToString("N");
            for(int i=0;i<3;i++){camera.transform.position=positions[i];camera.transform.LookAt(aims[i]);yield return Frame();using(var capture=new SalvageFrameCapture(camera))before[i]=capture.Capture(NextMeshMerge.Evidence+"/Before_"+i+"_"+id+".png");}
            foreach(var room in root.GetComponentsInChildren<RoomVisual>())owned.AddRange(NextMeshMerge.Merge(room,false).Select(r=>r.GetComponent<MeshFilter>().sharedMesh));
            for(int i=0;i<3;i++){camera.transform.position=positions[i];camera.transform.LookAt(aims[i]);yield return Frame();Color[] after;using(var capture=new SalvageFrameCapture(camera))after=capture.Capture(NextMeshMerge.Evidence+"/After_"+i+"_"+id+".png");float diff=SalvageFrameCapture.Difference(before[i],after);rows.Add(new JObject{{"room",i},{"difference",diff},{"limit",.002}});File.WriteAllText(NextMeshMerge.Evidence+"/ImageAB_"+id+".json",rows.ToString());Assert.That(diff,Is.LessThan(.002f));}
        }
        [UnityTearDown]public IEnumerator Cleanup(){if(!Application.isPlaying)yield break;if(volume)volume.profile=null;if(profile){foreach(var c in profile.components)if(c)Object.Destroy(c);Object.Destroy(profile);}if(old.IsValid())SceneManager.SetActiveScene(old);if(scene.IsValid()){var op=SceneManager.UnloadSceneAsync(scene);while(op!=null&&!op.isDone)yield return null;}foreach(var mesh in owned)if(mesh)Object.Destroy(mesh);owned.Clear();Application.runInBackground=background;yield return new ExitPlayMode();}
    }
}
