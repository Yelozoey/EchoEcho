using System;
using EchoStation.Player;
using EchoStation.Salvage;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace EchoStation.SalvageNext
{
    [DefaultExecutionOrder(50),RequireComponent(typeof(SalvageCarry),typeof(NextSession),typeof(PlayerInputReader))]
    public sealed class NextFocus:MonoBehaviour
    {
        [SerializeField]Volume focusVolume;
        VolumeProfile instance;DepthOfField depth;SalvageCarry carry;SalvageMotor motor;PlayerInputReader input;NextSession session;
        readonly InteractionFocusState state=new InteractionFocusState();
        public Volume FocusVolume=>focusVolume;
        public DepthOfField Depth=>depth;
        public InteractionFocusState State=>state;
        public void Configure(Volume value){if(!value||!value.sharedProfile)throw new ArgumentException("Dedicated focus profile required.");focusVolume=value;}
        void Awake()
        {
            carry=GetComponent<SalvageCarry>();motor=GetComponent<SalvageMotor>();input=GetComponent<PlayerInputReader>();session=GetComponent<NextSession>();
            if(!focusVolume||!focusVolume.sharedProfile)throw new InvalidOperationException("New focus Volume missing.");instance=focusVolume.profile;if(!instance.TryGet(out depth))throw new InvalidOperationException("New focus depth settings missing.");motor.View.GetComponent<Camera>().GetUniversalAdditionalCameraData().requiresDepthTexture=true;focusVolume.weight=0;
        }
        void LateUpdate()=>Evaluate(Time.unscaledDeltaTime);
        public void Evaluate(float seconds)
        {
            if(!isActiveAndEnabled){Reset();return;}CargoActor target=null;if(input.CanPlay&&carry.HeldId==null&&session.Quality!=RecoveryQuality.Low)target=carry.Focus();float? distance=target&&carry.Registry.Get(target.Id).Definition.Kilograms<=8?(float?)Vector3.Distance(motor.View.position,target.Body.ClosestPoint(motor.View.position)):null;
            state.Step(distance,seconds);depth.gaussianStart.value=state.Distance+.75f;depth.gaussianEnd.value=state.Distance+4.5f;focusVolume.weight=state.Weight;
        }
        void Reset(){state.Reset();if(focusVolume)focusVolume.weight=0;}
        void OnDisable()=>Reset();
        void OnDestroy(){if(focusVolume)focusVolume.profile=null;if(instance){foreach(var component in instance.components)if(component)Destroy(component);Destroy(instance);instance=null;}}
    }
}
