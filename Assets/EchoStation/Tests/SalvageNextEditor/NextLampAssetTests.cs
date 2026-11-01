using System;
using System.IO;
using System.Linq;
using EchoStation.SalvageNext.Editor;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace EchoStation.SalvageNext.Tests
{
    public sealed class NextLampAssetTests
    {
        [Test]public void DerivedWideLampPreservesEveryTriangleUvAndHasDownwardEmissionFace()
        {
            var data=JObject.Parse(File.ReadAllText(NextLampAssets.Evidence+"/Geometry_A.json"));Assert.That(NextLampAssets.Hash(NextLampAssets.SourcePath),Is.EqualTo(data["sourceShaBefore"].Value<string>()));var source=AssetDatabase.LoadAssetAtPath<GameObject>(NextLampAssets.SourcePath).GetComponentInChildren<MeshFilter>().sharedMesh;var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(NextLampAssets.MeshPath);Assert.That(mesh.subMeshCount,Is.EqualTo(2));Assert.That(mesh.bounds.center.magnitude,Is.LessThan(.0001f));Assert.That(Vector3.Distance(mesh.bounds.size,new Vector3(2.2f,.16f,.4f)),Is.LessThan(.0001f));long total=0;var originalUv=source.uv;var derivedUv=mesh.uv;for(int s=0;s<2;s++){var a=source.GetIndices(s);var b=mesh.GetIndices(s);Assert.That(b.Length,Is.EqualTo(a.Length));total+=b.Length;for(int i=0;i<a.Length;i++)Assert.That(Vector2.Distance(originalUv[a[i]],derivedUv[b[i]]),Is.LessThan(.000001f));}Assert.That(total,Is.EqualTo(data["sourceCorners"].Value<long>()));var normals=mesh.normals;Assert.That(mesh.GetIndices(data["emissiveSlot"].Value<int>()).Any(i=>normals[i].y<-.5f),Is.True,"Downward light-facing geometry required");
        }
        [Test]public void OwnedLampMaterialsActuallyEnableEmissionAndInstancingWithoutSourceMutation()
        {
            foreach(var path in new[]{NextLampAssets.BodyPath,NextLampAssets.LightPath}){var material=AssetDatabase.LoadAssetAtPath<Material>(path);Assert.That(material.shader.name,Is.EqualTo("Universal Render Pipeline/Lit"));Assert.That(material.enableInstancing,Is.True);Assert.That(material.IsKeywordEnabled("_EMISSION"),Is.True);Assert.That((material.globalIlluminationFlags&MaterialGlobalIlluminationFlags.EmissiveIsBlack)==0,Is.True);Assert.That(material.GetTexture("_EmissionMap"),Is.Not.Null);Assert.That(AssetDatabase.GetLabels(material),Does.Contain("EchoStation-SalvageNext-Generated"));}Assert.That(AssetDatabase.LoadAssetAtPath<Material>(NextLampAssets.LightPath).GetColor("_EmissionColor").maxColorComponent,Is.GreaterThan(1));var scope=JObject.Parse(File.ReadAllText(NextLampAssets.Evidence+"/PrepareSaveScope_A.json"));Assert.That(scope["passed"].Value<bool>(),Is.True);Assert.That(scope["unexpectedProjectSettingsChanges"].Count(),Is.Zero);
        }
    }
}
