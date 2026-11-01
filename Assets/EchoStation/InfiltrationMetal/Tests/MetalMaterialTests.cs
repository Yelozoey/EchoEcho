using System.IO;
using System.Linq;
using EchoStation.InfiltrationMetal.Editor;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace EchoStation.InfiltrationMetal.Tests
{
    public sealed class MetalMaterialTests
    {
        static JArray Rows()=>JArray.Parse(File.ReadAllText(MetalScene.Evidence+"/MaterialAssignments_A.json"));
        [Test]public void WallsAndDeviceBodiesUseRoughFinishAndSharedSourcesHaveIndependentVariants()
        {
            var rows=Rows();Assert.That(rows.Count(r=>(string)r["finish"]=="Rough"),Is.GreaterThan(100));Assert.That(rows.Count(r=>(string)r["finish"]=="Polished"),Is.GreaterThan(0));
            foreach(var row in rows.Where(r=>new[]{"Wall","EquipmentCase","Locker","Shelf","Toolbox","Barrel"}.Contains((string)r["role"])))
            {var source=AssetDatabase.LoadAssetAtPath<Material>((string)row["source"]);if(!MetalMaterials.Protected(source))Assert.That((string)row["finish"],Is.EqualTo("Rough"),source.name);}
            Assert.That(rows.GroupBy(r=>(string)r["source"]).Any(g=>g.Select(r=>(string)r["finish"]).Distinct().Count()>1),Is.True,"Shared atlas must get finish variants, not a global edit");
            foreach(var row in rows){Assert.That(((string)row["material"]).StartsWith(MetalScene.Generated+"/"),Is.True);Assert.That((string)row["source"],Is.Not.EqualTo((string)row["material"]));}
        }
        [Test]public void RoughAndPolishedMasksHaveActualMetalAndRoughnessChannels()
        {
            foreach(var group in Rows().Where(r=>(string)r["finish"]!="Preserved").GroupBy(r=>(string)r["material"]))
            {
                var row=group.First();bool polished=(string)row["finish"]=="Polished";var m=AssetDatabase.LoadAssetAtPath<Material>(group.Key);var tex=m.GetTexture("_MetallicGlossMap");
                if(tex){string path=AssetDatabase.GetAssetPath(tex);Assert.That(path.StartsWith(MetalScene.Generated+"/"),Is.True);var image=new Texture2D(2,2,TextureFormat.RGBA32,false,true);try{Assert.That(image.LoadImage(File.ReadAllBytes(path)),Is.True);Assert.That(image.width,Is.LessThanOrEqualTo(1024));var pixels=image.GetPixels32();Assert.That(pixels.All(p=>p.r==(polished?255:232)&&(polished?p.a==220:p.a>=90&&p.a<=140)),Is.True);Assert.That(m.IsKeywordEnabled("_METALLICSPECGLOSSMAP"),Is.True);Assert.That(m.GetFloat("_Smoothness"),Is.EqualTo(1));}finally{Object.DestroyImmediate(image);}}
                else{Assert.That(m.GetFloat("_Metallic"),Is.GreaterThanOrEqualTo(.9));Assert.That(m.GetFloat("_Smoothness"),polished?Is.GreaterThan(.8):Is.InRange(.35f,.55f));}
            }
        }
        [Test]public void AuthorDetailAndScreensLightsGlassClothRemainPreserved()
        {
            foreach(var row in Rows())
            {
                var source=AssetDatabase.LoadAssetAtPath<Material>((string)row["source"]);var m=AssetDatabase.LoadAssetAtPath<Material>((string)row["material"]);Assert.That(m.shader,Is.SameAs(source.shader));
                foreach(string field in new[]{"_BaseMap","_BumpMap","_OcclusionMap","_EmissionMap"})if(source.HasProperty(field))Assert.That(m.GetTexture(field),Is.SameAs(source.GetTexture(field)),field+source.name);
                if(source.HasProperty("_EmissionColor"))Assert.That(m.GetColor("_EmissionColor"),Is.EqualTo(source.GetColor("_EmissionColor")));if(source.HasProperty("_BaseColor"))Assert.That(m.GetColor("_BaseColor"),Is.EqualTo(source.GetColor("_BaseColor")));
                if(MetalMaterials.Protected(source)||new[]{"Mug","TarpCrate"}.Contains((string)row["role"]))Assert.That((string)row["finish"],Is.EqualTo("Preserved"));
                if((string)row["finish"]=="Preserved")foreach(string property in new[]{"_Metallic","_Smoothness","_Surface"})if(source.HasProperty(property))Assert.That(m.GetFloat(property),Is.EqualTo(source.GetFloat(property)),source.name);
            }
        }
    }
}
