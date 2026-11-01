using System.IO;
using System.Linq;
using EchoStation.InfiltrationCoated.Editor;
using EchoStation.InfiltrationArt;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace EchoStation.InfiltrationCoated.Tests
{
    public sealed class CoatMaterialTests
    {
        Scene old,sample;Transform root;JArray rows;
        [SetUp]public void Open(){old=SceneManager.GetActiveScene();sample=EditorSceneManager.OpenScene(CoatScene.ScenePath,OpenSceneMode.Additive);SceneManager.SetActiveScene(sample);root=CoatScene.World(sample);rows=JArray.Parse(File.ReadAllText(CoatScene.Evidence+"/MaterialAssignments_A.json"));}
        [TearDown]public void Close(){SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(sample,true);}
        [Test]public void OwnedVariantsSplitSharedAtlasesAndCoverPaintedSurfaces()
        {
            Assert.That(rows.Count,Is.EqualTo(root.GetComponentsInChildren<Renderer>().Sum(r=>r.sharedMaterials.Length)));Assert.That(rows.Count(r=>(string)r["finish"]=="Coated"),Is.GreaterThan(200));Assert.That(rows.All(r=>((string)r["material"]).StartsWith(CoatScene.Generated+"/")),Is.True);
            foreach(string role in new[]{"Wall","WallCap","EquipmentCase","Locker","Terminal","Computer","Toolbox"})Assert.That(rows.Any(r=>(string)r["role"]==role&&(string)r["finish"]=="Coated"),Is.True,role);
            Assert.That(rows.GroupBy(r=>(string)r["source"]).Any(g=>g.Select(r=>(string)r["finish"]).Distinct().Count()>1),Is.True,"Shared atlas needs separate surface variants");
        }
        [Test]public void PaintIsActualNonmetalWithDerivedAtlasAndUnchangedDetailMaps()
        {
            foreach(var row in rows.Where(r=>(string)r["finish"]=="Coated").GroupBy(r=>(string)r["material"]).Select(g=>g.First()))
            {
                var source=AssetDatabase.LoadAssetAtPath<Material>((string)row["source"]);var material=AssetDatabase.LoadAssetAtPath<Material>((string)row["material"]);Assert.That(material.shader.name,Is.EqualTo("Universal Render Pipeline/Lit"));Assert.That(material.GetFloat("_Metallic"),Is.Zero);Assert.That(material.GetFloat("_Smoothness"),Is.EqualTo(.42f));Assert.That(material.IsKeywordEnabled("_METALLICSPECGLOSSMAP"),Is.False);Assert.That(material.GetColor("_BaseColor"),Is.EqualTo(Color.white));
                string texture=AssetDatabase.GetAssetPath(material.GetTexture("_BaseMap"));Assert.That(texture.StartsWith(CoatScene.Generated+"/CF_Coating_"),Is.True);Assert.That(material.GetTexture("_BaseMap").width,Is.LessThanOrEqualTo(512));foreach(string key in new[]{"_BumpMap","_OcclusionMap","_EmissionMap"})Assert.That(material.GetTexture(key),Is.SameAs(source.GetTexture(key)));
                var image=new Texture2D(2,2);try{Assert.That(image.LoadImage(File.ReadAllBytes(texture)),Is.True);Assert.That(image.GetPixels32().All(c=>c.r>=180&&c.b<=235),Is.True);}finally{Object.DestroyImmediate(image);}
            }
        }
        [Test]public void ProtectedSurfacesKeepOriginalValuesAndRobotPalette()
        {
            foreach(var row in rows.Where(r=>(string)r["finish"]=="Original").GroupBy(r=>(string)r["material"]).Select(g=>g.First()))
            {
                var source=AssetDatabase.LoadAssetAtPath<Material>((string)row["source"]);var material=AssetDatabase.LoadAssetAtPath<Material>((string)row["material"]);Assert.That(material.shader,Is.SameAs(source.shader));Assert.That(material.shaderKeywords,Is.EquivalentTo(source.shaderKeywords));foreach(string t in source.GetTexturePropertyNames())Assert.That(material.GetTexture(t),Is.SameAs(source.GetTexture(t)));if(source.HasProperty("_BaseColor"))Assert.That(material.GetColor("_BaseColor"),Is.EqualTo(source.GetColor("_BaseColor")));if(source.HasProperty("_Metallic"))Assert.That(material.GetFloat("_Metallic"),Is.EqualTo(source.GetFloat("_Metallic")));
            }
            foreach(string role in new[]{"Guard","Echo","DoorFrame","Cable","Fan","Vent","Shelf","Mug","TarpCrate"})Assert.That(rows.Where(r=>(string)r["role"]==role).All(r=>(string)r["finish"]=="Original"),Is.True,role);
        }
        [Test]public void MainFloorsAndScreenGlassLampRubberRemainOutsideCoating()
        {
            foreach(var renderer in root.GetComponentsInChildren<Renderer>())
            {
                var art=renderer.GetComponentInParent<ArtSampleModel>();bool ceiling=false;for(var t=renderer.transform;t;t=t.parent)if(t.name.StartsWith("Ceiling_"))ceiling=true;
                foreach(var m in renderer.sharedMaterials)
                {
                    bool coated=m.name.StartsWith("CF_Coated_");if(art&&art.Role=="Floor"&&!ceiling)Assert.That(coated,Is.False);
                    if(new[]{"Screen","Glass","Decal","Emit","_Light","Black","Dark"}.Any(s=>m.name.Contains(s)))Assert.That(coated,Is.False,m.name);
                }
            }
        }
    }
}
