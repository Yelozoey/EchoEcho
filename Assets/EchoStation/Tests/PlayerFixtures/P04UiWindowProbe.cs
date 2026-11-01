using System;
using System.Collections;
using System.IO;
using EchoStation.Player;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace EchoStation.Tests.PlayerFixtures
{
    // Dedicated --p04-report scene only; no timing benchmark or physical-keyboard claim.
    public sealed class P04UiWindowProbe:MonoBehaviour
    {
        public PlayerInteraction interaction;public GameObject blocker;
        private bool running;
        [Serializable]private class Result{public bool passed,shown,hidden,internalHdr,displayHdrActive;public int width,height,onWhitePixels,offWhitePixels;public string text,font,unity,api,pipeline,error;}
        private void Update(){if(running){var input=interaction.GetComponent<PlayerInputReader>();input.SetApplicationFocus(true);input.SetPointerCaptured(true);}}
        private IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();var index=Array.IndexOf(args,"--p04-report");if(index<0||index+1>=args.Length)yield break;running=true;Application.runInBackground=true;var path=Path.GetFullPath(args[index+1]);Directory.CreateDirectory(Path.GetDirectoryName(path));var report=new Result{unity=Application.unityVersion,api=SystemInfo.graphicsDeviceType.ToString(),width=Screen.width,height=Screen.height};var run=Run(Path.GetDirectoryName(path),report);
            while(true){bool more;try{more=run.MoveNext();}catch(Exception exception){report.error=exception.ToString();break;}if(!more)break;yield return run.Current;}
            File.WriteAllText(path,JsonUtility.ToJson(report,true));running=false;if(Array.IndexOf(args,"--p04-exit")>=0)Application.Quit(report.passed?0:1);
        }
        private IEnumerator Run(string folder,Result result)
        {
            var camera=interaction.GetComponent<FirstPersonMotor>().View.GetComponent<Camera>();var prompt=interaction.GetComponent<InteractionPrompt>();for(var i=0;i<10;i++)yield return null;var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;result.pipeline=pipeline?pipeline.name:"null";result.internalHdr=pipeline&&pipeline.supportsHDR&&camera.allowHDR;result.displayHdrActive=HDROutputSettings.main!=null&&HDROutputSettings.main.active;result.text=prompt.CurrentText;result.font=RuntimeUiFont.Get().name;result.shown=prompt.IsVisible&&result.text.Contains("测试线索");yield return new WaitForEndOfFrame();result.onWhitePixels=Capture(folder+"/PromptVisible.png");blocker.SetActive(true);Physics.SyncTransforms();for(var i=0;i<5;i++)yield return null;result.hidden=!prompt.IsVisible&&string.IsNullOrEmpty(prompt.CurrentText);yield return new WaitForEndOfFrame();result.offWhitePixels=Capture(folder+"/PromptOccluded.png");result.passed=result.shown&&result.hidden&&result.onWhitePixels>100&&result.offWhitePixels<10&&result.width==1920&&result.height==1080&&result.internalHdr&&!result.displayHdrActive&&!camera.GetUniversalAdditionalCameraData().allowHDROutput&&result.api=="Direct3D11"&&result.unity=="6000.6.7f1";
        }
        private static int Capture(string path)
        {
            var image=ScreenCapture.CaptureScreenshotAsTexture();try{File.WriteAllBytes(path,image.EncodeToPNG());var pixels=image.GetPixels32();var count=0;for(var y=56;y<104;y++)for(var x=image.width/2-460;x<image.width/2+460;x++){var color=pixels[y*image.width+x];if(color.r>150&&color.g>150&&color.b>150)count++;}return count;}finally{Destroy(image);}
        }
    }
}
