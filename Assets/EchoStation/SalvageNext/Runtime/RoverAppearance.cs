using System;
using System.Linq;
using EchoStation.Echo;
using UnityEngine;
using Object=UnityEngine.Object;

namespace EchoStation.SalvageNext
{
    [DefaultExecutionOrder(-30),RequireComponent(typeof(EchoRig),typeof(EchoNavigation))]
    public sealed class RoverAppearance:MonoBehaviour
    {
        [SerializeField] RoverModelParts visualPrefab,visual;
        EchoRig rig;EchoNavigation navigation;EchoPresentation presentation;
        Transform oldBody,oldProbe,oldOrigin;Transform[] oldWheels;MeshRenderer oldStatus;Material oldBlue,oldYellow,oldRed;
        Renderer[] hidden;bool[] enabledBefore;Light scanLight;Transform oldLightParent;Vector3 oldLightPosition,oldLightScale;Quaternion oldLightRotation;
        Quaternion[] wheelBases;float[] angles;bool ownsRuntimeVisual;
        public RoverModelParts Visual=>visual;
        public bool Mounted=>visual&&wheelBases!=null;
        public void Configure(RoverModelParts prefab){if(!prefab||prefab.WheelCount!=6)throw new ArgumentException("Verified six-wheel Rover visual required.");visualPrefab=prefab;}
        void Awake(){if(visualPrefab||visual)Mount();}
        void OnEnable(){if(visualPrefab||visual)Mount();}
        public bool Mount()
        {
            rig=GetComponent<EchoRig>();navigation=GetComponent<EchoNavigation>();presentation=GetComponent<EchoPresentation>();
            if(visual){CacheWheels();return true;}
            if(!visualPrefab||!rig||!rig.Body||!rig.ProbePitch||!rig.ScanOrigin||rig.Wheels.Count!=4||!presentation)return false;
            oldBody=rig.Body;oldProbe=rig.ProbePitch;oldOrigin=rig.ScanOrigin;oldWheels=rig.Wheels.ToArray();oldStatus=rig.Status;oldBlue=rig.Calm;oldYellow=rig.Thinking;oldRed=rig.Alert;
            hidden=GetComponentsInChildren<Renderer>(true);enabledBefore=hidden.Select(r=>r.enabled).ToArray();scanLight=presentation.ScanLight;
            if(scanLight){oldLightParent=scanLight.transform.parent;oldLightPosition=scanLight.transform.localPosition;oldLightRotation=scanLight.transform.localRotation;oldLightScale=scanLight.transform.localScale;}
            visual=Instantiate(visualPrefab,transform,false);visual.name="EchoRoverVisual";
            try
            {
                foreach(var renderer in hidden)renderer.enabled=false;
                // EchoPresentation has a four-wheel cache. Keep these hidden driver pivots unchanged.
                rig.Configure(visual.Body,visual.Head,visual.ScanOrigin,oldWheels,visual.Indicator,visual.Calm,visual.Thinking,visual.Alert);
                visual.Indicator.sharedMaterial=presentation.Mood==EchoMood.Alert?visual.Alert:presentation.Mood==EchoMood.Thinking?visual.Thinking:visual.Calm;
                if(scanLight){scanLight.transform.SetParent(visual.ScanOrigin,false);scanLight.transform.localPosition=Vector3.zero;scanLight.transform.localRotation=Quaternion.identity;scanLight.transform.localScale=Vector3.one;}
                ownsRuntimeVisual=Application.isPlaying;CacheWheels();return true;
            }
            catch{RestoreOriginal();DisposeVisual();throw;}
        }
        void CacheWheels()
        {
            if(wheelBases!=null)return;wheelBases=new Quaternion[6];angles=new float[6];
            for(int i=0;i<6;i++)wheelBases[i]=visual.Wheel(i).localRotation;
        }
        void LateUpdate()
        {if(!Mounted)return;float speed=navigation&&navigation.IsBound?Vector3.Dot(navigation.Agent.velocity,transform.forward):0;AdvanceWheels(speed,Time.deltaTime);}
        public void AdvanceWheels(float signedSpeed,float seconds)
        {
            if(!float.IsFinite(signedSpeed)||!float.IsFinite(seconds)||seconds<0)throw new ArgumentException("Finite wheel frame required.");
            if(!Mounted)return;float speed=Mathf.Abs(signedSpeed)<.01f?0:Mathf.Clamp(signedSpeed,-4,4);
            for(int i=0;i<6;i++){angles[i]=Mathf.Repeat(angles[i]+speed/visual.Radius(i)*Mathf.Rad2Deg*seconds,360);visual.Wheel(i).localRotation=wheelBases[i]*Quaternion.AngleAxis(angles[i],Vector3.right);}
        }
        void RestoreOriginal()
        {
            if(rig&&oldBody&&oldProbe&&oldOrigin&&oldStatus&&oldWheels!=null&&oldWheels.All(w=>w))rig.Configure(oldBody,oldProbe,oldOrigin,oldWheels,oldStatus,oldBlue,oldYellow,oldRed);
            if(hidden!=null)for(int i=0;i<hidden.Length;i++)if(hidden[i])hidden[i].enabled=enabledBefore[i];
            if(oldStatus&&presentation)oldStatus.sharedMaterial=presentation.Mood==EchoMood.Alert?oldRed:presentation.Mood==EchoMood.Thinking?oldYellow:oldBlue;
            if(scanLight&&oldLightParent){scanLight.transform.SetParent(oldLightParent,false);scanLight.transform.localPosition=oldLightPosition;scanLight.transform.localRotation=oldLightRotation;scanLight.transform.localScale=oldLightScale;}
        }
        void DisposeVisual()
        {
            if(visual){visual.gameObject.SetActive(false);if(Application.isPlaying)Object.Destroy(visual.gameObject);else Object.DestroyImmediate(visual.gameObject);}
            visual=null;wheelBases=null;angles=null;
        }
        void OnDisable()
        {
            if(!ownsRuntimeVisual)return;RestoreOriginal();DisposeVisual();ownsRuntimeVisual=false;
        }
    }
}
