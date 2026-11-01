using System;
using System.Linq;
using UnityEngine;
namespace EchoStation.InfiltrationMetal.Tests
{
    public sealed class MetalTestScope:IDisposable
    {
        readonly bool background;readonly int fps;readonly AudioListener[] audio;readonly float scale;
        public MetalTestScope(){background=Application.runInBackground;fps=Application.targetFrameRate;scale=Time.timeScale;Application.runInBackground=true;Application.targetFrameRate=60;Time.timeScale=1;audio=UnityEngine.Object.FindObjectsByType<AudioListener>().Where(a=>a.enabled).ToArray();foreach(var a in audio)a.enabled=false;}
        public void Dispose(){foreach(var a in audio)if(a)a.enabled=true;Application.runInBackground=background;Application.targetFrameRate=fps;Time.timeScale=scale;}
    }
}
