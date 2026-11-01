using System;
using System.IO;
using System.Linq;
using EchoStation.Rendering.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace EchoStation.Station.Editor
{
 public static class StationWindowsBuild
 {
  private static GlobalObjectId[] selection=Array.Empty<GlobalObjectId>();private static int ticks;private static bool pending,performance,release;
  [MenuItem("Tools/EchoStation/Stage 2/V02/Build Windows Interaction Test")]
  public static void Schedule()=>Enqueue(false,false);
  [MenuItem("Tools/EchoStation/Stage 2/V03/Build Performance Test")]public static void Performance()=>Enqueue(true,false);
  [MenuItem("Tools/EchoStation/Stage 2/V03/Build S2 Delivery")]public static void Delivery()=>Enqueue(false,true);
  private static void Enqueue(bool measure,bool final){SceneSafetyGuard.RequireClean();if(pending||BuildPipeline.isBuildingPlayer)throw new InvalidOperationException("Build already pending");performance=measure;release=final;selection=Selection.objects.Select(GlobalObjectId.GetGlobalObjectIdSlow).ToArray();Selection.objects=Array.Empty<UnityEngine.Object>();ActiveEditorTracker.sharedTracker.ForceRebuild();pending=true;ticks=2;EditorApplication.update+=Queue;}
  private static void Queue(){if(--ticks>0)return;EditorApplication.update-=Queue;EditorApplication.update+=Build;}
  private static void Build()
  {
   EditorApplication.update-=Build;var frameStats=PlayerSettings.enableFrameTimingStats;var result=new Result{performance=performance,release=release,frameStatsBefore=frameStats};var folder="Docs/Tests/Stage2/Evidence/"+(performance||release?"S2-V03":"S2-V02");Directory.CreateDirectory(folder);var evidence=Path.Combine(folder,"Build_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmss")+".json");
   try{SceneSafetyGuard.RequireClean();if(!GraphicsSettings.currentRenderPipeline||GraphicsSettings.currentRenderPipeline.name!="ES_S2_ForwardHDR")throw new InvalidOperationException("S2 pipeline required");if(performance)PlayerSettings.enableFrameTimingStats=true;var output=release?"Builds/S2/EchoStation.exe":performance?"Builds/Tests/S2-V03/EchoStationPerformance.exe":"Builds/Tests/S2-V02/EchoStationTests.exe";Directory.CreateDirectory(Path.GetDirectoryName(output));var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{StationLayout.ScenePath},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=release?BuildOptions.None:BuildOptions.Development});result.result=report.summary.result.ToString();result.output=Path.GetFullPath(output);result.seconds=report.summary.totalTime.TotalSeconds;result.errors=report.summary.totalErrors;result.warnings=report.summary.totalWarnings;result.messages=report.steps.SelectMany(s=>s.messages).Where(m=>m.type==LogType.Error||m.type==LogType.Warning).Select(m=>m.content).ToArray();var notices=Path.Combine(Path.GetDirectoryName(output),"ThirdPartyNotices/Noto");Directory.CreateDirectory(notices);foreach(var file in new[]{"LICENSE.txt","COPYRIGHT.txt"})File.Copy("Assets/EchoStation/ThirdParty/Noto/"+file,Path.Combine(notices,file),true);}
   catch(Exception e){result.result="Exception";result.messages=new[]{e.ToString()};}
   finally{PlayerSettings.enableFrameTimingStats=frameStats;try{FlushFrameTiming(frameStats);result.frameStatsRestored=PlayerSettings.enableFrameTimingStats==frameStats;}catch(Exception e){result.settingsError=e.Message;result.frameStatsRestored=false;}Selection.objects=selection.Select(GlobalObjectId.GlobalObjectIdentifierToObjectSlow).Where(o=>o).ToArray();result.selectionRestored=Selection.objects.Length==selection.Length;selection=Array.Empty<GlobalObjectId>();pending=false;ActiveEditorTracker.sharedTracker.ForceRebuild();File.WriteAllText(evidence,JsonUtility.ToJson(result,true));}
  }
  // PlayerSettings setters restore memory but do not flush the disk file after BuildPlayer.
  // Persist only this task-owned scalar; never SaveAssets and save unrelated dirty user assets.
  private static void FlushFrameTiming(bool value)
  {
   const string path="ProjectSettings/ProjectSettings.asset";var text=File.ReadAllText(path);var regex=new System.Text.RegularExpressions.Regex(@"(?m)^  enableFrameTimingStats: ([01])\r?$");var matches=regex.Matches(text);if(matches.Count!=1)throw new InvalidOperationException("Expected exactly one FrameTiming setting");var digit=matches[0].Groups[1];var expected=value?"1":"0";if(digit.Value!=expected){var updated=text.Substring(0,digit.Index)+expected+text.Substring(digit.Index+digit.Length);var bytes=File.ReadAllBytes(path);var bom=bytes.Length>=3&&bytes[0]==239&&bytes[1]==187&&bytes[2]==191;var temp=path+".echostation.tmp";if(File.Exists(temp))throw new InvalidOperationException("Unresolved settings temp file preserved");File.WriteAllText(temp,updated,new System.Text.UTF8Encoding(bom));File.Replace(temp,path,null);}if(!regex.IsMatch(File.ReadAllText(path))||regex.Match(File.ReadAllText(path)).Groups[1].Value!=expected)throw new InvalidOperationException("Disk FrameTiming restore failed");
  }
  [Serializable]private class Result{public string result,output,settingsError;public int errors,warnings;public bool selectionRestored,frameStatsBefore,frameStatsRestored,performance,release;public double seconds;public string[] messages;}
 }
}
