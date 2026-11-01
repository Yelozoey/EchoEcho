using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement;
namespace EchoStation.SalvageNext.Tests
{
    public sealed class NextPackageTests
    {
        [Test]public void InstalledOfficialAssembliesUsePinnedPackagesAndKeepEngineAndPipeline()
        {
            Assert.That(Application.unityVersion,Is.EqualTo("6000.6.7f1"));var packages=PackageInfo.GetAllRegisteredPackages();foreach(var pair in new[]{new[]{"com.unity.addressables","2.11.2"},new[]{"com.unity.scriptablebuildpipeline","3.0.3"},new[]{"com.unity.render-pipelines.universal","17.6.0"},new[]{"com.unity.test-framework","1.8.0"},new[]{"com.unity.profiling.core","1.0.3"}}){var package=packages.Single(p=>p.name==pair[0]);Assert.That(package.version,Is.EqualTo(pair[1]));if(pair[0]=="com.unity.addressables"||pair[0]=="com.unity.scriptablebuildpipeline")Assert.That(package.source,Is.EqualTo(PackageSource.Registry),pair[0]);}Assert.That(PackageInfo.FindForAssembly(typeof(Addressables).Assembly).version,Is.EqualTo("2.11.2"));Assert.That(PackageInfo.FindForAssembly(typeof(ResourceManager).Assembly).name,Is.EqualTo("com.unity.addressables"));
        }
        [Test]public void EveryPriorLockEntryKeepsSourceHashDependenciesAndVersion()
        {
            const string evidence="Docs/Tests/SalvagePrototype/Evidence/R4-R03a-1/";var old=JObject.Parse(File.ReadAllText(evidence+"packages-lock.json.before"))["dependencies"];var current=JObject.Parse(File.ReadAllText("Packages/packages-lock.json"))["dependencies"];foreach(var entry in ((JObject)old).Properties()){var prior=(JObject)entry.Value.DeepClone();var now=(JObject)current[entry.Name].DeepClone();prior.Remove("depth");now.Remove("depth");Assert.That(JToken.DeepEquals(prior,now),Is.True,entry.Name);}Assert.That(((JObject)old).Properties().Count(),Is.EqualTo(73));var before=JObject.Parse(File.ReadAllText(evidence+"manifest.json.before"));before["dependencies"]["com.unity.addressables"]="2.11.2";Assert.That(JToken.DeepEquals(before,JObject.Parse(File.ReadAllText("Packages/manifest.json"))),Is.True);
        }
    }
}
