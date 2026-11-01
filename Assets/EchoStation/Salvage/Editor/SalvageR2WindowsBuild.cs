using System;
using System.IO;
using System.Linq;
using EchoStation.Rendering.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace EchoStation.Salvage.Editor
{
    public static class SalvageR2WindowsBuild
    {
        public static string ReceiptPath{get;private set;}static bool pending;static int ticks;static GlobalObjectId[] selection;
        [MenuItem("Tools/EchoStation/Salvage/R2-V01/Build Windows")]
        public static void Schedule(){SceneSafetyGuard.RequireClean();if(pending||BuildPipeline.isBuildingPlayer)throw new InvalidOperationException("Build busy.");ReceiptPath="Docs/Tests/SalvagePrototype/Evidence/R2-V01b/WindowsBuild_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmss")+"_"+System.Guid.NewGuid().ToString("N")+".json";pending=true;selection=Selection.objects.Select(GlobalObjectId.GetGlobalObjectIdSlow).ToArray();Selection.objects=Array.Empty<UnityEngine.Object>();ActiveEditorTracker.sharedTracker.ForceRebuild();ticks=2;EditorApplication.update+=Queue;}
        static void Queue(){if(--ticks>0)return;EditorApplication.update-=Queue;EditorApplication.update+=Build;}
        static void Build()
        {
            EditorApplication.update-=Build;
            var result=new Receipt();
            try
            {
                SceneSafetyGuard.RequireClean();if(!GraphicsSettings.currentRenderPipeline||GraphicsSettings.currentRenderPipeline.name!="ES_S2_ForwardHDR")throw new InvalidOperationException("Require confirmed S2 render host.");
                const string output="Builds/Salvage/R2/EchoStationSalvage.exe";Directory.CreateDirectory(Path.GetDirectoryName(output));
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{SalvageWorldSetup.ScenePath},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});result.result=report.summary.result.ToString();result.seconds=report.summary.totalTime.TotalSeconds;result.errors=report.summary.totalErrors;result.warnings=report.summary.totalWarnings;result.messages=report.steps.SelectMany(s=>s.messages).Where(m=>m.type==LogType.Error||m.type==LogType.Warning).Select(m=>m.content).ToArray();result.output=Path.GetFullPath(output);
                var notices="Builds/Salvage/R2/ThirdPartyNotices";Directory.CreateDirectory(notices+"/Noto");foreach(var file in new[]{"LICENSE.txt","COPYRIGHT.txt"})File.Copy("Assets/EchoStation/ThirdParty/Noto/"+file,notices+"/Noto/"+file,true);
                File.Copy(SalvageAssetImport.Root+"/ThirdParty/Quaternius/License_Standard.txt",notices+"/Quaternius_CC0.txt",true);File.Copy(SalvageAssetImport.Root+"/ThirdParty/PolyHaven/MetalToolbox/LICENSE_CC0.txt",notices+"/MetalToolbox_CC0.txt",true);
            }
            catch(Exception e){result.result="Exception";result.messages=new[]{e.ToString()};}
            finally{Selection.objects=selection.Select(GlobalObjectId.GlobalObjectIdentifierToObjectSlow).Where(o=>o).ToArray();pending=false;Directory.CreateDirectory("Docs/Tests/SalvagePrototype/Evidence/R2-V01b");File.WriteAllText(ReceiptPath,JsonUtility.ToJson(result,true));}
        }
        [Serializable]class Receipt{public string result,output;public int errors,warnings;public double seconds;public string[] messages;}
    }
}
