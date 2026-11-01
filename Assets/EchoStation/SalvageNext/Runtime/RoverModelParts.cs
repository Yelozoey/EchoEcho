using System;
using System.Linq;
using UnityEngine;

namespace EchoStation.SalvageNext
{
    // Traditional presentation data only; no model or intent access.
    public sealed class RoverModelParts:MonoBehaviour
    {
        [SerializeField] Transform body,head,scanOrigin;
        [SerializeField] Transform[] wheels;
        [SerializeField] float[] radii;
        [SerializeField] MeshRenderer indicator;
        [SerializeField] Material calm,thinking,alert;
        public Transform Body=>body;
        public Transform Head=>head;
        public Transform ScanOrigin=>scanOrigin;
        public MeshRenderer Indicator=>indicator;
        public Material Calm=>calm;
        public Material Thinking=>thinking;
        public Material Alert=>alert;
        public int WheelCount=>wheels?.Length??0;
        public Transform Wheel(int index)=>wheels[index];
        public float Radius(int index)=>radii[index];
        public void Configure(Transform chassis,Transform probe,Transform origin,Transform[] axles,float[] radius,MeshRenderer lamp,Material blue,Material yellow,Material red)
        {
            if(!chassis||!probe||!origin||!origin.IsChildOf(probe)||axles==null||axles.Length!=6||axles.Any(t=>!t||!t.IsChildOf(transform))||axles.Distinct().Count()!=6||radius==null||radius.Length!=6||radius.Any(r=>!float.IsFinite(r)||r<=0||r>.2f)||!lamp||!blue||!yellow||!red)throw new ArgumentException("Rover requires body, head, six measured wheel pivots and status palette.");
            body=chassis;head=probe;scanOrigin=origin;wheels=axles.ToArray();radii=radius.ToArray();indicator=lamp;calm=blue;thinking=yellow;alert=red;indicator.sharedMaterial=calm;
        }
    }
}
