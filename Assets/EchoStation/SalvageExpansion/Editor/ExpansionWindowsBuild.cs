using System;
using System.IO;
using System.Linq;
using EchoStation.Rendering.Editor;
using EchoStation.SalvageNext.Editor;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
namespace EchoStation.SalvageExpansion.Editor
{
    public static class ExpansionWindowsBuild
    {
        public const string Folder="Builds/SalvageExpansion",Output=Folder+"/EchoStationSalvageExpansion.exe",Evidence="Docs/Tests/SalvagePrototype/Evidence/Expansion/V01";
        public static string Receipt{get;private set;}static bool pending;static int ticks;static GlobalObjectId[] selection;
        public static void Schedule()
        {
            SceneSafetyGuard.RequireClean();if(pending||BuildPipeline.isBuildingPlayer)throw new InvalidOperationException("Build busy.");Directory.CreateDirectory(Evidence);Receipt=Evidence+"/Build_"+Guid.NewGuid().ToString("N")+".json";selection=Selection.objects.Select(GlobalObjectId.GetGlobalObjectIdSlow).ToArray();Selection.objects=Array.Empty<UnityEngine.Object>();ActiveEditorTracker.sharedTracker.ForceRebuild();pending=true;ticks=2;EditorApplication.update+=Queue;
        }
        static void Queue(){if(--ticks>0)return;EditorApplication.update-=Queue;EditorApplication.update+=Build;}
        static long Size(string path)=>Directory.Exists(path)?Directory.GetFiles(path,"*",SearchOption.AllDirectories).Sum(f=>new FileInfo(f).Length):0;
        static void CleanGeneratedLink(JObject row)
        {
            const string path="Assets/EchoStation/SalvageNext/Addressables/link.xml";if(NextAddressables.Settings.ConfigFolder.Replace('\\','/')+"/link.xml"!=path)throw new InvalidOperationException("Unexpected SDK linker location.");var baseline=JObject.Parse(File.ReadAllText("Docs/Tests/SalvagePrototype/Evidence/Expansion/Run01/Protection_before_A.json"));if(baseline["files"][path+".meta"]!=null)throw new InvalidOperationException("Pre-existing linker metadata must be preserved.");if(File.Exists(path)){File.Copy(path,Receipt+".GeneratedLink.xml");if(File.Exists(path+".meta"))File.Copy(path+".meta",Receipt+".GeneratedLink.meta.txt");if(!AssetDatabase.DeleteAsset(path))throw new IOException("Generated linker cleanup failed.");row["sdkGeneratedLinkArchivedAndRemoved"]=true;}
        }
        static void StripModels(JObject row)
        {
            string root=Path.GetFullPath(Folder)+Path.DirectorySeparatorChar,path=Path.GetFullPath(Folder+"/EchoStationSalvageExpansion_Data/StreamingAssets/EchoLLM");if(!path.StartsWith(root,StringComparison.OrdinalIgnoreCase)||!path.EndsWith(Path.Combine("EchoStationSalvageExpansion_Data","StreamingAssets","EchoLLM"),StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("New output model strip escaped scope.");row["newOutputModelBytesRemoved"]=Size(path);if(Directory.Exists(path))Directory.Delete(path,true);
        }
        static void Build()
        {
            EditorApplication.update-=Build;var timer=System.Diagnostics.Stopwatch.StartNew();var row=new JObject{{"passed",false},{"output",Path.GetFullPath(Output)},{"scene",ExpansionSceneBuilder.ScenePath}};
            try
            {
                SceneSafetyGuard.RequireClean();Directory.CreateDirectory(Folder);using(NextSdkSaveScope.Begin(Receipt+".save-scope.json"))using(new NextPlayerBuildScope(Receipt+".player-scope.json"))using(new NextLightmapBuildScope(Receipt+".lightmap-scope.json"))
                {var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ExpansionSceneBuilder.ScenePath},locationPathName=Output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});row["result"]=report.summary.result.ToString();row["errors"]=report.summary.totalErrors;row["warnings"]=report.summary.totalWarnings;row["messages"]=new JArray(report.steps.SelectMany(s=>s.messages).Where(m=>m.type==LogType.Error||m.type==LogType.Warning).Select(m=>m.content));row["passed"]=report.summary.result==UnityEditor.Build.Reporting.BuildResult.Succeeded;}
                row["outputBytesBeforeStrip"]=Size(Folder);StripModels(row);CleanGeneratedLink(row);if(!(bool)row["passed"])throw new InvalidOperationException("BuildPlayer failed.");string source="Builds/SalvageNext/ThirdPartyNotices",target=Folder+"/ThirdPartyNotices";foreach(var path in Directory.GetFiles(source,"*",SearchOption.AllDirectories)){string dest=Path.Combine(target,path.Substring(source.Length+1));Directory.CreateDirectory(Path.GetDirectoryName(dest));File.Copy(path,dest,true);}File.Copy("Docs/SalvagePrototype/Expansion/PlayGuide.md",Folder+"/PlayGuide.md",true);row["buildBytes"]=Size(Folder);row["originalModelDirectoryRetained"]=Directory.Exists("Assets/StreamingAssets/EchoLLM");
            }
            catch(Exception error){row["passed"]=false;row["error"]=error.ToString();try{StripModels(row);}catch(Exception strip){row["stripError"]=strip.ToString();}}
            finally{Selection.objects=selection.Select(GlobalObjectId.GlobalObjectIdentifierToObjectSlow).Where(o=>o).ToArray();pending=false;row["seconds"]=timer.Elapsed.TotalSeconds;File.WriteAllText(Receipt,row.ToString());}
        }
    }
}
