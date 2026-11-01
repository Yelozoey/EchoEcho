using System;
using EchoStation.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EchoStation.Echo
{
    [DefaultExecutionOrder(-30),RequireComponent(typeof(EchoRig)),DisallowMultipleComponent]
    public sealed class EchoScanController:MonoBehaviour
    {
        private EchoScanEngine scanner;
        private readonly Collider[] nearby=new Collider[32];
        private Func<IntentKind?> autonomousMode;private float searchTimer;private bool ownsAutomaticScan;private ulong automaticGeneration;
        public EchoScanEngine Scanner=>scanner??(scanner=new EchoScanEngine(GetComponent<EchoRig>(),gameObject.scene.GetPhysicsScene()));
        private void Awake(){_=Scanner;}
        public void BindAutonomousMode(Func<IntentKind?> currentMode){autonomousMode=currentMode;searchTimer=0;}
        private void Update(){Scanner.Tick(Time.deltaTime);TickAutonomous(Time.deltaTime);}
        private void TickAutonomous(float delta)
        {
            var mode=autonomousMode?.Invoke();
            if(ownsAutomaticScan)
            {
                if(Scanner.Generation!=automaticGeneration||!Scanner.IsBusy)ownsAutomaticScan=false;
                else if(mode!=IntentKind.Follow){Scanner.Cancel();ownsAutomaticScan=false;}
            }
            if(mode!=IntentKind.Follow||Scanner.IsBusy||Scanner.CooldownRemaining>0)return;
            var navigation=GetComponent<EchoNavigation>();if(navigation&&navigation.State==NavigationState.Moving)return;
            searchTimer-=delta;if(searchTimer>0)return;searchTimer=.5f;
            var rig=GetComponent<EchoRig>();if(!rig||!rig.ScanOrigin)return;var center=rig.ScanOrigin.position;var physics=gameObject.scene.GetPhysicsScene();
            var count=physics.OverlapSphere(center,2.2f,nearby,~0,QueryTriggerInteraction.Collide);WorldTarget nearest=null;var distance=float.PositiveInfinity;
            for(var i=0;i<count;i++)
            {
                var candidate=nearby[i]?nearby[i].GetComponentInParent<WorldTarget>():null;
                if(!candidate||Scanner.Ledger.HasDiscovered(candidate.Id)||Scanner.Eligibility(candidate)!=ScanFailure.None)continue;
                var squared=(candidate.transform.position-center).sqrMagnitude;if(squared<distance){distance=squared;nearest=candidate;}
            }
            if(nearest&&Scanner.TryBegin(nearest)){ownsAutomaticScan=true;automaticGeneration=Scanner.Generation;}
        }
        private void OnDisable()=>scanner?.Cancel();
    }
}
