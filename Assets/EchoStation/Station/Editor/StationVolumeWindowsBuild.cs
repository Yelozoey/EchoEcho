using System;
using System.IO;
using System.Linq;
using EchoStation.Rendering.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace EchoStation.Station.Editor
{
 public static class StationVolumeWindowsBuild
 {
  private static GlobalObjectId[] selection=Array.Empty<GlobalObjectId>();private static int warmups;private static bool pending;
  [MenuItem("Tools/EchoStation/Stage 2/P03/Build Volume GPU Test")]
  public static void Schedule(){SceneSafetyGuard.RequireClean();if(pending||BuildPipeline.isBuildingPlayer)throw new InvalidOperationException("Build already pending");pending=true;selection=Selection.objects.Select(GlobalObjectId.GetGlobalObjectIdSlow).ToArray();Selection.objects=Array.Empty<UnityEngine.Object>();ActiveEditorTracker.sharedTracker.ForceRebuild();warmups=2;EditorApplication.update+=Queue;}
  private static void Queue(){if(--warmups>0)return;EditorApplication.update-=Queue;EditorApplication.update+=Build;}
  private static void Build()
  {
   EditorApplication.update-=Build;var frameStats=PlayerSettings.enableFrameTimingStats;var evidence=NextEvidence();var result=new Result();try{
    SceneSafetyGuard.RequireClean();if(!GraphicsSettings.currentRenderPipeline||GraphicsSettings.currentRenderPipeline.name!="ES_S2_ForwardHDR")throw new InvalidOperationException("S2 Forward pipeline required");PlayerSettings.enableFrameTimingStats=true;
    const string output="Builds/Tests/S2-P03/EchoStationVolumes.exe";Directory.CreateDirectory(Path.GetDirectoryName(output));var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{StationLayout.ScenePath},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
    var notices=Path.Combine(Path.GetDirectoryName(output),"ThirdPartyNotices","Noto");Directory.CreateDirectory(notices);foreach(var file in new[]{"LICENSE.txt","COPYRIGHT.txt"})File.Copy("Assets/EchoStation/ThirdParty/Noto/"+file,Path.Combine(notices,file),true);
    result.result=report.summary.result.ToString();result.errors=report.summary.totalErrors;result.warnings=report.summary.totalWarnings;result.seconds=report.summary.totalTime.TotalSeconds;result.output=Path.GetFullPath(output);result.messages=report.steps.SelectMany(s=>s.messages).Where(m=>m.type==LogType.Error||m.type==LogType.Warning).Select(m=>m.content).ToArray();
   }catch(Exception e){result.result="Exception";result.messages=new[]{e.ToString()};}
   finally{PlayerSettings.enableFrameTimingStats=frameStats;Selection.objects=selection.Select(GlobalObjectId.GlobalObjectIdentifierToObjectSlow).Where(o=>o).ToArray();selection=Array.Empty<GlobalObjectId>();pending=false;ActiveEditorTracker.sharedTracker.ForceRebuild();AssetDatabase.SaveAssets();result.frameStatsRestored=PlayerSettings.enableFrameTimingStats==frameStats;File.WriteAllText(evidence,JsonUtility.ToJson(result,true));}
  }
  private static string NextEvidence(){const string folder="Docs/Tests/Stage2/Evidence/S2-P03";Directory.CreateDirectory(folder);for(var i=1;;i++){var path=folder+"/D-PlayerBuild-"+i.ToString("00")+".json";if(!File.Exists(path))return path;}}
  [Serializable]private class Result{public string result,output;public int errors,warnings;public bool frameStatsRestored;public double seconds;public string[] messages;}
 }
}
