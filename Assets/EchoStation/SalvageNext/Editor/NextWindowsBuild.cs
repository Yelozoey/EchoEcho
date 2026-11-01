using System;
using System.IO;
using System.Linq;
using EchoStation.Rendering.Editor;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
namespace EchoStation.SalvageNext.Editor
{
    public static class NextWindowsBuild
    {
        public const string Evidence="Docs/Tests/SalvagePrototype/Evidence/R4-P01b-2",Output="Builds/SalvageNext/EchoStationSalvageNext.exe";
        public static string Receipt{get;private set;}static bool pending;static int ticks;static GlobalObjectId[] selection;static string selectedScene;
        public static void Schedule(string scenePath=null)
        {SceneSafetyGuard.RequireClean();selectedScene=scenePath??NextRoomPartition.Main;if(selectedScene!=NextRoomPartition.Main&&selectedScene!=NextArtV2Stream.Main)throw new ArgumentException("Only owned release scene allowed");if(pending||BuildPipeline.isBuildingPlayer)throw new InvalidOperationException("Build busy.");Directory.CreateDirectory(Evidence);Receipt=Evidence+"/Build_"+Guid.NewGuid().ToString("N")+".json";selection=Selection.objects.Select(GlobalObjectId.GetGlobalObjectIdSlow).ToArray();Selection.objects=Array.Empty<UnityEngine.Object>();ActiveEditorTracker.sharedTracker.ForceRebuild();pending=true;ticks=2;EditorApplication.update+=Queue;}
        static void Queue(){if(--ticks>0)return;EditorApplication.update-=Queue;EditorApplication.update+=Build;}
        static void Build()
        {
            EditorApplication.update-=Build;var row=new JObject{{"passed",false},{"output",Path.GetFullPath(Output)},{"scene",selectedScene}};var timer=System.Diagnostics.Stopwatch.StartNew();
            try
            {
                SceneSafetyGuard.RequireClean();Directory.CreateDirectory(Path.GetDirectoryName(Output));
                using(NextSdkSaveScope.Begin(Receipt+".save-scope.json"))using(new NextPlayerBuildScope(Receipt+".player-scope.json"))using(new NextLightmapBuildScope(Receipt+".lightmap-scope.json"))
                {
                    var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{selectedScene},locationPathName=Output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});row["result"]=report.summary.result.ToString();row["errors"]=report.summary.totalErrors;row["warnings"]=report.summary.totalWarnings;row["messages"]=new JArray(report.steps.SelectMany(s=>s.messages).Where(m=>m.type==LogType.Warning||m.type==LogType.Error).Select(m=>m.content));row["passed"]=report.summary.result==UnityEditor.Build.Reporting.BuildResult.Succeeded;
                }
                if(!row["passed"].Value<bool>())throw new InvalidOperationException("BuildPlayer failed.");
                string root=Path.GetFullPath("Builds/SalvageNext")+Path.DirectorySeparatorChar,models=Path.GetFullPath("Builds/SalvageNext/EchoStationSalvageNext_Data/StreamingAssets/EchoLLM");if(!models.StartsWith(root,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Model strip path escaped new build.");long stripped=Directory.Exists(models)?Directory.GetFiles(models,"*",SearchOption.AllDirectories).Sum(p=>new FileInfo(p).Length):0;if(Directory.Exists(models))Directory.Delete(models,true);row["newOutputModelBytesRemoved"]=stripped;row["projectModelsUntouched"]=Directory.Exists("Assets/StreamingAssets/EchoLLM");
                string notices="Builds/SalvageNext/ThirdPartyNotices";Directory.CreateDirectory(notices+"/Noto");foreach(string name in new[]{"LICENSE.txt","COPYRIGHT.txt"})File.Copy("Assets/EchoStation/ThirdParty/Noto/"+name,notices+"/Noto/"+name,true);File.Copy(FacilityAssets.Raw+"/License_Standard.txt",notices+"/Quaternius_CC0.txt",true);File.Copy(RoverAssets.Raw+"/SOURCE.md",notices+"/MarsRover_CC0.md",true);File.Copy("Assets/EchoStation/Salvage/ThirdParty/PolyHaven/MetalToolbox/LICENSE_CC0.txt",notices+"/MetalToolbox_CC0.txt",true);
                Directory.CreateDirectory(notices+"/OnnxRuntime");foreach(var file in Directory.GetFiles("Assets/EchoStation/AI/ThirdParty/OnnxRuntime/Licenses","*.txt"))File.Copy(file,notices+"/OnnxRuntime/"+Path.GetFileName(file),true);
                string jsonPackage=Directory.GetDirectories("Library/PackageCache","com.unity.nuget.newtonsoft-json@*").Single(),addressablesPackage=Directory.GetDirectories("Library/PackageCache","com.unity.addressables@*").Single();File.Copy(jsonPackage+"/LICENSE.md",notices+"/Unity_Newtonsoft_License.md",true);File.Copy(jsonPackage+"/Third Party Notices.md",notices+"/Newtonsoft_ThirdPartyNotices.md",true);File.Copy(addressablesPackage+"/LICENSE.md",notices+"/Addressables_License.md",true);
                if(selectedScene==NextArtV2Stream.Main){Directory.CreateDirectory(notices+"/KenneyUI");foreach(var file in Directory.GetFiles(NextArtV2Ui.Root+"/Kenney","*_LICENSE.txt"))File.Copy(file,notices+"/KenneyUI/"+Path.GetFileName(file),true);File.Copy(NextArtV2Ui.Root+"/Kenney/SOURCES.json",notices+"/KenneyUI/SOURCES.json",true);}
                row["buildBytes"]=Directory.GetFiles("Builds/SalvageNext","*",SearchOption.AllDirectories).Sum(p=>new FileInfo(p).Length);
            }
            catch(Exception e){row["passed"]=false;row["error"]=e.ToString();}
            finally{Selection.objects=selection.Select(GlobalObjectId.GlobalObjectIdentifierToObjectSlow).Where(o=>o).ToArray();pending=false;row["seconds"]=timer.Elapsed.TotalSeconds;File.WriteAllText(Receipt,row.ToString());}
        }
    }
}
