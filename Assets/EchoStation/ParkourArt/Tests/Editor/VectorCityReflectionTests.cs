using System;
using System.IO;
using System.Linq;
using EchoStation.ParkourArt.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace EchoStation.ParkourArt.Tests
{
    public sealed class VectorCityReflectionTests
    {
        [Test] public void PK04_RegionalProbeBakesPersistAndContainRealCubemaps()
        {
            EditorSceneManager.OpenScene(VectorCityBuilder.ScenePath,OpenSceneMode.Single);
            try{
                var probes=Object.FindObjectsByType<ReflectionProbe>();Assert.That(probes.Length,Is.EqualTo(5));
                foreach(var p in probes){Assert.That(p.mode,Is.EqualTo(ReflectionProbeMode.Custom));Assert.That(p.customBakedTexture,Is.Not.Null,p.name);Assert.That(p.customBakedTexture.dimension,Is.EqualTo(TextureDimension.Cube));Assert.That(p.customBakedTexture.width,Is.EqualTo(512));Assert.That(new FileInfo(AssetDatabase.GetAssetPath(p.customBakedTexture)).Length,Is.GreaterThan(10000));Assert.That(p.boxProjection,Is.True);}
                var hashes=probes.Select(p=>Convert.ToBase64String(System.Security.Cryptography.SHA256.Create().ComputeHash(File.ReadAllBytes(AssetDatabase.GetAssetPath(p.customBakedTexture))))).Distinct().Count();Assert.That(hashes,Is.EqualTo(5),"Regional bakes must capture different geometry, not five identical skies.");
            }finally{EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);}
        }
        [Test] public void PK04_PlanarHasActualPixelsChangesFloorAndCleansUp()
        {
            var initialInvert=GL.invertCulling;EditorSceneManager.OpenScene(VectorCityBuilder.ScenePath,OpenSceneMode.Single);
            var path="Docs/Tests/ParkourArt/Evidence/PK04";Directory.CreateDirectory(path);
            try{
                var cam=GameObject.Find("Camera_Hero").GetComponent<Camera>();cam.transform.position=new Vector3(-7,17,-4);cam.transform.LookAt(new Vector3(5,16,8));
                var surfaces=Object.FindObjectsByType<VectorCityPlanarReflection>();
                using(var frames=new ParkourFrameCapture(cam)){
                    foreach(var p in surfaces)p.enabled=false;var off=frames.Capture(path+"/Planar_Off.png");
                    var floor=surfaces.Single(p=>p.name=="Planar_Atrium_Floor");var wall=surfaces.Single(p=>p.name=="Planar_Glass_Wall");
                    floor.enabled=true;float floorDiff=ParkourFrameCapture.Difference(off,frames.Capture(path+"/Planar_FloorOnly.png"));floor.enabled=false;
                    wall.enabled=true;float wallDiff=ParkourFrameCapture.Difference(off,frames.Capture(path+"/Planar_WallOnly.png"));
                    Assert.That(floorDiff,Is.GreaterThan(.0001f));Assert.That(wallDiff,Is.GreaterThan(.0001f));
                    foreach(var p in surfaces)p.enabled=true;var on=frames.Capture(path+"/Planar_On.png");
                    foreach(var p in surfaces){Assert.That(p.RenderCount,Is.GreaterThan(0),p.name);Assert.That(p.LastTexture&&p.LastTexture.IsCreated(),Is.True);Assert.That(p.LastTexture.width,Is.EqualTo(2048));}
                    var diff=ParkourFrameCapture.Difference(on,off);Assert.That(diff,Is.GreaterThan(.0001f));Assert.That(GL.invertCulling,Is.EqualTo(initialInvert));
                    File.WriteAllText(path+"/ActualPlanar.json","{\"imageDifference\":"+diff.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"floorDifference\":"+floorDiff.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"wallDifference\":"+wallDiff.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"surfaces\":2,\"cullingRestored\":true}");
                    foreach(var p in surfaces)p.enabled=false;Assert.That(Resources.FindObjectsOfTypeAll<Camera>().Count(c=>c.name.StartsWith("VC_PlanarCamera_")),Is.Zero);
                    Assert.That(Resources.FindObjectsOfTypeAll<RenderTexture>().Count(t=>t.name.StartsWith("VC_PlanarHDR_")),Is.Zero);
                }
            }finally{EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Assert.That(GL.invertCulling,Is.EqualTo(initialInvert));}
        }
    }
}
