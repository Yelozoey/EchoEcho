using EchoStation.Salvage;
using EchoStation.Echo;
using UnityEngine;
namespace EchoStation.SalvageNext
{
    public sealed class UnityDisplayBackend:IDisplayBackend
    {
        readonly NextSession session;
        public UnityDisplayBackend(NextSession session){this.session=session;}
        public DisplaySelection Capture()=>new DisplaySelection(session.Quality,Screen.width,Screen.height,Screen.fullScreenMode==FullScreenMode.Windowed?DisplayWindowMode.Windowed:Screen.fullScreenMode==FullScreenMode.ExclusiveFullScreen?DisplayWindowMode.Exclusive:DisplayWindowMode.Borderless);
        public bool TryApply(DisplaySelection value,out string error)
        {
            error="";if(Application.isEditor){error="显示模式请在Windows试玩版中测试。";return false;}
            session.SetQuality(value.Quality);Screen.SetResolution(value.Width,value.Height,value.Mode==DisplayWindowMode.Windowed?FullScreenMode.Windowed:value.Mode==DisplayWindowMode.Exclusive?FullScreenMode.ExclusiveFullScreen:FullScreenMode.FullScreenWindow);GameRuntimeOptions.UseLLM=false;return true;
        }
    }
}
