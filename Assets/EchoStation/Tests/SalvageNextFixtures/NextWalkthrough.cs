#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EchoStation.Player;
using EchoStation.Salvage;
using EchoStation.Salvage.Validation;
using EchoStation.Echo;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace EchoStation.SalvageNext.Validation
{
    public sealed class NextWalkthrough:MonoBehaviour
    {
        SalvageMotor motor;SalvageCarry carry;SalvageDelivery delivery;SalvageMission mission;NextSession save;SalvageEcho echo;PlayerInputReader input;
        SalvageEchoCoordinator brain;SalvageCommandHub hub;NextCommandPanel panel;SalvageR2InputScope devices;bool oldBackground,cancelTested;int scannedBeforeRestart;
        public bool StartDepleted{get;set;}
        string output;readonly List<string> actions=new List<string>();System.Diagnostics.Stopwatch timer;float distance;int frames;
        [Serializable]public class Result{public bool passed,completed,hdr,displayHdr;public string mode,error,api,unity,pipeline,held,scene;public int count,points,width,height,frames,known,history,warningCount,paidScans,scannedBeforeRestart;public bool useLLM,inputRestored;public string baseMode;public float distance,charge;public double wallSeconds;public string[] actions;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Bootstrap()
        {
            if(Application.isEditor)return;var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--salvage-next-validation");if(index<0)return;
            if(index+1>=args.Length||!new[]{"startup","route","alternate","write","read"}.Contains(args[index+1]))return;
            int sessionIndex=Array.IndexOf(args,"--salvage-next-validation-session");string session=sessionIndex>=0&&sessionIndex+1<args.Length?args[sessionIndex+1]:"Default";
            if(session.Length<1||session.Length>40||session.Any(c=>!(c>='a'&&c<='z'||c>='A'&&c<='Z'||c>='0'&&c<='9'||c=='_'||c=='-'))){Application.Quit(2);return;}
            var display=UnityEngine.Object.FindAnyObjectByType<NextDisplayPanel>();var settings=Path.Combine(Path.GetDirectoryName(Application.dataPath),"Validation",session,"Settings");display.ConfigureForTests(new UnityDisplayBackend(display.GetComponent<NextSession>()),settings);
            var driver=new GameObject("ExplicitSalvageNextValidation").AddComponent<NextWalkthrough>();var folder=Path.Combine(Path.GetDirectoryName(Application.dataPath),"Validation",session);Directory.CreateDirectory(folder);driver.StartCoroutine(driver.Observe(driver.Run(args[index+1],folder),args[index+1]));
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
            output=folder;Directory.CreateDirectory(output);oldBackground=Application.runInBackground;Application.runInBackground=true;timer=System.Diagnostics.Stopwatch.StartNew();
            yield return null;var scene=gameObject.scene;
            motor=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvageMotor>()).Single();carry=motor.GetComponent<SalvageCarry>();delivery=motor.GetComponent<SalvageDelivery>();mission=motor.GetComponent<SalvageMission>();save=motor.GetComponent<NextSession>();echo=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvageEcho>()).Single();input=motor.GetComponent<PlayerInputReader>();
            hub=motor.GetComponent<SalvageCommandHub>();panel=motor.GetComponent<NextCommandPanel>();brain=echo.GetComponent<SalvageEchoCoordinator>();var stream=motor.GetComponent<NextRoomStreaming>();var loadTimer=System.Diagnostics.Stopwatch.StartNew();while(stream.Blocking){Check(loadTimer.Elapsed.TotalSeconds<20,"Stream startup");yield return null;}if(mode=="startup"){Check(motor.enabled&&echo.enabled&&brain.enabled&&!GameRuntimeOptions.UseLLM&&hub.Known.Count==0&&delivery.Count==0,"Untouched startup state");yield return Screenshot("Startup.png");actions.Add("startup_no_input_or_pose_override");WriteResult(mode,true,"");yield break;}motor.enabled=false;brain.enabled=false;echo.enabled=false;Check(brain.Initialize(),"Echo initialize");devices=new SalvageR2InputScope();Check(!GameRuntimeOptions.UseLLM,"Script mode required");input.SetApplicationFocus(true);input.SetPointerCaptured(true);save.ConfigureStoreForTests(Path.Combine(output,"CrossProcessSave"));if(StartDepleted){Check(echo.Navigation.TryBind(new Vector3(8,0,8)),"Depleted fixture start");brain.ResetBase(IntentKind.Wait);echo.Battery.Restore(0,true);actions.Add("depleted_fixture_start");}
            if(mode=="read")
            {
                yield return KeyPress(Key.F9);Check(carry.HeldId=="cargo_b"&&delivery.Count==1&&delivery.Points==2,"Mixed state load");Check(Vector3.Distance(carry.Registry.Get("cargo_c").Position,new Vector3(20,0,5))>.5f,"Dropped position restored");Check(hub.Known.Count==3&&brain.BaseMode==IntentKind.Wait&&hub.History.Messages.Count>0,"Collaboration cross-process load");Check(echo.Battery.Charge<100,"Battery restored");actions.Add("cross_process_loaded_mixed_state");byte[] valid=File.ReadAllBytes(save.Store.Primary);Check(!File.Exists(save.Store.Backup),"Fault fixture requires primary-only slot");long epoch=save.Epoch;var pose=motor.transform.position;int history=hub.History.Messages.Count;try{File.WriteAllText(save.Store.Primary,"invalid-owned-validation-slot");Check(!save.Load(),"Bad slot must reject");Check(save.Epoch==epoch&&carry.HeldId=="cargo_b"&&delivery.Count==1&&delivery.Points==2&&hub.Known.Count==3&&hub.History.Messages.Count==history&&motor.transform.position==pose,"Bad slot has no world mutation");Check(File.ReadAllText(save.Store.Primary)=="invalid-owned-validation-slot","Bad slot bytes retained");actions.Add("native_bad_slot_rejected_without_world_change");}finally{File.WriteAllBytes(save.Store.Primary,valid);}
                yield return ReturnDock(true);yield return Deliver(false);yield return RecoverD();
            }
            else if(mode=="alternate")
            {
                yield return RecoverC();yield return ReturnDock(false);yield return Deliver(false);
                yield return GoWorkshop();yield return Walk(new Vector3(33,0,4));yield return PickCargo("cargo_f");yield return ReturnDock(false);yield return Deliver(false);
                yield return RecoverB();yield return ReturnDock(true);yield return Deliver(false);
            }
            else
            {
                yield return Walk(new Vector3(9,0,17));yield return PickCargo("cargo_a");yield return ReturnDock(true);yield return Deliver(true);
                if(mode=="write")
                {
                    yield return RecoverC();Check(carry.TryDrop(),"Drop C: "+carry.LastMessage);Physics.SyncTransforms();actions.Add("dropped_c");yield return Walk(new Vector3(18.5f,0,3));yield return ReturnDock(false);yield return RecoverB();yield return WaitAndAskCharge();echo.Battery.Restore(37,true);yield return KeyPress(Key.F5);Check(File.Exists(save.Store.Primary),"Actual F5 mixed save: "+mission.Notice);actions.Add("saved_mixed_state");yield return Screenshot("Write_Mixed.png");WriteResult(mode,true,"");yield break;
                }
                yield return RecoverB();yield return ReturnDock(true);yield return Deliver(false);yield return RecoverD();
            }
            scannedBeforeRestart=hub.Known.Count;Check(StartDepleted?hub.Known.Count==0:hub.Known.Count>=3,"Actual scan count");Check(delivery.Count==3&&delivery.Points==12&&mission.CanComplete&&!mission.Completed,"Goal must not auto-complete");mission.OpenBoard();Check(mission.RequestCompletion(),"Request end");mission.CloseBoard();Check(!mission.Completed,"Cancel end must preserve exploring");mission.OpenBoard();Check(mission.RequestCompletion()&&mission.ConfirmCompletion(),"Explicit end");actions.Add("explicit_completion");yield return Screenshot(mode+"_Complete.png");
            if(mode!="read"){Check(mission.RequestRestart()&&mission.ConfirmRestart(),"Restart");Check(delivery.Count==0&&carry.HeldId==null&&echo.Battery.Charge==100&&hub.Known.Count==0&&hub.History.Messages.Count==0,"Restart state");actions.Add("restart_verified");}
            WriteResult(mode,true,"");
        }
        IEnumerator RecoverB(){yield return Walk(new Vector3(10,0,18));yield return Walk(new Vector3(14,0,18));yield return Walk(new Vector3(16,0,16));yield return PickCargo("cargo_b");}
        IEnumerator RecoverC(){yield return Walk(new Vector3(10,0,6));yield return Walk(new Vector3(14,0,6));yield return Walk(new Vector3(20,0,3));yield return PickCargo("cargo_c");}
        IEnumerator GoWorkshop(){yield return Walk(new Vector3(10,0,6));yield return Walk(new Vector3(14,0,6));yield return Walk(new Vector3(21,0,6));yield return Walk(new Vector3(21,0,12));yield return Walk(new Vector3(26,0,12));}
        IEnumerator RecoverD(){yield return Walk(new Vector3(10,0,18));yield return Walk(new Vector3(14,0,18));yield return Walk(new Vector3(21,0,18));yield return Walk(new Vector3(21,0,12));yield return Walk(new Vector3(26,0,12));yield return Walk(new Vector3(26,0,10));yield return Walk(new Vector3(27,0,10));yield return PickCargo("cargo_d");Check(carry.TryDrop(),"Drop D");Physics.SyncTransforms();var p=carry.Registry.Get("cargo_d").Position;yield return Walk(p+new Vector3(0,0,-2));yield return PickCargo("cargo_d");actions.Add("drop_repick_d");yield return ReturnDock(true);yield return Deliver(false);}
        IEnumerator ReturnDock(bool main)
        {
            if(motor.transform.position.x>24){yield return Walk(new Vector3(26,0,10));yield return Walk(new Vector3(26,0,12));yield return Walk(new Vector3(21,0,12));}
            if(motor.transform.position.x>12){float z=main?18:6;yield return Walk(new Vector3(motor.transform.position.x,0,z));yield return Walk(new Vector3(14,0,z));yield return Walk(new Vector3(10,0,z));}
            yield return Walk(new Vector3(5,0,12));
        }
        IEnumerator Deliver(bool cancelFirst)
        {Check(delivery.Request(),"Delivery request");if(cancelFirst){delivery.Cancel();Check(carry.HeldId!=null&&delivery.Count==0,"Cancel delivery");Check(delivery.Request(),"Reopen delivery");actions.Add("cancel_delivery_verified");}Check(delivery.Confirm(),"Confirm delivery");actions.Add("delivered_"+delivery.Count);yield return null;}
        IEnumerator PickCargo(string id)
        {
            var p=carry.Registry.Get(id).Position;float side=p.x>24&&p.x<26.6f?2:-2;
            yield return Walk(new Vector3(p.x+side,0,motor.transform.position.z));yield return Walk(new Vector3(p.x+side,0,p.z));var actor=hub.Targets.Single(t=>t.Id==id).GetComponent<CargoActor>();var aim=actor.Body.bounds.center-motor.View.position;Face(Mathf.Atan2(aim.x,aim.z)*Mathf.Rad2Deg,-Mathf.Atan2(aim.y,new Vector2(aim.x,aim.z).magnitude)*Mathf.Rad2Deg);Physics.SyncTransforms();if(hub.PointedId()!=id)yield return Screenshot("AimFailure_"+id+".png");
            Check(hub.PointedId()==id,"Real pointed target "+id+" from "+motor.transform.position+" actual="+hub.PointedId()+" hits="+string.Join(";",Physics.RaycastAll(motor.View.position,motor.View.forward,3).OrderBy(h=>h.distance).Select(h=>h.collider.name+":"+h.distance)));float before=echo.Battery.Charge;int paid=echo.GetComponent<SalvageScanner>().PaidDiscoveries;bool known=hub.Known.Contains(id);
            yield return KeyPress(Key.R);Check(panel.IsOpen,"R opens panel");yield return Click(panel.InvestigateButton);yield return KeyPress(Key.Escape);Check(!panel.IsOpen,"Escape closes panel");
            if(StartDepleted){Check(hub.LastReply.Kind==ScriptReplyKind.Refuse&&!hub.Known.Contains(id)&&echo.Battery.Charge==0,"Empty charge rejects scan without locking recovery");yield return KeyPress(Key.E);Check(carry.HeldId==id,"Unscanned manual pickup "+id);actions.Add("unscanned_pickup_"+id);yield break;}
            if(id=="cargo_b"&&!known&&!cancelTested){yield return KeyPress(Key.R);yield return Click(panel.CancelButton);yield return KeyPress(Key.Escape);Check(!hub.Known.Contains(id)&&echo.GetComponent<SalvageScanner>().PaidDiscoveries==paid,"Cancel must not discover/pay");cancelTested=true;actions.Add("cancel_investigation_verified");yield return KeyPress(Key.R);yield return Click(panel.InvestigateButton);yield return KeyPress(Key.Escape);}
            if(id=="cargo_d"){yield return Walk(new Vector3(26,0,14));actions.Add("stepped_aside_for_echo_d");}var wait=System.Diagnostics.Stopwatch.StartNew();while(!hub.Known.Contains(id)||brain.CurrentMode==IntentKind.Investigate){if(wait.Elapsed.TotalSeconds>24)throw new InvalidOperationException("Investigation stalled "+id+" "+brain.LastFailure+" "+echo.GetComponent<SalvageScanner>().LastError+" robot="+echo.transform.position);yield return Frame();}
            Check(echo.GetComponent<SalvageScanner>().PaidDiscoveries==paid+(known?0:1),"Paid scan count "+id);actions.Add("scanned_"+id);
            yield return Screenshot("Scan_"+id+"_"+actions.Count+".png");if(id=="cargo_d"){yield return Walk(new Vector3(p.x+side,0,14));yield return Walk(new Vector3(p.x+side,0,p.z));var focus=actor.Body.bounds.center-motor.View.position;Face(Mathf.Atan2(focus.x,focus.z)*Mathf.Rad2Deg,-Mathf.Atan2(focus.y,new Vector2(focus.x,focus.z).magnitude)*Mathf.Rad2Deg);Physics.SyncTransforms();}yield return KeyPress(Key.E);Check(carry.HeldId==id,"Actual E picks "+id+": "+carry.LastMessage);actions.Add("picked_"+id);
        }
        IEnumerator WaitAndAskCharge()
        {
            yield return KeyPress(Key.R);yield return Click(panel.WaitButton);yield return KeyPress(Key.Escape);Check(brain.BaseMode==IntentKind.Wait,"Wait base saved");yield return KeyPress(Key.T);Check(panel.IsOpen&&panel.TextMode,"T opens actual text input");yield return Frame();foreach(var c in "电量")InputSystem.QueueTextEvent(devices.Keyboard,c);yield return Frame();yield return Frame();yield return KeyPress(Key.Enter);Check(hub.History.Messages.Last().Text.Contains("电量"),"Chinese InputSystem text and Enter submit");yield return Screenshot("Text_Charge.png");yield return KeyPress(Key.Escape);actions.Add("r_t_chinese_input_verified");
        }
        IEnumerator Frame(){input.SetApplicationFocus(true);int frame=Time.frameCount;var timeout=System.Diagnostics.Stopwatch.StartNew();while(Time.frameCount<=frame){if(timeout.Elapsed.TotalSeconds>5)throw new TimeoutException("Actual input frame stalled.");yield return null;}brain.Advance(Mathf.Min(Time.deltaTime,.05f),input.UiBlocked);}
        IEnumerator KeyPress(Key key){InputSystem.QueueStateEvent(devices.Keyboard,new KeyboardState());yield return Frame();InputSystem.QueueStateEvent(devices.Keyboard,new KeyboardState(key));yield return Frame();InputSystem.QueueStateEvent(devices.Keyboard,new KeyboardState());yield return Frame();}
        IEnumerator Click(Button button){Canvas.ForceUpdateCanvases();var rect=button.GetComponent<RectTransform>();var p=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));InputSystem.QueueStateEvent(devices.Mouse,new MouseState{position=p});yield return Frame();InputSystem.QueueStateEvent(devices.Mouse,new MouseState{position=p}.WithButton(MouseButton.Left));yield return Frame();InputSystem.QueueStateEvent(devices.Mouse,new MouseState{position=p});yield return Frame();}
        public void Cleanup(){devices?.Dispose();Application.runInBackground=oldBackground;}
        void OnDestroy(){Cleanup();}
        void Face(float yaw,float pitch)
        {float currentPitch=Mathf.DeltaAngle(0,motor.View.localEulerAngles.x);motor.ApplyInput(Vector2.zero,new Vector2(Mathf.DeltaAngle(motor.transform.eulerAngles.y,yaw)/FirstPersonMotion.Sensitivity,(currentPitch-pitch)/FirstPersonMotion.Sensitivity),0);}
        IEnumerator Walk(Vector3 goal)
        {
            var timeout=System.Diagnostics.Stopwatch.StartNew();
            while(true)
            {
                var delta=goal-motor.transform.position;delta.y=0;if(delta.magnitude<.09f)break;
                if(timeout.Elapsed.TotalSeconds>25||timer.Elapsed.TotalSeconds>240)throw new InvalidOperationException("Blocked path "+goal+" from "+motor.transform.position+" robot="+echo.transform.position+" navigation="+echo.Navigation.State+" goal="+echo.Navigation.Goal);
                yield return Frame();if(motor.GetComponent<NextRoomStreaming>().Blocking)continue;float dt=Mathf.Min(Time.deltaTime,.05f);var local=Quaternion.Inverse(motor.transform.rotation)*delta.normalized;var before=motor.transform.position;motor.ApplyInput(new Vector2(local.x,local.z),Vector2.zero,dt);distance+=Vector3.Distance(before,motor.transform.position);frames++;
            }
        }
        IEnumerator Screenshot(string name)
        {if(!Application.isEditor){var timer=System.Diagnostics.Stopwatch.StartNew();while(timer.Elapsed.TotalSeconds<.3)yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(output,name));yield return null;yield return null;}}
        void WriteResult(string mode,bool passed,string error)
        {
            Cleanup();var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;var result=new Result{scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,passed=passed,error=error,mode=mode,completed=mission&&mission.Completed,count=delivery?delivery.Count:0,points=delivery?delivery.Points:0,held=carry?carry.HeldId:null,wallSeconds=timer?.Elapsed.TotalSeconds??0,distance=distance,frames=frames,charge=echo?.Battery.Charge??0,api=SystemInfo.graphicsDeviceType.ToString(),unity=Application.unityVersion,width=Screen.width,height=Screen.height,pipeline=pipeline?pipeline.name:"null",hdr=pipeline&&pipeline.supportsHDR,displayHdr=HDROutputSettings.main!=null&&HDROutputSettings.main.active,actions=actions.ToArray(),known=hub?hub.Known.Count:0,history=hub?hub.History.Messages.Count:0,baseMode=brain?brain.BaseMode.ToString():"missing",warningCount=brain?brain.WarningCount:0,paidScans=echo?echo.GetComponent<SalvageScanner>().PaidDiscoveries:0,scannedBeforeRestart=scannedBeforeRestart,useLLM=GameRuntimeOptions.UseLLM,inputRestored=devices==null||devices.IsRestored};File.WriteAllText(Path.Combine(output,mode+"_Result.json"),JsonUtility.ToJson(result,true));
        }
        static void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    }
}
#endif
