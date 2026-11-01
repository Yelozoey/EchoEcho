using System;
using System.Linq;
using EchoStation.Rendering.Editor;
using UnityEditor;
using UnityEngine;

namespace EchoStation.ParkourArt.Editor
{
    public static class VectorCityMaterials
    {
        [MenuItem("Tools/EchoStation/Parkour Art/02 Apply Pure PBR Materials")]
        public static void Apply()
        {
            SceneSafetyGuard.RequireClean();
            var shader=AssetDatabase.LoadAssetAtPath<Shader>(VectorCityBuilder.Root+"/Shaders/VectorCityPBR.shader");
            if(!shader||ShaderUtil.ShaderHasError(shader))throw new InvalidOperationException("PBR shader missing or has compile errors.");
            foreach(var guid in AssetDatabase.FindAssets("t:Material",new[]{VectorCityBuilder.Root+"/Materials"}))
            {
                var m=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));var color=m.GetColor("_BaseColor");
                m.shader=shader;m.shaderKeywords=Array.Empty<string>();m.SetColor("_BaseColor",color);
                var polished=m.name=="Glass_Cyan";
                m.SetFloat("_Roughness",polished?.1f:m.name=="Runner_Red"?.31f:m.name=="Atrium_Cyan"?.33f:.48f);
                m.SetFloat("_Metallic",polished?.24f:0);
                m.SetFloat("_PlanarStrength",polished?.85f:0);
                m.enableInstancing=true;EditorUtility.SetDirty(m);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
