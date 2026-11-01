using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using EchoStation.Player;
using EchoStation.Salvage;
using EchoStation.SalvageNext;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace EchoStation.LLMShortTasks
{
    // Explicit standalone probe / opt-in diagnostics only. Ordinary releases do not log player text.
    public sealed class ImeProbe : MonoBehaviour
    {
        public bool standalone;
        GameTextInput field; Text state; Keyboard keyboard; string report; float next; int textEvents, compositionEvents, submissions;
        readonly JArray events=new JArray(); string last="";
        [DllImport("user32.dll")] static extern IntPtr GetActiveWindow();
        [DllImport("user32.dll")] static extern IntPtr GetKeyboardLayout(uint thread);
        [DllImport("imm32.dll")] static extern IntPtr ImmGetContext(IntPtr window);
        [DllImport("imm32.dll")] static extern bool ImmReleaseContext(IntPtr window,IntPtr context);
        [DllImport("imm32.dll")] static extern bool ImmGetOpenStatus(IntPtr context);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void OptionalAttach()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--echo-ime-report");
            if(i<0||i+1>=args.Length||FindFirstObjectByType<ImeProbe>())return;
            var go=new GameObject("IME01_ExplicitDiagnostic");var probe=go.AddComponent<ImeProbe>();probe.report=args[i+1];
        }
        IEnumerator Start()
        {
            if(standalone)Application.targetFrameRate=60;
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--echo-ime-report");if(i>=0&&i+1<args.Length)report=args[i+1];
            if(standalone)Build();
            else
            {
                var timer=Time.realtimeSinceStartup;
                while(!field){var panel=FindAnyObjectByType<NextCommandPanel>();if(panel){var session=panel.GetComponent<NextSession>();if(session&&!string.IsNullOrEmpty(report))session.ConfigureStoreForTests(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(report)),"Save"));if(panel.Open(true))field=panel.InputField;}if(Time.realtimeSinceStartup-timer>20)throw new TimeoutException("Native probe could not open owned T panel");yield return null;}
            }
            yield return null;yield return null;field.Select();field.ActivateInputField();
            field.SubmitRequested+=()=>{submissions++;Record("submit");};Record("ready");
        }
        void Build()
        {
            if(!EventSystem.current){var system=new GameObject("IME_EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));system.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();}
            var go=new GameObject("IME_Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));go.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;var scale=go.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1920,1080);
            SalvageWidgets.Label(SalvageWidgets.Rect("Title",go.transform,80,80,1720,65),"微软拼音诊断 · 输入拼音并用空格选字",32);
            state=SalvageWidgets.Label(SalvageWidgets.Rect("State",go.transform,80,165,1720,260),"",23);
            var rect=SalvageWidgets.Rect("Input",go.transform,80,450,1720,90);rect.gameObject.AddComponent<Image>().color=new Color(.08f,.16f,.22f);rect.gameObject.SetActive(false);var text=SalvageWidgets.Label(SalvageWidgets.Rect("Text",rect,18,16,1680,65),"",30);
            field=rect.gameObject.AddComponent<GameTextInput>();field.textComponent=text;field.targetGraphic=rect.GetComponent<Image>();field.characterLimit=200;field.lineType=InputField.LineType.SingleLine;rect.gameObject.SetActive(true);
            SalvageWidgets.Button(go.transform,"Refocus","重新聚焦输入框",80,590,340,()=>{field.Select();field.ActivateInputField();Record("refocus");});
            SalvageWidgets.Button(go.transform,"LegacyIme","引擎IME On 对照",460,590,340,()=>{try{UnityEngine.Input.imeCompositionMode=IMECompositionMode.On;Record("legacy-on");}catch(Exception e){Record("legacy-error-"+e.GetType().Name);}field.Select();field.ActivateInputField();});
            SalvageWidgets.Button(go.transform,"Quit","退出诊断",80,720,340,()=>Application.Quit());
        }
        void Update()
        {
            if(keyboard!=Keyboard.current){if(keyboard!=null){keyboard.onTextInput-=OnText;keyboard.onIMECompositionChange-=OnComposition;}keyboard=Keyboard.current;if(keyboard!=null){keyboard.onTextInput+=OnText;keyboard.onIMECompositionChange+=OnComposition;}}
            if(!field||Time.unscaledTime<next)return;next=Time.unscaledTime+.25f;
            var snapshot=Snapshot();string value=snapshot.ToString();if(value!=last){last=value;Record("state",snapshot);}if(state)state.text="Focused="+field.isFocused+"  Application="+Application.isFocused+"  IME request="+field.ImeMode+"\nKeyboard="+(keyboard?.keyboardLayout??"none")+"  IME selected="+(keyboard?.imeSelected.isPressed??false)+"\nText events="+textEvents+"  Composition events="+compositionEvents+"  Submits="+submissions+"\nNative context/open="+snapshot["context"]+"/"+snapshot["nativeOpen"]+"  Composition="+field.Composition;
        }
        JObject Snapshot()
        {
            var row=new JObject{{"focused",field&&field.isFocused},{"appFocused",Application.isFocused},{"imeMode",field?field.ImeMode.ToString():"none"},{"nativeBridge",field&&field.NativeImeBridgeActive},{"imeCommandResult",field?field.LastImeCommandResult:0},{"layout",keyboard?.keyboardLayout??"none"},{"imeSelected",keyboard?.imeSelected.isPressed??false},{"compositionEvents",compositionEvents},{"textEvents",textEvents},{"submissions",submissions},{"characters",field?field.text.Length:0}};
            if(Application.platform==RuntimePlatform.WindowsPlayer){row["nativeMode"]=UnityEngine.Input.imeCompositionMode.ToString();var window=GetActiveWindow();var context=window==IntPtr.Zero?IntPtr.Zero:ImmGetContext(window);try{row["window"]=window.ToInt64();row["context"]=context.ToInt64();row["nativeOpen"]=context!=IntPtr.Zero&&ImmGetOpenStatus(context);row["hkl"]=GetKeyboardLayout(0).ToInt64();}finally{if(context!=IntPtr.Zero)ImmReleaseContext(window,context);}}return row;
        }
        void OnText(char c){textEvents++;Record("text",new JObject{{"codepoint",(int)c}});}
        void OnComposition(IMECompositionString s){compositionEvents++;Record("composition",new JObject{{"length",s.ToString().Length}});}
        void Record(string kind,JObject data=null){events.Add(new JObject{{"kind",kind},{"frame",Time.frameCount},{"data",data}});if(events.Count>2000)events.RemoveAt(0);if(!string.IsNullOrEmpty(report)){Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(report)));File.WriteAllText(report,new JObject{{"unity",Application.unityVersion},{"events",events}}.ToString());}}
        void OnApplicationFocus(bool value){Record(value?"focus-gained":"focus-lost");}
        void OnDestroy(){if(keyboard!=null){keyboard.onTextInput-=OnText;keyboard.onIMECompositionChange-=OnComposition;}}
    }
}
