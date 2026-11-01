using System;
using System.IO;
using System.Linq;
using EchoStation.Player;
using EchoStation.Rendering;
using EchoStation.Rendering.Editor;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace EchoStation.SalvageNext.Editor
{
    public static class NextEffects
    {
        public const string Evidence="Docs/Tests/SalvagePrototype/Evidence/R4-R02c",PrefabPath=FacilityAssets.Root+"/Prefabs/FX/ES_Next_FaultSparks.prefab";
        public static void Prepare()
        {
            SceneSafetyGuard.RequireClean();if(AssetDatabase.LoadMainAssetAtPath(PrefabPath))throw new InvalidOperationException("Effects already prepared.");FacilityAssets.Folder(Path.GetDirectoryName(PrefabPath).Replace('\\','/'));Directory.CreateDirectory(Evidence);File.Copy(NextLayout.ScenePath,Evidence+"/BeforeEffects.unity");var material=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")){name="ES_Next_Sparks"};material.SetColor("_BaseColor",Color.white);AssetDatabase.CreateAsset(material,FacilityAssets.Generated+"/ES_Next_Sparks.mat");AssetDatabase.SetLabels(material,new[]{"EchoStation-SalvageNext-Generated"});AssetDatabase.SaveAssetIfDirty(material);
            var go=new GameObject("FaultSparks");var system=go.AddComponent<ParticleSystem>();system.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=system.main;main.loop=false;main.playOnAwake=false;main.stopAction=ParticleSystemStopAction.None;main.duration=.5f;main.startLifetime=.35f;main.startSpeed=1.1f;main.startSize=.035f;main.startColor=new Color(1,.38f,.08f);main.gravityModifier=.7f;main.maxParticles=8;main.simulationSpace=ParticleSystemSimulationSpace.World;var emission=system.emission;emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(0,8)});var shape=system.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=20;shape.radius=.04f;system.useAutoRandomSeed=false;system.randomSeed=42;var psRenderer=system.GetComponent<ParticleSystemRenderer>();psRenderer.sharedMaterial=material;psRenderer.renderMode=ParticleSystemRenderMode.Billboard;ParticleEffectPool.Validate(system,2);var prefab=PrefabUtility.SaveAsPrefabAsset(go,PrefabPath);Object.DestroyImmediate(go);if(!prefab)throw new IOException("Spark prefab save failed.");
            var old=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(NextLayout.ScenePath,OpenSceneMode.Additive);
            try{SceneManager.SetActiveScene(scene);var root=scene.GetRootGameObjects().Single(r=>r.name=="ES_SalvageNext_World");var fx=new GameObject("NextFaultSparks");fx.transform.SetParent(root.transform,false);var pool=fx.AddComponent<ParticleEffectPool>();var so=new SerializedObject(pool);so.FindProperty("source").objectReferenceValue=prefab.GetComponent<ParticleSystem>();so.FindProperty("capacity").intValue=2;so.ApplyModifiedPropertiesWithoutUndo();fx.AddComponent<NextFaultSparks>().Configure(pool,root.GetComponentInChildren<Camera>(),root.GetComponentInChildren<PlayerInputReader>());EditorSceneManager.SaveScene(scene);File.WriteAllText(Evidence+"/Prepared_A.json",new JObject{{"poolCapacity",2},{"particlesPerSystem",8},{"localMaximumParticles",16},{"globalSystemsBudget",ParticleBudget.MaximumConcurrentSystems},{"scanUsesParticles",false}}.ToString());}
            finally{SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
