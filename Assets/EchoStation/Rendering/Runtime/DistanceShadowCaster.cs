using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace EchoStation.Rendering
{
    /// <summary>Per-group shadow budget. Visibility and receiving shadows remain untouched.</summary>
    public sealed class DistanceShadowCaster : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Renderer[] targets = Array.Empty<Renderer>();
        [SerializeField, Min(.01f)] private float disableDistance = 30;
        [SerializeField, Min(0)] private float hysteresis = 2;
        [SerializeField, Min(.01f)] private float interval = .1f;
        private ShadowCastingMode[] original = Array.Empty<ShadowCastingMode>();
        private bool[] culled = Array.Empty<bool>();
        private float nextUpdate;

        public float DisableDistance => disableDistance;
        public float Hysteresis => hysteresis;
        public float Interval => interval;
        public Camera TargetCamera => targetCamera;

        public void Configure(Camera camera, Renderer[] renderers, float distance = 30, float margin = 2, float seconds = .1f)
        {
            if (float.IsNaN(distance) || float.IsInfinity(distance) || distance <= 0 ||
                float.IsNaN(margin) || margin < 0 || margin >= distance ||
                float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(distance), "Require finite distance>margin>=0 and interval>0.");
            Restore();
            targetCamera = camera;
            targets = renderers == null ? Array.Empty<Renderer>() : (Renderer[])renderers.Clone();
            disableDistance = distance; hysteresis = margin; interval = seconds;
            Capture(); nextUpdate = 0;
        }

        private void OnEnable() { Capture(); nextUpdate = 0; }
        private void OnDisable() => Restore();
        private void OnDestroy() => Restore();

        private void Capture()
        {
            original = new ShadowCastingMode[targets.Length]; culled = new bool[targets.Length];
            for (var i = 0; i < targets.Length; i++)
                if (targets[i]) original[i] = targets[i].shadowCastingMode;
        }

        private void Restore()
        {
            for (var i = 0; i < targets.Length && i < original.Length; i++)
                if (targets[i]) targets[i].shadowCastingMode = original[i];
        }

        private void Update()
        {
            if (!targetCamera || Time.unscaledTime < nextUpdate) return;
            nextUpdate = Time.unscaledTime + interval;
            Evaluate(targetCamera.transform.position);
        }

        public void Evaluate(Vector3 cameraPosition)
        {
            if (original.Length != targets.Length) return;
            var offSquared = disableDistance * disableDistance;
            var onSquared = (disableDistance - hysteresis) * (disableDistance - hysteresis);
            for (var i = 0; i < targets.Length; i++)
            {
                var renderer = targets[i];
                if (!renderer || original[i] == ShadowCastingMode.Off) continue;
                var squaredDistance = renderer.bounds.SqrDistance(cameraPosition);
                if (!culled[i] && squaredDistance >= offSquared)
                {
                    culled[i] = true; renderer.shadowCastingMode = ShadowCastingMode.Off;
                }
                else if (culled[i] && squaredDistance <= onSquared)
                {
                    culled[i] = false; renderer.shadowCastingMode = original[i];
                }
            }
        }
    }
}
