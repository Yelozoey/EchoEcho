#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EchoStation.Player;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace EchoStation.Salvage.Validation
{
    public sealed class SalvageWalkthrough:MonoBehaviour
    {
        SalvageMotor motor;SalvageCarry carry;SalvageDelivery delivery;SalvageMission mission;SalvagePersistence save;SalvageEcho echo;PlayerInputReader input;
        string output;readonly List<string> actions=new List<string>();System.Diagnostics.Stopwatch timer;float distance;int frames;
        [Serializable]public class Result{public bool passed,completed,hdr,displayHdr;public string mode,error,api,unity,pipeline,held;public int count,points,width,height,frames;public float distance,charge;public double wallSeconds;public string[] actions;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Bootstrap()
        {
            if(Application.isEditor)return;var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--salvage-validation");if(index<0)return;
            if(index+1>=args.Length||!new[]{"route","alternate","write","read"}.Contains(args[index+1])){Application.Quit(2);return;}
            int sessionIndex=Array.IndexOf(args,"--salvage-validation-session");string session=sessionIndex>=0&&sessionIndex+1<args.Length?args[sessionIndex+1]:"Default";
            if(session.Length<1||session.Length>40||session.Any(c=>!(c>='a'&&c<='z'||c>='A'&&c<='Z'||c>='0'&&c<='9'||c=='_'||c=='-'))){Application.Quit(2);return;}
            var driver=new GameObject("ExplicitSalvageValidation").AddComponent<SalvageWalkthrough>();var folder=Path.Combine(Path.GetDirectoryName(Application.dataPath),"Validation",session);Directory.CreateDirectory(folder);driver.StartCoroutine(driver.Observe(driver.Run(args[index+1],folder),args[index+1]));
        }
        IEnumerator Observe(IEnumerator routine,string mode)
        {
            var stack=new Stack<IEnumerator>();stack.Push(routine);
            while(stack.Count>0)
            {
                bool has;object next=null;
                try{has=stack.Peek().MoveNext();if(has)next=stack.Peek().Current;}
                catch(Exception e){WriteResult(mode,false,e.ToString());Application.Quit(1);yield break;}
                if(!has){stack.Pop();continue;}if(next is IEnumerator child){stack.Push(child);continue;}yield return next;
            }
            Application.Quit(0);
        }
        public IEnumerator Run(string mode,string folder)
        {
            output=folder;Directory.CreateDirectory(output);Application.runInBackground=true;timer=System.Diagnostics.Stopwatch.StartNew();
            yield return null;var scene=gameObject.scene;
            motor=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvageMotor>()).Single();carry=motor.GetComponent<SalvageCarry>();delivery=motor.GetComponent<SalvageDelivery>();mission=motor.GetComponent<SalvageMission>();save=motor.GetComponent<SalvagePersistence>();echo=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvageEcho>()).Single();input=motor.GetComponent<PlayerInputReader>();
            motor.enabled=false;echo.enabled=false;Check(echo.Initialize(),"Echo initialize");input.SetApplicationFocus(true);input.SetPointerCaptured(true);save.ConfigureTestStore(Path.Combine(output,"CrossProcessSave"));
            if(mode=="read")
            {
                Check(save.Load(),"Cross-process load: "+mission.Notice);Check(carry.HeldId=="cargo_b"&&delivery.Count==1&&delivery.Points==2,"Mixed state load");Check(carry.Registry.Get("cargo_c").Position.z<4.8f,"Dropped position restored");Check(echo.Battery.Charge<100,"Battery restored");actions.Add("cross_process_loaded_mixed_state");
                yield return ReturnDock(true);yield return Deliver(false);yield return RecoverD();
            }
            else if(mode=="alternate")
            {
                yield return RecoverC();yield return ReturnDock(false);yield return Deliver(false);
                yield return GoWorkshop();yield return Walk(new Vector3(33,0,4));Pick("cargo_f");yield return ReturnDock(false);yield return Deliver(false);
                yield return RecoverB();yield return ReturnDock(true);yield return Deliver(false);
            }
            else
            {
                yield return Walk(new Vector3(9,0,17));Pick("cargo_a");yield return ReturnDock(true);yield return Deliver(true);
                if(mode=="write")
                {
                    yield return RecoverC();Check(carry.TryDrop(),"Drop C: "+carry.LastMessage);Physics.SyncTransforms();actions.Add("dropped_c");yield return Walk(new Vector3(18.5f,0,3));yield return ReturnDock(false);yield return RecoverB();echo.Battery.Restore(37,true);Check(save.Save(),"Mixed save: "+mission.Notice);actions.Add("saved_mixed_state");yield return Screenshot("Write_Mixed.png");WriteResult(mode,true,"");yield break;
                }
                yield return RecoverB();yield return ReturnDock(true);yield return Deliver(false);yield return RecoverD();
            }
            Check(delivery.Count==3&&delivery.Points==12&&mission.CanComplete&&!mission.Completed,"Goal must not auto-complete");mission.OpenBoard();Check(mission.RequestCompletion(),"Request end");mission.CloseBoard();Check(!mission.Completed,"Cancel end must preserve exploring");mission.OpenBoard();Check(mission.RequestCompletion()&&mission.ConfirmCompletion(),"Explicit end");actions.Add("explicit_completion");yield return Screenshot(mode+"_Complete.png");
            if(mode!="read"){Check(mission.RequestRestart()&&mission.ConfirmRestart(),"Restart");Check(delivery.Count==0&&carry.HeldId==null&&echo.Battery.Charge==100,"Restart state");actions.Add("restart_verified");}
            WriteResult(mode,true,"");
        }
        IEnumerator RecoverB(){yield return Walk(new Vector3(10,0,18));yield return Walk(new Vector3(14,0,18));yield return Walk(new Vector3(16,0,16));Pick("cargo_b");}
        IEnumerator RecoverC(){yield return Walk(new Vector3(10,0,6));yield return Walk(new Vector3(14,0,6));yield return Walk(new Vector3(20,0,3));Pick("cargo_c");}
        IEnumerator GoWorkshop(){yield return Walk(new Vector3(10,0,6));yield return Walk(new Vector3(14,0,6));yield return Walk(new Vector3(21,0,6));yield return Walk(new Vector3(21,0,12));yield return Walk(new Vector3(26,0,12));}
        IEnumerator RecoverD(){yield return Walk(new Vector3(10,0,18));yield return Walk(new Vector3(14,0,18));yield return Walk(new Vector3(21,0,18));yield return Walk(new Vector3(21,0,12));yield return Walk(new Vector3(26,0,12));yield return Walk(new Vector3(26,0,10));yield return Walk(new Vector3(27,0,10));Pick("cargo_d");Check(carry.TryDrop(),"Drop D");Physics.SyncTransforms();var p=carry.Registry.Get("cargo_d").Position;yield return Walk(p+new Vector3(0,0,-2));Pick("cargo_d");actions.Add("drop_repick_d");yield return ReturnDock(true);yield return Deliver(false);}
        IEnumerator ReturnDock(bool main)
        {
            if(motor.transform.position.x>24){yield return Walk(new Vector3(26,0,10));yield return Walk(new Vector3(26,0,12));yield return Walk(new Vector3(21,0,12));}
            if(motor.transform.position.x>12){float z=main?18:6;yield return Walk(new Vector3(motor.transform.position.x,0,z));yield return Walk(new Vector3(14,0,z));yield return Walk(new Vector3(10,0,z));}
            yield return Walk(new Vector3(5,0,12));
        }
        IEnumerator Deliver(bool cancelFirst)
        {Check(delivery.Request(),"Delivery request");if(cancelFirst){delivery.Cancel();Check(carry.HeldId!=null&&delivery.Count==0,"Cancel delivery");Check(delivery.Request(),"Reopen delivery");actions.Add("cancel_delivery_verified");}Check(delivery.Confirm(),"Confirm delivery");actions.Add("delivered_"+delivery.Count);yield return null;}
        void Pick(string id)
        {
            Face(0,35);Physics.SyncTransforms();input.SetApplicationFocus(true);input.SetPointerCaptured(true);Check(carry.TryPickFromView()&&carry.HeldId==id,"Pick "+id+": "+carry.LastMessage);actions.Add("picked_"+id);
        }
        void Face(float yaw,float pitch)
        {float currentPitch=Mathf.DeltaAngle(0,motor.View.localEulerAngles.x);motor.ApplyInput(Vector2.zero,new Vector2(Mathf.DeltaAngle(motor.transform.eulerAngles.y,yaw)/FirstPersonMotion.Sensitivity,(currentPitch-pitch)/FirstPersonMotion.Sensitivity),0);}
        IEnumerator Walk(Vector3 goal)
        {
            var timeout=System.Diagnostics.Stopwatch.StartNew();
            while(true)
            {
                var delta=goal-motor.transform.position;delta.y=0;if(delta.magnitude<.09f)break;
                if(timeout.Elapsed.TotalSeconds>25||timer.Elapsed.TotalSeconds>240)throw new InvalidOperationException("Blocked path "+goal+" from "+motor.transform.position+" robot="+echo.transform.position+" navigation="+echo.Navigation.State+" goal="+echo.Navigation.Goal);
                yield return null;float dt=Mathf.Min(Time.deltaTime,.05f);var local=Quaternion.Inverse(motor.transform.rotation)*delta.normalized;var before=motor.transform.position;motor.ApplyInput(new Vector2(local.x,local.z),Vector2.zero,dt);distance+=Vector3.Distance(before,motor.transform.position);frames++;echo.Advance(dt,false);
            }
        }
        IEnumerator Screenshot(string name)
        {if(!Application.isEditor){var timer=System.Diagnostics.Stopwatch.StartNew();while(timer.Elapsed.TotalSeconds<.3)yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(output,name));yield return null;yield return null;}}
        void WriteResult(string mode,bool passed,string error)
        {
            var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;var result=new Result{passed=passed,error=error,mode=mode,completed=mission&&mission.Completed,count=delivery?delivery.Count:0,points=delivery?delivery.Points:0,held=carry?carry.HeldId:null,wallSeconds=timer?.Elapsed.TotalSeconds??0,distance=distance,frames=frames,charge=echo?.Battery.Charge??0,api=SystemInfo.graphicsDeviceType.ToString(),unity=Application.unityVersion,width=Screen.width,height=Screen.height,pipeline=pipeline?pipeline.name:"null",hdr=pipeline&&pipeline.supportsHDR,displayHdr=HDROutputSettings.main!=null&&HDROutputSettings.main.active,actions=actions.ToArray()};File.WriteAllText(Path.Combine(output,mode+"_Result.json"),JsonUtility.ToJson(result,true));
        }
        static void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    }
}
#endif
