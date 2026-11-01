using System;
using System.IO;
using System.Linq;
using EchoStation.Rendering.Editor;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace EchoStation.SalvageNext.Editor
{
    public static class NextProbeAssets
    {
        public const string Path=FacilityAssets.Generated+"/ES_Next_Streamed_Probes.asset",Evidence="Docs/Tests/SalvagePrototype/Evidence/R4-R03b-1a";
        public static readonly Vector3[] Samples={new Vector3(5,.6f,10),new Vector3(18,.6f,12),new Vector3(30,.6f,20)};
        public static JArray Coefficients(SphericalHarmonicsL2[] probes)=>new JArray(probes.Select(probe=>{var values=new JArray();for(int color=0;color<3;color++)for(int coefficient=0;coefficient<9;coefficient++)values.Add(probe[color,coefficient]);return values;}));
        public static void Prepare()
        {
            SceneSafetyGuard.RequireClean();if(AssetDatabase.LoadMainAssetAtPath(Path))throw new InvalidOperationException("Keep existing independent probes.");Directory.CreateDirectory(Evidence);var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(NextLayout.ScenePath,OpenSceneMode.Additive);LightProbes copy=null;
            try
            {
                SceneManager.SetActiveScene(scene);var source=LightmapSettings.lightProbes;if(!source||source.count<180)throw new InvalidOperationException("Source probes unavailable.");var interpolated=new SphericalHarmonicsL2[Samples.Length];for(int i=0;i<Samples.Length;i++)LightProbes.GetInterpolatedProbe(Samples[i],null,out interpolated[i]);copy=Object.Instantiate(source);copy.name="ES_Next_Streamed_Probes";copy.hideFlags=HideFlags.None;AssetDatabase.CreateAsset(copy,Path);AssetDatabase.SetLabels(copy,new[]{"EchoStation-SalvageNext-Generated"});EditorUtility.SetDirty(copy);AssetDatabase.SaveAssetIfDirty(copy);var row=new JObject{{"sourceScene",NextLayout.ScenePath},{"asset",Path},{"count",source.count},{"cells",source.cellCount},{"positions",new JArray(source.positions.Select(p=>new JArray(p.x,p.y,p.z)))},{"baked",Coefficients(source.bakedProbes)},{"sampleCoefficients",Coefficients(interpolated)},{"dependencies",new JArray(AssetDatabase.GetDependencies(Path,true))}};File.WriteAllText(Evidence+"/Prepared_A.json",row.ToString());
            }
            catch(Exception e){File.WriteAllText(Evidence+"/PrepareFailure_"+Guid.NewGuid().ToString("N")+".txt",e.ToString());throw;}
            finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);if(copy&&!AssetDatabase.Contains(copy))Object.DestroyImmediate(copy);}
        }
    }
}
