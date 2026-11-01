using System;
using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.Player;
using EchoStation.Player.Editor;
using EchoStation.World.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace EchoStation.Tests.Player
{
    public class PlayerCabinCollisionTests
    {
        private Scene scene,previous;private bool background;private FirstPersonMotor motor;private CharacterController controller;private CapsuleCollider proxy;private Collider[] solids;private float maximum;
        [Serializable]private class Result{public bool passed;public Vector3 mainArrival,storageArrival;public float maximumPenetration,wallStopX,deskStopZ;}
        private void Check(){foreach(var solid in solids)if(Physics.ComputePenetration(proxy,motor.transform.position,motor.transform.rotation,solid,solid.transform.position,solid.transform.rotation,out _,out var depth))maximum=Mathf.Max(maximum,depth);}
        private void Walk(Vector3 target)
        {
            for(var i=0;i<500;i++){var delta=target-motor.transform.position;delta.y=0;if(delta.magnitude<.06f)return;motor.SetLookAngles(Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg,0);motor.ApplyInput(Vector2.up,Vector2.zero,1f/60);Check();}Assert.Fail("Player could not reach approved cabin path point "+target+" from "+motor.transform.position);
        }
        [UnityTest]public IEnumerator P02_B04_ActualPlayerCrossesBothDoorsAndCannotWalkThroughOuterWallOrDesk()
        {
            yield return new EnterPlayMode();background=Application.runInBackground;Application.runInBackground=true;previous=SceneManager.GetActiveScene();scene=EditorSceneManager.LoadSceneInPlayMode(CabinGreyboxSetup.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive,LocalPhysicsMode.Physics3D));var timer=System.Diagnostics.Stopwatch.StartNew();while(!scene.isLoaded){if(timer.Elapsed.TotalSeconds>5)Assert.Fail("Cabin load timeout.");yield return null;}SceneManager.SetActiveScene(scene);var actor=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabSetup.PrefabPath));actor.GetComponentInChildren<Camera>().enabled=false;actor.GetComponent<PlayerInputReader>().enabled=false;motor=actor.GetComponent<FirstPersonMotor>();motor.enabled=false;controller=actor.GetComponent<CharacterController>();controller.enabled=false;actor.transform.position=new Vector3(0,.05f,-6);controller.enabled=true;proxy=new GameObject("ReferenceCapsule").AddComponent<CapsuleCollider>();proxy.isTrigger=true;proxy.radius=.3f;proxy.height=1.75f;proxy.center=new Vector3(0,.875f,0);solids=scene.GetRootGameObjects().Where(o=>o.name=="ES_Cabin_Greybox").SelectMany(o=>o.GetComponentsInChildren<BoxCollider>()).Where(c=>!c.isTrigger).Cast<Collider>().ToArray();Assert.That(solids.Length,Is.EqualTo(30));scene.GetPhysicsScene().Simulate(.02f);
            Walk(new Vector3(0,0,0));var result=new Result{mainArrival=actor.transform.position};Walk(new Vector3(0,0,1));Walk(new Vector3(7.5f,0,1));result.storageArrival=actor.transform.position;motor.SetLookAngles(0,0);for(var i=0;i<120;i++){motor.ApplyInput(Vector2.right,Vector2.zero,1f/60);Check();}result.wallStopX=actor.transform.position.x;
            controller.enabled=false;actor.transform.position=new Vector3(-2.8f,.05f,1);controller.enabled=true;motor.ResetMotion();motor.SetLookAngles(0,0);scene.GetPhysicsScene().Simulate(.02f);for(var i=0;i<120;i++){motor.ApplyInput(Vector2.up,Vector2.zero,1f/60);Check();}result.deskStopZ=actor.transform.position.z;result.maximumPenetration=maximum;result.passed=result.storageArrival.x>7.4f&&result.wallStopX<8.7f&&result.wallStopX>8.4f&&result.deskStopZ<1.66f&&result.deskStopZ>1.5f&&maximum<.035f;Directory.CreateDirectory("Docs/Tests/Stage1/Evidence/S1-P02");File.WriteAllText("Docs/Tests/Stage1/Evidence/S1-P02/ActualCabinPlayer.json",JsonUtility.ToJson(result,true));Assert.That(result.passed,Is.True,JsonUtility.ToJson(result,true));
        }
        [UnityTearDown]public IEnumerator After()
        {
            if(Application.isPlaying){if(previous.IsValid())SceneManager.SetActiveScene(previous);var operation=scene.IsValid()?SceneManager.UnloadSceneAsync(scene):null;var timer=System.Diagnostics.Stopwatch.StartNew();while(operation!=null&&!operation.isDone){if(timer.Elapsed.TotalSeconds>5)throw new TimeoutException("Cabin player fixture unload timeout.");yield return null;}Application.runInBackground=background;yield return new ExitPlayMode();}
        }
    }
}
