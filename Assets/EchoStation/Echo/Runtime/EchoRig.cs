using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EchoStation.Echo
{
    [DisallowMultipleComponent]
    public sealed class EchoRig : MonoBehaviour
    {
        [SerializeField]private Transform body,probePitch,scanOrigin;
        [SerializeField]private Transform[] wheels;
        [SerializeField]private MeshRenderer status;
        [SerializeField]private Material calm,thinking,alert;
        public Transform Body=>body;public Transform ProbePitch=>probePitch;public Transform ScanOrigin=>scanOrigin;
        public IReadOnlyList<Transform> Wheels=>wheels;public MeshRenderer Status=>status;
        public Material Calm=>calm;public Material Thinking=>thinking;public Material Alert=>alert;
        public void Configure(Transform chassis,Transform probe,Transform scan,Transform[] axles,MeshRenderer indicator,Material blue,Material yellow,Material red)
        {
            if(!chassis||!probe||!scan||!scan.IsChildOf(probe)||axles==null||axles.Length!=4||axles.Any(t=>!t||!t.IsChildOf(transform))||axles.Distinct().Count()!=4||
                !indicator||!blue||!yellow||!red)throw new ArgumentException("Echo requires independent body, four wheel pivots, probe, scan origin and status materials.");
            body=chassis;probePitch=probe;scanOrigin=scan;wheels=axles;status=indicator;calm=blue;thinking=yellow;alert=red;status.sharedMaterial=calm;
        }
    }
}
