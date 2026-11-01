using System;
using System.Collections;
using System.IO;
using System.Linq;
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
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EchoStation.SalvageNext.Tests
{
    public sealed class NextLayoutTests
    {
        Scene old,scene;bool background;
        [Test] public void NewSceneHasThreeArtZonesIndependentNavAndUnchangedCargoDefinitions()
        {
            old=SceneManager.GetActiveScene();scene=EditorSceneManager.OpenScene(NextLayout.ScenePath,OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);var roots=scene.GetRootGameObjects();var rooms=roots.SelectMany(r=>r.GetComponentsInChildren<RoomVisual>()).OrderBy(r=>r.Index).ToArray();Assert.That(rooms.Length,Is.EqualTo(3));
                for(int i=0;i<3;i++){Assert.That(rooms[i].Region.center,Is.EqualTo(new Vector3(6+12*i,1.5f,12)));var merged=rooms[i].GetComponentsInChildren<MergedRoomMesh>();if(merged.Length>0){var row=JArray.Parse(File.ReadAllText(NextMeshMerge.Evidence+"/Geometry_A.json")).Single(r=>r["room"].Value<int>()==i);Assert.That(merged.Length,Is.EqualTo(row["afterRenderers"].Value<int>()));Assert.That(rooms[i].GetComponentsInChildren<MeshFilter>().Sum(f=>(long)f.sharedMesh.GetIndexCount(0)),Is.EqualTo(row["triangleCorners"].Value<long>()));}else Assert.That(rooms[i].GetComponentsInChildren<MeshRenderer>().Length,Is.GreaterThan(40));foreach(var renderer in rooms[i].GetComponentsInChildren<MeshRenderer>())Assert.That(renderer.bounds.size.magnitude,Is.LessThan(30));}
                foreach(var room in rooms)foreach(Transform wall in room.transform)if(wall.name.StartsWith("ES_WallBand_",StringComparison.Ordinal))foreach(var renderer in wall.GetComponentsInChildren<MeshRenderer>())foreach(var mat in renderer.sharedMaterials){Assert.That(mat.GetFloat("_Cull"),Is.Zero);Assert.That(mat.doubleSidedGI,Is.True);}
                var surface=roots.SelectMany(r=>r.GetComponentsInChildren<Unity.AI.Navigation.NavMeshSurface>()).Single();Assert.That(AssetDatabase.GetAssetPath(surface.navMeshData),Is.EqualTo(NextLayout.NavPath));
                var actors=roots.SelectMany(r=>r.GetComponentsInChildren<CargoActor>()).ToArray();Assert.That(actors.Length,Is.EqualTo(7));for(int i=0;i<7;i++){var actor=actors.Single(a=>a.Id==SalvageCatalog.Items[i].Id);Assert.That(actor.transform.position,Is.EqualTo(SalvageCargoSetup.Positions[i]));Assert.That(actor.GetComponentInChildren<CargoVisualModel>(),Is.Not.Null);}
                Assert.That(roots.SelectMany(r=>r.GetComponentsInChildren<SalvagePersistence>()).Single().enabled,Is.False);Assert.That(roots.SelectMany(r=>r.GetComponentsInChildren<RoverModelParts>()).Single().WheelCount,Is.EqualTo(6));
                var camera=roots.SelectMany(r=>r.GetComponentsInChildren<Camera>()).Single();camera.transform.position=new Vector3(3,1.65f,9);camera.transform.LookAt(new Vector3(10,1.1f,18));Directory.CreateDirectory(NextLayout.Evidence);string path=NextLayout.Evidence+"/Layout_"+Guid.NewGuid().ToString("N")+".png";Color[] pixels;using(var capture=new SalvageFrameCapture(camera))pixels=capture.Capture(path);Assert.That(pixels.Any(p=>p.maxColorComponent>.1f),Is.True);
            }
            finally{SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(scene,true);scene=default;}
        }
        [UnityTest,Timeout(180000)] public IEnumerator RoverTravelsAllDoorwaysAndReturnsByShortcutWithRealCollision()
        {
            yield return new EnterPlayMode();background=Application.runInBackground;Application.runInBackground=true;old=SceneManager.GetActiveScene();scene=EditorSceneManager.LoadSceneInPlayMode(NextLayout.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));while(!scene.isLoaded)yield return null;SceneManager.SetActiveScene(scene);yield return null;
            var roots=scene.GetRootGameObjects();var motor=roots.SelectMany(r=>r.GetComponentsInChildren<SalvageMotor>()).Single();motor.enabled=false;motor.RestorePose(new Vector3(3,.1f,4),0,0);
            var echo=roots.SelectMany(r=>r.GetComponentsInChildren<SalvageEcho>()).Single();echo.GetComponent<SalvageEchoCoordinator>().enabled=false;echo.enabled=false;var nav=echo.Navigation;Assert.That(nav.TryBind(new Vector3(10,0,18)),Is.True);nav.Agent.speed=3;var route=new[]{new Vector3(14,0,18),new Vector3(22,0,12),new Vector3(26,0,12),new Vector3(22,0,12),new Vector3(14,0,6),new Vector3(10,0,6)};var rows=new JArray();
            foreach(var goal in route)
            {
                Assert.That(nav.TryMove(goal),Is.True,nav.Failure.ToString());var timer=System.Diagnostics.Stopwatch.StartNew();while(Vector3.Distance(Vector3.ProjectOnPlane(echo.transform.position-goal,Vector3.up),Vector3.zero)>.3f)
                {Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(15),"Route "+goal+" "+nav.State);yield return null;var body=echo.GetComponent<BoxCollider>();foreach(var hit in Physics.OverlapBox(body.bounds.center,body.bounds.extents*.9f,Quaternion.identity,~0,QueryTriggerInteraction.Ignore))if(!hit.transform.IsChildOf(echo.transform)&&!hit.GetComponentInParent<SalvageDropSurface>()&&!hit.GetComponentInParent<CargoActor>())Assert.Fail("Rover collision penetration "+hit.name);}
                rows.Add(new JObject{{"goal",goal.ToString("F2")},{"seconds",timer.Elapsed.TotalSeconds}});
            }
            foreach(var actor in roots.SelectMany(r=>r.GetComponentsInChildren<CargoActor>())){var path=new NavMeshPath();Assert.That(nav.Agent.CalculatePath(actor.transform.position,path),Is.True,actor.Id);Assert.That(path.status,Is.EqualTo(NavMeshPathStatus.PathComplete),actor.Id);}
            Directory.CreateDirectory(NextLayout.Evidence);File.WriteAllText(NextLayout.Evidence+"/Route_"+Guid.NewGuid().ToString("N")+".json",new JObject{{"steps",rows},{"allCargoReachable",true},{"doorways",3},{"UseLLM",false}}.ToString());
        }
        [UnityTearDown]public IEnumerator Cleanup(){if(!Application.isPlaying)yield break;if(old.IsValid())SceneManager.SetActiveScene(old);if(scene.IsValid()){var op=SceneManager.UnloadSceneAsync(scene);while(op!=null&&!op.isDone)yield return null;}Application.runInBackground=background;yield return new ExitPlayMode();}
    }
}
