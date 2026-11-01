using UnityEngine;

namespace EchoStation.Rendering
{
    [RequireComponent(typeof(Light))]
    public sealed class FaultLightFlicker : MonoBehaviour
    {
        [SerializeField, Min(.3f)] private float period = .8f;
        [SerializeField, Range(0,1)] private float dimFraction = .08f;
        private Light source;
        private float originalIntensity, started;
        private void OnEnable()
        {
            source=GetComponent<Light>(); originalIntensity=source.intensity; started=Time.time;
        }
        private void Update() => ApplyAtTime(Time.time-started);
        public void ApplyAtTime(float elapsed)
        {
            if(!source) return;
            var phase=Mathf.Repeat(elapsed,period)/period;
            var on=phase<.12f || (phase>=.2f && phase<.3f) || phase>=.55f;
            source.intensity=originalIntensity*(on ? 1 : dimFraction);
        }
        private void OnDisable() { if(source) source.intensity=originalIntensity; }
    }
}
