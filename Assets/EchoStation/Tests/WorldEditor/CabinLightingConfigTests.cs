using System.Linq;
using EchoStation.Rendering.Editor;
using EchoStation.World.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace EchoStation.Tests.World
{
    public class CabinLightingConfigTests
    {
        [Test]public void B201_LightingSettingsAreDedicatedAndMatchValidatedBakeBaseline()
        {
            var settings=AssetDatabase.LoadAssetAtPath<LightingSettings>(CabinLightingSetup.SettingsPath);
            Assert.That(settings,Is.Not.Null);Assert.That(settings,Is.Not.SameAs(AssetDatabase.LoadAssetAtPath<LightingSettings>(BakedLightingSetup.SettingsPath)));
            Assert.That(settings.bakedGI&&!settings.realtimeGI&&!settings.autoGenerate,Is.True);Assert.That(settings.lightmapper,Is.EqualTo(LightingSettings.Lightmapper.ProgressiveCPU));
            Assert.That(settings.lightmapResolution,Is.EqualTo(20));Assert.That(settings.lightmapMaxSize,Is.EqualTo(1024));Assert.That(settings.directionalityMode,Is.EqualTo(LightmapsMode.CombinedDirectional));
        }
        [Test]public void B202_CabinHasBakedLightsClearProbesAndVolumeWiring()
        {
            var scene=EditorSceneManager.OpenPreviewScene(CabinGreyboxSetup.ScenePath);
            try
            {
                var roots=scene.GetRootGameObjects();var root=roots.Single(o=>o.name=="ES_Cabin_Greybox");
                Assert.That(root.GetComponentsInChildren<Light>().Count(l=>l.lightmapBakeType==LightmapBakeType.Baked),Is.EqualTo(5));
                Assert.That(root.GetComponentsInChildren<Light>().Single(l=>l.name=="Cabin_MainCoolA").intensity,Is.EqualTo(9));
                Assert.That(root.GetComponentsInChildren<Light>().Single(l=>l.name=="Cabin_MainCoolA").color,Is.EqualTo(new Color(.78f,.86f,1)));
                var group=root.GetComponentInChildren<LightProbeGroup>();Assert.That(group.probePositions.Length,Is.GreaterThanOrEqualTo(100));
                var boxes=root.GetComponentsInChildren<BoxCollider>().Select(c=>new Bounds(c.transform.TransformPoint(c.center),Vector3.Scale(c.size,c.transform.lossyScale))).ToArray();
                Assert.That(group.probePositions.All(p=>!boxes.Any(b=>b.Contains(p))),Is.True);
                var volume=roots.SelectMany(o=>o.GetComponentsInChildren<Volume>()).Single();Assert.That(AssetDatabase.GetAssetPath(volume.sharedProfile),Is.EqualTo(ExplorationVolumeSetup.ExplorationProfilePath));
                var camera=roots.Single(o=>o.name=="Cabin_ValidationCamera").GetComponent<Camera>();var data=camera.GetUniversalAdditionalCameraData();
                Assert.That(data.renderPostProcessing&&!data.allowHDROutput&&camera.allowHDR,Is.True);Assert.That(data.volumeLayerMask.value,Is.EqualTo(256));
                Assert.That(roots.Single(o=>o.name=="Cabin_Fault_Realtime").GetComponent<Light>().lightmapBakeType,Is.EqualTo(LightmapBakeType.Realtime));
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
    }
}
