using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
namespace EchoStation.SalvageNext.Editor
{
    /// <summary>SBP cannot infer runtime-assigned lightmaps from inactive additive visual scenes.</summary>
    public sealed class NextLightmapBuildScope:IDisposable
    {
        const string Path="ProjectSettings/GraphicsSettings.asset";readonly UnityEngine.Object settings;readonly string json,receipt;readonly byte[] bytes;readonly bool dirty;readonly int stripping;readonly bool[] flags=new bool[6];bool disposed;
        static readonly string[] Names={"m_LightmapKeepPlain","m_LightmapKeepDirCombined","m_LightmapKeepDynamicPlain","m_LightmapKeepDynamicDirCombined","m_LightmapKeepShadowMask","m_LightmapKeepSubtractive"};
        public NextLightmapBuildScope(string evidence)
        {
            receipt=evidence;settings=AssetDatabase.LoadAllAssetsAtPath(Path).Single();json=EditorJsonUtility.ToJson(settings);bytes=File.ReadAllBytes(Path);dirty=EditorUtility.IsDirty(settings);var so=new SerializedObject(settings);stripping=so.FindProperty("m_LightmapStripping").intValue;
            so.FindProperty("m_LightmapStripping").intValue=1;for(int i=0;i<Names.Length;i++){var property=so.FindProperty(Names[i]);flags[i]=property.boolValue;property.boolValue=i<2;}so.ApplyModifiedPropertiesWithoutUndo();
        }
        public void Dispose()
        {
            if(disposed)return;disposed=true;var so=new SerializedObject(settings);so.FindProperty("m_LightmapStripping").intValue=stripping;for(int i=0;i<Names.Length;i++)so.FindProperty(Names[i]).boolValue=flags[i];so.ApplyModifiedPropertiesWithoutUndo();bool native=EditorJsonUtility.ToJson(settings)==json,disk=bytes.SequenceEqual(File.ReadAllBytes(Path));if(native&&!dirty)EditorUtility.ClearDirty(settings);Directory.CreateDirectory(System.IO.Path.GetDirectoryName(receipt));File.WriteAllText(receipt,new JObject{{"nativeExactRestored",native},{"diskBytesUnchanged",disk},{"dirtyFlagBefore",dirty},{"dirtyFlagAfter",EditorUtility.IsDirty(settings)},{"manualBuildOnly",true},{"keepPlain",true},{"keepDirectional",true},{"unusedGiModesDisabledDuringBuild",true}}.ToString());if(!native||!disk||EditorUtility.IsDirty(settings)!=dirty)throw new InvalidOperationException("Lightmap build flags did not restore exactly.");
        }
    }
}
