using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EchoStation.Rendering.Editor;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace EchoStation.SalvageNext.Editor
{
    /// <summary>Bounds official SDK SaveAssets calls to this run's own assets.</summary>
    public sealed class NextSdkSaveScope:AssetModificationProcessor,IDisposable
    {
        static NextSdkSaveScope current;
        readonly string receipt;readonly Dictionary<string,byte[]> settings=new Dictionary<string,byte[]>();
        readonly HashSet<string> allowedExtra=new HashSet<string>(StringComparer.Ordinal);
        readonly List<string> saved=new List<string>(),blocked=new List<string>();
        readonly List<Object> foreignDirty=new List<Object>();
        NextSdkSaveScope(string path,string[] extra)
        {
            SceneSafetyGuard.RequireClean();if(current!=null)throw new InvalidOperationException("SDK save scope already active.");receipt=path;foreach(var item in extra)allowedExtra.Add(item);
            foreach(var file in Directory.GetFiles("ProjectSettings","*.asset"))settings[file.Replace('\\','/')]=File.ReadAllBytes(file);
            // Unknown dirty project configuration must never be implicitly saved.
            foreach(var obj in Resources.FindObjectsOfTypeAll<Object>())if(obj&&EditorUtility.IsPersistent(obj)&&EditorUtility.IsDirty(obj)){var asset=AssetDatabase.GetAssetPath(obj);if(!asset.StartsWith(FacilityAssets.Root+"/",StringComparison.Ordinal)&&!allowedExtra.Contains(asset))foreignDirty.Add(obj);if(asset.StartsWith("ProjectSettings/",StringComparison.Ordinal)&&asset!="ProjectSettings/GraphicsSettings.asset"&&asset!="ProjectSettings/QualitySettings.asset"&&!allowedExtra.Contains(asset))throw new InvalidOperationException("Unrelated dirty project settings: "+asset);}
            current=this;
        }
        public static NextSdkSaveScope Begin(string receipt,params string[] allowedProjectFiles)=>new NextSdkSaveScope(receipt,allowedProjectFiles);
        static string[] OnWillSaveAssets(string[] paths)
        {
            if(current==null)return paths;var selected=new List<string>();foreach(var path in paths){if(path.StartsWith(FacilityAssets.Root+"/",StringComparison.Ordinal)||current.allowedExtra.Contains(path)){selected.Add(path);current.saved.Add(path);}else current.blocked.Add(path);}return selected.ToArray();
        }
        public void Dispose()
        {
            if(current!=this)return;current=null;foreach(var obj in foreignDirty)if(obj&&!EditorUtility.IsDirty(obj))EditorUtility.SetDirty(obj);var changed=settings.Where(pair=>!allowedExtra.Contains(pair.Key)&&(!File.Exists(pair.Key)||!pair.Value.SequenceEqual(File.ReadAllBytes(pair.Key)))).Select(pair=>pair.Key).ToArray();Directory.CreateDirectory(Path.GetDirectoryName(receipt));File.WriteAllText(receipt,new JObject{{"passed",changed.Length==0},{"saved",new JArray(saved.Distinct())},{"blocked",new JArray(blocked.Distinct())},{"foreignDirtyFlagsPreserved",foreignDirty.Where(obj=>obj).All(EditorUtility.IsDirty)},{"unexpectedProjectSettingsChanges",new JArray(changed)}}.ToString());if(changed.Length>0)throw new InvalidOperationException("SDK unexpectedly saved project settings: "+string.Join(",",changed));
        }
    }
}
