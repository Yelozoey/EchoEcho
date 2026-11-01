using System;
using System.IO;
using System.Linq;
using EchoStation.InfiltrationArt.Editor;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace EchoStation.InfiltrationArt.Tests
{
    // Additive PlayMode tests must not mix the runner's listener with the sample listener.
    public sealed class ArtSampleAudioScope : IDisposable
    {
        readonly AudioListener[] previous;
        public ArtSampleAudioScope()
        {
            previous=UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Where(a=>a.enabled).ToArray();
            var rows=new JArray(previous.Select(a=>new JObject{{"name",a.name},{"scene",a.gameObject.scene.path},{"enabled",a.enabled}}));
            File.WriteAllText(ArtSampleAssets.Evidence+"/RunnerListeners_"+Guid.NewGuid().ToString("N").Substring(0,8)+".json",rows.ToString());
            foreach(var listener in previous)listener.enabled=false;
        }
        public void Dispose()
        {
            foreach(var listener in previous)if(listener){listener.enabled=true;Assert.That(listener.enabled,Is.True);}
        }
    }
}
