using System;
using UnityEngine;

namespace EchoStation.Echo
{
    public enum EchoMood{Calm,Thinking,Alert}
    [RequireComponent(typeof(EchoRig)),DisallowMultipleComponent]
    public sealed class EchoPresentation:MonoBehaviour
    {
        [SerializeField]private Light scanLight;
        private EchoRig rig;private EchoNavigation navigation;private EchoBrain brain;private EchoScanController scans;
        private Quaternion[] wheelBase;private Quaternion probeBase;private float wheelAngle;private EchoMood? materialMood;
        public bool InferenceThinking{get;set;}
        public EchoMood Mood{get;private set;}
        public Light ScanLight=>scanLight;
        public void ConfigureScanLight(Light source)=>scanLight=source;
        private void Awake()=>Cache();
        private void Cache()
        {
            if(wheelBase!=null)return;rig=GetComponent<EchoRig>();navigation=GetComponent<EchoNavigation>();brain=GetComponent<EchoBrain>();scans=GetComponent<EchoScanController>();
            wheelBase=new Quaternion[rig.Wheels.Count];for(var i=0;i<wheelBase.Length;i++)wheelBase[i]=rig.Wheels[i].localRotation;probeBase=rig.ProbePitch.localRotation;
        }
        private void LateUpdate()
        {
            Cache();var busy=scans&&scans.Scanner.IsBusy;var mood=brain&&brain.CurrentMode==IntentKind.Warn?EchoMood.Alert:busy||InferenceThinking?EchoMood.Thinking:EchoMood.Calm;
            var speed=navigation&&navigation.IsBound?Vector3.Dot(navigation.Agent.velocity,transform.forward):0;
            ApplyFrame(speed,mood,Time.deltaTime,Time.unscaledTime,busy);
        }
        public void ApplyFrame(float signedSpeed,EchoMood mood,float deltaSeconds,float visualTime,bool scannerOwnsProbe)
        {
            if(!float.IsFinite(signedSpeed)||!float.IsFinite(deltaSeconds)||!float.IsFinite(visualTime)||deltaSeconds<0||(uint)mood>(uint)EchoMood.Alert)throw new ArgumentException("Invalid presentation frame.");Cache();Mood=mood;
            if(materialMood!=mood){rig.Status.sharedMaterial=mood==EchoMood.Calm?rig.Calm:mood==EchoMood.Thinking?rig.Thinking:rig.Alert;materialMood=mood;}
            rig.Status.enabled=mood!=EchoMood.Thinking||Mathf.Repeat(visualTime,.6f)<.3f;
            var speed=Mathf.Abs(signedSpeed)<.01f?0:Mathf.Clamp(signedSpeed,-4,4);wheelAngle=Mathf.Repeat(wheelAngle+speed/.14f*Mathf.Rad2Deg*deltaSeconds,360);
            for(var i=0;i<wheelBase.Length;i++)rig.Wheels[i].localRotation=wheelBase[i]*Quaternion.AngleAxis(wheelAngle,Vector3.right);
            if(!scannerOwnsProbe)
            {
                var pose=mood==EchoMood.Thinking?probeBase*Quaternion.Euler(Mathf.Sin(visualTime*Mathf.PI*2*.7f)*8,Mathf.Sin(visualTime*Mathf.PI*2*.35f)*35,0):mood==EchoMood.Alert?probeBase*Quaternion.Euler(-8,0,0):probeBase;
                rig.ProbePitch.localRotation=Quaternion.Slerp(rig.ProbePitch.localRotation,pose,1-Mathf.Exp(-8*deltaSeconds));
            }
            if(scanLight)scanLight.enabled=scannerOwnsProbe;
        }
        private void OnDisable(){if(scanLight)scanLight.enabled=false;if(rig&&rig.Status)rig.Status.enabled=true;}
    }
}
