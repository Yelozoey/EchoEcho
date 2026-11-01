using System;
using System.IO;
using System.Linq;
using EchoStation.Rendering.Editor;
using EchoStation.SalvageNext.Editor;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EchoStation.LLMShortTasks.Editor
{
    public static class ImeWindowsBuild
    {
        public const string Evidence="Docs/Tests/SalvagePrototype/Evidence/LLMShortTasks/IME01",ProbeScene="Assets/EchoStation/LLMShortTasks/Scenes/IME01_Probe.unity";
        public static string Receipt{get;private set;}static string folder,scene;static GlobalObjectId[] selection;static bool pending;static int ticks;
        public static void CreateProbe()
        {
            SceneSafetyGuard.RequireClean();if(File.Exists(ProbeScene))throw new InvalidOperationException("Preserve probe scene");Directory.CreateDirectory(Path.GetDirectoryName(ProbeScene));var previous=SceneManager.GetActiveScene();var s=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try{SceneManager.SetActiveScene(s);var camera=new GameObject("Camera",typeof(Camera));camera.GetComponent<Camera>().clearFlags=CameraClearFlags.SolidColor;camera.GetComponent<Camera>().backgroundColor=new Color(.02f,.04f,.06f);var p=new GameObject("Probe",typeof(ImeProbe));p.GetComponent<ImeProbe>().standalone=true;if(!EditorSceneManager.SaveScene(s,ProbeScene))throw new IOException("Probe scene save failed");}finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(s,true);}
        }
        public static void Schedule(string label,bool game=false)
        {
            SceneSafetyGuard.RequireClean();if(pending||BuildPipeline.isBuildingPlayer||string.IsNullOrEmpty(label)||label.Any(c=>!char.IsLetterOrDigit(c)&&c!='_'))throw new InvalidOperationException("Build busy/invalid label");folder="Builds/LLMShortTasks/IME01/"+label;if(Directory.Exists(folder))throw new IOException("Preserve previous output");scene=game?NextArtV2Stream.Main:ProbeScene;Directory.CreateDirectory(Evidence);Receipt=Evidence+"/Build_"+label+".json";if(File.Exists(Receipt))throw new IOException("Preserve receipt");selection=Selection.objects.Select(GlobalObjectId.GetGlobalObjectIdSlow).ToArray();Selection.objects=Array.Empty<UnityEngine.Object>();pending=true;ticks=2;EditorApplication.update+=Queue;
        }
        static void Queue(){if(--ticks>0)return;EditorApplication.update-=Queue;EditorApplication.update+=Build;}
        static long Size(string p)=>Directory.Exists(p)?Directory.GetFiles(p,"*",SearchOption.AllDirectories).Sum(f=>new FileInfo(f).Length):0;
        static void Build()
        {
            EditorApplication.update-=Build;var result=new JObject{{"passed",false},{"scene",scene},{"output",Path.GetFullPath(folder+"/EchoStationIME.exe")}};var timer=System.Diagnostics.Stopwatch.StartNew();
            try
            {
                SceneSafetyGuard.RequireClean();Directory.CreateDirectory(folder);
                using(NextSdkSaveScope.Begin(Receipt+".save.json"))using(new NextPlayerBuildScope(Receipt+".player.json"))using(new NextLightmapBuildScope(Receipt+".lighting.json"))
                {var r=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{scene},locationPathName=folder+"/EchoStationIME.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});result["passed"]=r.summary.result==UnityEditor.Build.Reporting.BuildResult.Succeeded;result["errors"]=r.summary.totalErrors;result["warnings"]=r.summary.totalWarnings;result["messages"]=new JArray(r.steps.SelectMany(s=>s.messages).Where(m=>m.type==LogType.Error||m.type==LogType.Warning).Select(m=>m.content));}
                result["outputBytesBeforeStrip"]=Size(folder);string root=Path.GetFullPath(folder)+Path.DirectorySeparatorChar,models=Path.GetFullPath(folder+"/EchoStationIME_Data/StreamingAssets/EchoLLM");if(!models.StartsWith(root,StringComparison.OrdinalIgnoreCase))throw new IOException("Output escaped owned scope");result["removedOutputModelBytes"]=Size(models);if(Directory.Exists(models))Directory.Delete(models,true);
                const string link="Assets/EchoStation/SalvageNext/Addressables/link.xml";var before=JObject.Parse(File.ReadAllText(Evidence+"/Protection_Before.json"));if(before["files"][link]!=null||before["files"][link+".meta"]!=null)throw new IOException("Protect existing link metadata");if(File.Exists(link)){File.Copy(link,Receipt+".link.xml");if(File.Exists(link+".meta"))File.Copy(link+".meta",Receipt+".link.meta.txt");if(!AssetDatabase.DeleteAsset(link))throw new IOException("Owned SDK linker cleanup failed");}
                var notices=folder+"/ThirdPartyNotices";Directory.CreateDirectory(notices);foreach(var p in Directory.GetFiles("Builds/SalvageNext/ThirdPartyNotices","*",SearchOption.AllDirectories)){var relative=p.Substring("Builds/SalvageNext/ThirdPartyNotices/".Length);var target=Path.Combine(notices,relative);Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy(p,target);}
                result["outputBytes"]=Size(folder);
            }
            catch(Exception e){result["passed"]=false;result["error"]=e.ToString();}
            finally{pending=false;Selection.objects=selection.Select(GlobalObjectId.GlobalObjectIdentifierToObjectSlow).Where(o=>o).ToArray();result["seconds"]=timer.Elapsed.TotalSeconds;File.WriteAllText(Receipt,result.ToString());}
        }
    }
}
