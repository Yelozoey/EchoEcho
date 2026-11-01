using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EchoStation.Rendering.Editor;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EchoStation.InfiltrationArt.Editor
{
    /// <summary>Bake outside NUnit's unnamed scene; save only this task's owned assets.</summary>
    public static class ArtSampleBake
    {
        static SceneSetup[] previous;
        static ArtSaveScope saveScope;
        static DateTime started;
        static bool completed, cancelled;
        static string receipt;
        static readonly List<string> errors=new List<string>();
        static void Log(string text,string stack,LogType type)
        {if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)lock(errors)errors.Add(text);}
        public static void Start(string version)
        {
            SceneSafetyGuard.RequireClean();
            if(previous!=null||Lightmapping.isRunning||EditorApplication.isPlaying)throw new InvalidOperationException("Bake busy");
            if(string.IsNullOrEmpty(version)||version.Any(c=>!char.IsLetterOrDigit(c)))throw new ArgumentException("Safe bake version required");
            receipt=ArtSampleAssets.Evidence+"/BakeJob_"+version+".json";
            if(File.Exists(receipt))throw new InvalidOperationException("Preserve bake receipt");
            previous=EditorSceneManager.GetSceneManagerSetup();
            if(previous.Any(s=>s.isLoaded&&string.IsNullOrEmpty(s.path))){previous=null;throw new InvalidOperationException("Preserve unnamed scene");}
            started=DateTime.UtcNow;completed=cancelled=false;lock(errors)errors.Clear();
            saveScope=new ArtSaveScope(receipt+".scope.json");
            Application.logMessageReceivedThreaded+=Log;
            try
            {
                EditorSceneManager.OpenScene(ArtSampleScene.ScenePath,OpenSceneMode.Single);
                Lightmapping.bakeCompleted+=Complete;Lightmapping.bakeCancelled+=Cancel;EditorApplication.update+=Tick;
                if(!Lightmapping.BakeAsync())Finish("BakeAsync rejected");
            }
            catch(Exception e){Finish(e.ToString());}
        }
        static void Complete()=>completed=true;
        static void Cancel()=>cancelled=true;
        static void Tick()
        {
            if((DateTime.UtcNow-started).TotalSeconds>300){Lightmapping.Cancel();Finish("Bake exceeded 300 seconds");}
            else if(!Lightmapping.isRunning&&(completed||cancelled))Finish(cancelled?"Cancelled":null);
        }
        static void Finish(string error)
        {
            Lightmapping.bakeCompleted-=Complete;Lightmapping.bakeCancelled-=Cancel;EditorApplication.update-=Tick;
            var row=new JObject{{"passed",false},{"error",error},{"seconds",(DateTime.UtcNow-started).TotalSeconds}};
            bool owned=SceneManager.sceneCount==1&&SceneManager.GetActiveScene().path==ArtSampleScene.ScenePath;
            try
            {
                if(!owned)throw new InvalidOperationException("Scene changed during bake; preserve current layout");
                if(error!=null)throw new InvalidOperationException(error);
                var maps=LightmapSettings.lightmaps;
                if(maps.Length==0||maps.Any(m=>!m.lightmapColor))throw new InvalidOperationException("No actual lightmap output");
                if(!EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),ArtSampleScene.ScenePath))throw new IOException("Owned scene save failed");
                if(Lightmapping.lightingDataAsset)AssetDatabase.SaveAssetIfDirty(Lightmapping.lightingDataAsset);
                row["lightmaps"]=maps.Length;row["colors"]=new JArray(maps.Select(m=>AssetDatabase.GetAssetPath(m.lightmapColor)));
                row["probes"]=LightmapSettings.lightProbes?LightmapSettings.lightProbes.count:0;
                row["lightmapper"]=Lightmapping.lightingSettings.lightmapper.ToString();
                lock(errors){row["errors"]=new JArray(errors);row["passed"]=errors.Count==0;}
            }
            catch(Exception e){row["error"]=e.ToString();row["passed"]=false;}
            finally
            {
                Application.logMessageReceivedThreaded-=Log;
                try
                {
                    if(owned){EditorSceneManager.RestoreSceneManagerSetup(previous);row["layoutRestored"]=true;}
                    saveScope?.Dispose();
                }
                catch(Exception e){row["restoreError"]=e.ToString();row["passed"]=false;}
                finally{previous=null;saveScope=null;File.WriteAllText(receipt,row.ToString());}
            }
        }
    }

    public sealed class ArtSaveScope:AssetModificationProcessor,IDisposable
    {
        static ArtSaveScope current;
        readonly string receipt;
        readonly Dictionary<string,byte[]> settings=new Dictionary<string,byte[]>();
        readonly List<string> saved=new List<string>(),blocked=new List<string>();
        readonly List<UnityEngine.Object> foreignDirty=new List<UnityEngine.Object>();
        public ArtSaveScope(string path)
        {
            SceneSafetyGuard.RequireClean();if(current!=null)throw new InvalidOperationException("Art save scope busy");receipt=path;
            foreach(var file in Directory.GetFiles("ProjectSettings","*.asset"))settings[file.Replace('\\','/')]=File.ReadAllBytes(file);
            foreach(var obj in Resources.FindObjectsOfTypeAll<UnityEngine.Object>())
                if(obj&&EditorUtility.IsPersistent(obj)&&EditorUtility.IsDirty(obj)&&!AssetDatabase.GetAssetPath(obj).StartsWith(ArtSampleAssets.Root+"/",StringComparison.Ordinal))foreignDirty.Add(obj);
            current=this;
        }
        static string[] OnWillSaveAssets(string[] paths)
        {
            if(current==null)return paths;
            var selected=new List<string>();
            foreach(string path in paths)
                if(path.StartsWith(ArtSampleAssets.Root+"/",StringComparison.Ordinal)){selected.Add(path);current.saved.Add(path);}
                else current.blocked.Add(path);
            return selected.ToArray();
        }
        public void Dispose()
        {
            if(current!=this)return;current=null;
            foreach(var obj in foreignDirty)if(obj&&!EditorUtility.IsDirty(obj))EditorUtility.SetDirty(obj);
            var changed=settings.Where(p=>!File.Exists(p.Key)||!p.Value.SequenceEqual(File.ReadAllBytes(p.Key))).Select(p=>p.Key).ToArray();
            File.WriteAllText(receipt,new JObject{{"passed",changed.Length==0},{"saved",new JArray(saved.Distinct())},{"blocked",new JArray(blocked.Distinct())},{"foreignDirtyPreserved",foreignDirty.Where(o=>o).All(EditorUtility.IsDirty)},{"unexpectedSettings",new JArray(changed)}}.ToString());
            if(changed.Length>0)throw new InvalidOperationException("Unexpected project settings save: "+string.Join(",",changed));
        }
    }
}
