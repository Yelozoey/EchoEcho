using System;
using System.Collections;
using System.IO;
using System.Text;
using EchoStation.Player;
using EchoStation.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace EchoStation.Tests.PlayerFixtures
{
    public sealed class P05JournalWindowProbe:MonoBehaviour
    {
        public JournalPanel panel;
        private bool running;private Keyboard keyboard;private Mouse mouse;private InputSettings settings;private InputSettings.BackgroundBehavior oldBackground;
        [Serializable]private class Result{public bool passed,opened,noteSelected,scrolled,dataSelected,closed,internalHdr,displayHdrActive;public int width,height,noteWhitePixels;public float scrollPosition;public string unity,api,pipeline,error,font;}
        private void Update(){if(running)panel.GetComponent<PlayerInputReader>().SetApplicationFocus(true);}
        private IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();var index=Array.IndexOf(args,"--p05-report");if(index<0||index+1>=args.Length)yield break;running=true;Application.runInBackground=true;settings=InputSystem.settings;oldBackground=settings.backgroundBehavior;settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();var path=Path.GetFullPath(args[index+1]);Directory.CreateDirectory(Path.GetDirectoryName(path));var result=new Result{unity=Application.unityVersion,api=SystemInfo.graphicsDeviceType.ToString(),width=Screen.width,height=Screen.height};var run=Run(Path.GetDirectoryName(path),result);
            while(true){bool more;try{more=run.MoveNext();}catch(Exception exception){result.error=exception.ToString();break;}if(!more)break;yield return run.Current;}
            if(keyboard.added)InputSystem.RemoveDevice(keyboard);if(mouse.added)InputSystem.RemoveDevice(mouse);settings.backgroundBehavior=oldBackground;running=false;File.WriteAllText(path,JsonUtility.ToJson(result,true));if(Array.IndexOf(args,"--p05-exit")>=0)Application.Quit(result.passed?0:1);
        }
        private IEnumerator Key(Key value){InputSystem.QueueStateEvent(keyboard,new KeyboardState(value));yield return null;InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;}
        private IEnumerator Click(Button button)
        {
            if(!button)throw new InvalidOperationException("Expected UI button missing.");var rect=button.GetComponent<RectTransform>();var point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));InputSystem.QueueStateEvent(mouse,new MouseState{position=point});yield return null;InputSystem.QueueStateEvent(mouse,new MouseState{position=point,buttons=1});yield return null;InputSystem.QueueStateEvent(mouse,new MouseState{position=point});yield return null;yield return null;
        }
        private IEnumerator Run(string directory,Result result)
        {
            var splash=System.Diagnostics.Stopwatch.StartNew();while(!SplashScreen.isFinished){if(splash.Elapsed.TotalSeconds>15)throw new TimeoutException("Splash did not finish.");yield return null;}for(var i=0;i<10;i++)yield return null;
            var journal=panel.GetComponent<PlayerJournal>();var full=new StringBuilder("环境采样与巡检记录\n");for(var i=1;i<=80;i++)full.Append("观测").Append(i).Append("：温度稳定，微粒浓度变化需要与设备记录交叉核对。\n");full.Append("末尾记录：核对完成。\n<原文中的标记按文字显示>");journal.Store.Add(new JournalEntry("window_long_note","观测站巡检记录",full.ToString(),"多次观测需要交叉核对。",ClueKind.Note));journal.Store.Add(new JournalEntry("window_short_note","值班笔记","完整值班笔记：校准记录已移交。","值班摘要。",ClueKind.Note));journal.Store.Add(new JournalEntry("window_data","采样数据","采样序列07\n温度稳定，空气微粒浓度持续缓慢上升。\n当前数据需要继续核对其他观察资料。","温度与微粒浓度记录。",ClueKind.Data));journal.Store.Add(new JournalEntry("window_scan","扫描结果","Echo扫描记录：发现一处校准定位标记。","校准定位标记。",ClueKind.Scan));
            yield return Key(UnityEngine.InputSystem.Key.J);result.opened=panel.IsOpen;yield return Click(panel.FilterButton(ClueKind.Note));yield return Click(panel.EntryButton("window_long_note"));result.noteSelected=panel.SelectedId=="window_long_note"&&panel.DisplayedFullText==full.ToString();yield return new WaitForEndOfFrame();result.noteWhitePixels=Capture(directory+"/JournalNote.png");
            var viewport=panel.BodyScroll.viewport;var point=RectTransformUtility.WorldToScreenPoint(null,viewport.TransformPoint(viewport.rect.center));InputSystem.QueueStateEvent(mouse,new MouseState{position=point});yield return null;InputSystem.QueueStateEvent(mouse,new MouseState{position=point,scroll=new Vector2(0,-120)});yield return null;InputSystem.QueueStateEvent(mouse,new MouseState{position=point});for(var i=0;i<5;i++)yield return null;result.scrollPosition=panel.BodyScroll.verticalNormalizedPosition;result.scrolled=result.scrollPosition<.8f;yield return new WaitForEndOfFrame();Capture(directory+"/JournalScrolled.png");
            yield return Click(panel.FilterButton(ClueKind.Data));result.dataSelected=panel.Filter==ClueKind.Data&&panel.VisibleCount==1&&panel.DisplayedTitle=="采样数据";yield return new WaitForEndOfFrame();Capture(directory+"/JournalData.png");yield return Key(UnityEngine.InputSystem.Key.Escape);result.closed=!panel.IsOpen&&!panel.GetComponent<PlayerInputReader>().UiBlocked&&!panel.GetComponent<PlayerInputReader>().PointerCaptured;yield return new WaitForEndOfFrame();Capture(directory+"/JournalClosed.png");
            var camera=panel.GetComponent<FirstPersonMotor>().View.GetComponent<Camera>();var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;result.pipeline=pipeline?pipeline.name:"null";result.internalHdr=pipeline&&pipeline.supportsHDR&&camera.allowHDR;result.displayHdrActive=HDROutputSettings.main!=null&&HDROutputSettings.main.active;result.font=RuntimeUiFont.Get().name;result.passed=result.opened&&result.noteSelected&&result.scrolled&&result.dataSelected&&result.closed&&result.noteWhitePixels>1000&&result.internalHdr&&!result.displayHdrActive&&!camera.GetUniversalAdditionalCameraData().allowHDROutput&&result.width==1920&&result.height==1080&&result.api=="Direct3D11"&&result.unity=="6000.6.7f1";
        }
        private static int Capture(string path)
        {
            var image=ScreenCapture.CaptureScreenshotAsTexture();try{File.WriteAllBytes(path,image.EncodeToPNG());var pixels=image.GetPixels32();var count=0;for(var y=140;y<940;y++)for(var x=240;x<1680;x++){var color=pixels[y*image.width+x];if(color.r>150&&color.g>150&&color.b>150)count++;}return count;}finally{Destroy(image);}
        }
    }
}
