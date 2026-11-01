using System;
using UnityEngine;

namespace EchoStation.Player
{
    [RequireComponent(typeof(PlayerInputReader)),DisallowMultipleComponent]
    public sealed class PlayerFlashlight:MonoBehaviour
    {
        [SerializeField]private Light source;
        [SerializeField]private bool on;
        private PlayerInputReader input;
        public bool IsOn=>on;
        public Light Source=>source;
        public void Configure(Light light){if(!light||!light.transform.IsChildOf(transform))throw new ArgumentException("Flashlight must belong to this player.");source=light;SetOn(false);}
        private void Awake(){input=GetComponent<PlayerInputReader>();Apply();}
        private void Update(){if(input.TorchPressed)SetOn(!on);}
        public void SetOn(bool value){on=value;Apply();}
        private void Apply(){if(source)source.enabled=on&&isActiveAndEnabled;}
        private void OnEnable()=>Apply();
        private void OnDisable(){if(source)source.enabled=false;}
    }
}
