using System;
using System.IO;
using System.Linq;
using EchoStation.Rendering.Editor;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace EchoStation.AI.ProbeEditor
{
 public static class G01ProbeSetup
 {
  public const string Scene="Assets/EchoStation/AI/ProbeScene/ES_S3_Compatibility.unity";
  private const string Folder="Docs/Tests/Stage3/Evidence/S3-G01";
  [MenuItem("Tools/EchoStation/Stage 3/G01/Prepare Isolated Scene")]
  public static void Prepare()
  {
   SceneSafetyGuard.RequireClean();if(File.Exists(Scene))throw new IOException("Probe scene exists; preserve it");
   Directory.CreateDirectory(Path.GetDirectoryName(Scene));AssetDatabase.Refresh();
   var source="Assets/EchoStation/Station/Scenes/ES_Station_Greybox.unity";
   var navSource=AssetDatabase.GUIDToAssetPath("7076a28f627fb964b88368ee1aad74df");var nav="Assets/EchoStation/AI/ProbeScene/S3ProbeNav.asset";
   if(string.IsNullOrEmpty(navSource)||!AssetDatabase.CopyAsset(navSource,nav)||!AssetDatabase.CopyAsset(source,Scene))throw new IOException("Independent scene/nav copy failed");
   var text=File.ReadAllText(Scene);var guid=AssetDatabase.AssetPathToGUID(nav);if(string.IsNullOrEmpty(guid))throw new IOException("Nav GUID missing");File.WriteAllText(Scene,text.Replace("7076a28f627fb964b88368ee1aad74df",guid));AssetDatabase.ImportAsset(Scene,ImportAssetOptions.ForceUpdate);
   File.WriteAllText(Path.Combine(Folder,"ScenePrepared.json"),JsonConvert.SerializeObject(new{scene=Scene,nav=nav,navSource=navSource,navGuid=guid,sourceReadOnly=true},Formatting.Indented));
  }
  [MenuItem("Tools/EchoStation/Stage 3/G01/Configure Windows Plugins")]
  public static void ConfigurePlugins()
  {
   SceneSafetyGuard.RequireClean();
   foreach(var path in new[]{"Assets/EchoStation/AI/ThirdParty/OnnxRuntime/Managed/Microsoft.ML.OnnxRuntime.dll","Assets/EchoStation/AI/ThirdParty/OnnxRuntime/Plugins/x86_64/onnxruntime.dll","Assets/EchoStation/AI/ThirdParty/OnnxRuntime/Plugins/x86_64/onnxruntime_providers_shared.dll"})
   {
    var p=AssetImporter.GetAtPath(path) as PluginImporter;if(!p)throw new IOException("Plugin importer missing: "+path);
    p.SetCompatibleWithAnyPlatform(false);p.SetCompatibleWithEditor(true);p.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows64,true);
    p.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows,false);p.SetCompatibleWithPlatform(BuildTarget.StandaloneLinux64,false);p.SetCompatibleWithPlatform(BuildTarget.StandaloneOSX,false);
    if(p.isNativePlugin){p.SetEditorData("OS","Windows");p.SetEditorData("CPU","x86_64");p.SetPlatformData(BuildTarget.StandaloneWindows64,"CPU","x86_64");}
    p.SaveAndReimport();
   }
  }
  private static int ticks;private static GlobalObjectId[] selected;
  [MenuItem("Tools/EchoStation/Stage 3/G01/Build Windows Probe")]
  public static void Schedule()
  {
   SceneSafetyGuard.RequireClean();if(BuildPipeline.isBuildingPlayer)throw new InvalidOperationException("Build busy");
   selected=Selection.objects.Select(GlobalObjectId.GetGlobalObjectIdSlow).ToArray();Selection.objects=Array.Empty<UnityEngine.Object>();ActiveEditorTracker.sharedTracker.ForceRebuild();ticks=2;EditorApplication.update+=Wait;
  }
  private static void Wait(){if(--ticks>0)return;EditorApplication.update-=Wait;EditorApplication.update+=Build;}
  private static void Build()
  {
   EditorApplication.update-=Build;var previous=PlayerSettings.enableFrameTimingStats;object result;
   try
   {
    SceneSafetyGuard.RequireClean();if(!File.Exists(Scene))throw new IOException("Prepare scene first");PlayerSettings.enableFrameTimingStats=true;
    const string output="Builds/Tests/S3-G01/EchoStationAIProbe.exe";Directory.CreateDirectory(Path.GetDirectoryName(output));
    var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Scene},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
    result=new{result=report.summary.result.ToString(),seconds=report.summary.totalTime.TotalSeconds,errors=report.summary.totalErrors,warnings=report.summary.totalWarnings,messages=report.steps.SelectMany(s=>s.messages).Where(m=>m.type==LogType.Error||m.type==LogType.Warning).Select(m=>m.content).ToArray()};
   }
   catch(Exception error){result=new{result="Exception",error=error.ToString()};}
   finally
   {
    PlayerSettings.enableFrameTimingStats=previous;Flush(previous);Selection.objects=selected.Select(GlobalObjectId.GlobalObjectIdentifierToObjectSlow).Where(o=>o).ToArray();ActiveEditorTracker.sharedTracker.ForceRebuild();
   }
   File.WriteAllText(Path.Combine(Folder,"Build_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmss")+".json"),JsonConvert.SerializeObject(result,Formatting.Indented));
  }
  private static void Flush(bool value)
  {
   const string path="ProjectSettings/ProjectSettings.asset";var text=File.ReadAllText(path);var regex=new System.Text.RegularExpressions.Regex(@"(?m)^  enableFrameTimingStats: ([01])\r?$");var matches=regex.Matches(text);if(matches.Count!=1)throw new InvalidOperationException("Expected one FrameTiming scalar");var digit=matches[0].Groups[1];var expected=value?"1":"0";
   if(digit.Value!=expected){var update=text.Substring(0,digit.Index)+expected+text.Substring(digit.Index+digit.Length);var bytes=File.ReadAllBytes(path);var bom=bytes.Length>=3&&bytes[0]==239&&bytes[1]==187&&bytes[2]==191;var temp=path+".echostation.tmp";if(File.Exists(temp))throw new IOException("Unresolved setting temp preserved");File.WriteAllText(temp,update,new System.Text.UTF8Encoding(bom));File.Replace(temp,path,null);}
  }
 }
}
