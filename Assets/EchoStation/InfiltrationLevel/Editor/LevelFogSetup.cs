using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
namespace EchoStation.InfiltrationLevel.Editor
{
    public static class LevelFogSetup
    {
        public static void Apply()
        {
            var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(LevelLighting.RendererPath);
            if(renderer.rendererFeatures.Any(f=>f is LevelFogFeature))throw new InvalidOperationException("Preserve fog setup");
            var shader=AssetDatabase.LoadAssetAtPath<Shader>(LevelBuilder.Root+"/Shaders/LevelLocalFog.shader");if(!shader)throw new InvalidOperationException("Fog shader missing");
            var feature=ScriptableObject.CreateInstance<LevelFogFeature>();feature.name="IL_LocalFog";feature.Configure(shader);AssetDatabase.AddObjectToAsset(feature,renderer);renderer.rendererFeatures.Add(feature);renderer.SetDirty();LevelBuilder.Save(feature);LevelBuilder.Save(renderer);
            LevelBuilder.Edit(root=>{var go=new GameObject("IL_LocalFogVolumes");go.transform.SetParent(root,false);var field=go.AddComponent<LevelFogField>();root.GetComponentInChildren<Camera>().gameObject.AddComponent<LevelFogView>().Source=field;});
            File.WriteAllText(LevelBuilder.Evidence+"/FogSetup_A.json",new JObject{{"boxes",3},{"steps",16},{"resolution","half"},{"mainLightShadows",true},{"gameplayDependency",false}}.ToString());
        }
    }
}
