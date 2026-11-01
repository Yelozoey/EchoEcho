using System;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
namespace EchoStation.ReflectionSample.Editor
{
    public static class ReflectionDynamicSetup
    {
        public static void Apply()
        {
            string receipt=ReflectionSampleBuilder.Evidence+"/DynamicSetup_A.json";if(File.Exists(receipt))throw new InvalidOperationException("Preserve setup");
            ReflectionSampleBuilder.Edit(root=>
            {
                var floor=root.Find("RS_MirrorSurface");if(floor.GetComponent<ReflectionPlanar>())throw new InvalidOperationException("Already configured");
                var m=new Material(Shader.Find("EchoStation/ReflectionSample/Planar PBR")){name="RS_PlanarFloor"};m.SetColor("_BaseColor",new Color(.65f,.69f,.74f));m.SetFloat("_Metallic",1);m.SetFloat("_Roughness",.06f);m.SetFloat("_PlanarStrength",1);AssetDatabase.CreateAsset(m,ReflectionSampleBuilder.Generated+"/RS_PlanarFloor.mat");ReflectionSampleBuilder.Save(m);floor.GetComponent<Renderer>().sharedMaterial=m;
                var planar=floor.gameObject.AddComponent<ReflectionPlanar>();planar.sourceCamera=root.GetComponentInChildren<Camera>();planar.textureWidth=1024;
                var motion=root.gameObject.AddComponent<ReflectionDemoMotion>();motion.Configure(root.Find("RS_RobotDemo"));root.GetComponent<ReflectionPreview>().ConfigureDynamic(planar);
                File.WriteAllText(receipt,new JObject{{"planeCount",1},{"textureWidth",1024},{"onlySampleCamera",true},{"nonPlanarSpheresUseProbe",true},{"hardwareRayTracing",false},{"oldCodeChanged",false}}.ToString());
            });
        }
    }
}
