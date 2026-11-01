using System;
using System.Collections;
using System.IO;
using EchoStation.Player;
using EchoStation.Player.Editor;
using EchoStation.Tests.Fixtures;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace EchoStation.Tests.Player
{
    public class PlayerPhysicsTests
    {
        private Scene scene,previous;private bool background;private FirstPersonMotor motor;private CharacterController controller;private BoxCollider floor;private CapsuleCollider proxy;
        private const string Evidence="Docs/Tests/Stage1/Evidence/S1-P02";
        [Serializable]private class JumpResult{public bool passed,airJumpIgnored,landed;public float peak,finalY;}
        [Serializable]private class SlopeResult{public bool passed,shallowGrounded;public float shallowY,shallowZ,steepY,steepZ;}
        [Serializable]private class CollisionResult{public bool passed,hitCeiling;public float maxCeilingHeight,maxPenetration,wallStopX;}
        private BoxCollider Box(string name,Vector3 position,Vector3 size,float pitch=0){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetPositionAndRotation(position,Quaternion.Euler(pitch,0,0));go.transform.localScale=size;return go.GetComponent<BoxCollider>();}
        private void Setup()
        {
            background=Application.runInBackground;Application.runInBackground=true;previous=SceneManager.GetActiveScene();scene=SceneManager.CreateScene("PlayerPhysicsFixture",new CreateSceneParameters(LocalPhysicsMode.Physics3D));SceneManager.SetActiveScene(scene);floor=Box("Floor",new Vector3(0,-.1f,0),new Vector3(30,.2f,30));var actor=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabSetup.PrefabPath));actor.GetComponentInChildren<Camera>().enabled=false;actor.GetComponent<PlayerInputReader>().enabled=false;motor=actor.GetComponent<FirstPersonMotor>();motor.enabled=false;controller=actor.GetComponent<CharacterController>();proxy=new GameObject("ReferenceCapsule").AddComponent<CapsuleCollider>();proxy.isTrigger=true;proxy.height=1.75f;proxy.radius=.3f;proxy.center=new Vector3(0,.875f,0);Position(new Vector3(0,.03f,0));Assert.That(Physics.ComputePenetration(proxy,new Vector3(0,-.2f,0),Quaternion.identity,floor,floor.transform.position,floor.transform.rotation,out _,out var positiveDepth)&&positiveDepth>.1f,Is.True,"Reference capsule penetration positive control must detect actual overlap.");Directory.CreateDirectory(Evidence);
        }
        private void Position(Vector3 value){controller.enabled=false;motor.transform.position=value;controller.enabled=true;motor.ResetMotion();motor.SetLookAngles(0,0);scene.GetPhysicsScene().Simulate(.02f);}
        private void Settle(){for(var i=0;i<60;i++)motor.ApplyInput(Vector2.zero,Vector2.zero,1f/60);Assert.That(motor.IsGrounded,Is.True);}
        private float Penetration(params Collider[] solids){float maximum=0;foreach(var solid in solids)if(Physics.ComputePenetration(proxy,motor.transform.position,motor.transform.rotation,solid,solid.transform.position,solid.transform.rotation,out _,out var depth))maximum=Mathf.Max(maximum,depth);return maximum;}
        [UnityTest]public IEnumerator P02_B01_ActualGroundJumpLandsAndRejectsAirJump()
        {
            yield return new EnterPlayMode();Setup();Settle();var baseline=motor.transform.position.y;var result=new JumpResult();motor.ApplyInput(Vector2.zero,Vector2.zero,1f/60,true);
            for(var i=0;i<180;i++){var before=motor.VerticalSpeed;motor.ApplyInput(Vector2.zero,Vector2.zero,1f/60,i==8);if(i==8)result.airJumpIgnored=motor.VerticalSpeed<before;result.peak=Mathf.Max(result.peak,motor.transform.position.y-baseline);}
            result.finalY=motor.transform.position.y-baseline;result.landed=motor.IsGrounded;result.passed=result.peak>.76f&&result.peak<.9f&&Mathf.Abs(result.finalY)<.01f&&result.landed&&result.airJumpIgnored;File.WriteAllText(Evidence+"/ActualJump.json",JsonUtility.ToJson(result,true));Assert.That(result.passed,Is.True,JsonUtility.ToJson(result,true));
        }
        [UnityTest]public IEnumerator P02_B02_ActualSlopeLimitAllowsThirtyButRejectsSixtyDegrees()
        {
            yield return new EnterPlayMode();Setup();var ramp=Box("Slope30",new Vector3(0,2,3.4641016f),new Vector3(4,.2f,8),-30);Position(new Vector3(0,.03f,-.7f));var result=new SlopeResult();for(var i=0;i<120;i++)motor.ApplyInput(Vector2.up,Vector2.zero,1f/60);result.shallowY=motor.transform.position.y;result.shallowZ=motor.transform.position.z;result.shallowGrounded=motor.IsGrounded;
            Object.Destroy(ramp.gameObject);yield return R04ShadowProbe.Frame();ramp=Box("Slope60",new Vector3(0,3.4641016f,2),new Vector3(4,.2f,8),-60);Position(new Vector3(0,.03f,-.7f));for(var i=0;i<180;i++)motor.ApplyInput(Vector2.up,Vector2.zero,1f/60);result.steepY=motor.transform.position.y;result.steepZ=motor.transform.position.z;result.passed=result.shallowGrounded&&result.shallowY>1.5f&&result.shallowZ>2&&result.steepY<.5f&&result.steepZ<.6f;File.WriteAllText(Evidence+"/ActualSlopes.json",JsonUtility.ToJson(result,true));Assert.That(result.passed,Is.True,JsonUtility.ToJson(result,true));
        }
        [UnityTest]public IEnumerator P02_B03_ActualCeilingAndWallBlockWithOnlyApprovedSkinOverlap()
        {
            yield return new EnterPlayMode();Setup();var ceiling=Box("LowCeiling",new Vector3(0,1.95f,0),new Vector3(4,.2f,4));scene.GetPhysicsScene().Simulate(.02f);Settle();var result=new CollisionResult();
            for(var i=0;i<120;i++){motor.ApplyInput(Vector2.zero,Vector2.zero,1f/60,i==0);result.hitCeiling|=(motor.LastCollision&CollisionFlags.Above)!=0;result.maxCeilingHeight=Mathf.Max(result.maxCeilingHeight,motor.transform.position.y);result.maxPenetration=Mathf.Max(result.maxPenetration,Penetration(floor,ceiling));}
            Object.Destroy(ceiling.gameObject);yield return R04ShadowProbe.Frame();var wall=Box("SolidWall",new Vector3(2,1.5f,0),new Vector3(.2f,3,8));Position(new Vector3(0,.03f,0));for(var i=0;i<180;i++){motor.ApplyInput(Vector2.right,Vector2.zero,1f/60);result.maxPenetration=Mathf.Max(result.maxPenetration,Penetration(floor,wall));}result.wallStopX=motor.transform.position.x;result.passed=result.hitCeiling&&result.maxCeilingHeight<.15f&&result.maxPenetration<.035f&&result.wallStopX>1.5f&&result.wallStopX<1.65f;File.WriteAllText(Evidence+"/ActualCeilingWall.json",JsonUtility.ToJson(result,true));Assert.That(result.passed,Is.True,JsonUtility.ToJson(result,true));
        }
        [UnityTearDown]public IEnumerator After()
        {
            if(Application.isPlaying){if(previous.IsValid())SceneManager.SetActiveScene(previous);var operation=scene.IsValid()?SceneManager.UnloadSceneAsync(scene):null;var timer=System.Diagnostics.Stopwatch.StartNew();while(operation!=null&&!operation.isDone){if(timer.Elapsed.TotalSeconds>5)throw new TimeoutException("Physics fixture unload timeout.");yield return null;}Application.runInBackground=background;yield return new ExitPlayMode();}
        }
    }
}
