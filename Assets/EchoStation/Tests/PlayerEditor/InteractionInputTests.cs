using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using EchoStation.Player;
using EchoStation.Player.Editor;
using EchoStation.Tests.Fixtures;
using EchoStation.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace EchoStation.Tests.Player
{
    public class InteractionInputTests
    {
        private Scene scene,previous;private bool background;private SyntheticInputScope inputs;private PlayerInteraction interaction;private PlayerInputReader reader;private ClueDefinition definition;private WorldTarget target;
        [Serializable]private class Result{public bool passed;public int reads;public string id,fullText,summary;}
        private void Setup(bool clue=true)
        {
            background=Application.runInBackground;Application.runInBackground=true;previous=SceneManager.GetActiveScene();scene=SceneManager.CreateScene("InteractionInputFixture",new CreateSceneParameters(LocalPhysicsMode.Physics3D));SceneManager.SetActiveScene(scene);inputs=new SyntheticInputScope();var actor=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabSetup.PrefabPath));actor.GetComponent<FirstPersonMotor>().enabled=false;actor.GetComponentInChildren<Camera>().enabled=false;reader=actor.GetComponent<PlayerInputReader>();reader.SetApplicationFocus(true);reader.SetPointerCaptured(true);interaction=actor.GetComponent<PlayerInteraction>();var go=new GameObject("ClueInputFixture");go.SetActive(false);go.transform.position=new Vector3(0,1.6f,2);var box=go.AddComponent<BoxCollider>();box.size=Vector3.one*.2f;box.isTrigger=true;target=go.AddComponent<WorldTarget>();target.Configure("input_clue_fixture","测试线索");if(clue){definition=ScriptableObject.CreateInstance<ClueDefinition>();definition.Configure(target.Id,"测试笔记","完整测试原文，保留供玩家阅读。","独立摘要。",ClueKind.Note);go.AddComponent<ClueSource>().Configure(definition,null);}go.SetActive(true);scene.GetPhysicsScene().Simulate(.02f);Directory.CreateDirectory("Docs/Tests/Stage1/Evidence/S1-P04");
        }
        private IEnumerator Press(){InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState());yield return R04ShadowProbe.Frame();InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState(Key.E));yield return R04ShadowProbe.Frame();}
        [UnityTest]public IEnumerator P04_B01_ActualEReadsOncePerEdgeWithStableFullTextAndSummary()
        {
            yield return new EnterPlayMode();Setup();var records=new List<ClueRecord>();interaction.ClueRead+=records.Add;yield return R04ShadowProbe.Frame();Assert.That(interaction.Selection.Target,Is.SameAs(target));Assert.That(interaction.Prompt,Does.Contain("[E]"));yield return Press();Assert.That(records.Count,Is.EqualTo(1));for(var i=0;i<5;i++)yield return R04ShadowProbe.Frame();Assert.That(records.Count,Is.EqualTo(1));yield return Press();Assert.That(records.Count,Is.EqualTo(2));var result=new Result{reads=records.Count,id=records[0].Id,fullText=records[0].FullText,summary=records[0].Summary};result.passed=result.id==target.Id&&result.fullText!=result.summary&&records[1].FullText==result.fullText;File.WriteAllText("Docs/Tests/Stage1/Evidence/S1-P04/ActualERead.json",JsonUtility.ToJson(result,true));Assert.That(result.passed,Is.True);
        }
        [UnityTest]public IEnumerator P04_B02_UiAndLostFocusSuppressActualEAndPrompt()
        {
            yield return new EnterPlayMode();Setup();var reads=0;interaction.ClueRead+=_=>reads++;reader.SetUiBlocked(true);yield return Press();Assert.That(reads,Is.Zero);Assert.That(interaction.Prompt,Is.Empty);reader.SetUiBlocked(false);reader.SetPointerCaptured(true);reader.SetApplicationFocus(false);yield return Press();Assert.That(reads,Is.Zero);Assert.That(interaction.Prompt,Is.Empty);reader.SetApplicationFocus(true);Assert.That(reader.PointerCaptured,Is.False);
        }
        [UnityTest]public IEnumerator P04_B03_InteractRecastsAndRejectsCachedTargetAfterWallOrDisable()
        {
            yield return new EnterPlayMode();Setup();var reads=0;interaction.ClueRead+=_=>reads++;interaction.RefreshSelection();Assert.That(interaction.Selection.Target,Is.SameAs(target));var wall=new GameObject("NewWall").AddComponent<BoxCollider>();wall.transform.position=new Vector3(0,1.6f,1);wall.size=new Vector3(1,1,.1f);scene.GetPhysicsScene().Simulate(.02f);Assert.That(interaction.TryInteract(),Is.False);Assert.That(reads,Is.Zero);wall.gameObject.SetActive(false);scene.GetPhysicsScene().Simulate(.02f);interaction.RefreshSelection();Assert.That(interaction.Selection.Target,Is.SameAs(target));target.enabled=false;Assert.That(interaction.TryInteract(),Is.False);Assert.That(reads,Is.Zero);
        }
        [UnityTest]public IEnumerator P04_B04_EquipmentOnlyReturnsInformationAndDoesNotMoveTarget()
        {
            yield return new EnterPlayMode();Setup(false);var initial=target.transform.position;var messages=new List<string>();interaction.InformationRead+=messages.Add;yield return Press();Assert.That(messages.Count,Is.EqualTo(1));Assert.That(messages[0],Does.Contain("扫描信息"));Assert.That(target.transform.position,Is.EqualTo(initial));Assert.That(interaction.Prompt,Does.Contain("查看"));
        }
        [UnityTest]public IEnumerator P04_B05_DestroyedUnityTargetCannotLeaveStalePrompt()
        {
            yield return new EnterPlayMode();Setup();interaction.RefreshSelection();Assert.That(interaction.Prompt,Is.Not.Empty);Object.Destroy(target.gameObject);yield return R04ShadowProbe.Frame();yield return R04ShadowProbe.Frame();Assert.That(interaction.Selection.IsValid,Is.False);Assert.That(interaction.Prompt,Is.Empty);Assert.That(interaction.TryInteract(),Is.False);
        }
        [UnityTearDown]public IEnumerator After()
        {
            if(inputs!=null){inputs.Dispose();Assert.That(inputs.IsRestored,Is.True);inputs=null;}if(Application.isPlaying){if(previous.IsValid())SceneManager.SetActiveScene(previous);var operation=scene.IsValid()?SceneManager.UnloadSceneAsync(scene):null;var timer=System.Diagnostics.Stopwatch.StartNew();while(operation!=null&&!operation.isDone){if(timer.Elapsed.TotalSeconds>5)throw new TimeoutException("Interaction input fixture unload timeout.");yield return null;}if(definition)Object.Destroy(definition);Application.runInBackground=background;yield return new ExitPlayMode();}
        }
    }
}
