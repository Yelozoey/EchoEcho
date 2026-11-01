using System;
using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.InfiltrationLevel;
using EchoStation.InfiltrationRefinement;
using EchoStation.InfiltrationRefinement.Editor;
using EchoStation.InfiltrationMetal.Editor;
using EchoStation.Player;
using EchoStation.Salvage;
using EchoStation.Tests.Player;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EchoStation.InfiltrationMetal.Tests
{
    public sealed class MetalLayoutTests
    {
        Scene previous,sample;Transform world;MetalTestScope scope;SyntheticInputScope inputs;
        void LoadEdit(){previous=SceneManager.GetActiveScene();sample=EditorSceneManager.OpenScene(MetalScene.ScenePath,OpenSceneMode.Additive);SceneManager.SetActiveScene(sample);world=MetalScene.World(sample);Physics.SyncTransforms();}
        [Test]
        public void ActualFloorAndConservativeFreeAreaExceedNineSampleRooms()
        {
            LoadEdit();var floors=world.GetComponentsInChildren<LevelFloor>();Assert.That(floors.Length,Is.EqualTo(72));
            var rectangles=floors.Select(f=>f.GetComponent<BoxCollider>().bounds).Select(b=>Rect.MinMaxRect(b.min.x,b.min.z,b.max.x,b.max.z));
            float floor=LevelLayout.UnionArea(rectangles),net=LevelLayout.ConservativeNetArea(world);
            Assert.That(floor,Is.EqualTo(1152).Within(.1));Assert.That(net,Is.GreaterThanOrEqualTo(864));Assert.That(net,Is.LessThan(floor));
            Assert.That(floors.Select(f=>f.Zone).Distinct().Count(),Is.EqualTo(6));
            var tri=NavMesh.CalculateTriangulation();float area=0;for(int i=0;i<tri.indices.Length;i+=3){var a=tri.vertices[tri.indices[i]];var b=tri.vertices[tri.indices[i+1]];var c=tri.vertices[tri.indices[i+2]];if(Mathf.Max(a.y,b.y,c.y)<1)area+=Vector3.Cross(b-a,c-a).magnitude*.5f;}
            File.WriteAllText(MetalScene.Evidence+"/Area_"+Guid.NewGuid().ToString("N").Substring(0,8)+".json",new JObject{{"floorUnion",floor},{"conservativeNet",net},{"oldSampleFloor",96},{"ratio",net/96},{"groundNavMeshAreaSeparate",area},{"method","Axis-aligned collider footprint union, clipped to floor; frame empty spans conservatively excluded"}}.ToString());
        }
        [Test]
        public void PortalCentersAreClearAndOuterWallsStopStandingPlayerHeightRays()
        {
            LoadEdit();foreach(var portal in world.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("CrossPortal_")&&!t.name.Contains("Header")))Assert.That(Physics.Linecast(portal.position+new Vector3(0,1.2f,-.7f),portal.position+new Vector3(0,1.2f,.7f)),Is.False,portal.name);
            foreach(var portal in world.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("SidePortal_")&&!t.name.Contains("Header")))Assert.That(Physics.Linecast(portal.position+new Vector3(-.7f,1.2f,0),portal.position+new Vector3(.7f,1.2f,0)),Is.False,portal.name);
            foreach(var p in new[]{new Vector3(-20,1.2f,-12),new Vector3(20,1.2f,-12),new Vector3(0,1.2f,-18),new Vector3(0,1.2f,18)}){var inside=p;inside.x=Mathf.Clamp(p.x,-16,16);inside.z=Mathf.Clamp(p.z,-14,14);Assert.That(Physics.Linecast(inside,p),Is.True);}
        }
        [TestCase(0),TestCase(1),TestCase(2)]
        public void ThreeDistinctWaypointRoutesHaveCompleteGroundNavigation(int route)
        {LoadEdit();CheckNavigation(LevelLayout.Routes[route]);}
        [TestCase(0),TestCase(1)]
        public void BothPhysicalLoopsHaveCompleteGroundNavigation(int loop)
        {LoadEdit();CheckNavigation(LevelLayout.Loops[loop]);Assert.That(LevelLayout.Loops[loop][0],Is.EqualTo(LevelLayout.Loops[loop].Last()));}
        static void CheckNavigation(Vector3[] points)
        {
            for(int i=1;i<points.Length;i++)
            {
                Assert.That(NavMesh.SamplePosition(points[i-1],out var a,.6f,NavMesh.AllAreas),Is.True,"Start "+points[i-1]);Assert.That(NavMesh.SamplePosition(points[i],out var b,.6f,NavMesh.AllAreas),Is.True,"Target "+points[i]);
                var path=new NavMeshPath();Assert.That(NavMesh.CalculatePath(a.position,b.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete,Is.True,points[i].ToString());Assert.That(path.corners.All(p=>Mathf.Abs(p.y)<.5f),Is.True,"Ground route only");
            }
        }
        [Test]
        public void RealControllerTraversesEveryRouteAndBothLoopsWithoutTeleportingBetweenWaypoints()
        {
            LoadEdit();var motor=world.GetComponentInChildren<SalvageMotor>();motor.enabled=false;var rows=new JArray();
            foreach(var route in LevelLayout.Routes.Concat(LevelLayout.Loops))
            {
                motor.RestorePose(route[0]+Vector3.up*.06f,0,0);Physics.SyncTransforms();for(int f=0;f<20;f++)motor.ApplyInput(Vector2.zero,Vector2.zero,.02f);
                float walked=0;
                for(int i=1;i<route.Length;i++)
                {
                    var path=new NavMeshPath();Assert.That(NavMesh.CalculatePath(motor.transform.position,route[i],NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete,Is.True);
                    foreach(var corner in path.corners.Skip(1))
                    {
                        int steps=0;
                        while(Vector2.Distance(new Vector2(motor.transform.position.x,motor.transform.position.z),new Vector2(corner.x,corner.z))>.14f)
                        {
                            Assert.That(++steps,Is.LessThan(1600),"Controller stuck before "+corner);var before=motor.transform.position;var d=corner-before;
                            motor.RestorePose(before,Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg,0);motor.ApplyInput(Vector2.up,Vector2.zero,.025f);walked+=Vector3.Distance(before,motor.transform.position);
                            Assert.That(motor.transform.position.y,Is.InRange(-.1f,.3f));
                        }
                    }
                }
                Assert.That(Vector3.Distance(motor.transform.position,route.Last()),Is.LessThan(.3f));rows.Add(new JObject{{"waypoints",route.Length},{"walkedMeters",walked},{"passed",true}});
            }
            File.WriteAllText(MetalScene.Evidence+"/ControllerRoutes_"+Guid.NewGuid().ToString("N").Substring(0,8)+".json",rows.ToString());
        }
        static IEnumerator Frames(int count){for(int i=0;i<count;i++){int frame=Time.frameCount;var timer=System.Diagnostics.Stopwatch.StartNew();while(Time.frameCount==frame){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(5));yield return null;}}}
        [UnityTest,Timeout(90000)]
        public IEnumerator BoundPlayerInputMovesLooksAndRespectsEscapeAndClick()
        {
            yield return new EnterPlayMode();scope=new MetalTestScope();previous=SceneManager.GetActiveScene();sample=EditorSceneManager.LoadSceneInPlayMode(MetalScene.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));while(!sample.isLoaded)yield return null;SceneManager.SetActiveScene(sample);world=MetalScene.World(sample);
            var game=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");EditorWindow.GetWindow(game).Focus();inputs=new SyntheticInputScope();var motor=world.GetComponentInChildren<SalvageMotor>();var reader=motor.GetComponent<PlayerInputReader>();reader.SetApplicationFocus(true);reader.SetPointerCaptured(true);yield return Frames(2);
            var start=motor.transform.position;InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState(Key.W));float held=0;var timer=System.Diagnostics.Stopwatch.StartNew();while(held<.5f){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(8));yield return Frames(1);held+=Mathf.Min(Time.deltaTime,.1f);}InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState());yield return Frames(2);Assert.That(motor.transform.position.z-start.z,Is.GreaterThan(1.2f));
            float yaw=motor.transform.eulerAngles.y;InputSystem.QueueStateEvent(inputs.Mouse,new MouseState{delta=new Vector2(24,0)});yield return Frames(2);Assert.That(Mathf.Abs(Mathf.DeltaAngle(yaw,motor.transform.eulerAngles.y)),Is.GreaterThan(.5));
            InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState(Key.Escape));yield return Frames(2);Assert.That(reader.PointerCaptured,Is.False);InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState());InputSystem.QueueStateEvent(inputs.Mouse,new MouseState().WithButton(MouseButton.Left));yield return Frames(2);Assert.That(reader.PointerCaptured,Is.True);
        }
        [TearDown]public void CloseEdit(){if(Application.isPlaying)return;if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);if(sample.IsValid()&&sample.isLoaded)EditorSceneManager.CloseScene(sample,true);}
        [UnityTearDown]public IEnumerator ClosePlay(){if(!Application.isPlaying)yield break;if(inputs!=null){inputs.Dispose();Assert.That(inputs.IsRestored,Is.True);inputs=null;}if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);if(sample.IsValid()&&sample.isLoaded){var unload=SceneManager.UnloadSceneAsync(sample);while(!unload.isDone)yield return null;}scope?.Dispose();scope=null;yield return new ExitPlayMode();}
    }
}
