using System.Collections;
using EchoStation.Rendering;
using EchoStation.Rendering.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

namespace EchoStation.Tests.Rendering
{
    public class RealtimeLightTemplateTests
    {
        [TestCase("ES_Flashlight",LightType.Spot,2,20)] [TestCase("ES_ScanLight",LightType.Spot,1,8)] [TestCase("ES_FaultLight",LightType.Point,1,5)]
        public void C01_PrefabsAreRealtimeSoftShadowAndUseExpectedTier(string name,LightType type,int tier,int range)
        {
            var go=AssetDatabase.LoadAssetAtPath<GameObject>($"{RealtimeLightTemplates.Folder}/{name}.prefab"); Assert.That(go,Is.Not.Null);
            var light=go.GetComponent<Light>(); Assert.That(light.type,Is.EqualTo(type)); Assert.That(light.lightmapBakeType,Is.EqualTo(LightmapBakeType.Realtime));
            Assert.That(light.shadows,Is.EqualTo(LightShadows.Soft)); Assert.That(light.range,Is.EqualTo(range));
            Assert.That(go.GetComponent<UniversalAdditionalLightData>().additionalLightsShadowResolutionTier,Is.EqualTo(tier));
        }
        [UnityTest] public IEnumerator C02_FaultLightActuallyChangesAndRestoresIntensity()
        {
            yield return new EnterPlayMode();
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>($"{RealtimeLightTemplates.Folder}/ES_FaultLight.prefab");
            var go=Object.Instantiate(prefab);
            try
            {
                var light=go.GetComponent<Light>(); var flicker=go.GetComponent<FaultLightFlicker>();
                flicker.ApplyAtTime(0); Assert.That(light.intensity,Is.EqualTo(2));
                flicker.ApplyAtTime(.12f); Assert.That(light.intensity,Is.EqualTo(.16f).Within(.001f));
                flicker.ApplyAtTime(.6f); Assert.That(light.intensity,Is.EqualTo(2));
                flicker.ApplyAtTime(.12f); flicker.enabled=false; Assert.That(light.intensity,Is.EqualTo(2));
            }
            finally { Object.DestroyImmediate(go); }
        }
        [UnityTearDown] public IEnumerator LeaveRuntime() { if(Application.isPlaying) yield return new ExitPlayMode(); }
    }
}
