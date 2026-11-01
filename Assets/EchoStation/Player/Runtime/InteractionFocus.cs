using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace EchoStation.Player
{
    [DefaultExecutionOrder(50),RequireComponent(typeof(PlayerInteraction)),DisallowMultipleComponent]
    public sealed class InteractionFocus:MonoBehaviour
    {
        [SerializeField]private Volume focusVolume;
        private VolumeProfile instance;private DepthOfField depth;private PlayerInteraction interaction;private PlayerInputReader input;
        private readonly InteractionFocusState state=new InteractionFocusState();
        public InteractionFocusState State=>state;
        public Volume FocusVolume=>focusVolume;
        public DepthOfField Depth=>depth;
        private void Awake()
        {
            interaction=GetComponent<PlayerInteraction>();input=GetComponent<PlayerInputReader>();
            if(!focusVolume||!focusVolume.sharedProfile)throw new InvalidOperationException("Dedicated interaction focus Volume missing.");
            instance=focusVolume.profile;if(!instance.TryGet(out depth))throw new InvalidOperationException("Focus profile requires DepthOfField.");
            GetComponent<FirstPersonMotor>().View.GetComponent<Camera>().GetUniversalAdditionalCameraData().requiresDepthTexture=true;
            focusVolume.weight=0;
        }
        private void LateUpdate()
        {
            var selection=interaction.Selection;state.Step(input.CanPlay&&selection.IsValid?(float?)selection.Distance:null,Time.unscaledDeltaTime);
            depth.focusDistance.value=state.Distance;depth.aperture.value=Mathf.Lerp(32,5.6f,state.Weight);focusVolume.weight=state.Active?1:0;
        }
        private void OnDisable(){state.Reset();if(focusVolume)focusVolume.weight=0;}
        private void OnDestroy(){if(instance)Destroy(instance);}
    }
}
