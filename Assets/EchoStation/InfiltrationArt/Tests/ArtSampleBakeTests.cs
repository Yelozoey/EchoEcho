using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.InfiltrationArt.Editor;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EchoStation.InfiltrationArt.Tests
{
    public sealed class ArtSampleBakeTests
    {
        Scene previous, sample;
        [Test]
        public void OwnedSampleHasCompletedRealBakeAndRestoredItsLayout()
        {
            string receipt=Directory.GetFiles(ArtSampleAssets.Evidence,"BakeJob_*.json").Where(p=>!p.EndsWith(".scope.json")).OrderBy(File.GetLastWriteTimeUtc).Last();
            var row=JObject.Parse(File.ReadAllText(receipt));Assert.That((bool)row["passed"],Is.True,row.ToString());Assert.That((bool)row["layoutRestored"],Is.True);
            previous=SceneManager.GetActiveScene();sample=EditorSceneManager.OpenScene(ArtSampleScene.ScenePath,OpenSceneMode.Additive);SceneManager.SetActiveScene(sample);
            Assert.That(LightmapSettings.lightmaps.Length,Is.GreaterThan(0));
            Assert.That(LightmapSettings.lightmaps.All(l=>l.lightmapColor),Is.True);
            Assert.That(row["colors"].All(p=>File.Exists((string)p)),Is.True);
        }
        [TearDown]
        public void Cleanup()
        {
            if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
            if(sample.IsValid()&&sample.isLoaded)EditorSceneManager.CloseScene(sample,true);
        }
    }
}
