using System.IO;
using System.Linq;
using EchoStation.SalvageNext.Editor;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace EchoStation.SalvageNext.Tests
{
    public sealed class NextProbeAssetTests
    {
        [Test]public void IndependentProbeAssetKeepsAllPositionsCoefficientsAndTetrahedraWithoutAtlasReferences()
        {
            var expected=JObject.Parse(File.ReadAllText(NextProbeAssets.Evidence+"/Prepared_A.json"));var asset=AssetDatabase.LoadAssetAtPath<LightProbes>(NextProbeAssets.Path);Assert.That(asset,Is.Not.Null);Assert.That(EditorUtility.IsPersistent(asset),Is.True);Assert.That(asset.count,Is.EqualTo(expected["count"].Value<int>()));Assert.That(asset.cellCount,Is.EqualTo(expected["cells"].Value<int>()));Assert.That(asset.cellCount,Is.GreaterThan(0));Assert.That(asset.positions.Length,Is.EqualTo(180));for(int i=0;i<180;i++){var row=expected["positions"][i];Assert.That(asset.positions[i],Is.EqualTo(new Vector3(row[0].Value<float>(),row[1].Value<float>(),row[2].Value<float>())));}var coefficients=NextProbeAssets.Coefficients(asset.bakedProbes);for(int i=0;i<180;i++)for(int k=0;k<27;k++)Assert.That(coefficients[i][k].Value<float>(),Is.EqualTo(expected["baked"][i][k].Value<float>()));CollectionAssert.AreEqual(new[]{NextProbeAssets.Path},AssetDatabase.GetDependencies(NextProbeAssets.Path,true));
        }
        [Test]public void ReloadedStandaloneProbesProduceSameShInterpolationWithSourceSceneClosed()
        {
            Assert.That(UnityEngine.SceneManagement.SceneManager.GetSceneByPath(NextLayout.ScenePath).isLoaded,Is.False);var expected=JObject.Parse(File.ReadAllText(NextProbeAssets.Evidence+"/Prepared_A.json"));var prior=LightmapSettings.lightProbes;try{AssetDatabase.ImportAsset(NextProbeAssets.Path,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);var asset=AssetDatabase.LoadAssetAtPath<LightProbes>(NextProbeAssets.Path);LightmapSettings.lightProbes=asset;var actual=new SphericalHarmonicsL2[NextProbeAssets.Samples.Length];for(int i=0;i<actual.Length;i++)LightProbes.GetInterpolatedProbe(NextProbeAssets.Samples[i],null,out actual[i]);var values=NextProbeAssets.Coefficients(actual);float maximum=0;for(int i=0;i<actual.Length;i++)for(int k=0;k<27;k++)maximum=Mathf.Max(maximum,Mathf.Abs(values[i][k].Value<float>()-expected["sampleCoefficients"][i][k].Value<float>()));File.WriteAllText(NextProbeAssets.Evidence+"/Interpolation_A.json",new JObject{{"maximumCoefficientDifference",maximum},{"samples",actual.Length},{"sourceSceneLoaded",false},{"probeCount",asset.count},{"cellCount",asset.cellCount}}.ToString());Assert.That(maximum,Is.LessThan(.00001f));}finally{LightmapSettings.lightProbes=prior;}
        }
    }
}
