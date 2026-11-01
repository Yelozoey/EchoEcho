using System;
using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.InfiltrationLevel;
using EchoStation.InfiltrationRefinement.Editor;
using EchoStation.Salvage;
using EchoStation.Tests.Fixtures;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EchoStation.InfiltrationRefinement.Tests
{
    public sealed class RefinementViewTests
    {
        Scene previous,sample;RefinementTestScope scope;
        public static readonly Vector3[] Positions={new Vector3(0,1.6f,-14),new Vector3(-12,1.6f,-5),new Vector3(12,1.6f,-7),new Vector3(0,1.6f,14),new Vector3(0,1.6f,-5),new Vector3(-12,1.6f,8)};
        public static readonly Vector3[] Targets={new Vector3(0,1.8f,-7),new Vector3(-12,1.2f,2),new Vector3(12,1.2f,2),new Vector3(0,1.3f,7),new Vector3(1,3,0),new Vector3(-17.5f,2.6f,10.5f)};
        [UnityTest,Timeout(180000)]
        public IEnumerator SixActualViewsExposeFunctionalDetailsAndRoutes()
        {
            yield return new EnterPlayMode();scope=new RefinementTestScope();previous=SceneManager.GetActiveScene();sample=EditorSceneManager.LoadSceneInPlayMode(RefinementScene.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));while(!sample.isLoaded)yield return null;SceneManager.SetActiveScene(sample);var root=RefinementScene.World(sample);root.GetComponentInChildren<SalvageMotor>().enabled=false;root.GetComponentInChildren<EchoStation.Player.PlayerInputReader>().enabled=false;var camera=root.GetComponentInChildren<Camera>();camera.scene=sample;
            bool final=File.Exists(RefinementScene.Evidence+"/LightingPolished_A.json")&&File.Exists(RefinementScene.Evidence+"/BakeJob_A.json");
            foreach(var a in root.GetComponentsInChildren<Animator>())a.enabled=false;
            root.GetComponentInChildren<UnityEngine.Rendering.Volume>().profile.TryGet<UnityEngine.Rendering.Universal.FilmGrain>(out var grain);grain.intensity.value=0;
            string version=Guid.NewGuid().ToString("N").Substring(0,8);var rows=new JArray();
            for(int i=0;i<Positions.Length;i++)
            {
                camera.transform.position=Positions[i];camera.transform.LookAt(Targets[i]);for(int f=0;f<4;f++)yield return null;
                string path=RefinementScene.Evidence+"/ArtView_"+i+"_"+version+".png";Color[] pixels;using(var capture=new SceneFrameCapture(camera))pixels=capture.Capture(path);
                double mean=pixels.Average(p=>.2126*p.r+.7152*p.g+.0722*p.b);rows.Add(new JObject{{"view",i},{"meanLinear",mean},{"path",path},{"usesInheritedA01BakeForD01Only",!final},{"bakedFinalScene",final}});File.WriteAllText(RefinementScene.Evidence+"/ArtViews_"+version+".json",rows.ToString());Assert.That(mean,Is.GreaterThan(.005),"No black model review image");
            }
        }
        [UnityTearDown]public IEnumerator Close(){if(!Application.isPlaying)yield break;if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);if(sample.IsValid()&&sample.isLoaded){var u=SceneManager.UnloadSceneAsync(sample);while(!u.isDone)yield return null;}scope?.Dispose();scope=null;yield return new ExitPlayMode();}
    }
}
