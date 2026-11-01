using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EchoStation.Echo;
using EchoStation.World;
using EchoStation.World.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace EchoStation.Tests.Echo
{
    public class IntentContractTests
    {
        private sealed class Sink:IIntentSink
        {
            public readonly List<BehaviorIntent> Behaviors=new List<BehaviorIntent>();public readonly List<string> Messages=new List<string>();public bool Reject;
            public bool TryBeginBehavior(BehaviorIntent intent){if(Reject)return false;Behaviors.Add(intent);return true;}
            public void Speak(string content,bool refusal)=>Messages.Add((refusal?"refuse:":"answer:")+content);
        }
        private readonly List<GameObject> created=new List<GameObject>();private bool previousLlm;
        [SetUp]public void Before()=>previousLlm=GameRuntimeOptions.UseLLM;
        private WorldTarget Target(TargetRegistry registry,string id,bool? investigation,bool danger=false)
        {
            var go=new GameObject("IntentFixture");go.SetActive(false);created.Add(go);var target=go.AddComponent<WorldTarget>();target.Configure(id,"目标");
            if(investigation.HasValue)go.AddComponent<EchoTargetPermissions>().Configure(investigation.Value,danger);Assert.That(registry.TryRegister(target,out _),Is.True);return target;
        }
        [Test]public void E04_01_SixIntentsRouteToFourBehaviorsAndSpeechOnlyResponses()
        {
            var registry=new TargetRegistry();Target(registry,"clue_01",true);Target(registry,"danger_01",true,true);var sink=new Sink();var dispatcher=new IntentDispatcher(registry,sink);
            foreach(var request in new[]{ScriptCommands.Follow(),ScriptCommands.Investigate("clue_01"),ScriptCommands.Wait(),ScriptCommands.Warn("danger_01"),ScriptCommands.Answer("记录已核对。"),ScriptCommands.Refuse("我不能替你操作机关。")})Assert.That(dispatcher.TryDispatch(request,out _),Is.True);
            Assert.That(sink.Behaviors.Select(b=>b.Kind),Is.EqualTo(new[]{IntentKind.Follow,IntentKind.Investigate,IntentKind.Wait,IntentKind.Warn}));Assert.That(sink.Messages.Count,Is.EqualTo(4));Assert.That(sink.Messages.Last(),Does.StartWith("refuse:"));
            Assert.That(sink.Behaviors[1].Target.Id,Is.EqualTo("clue_01"));Assert.That(sink.Behaviors[3].Target.Id,Is.EqualTo("danger_01"));
        }
        [Test]public void E04_02_InvalidFieldsTargetsAndUnknownIntentHaveNoExecutorSideEffects()
        {
            var registry=new TargetRegistry();Target(registry,"clue_01",true);var sink=new Sink();var dispatcher=new IntentDispatcher(registry,sink);
            var invalid=new[]{null,new IntentRequest((IntentKind)999),new IntentRequest(IntentKind.Follow,content:"额外参数"),new IntentRequest(IntentKind.Wait,targetName:"clue_01"),
                new IntentRequest(IntentKind.Investigate,targetName:"clue_01"),new IntentRequest(IntentKind.Investigate,targetName:"clue_01",dangerTarget:"clue_01",content:"调查。"),
                ScriptCommands.Investigate("missing_01"),ScriptCommands.Investigate("Clue_01"),ScriptCommands.Answer(" "),ScriptCommands.Answer(new string('字',281)),new IntentRequest(IntentKind.Refuse,targetName:"clue_01",content:"拒绝。")};
            foreach(var request in invalid)Assert.That(dispatcher.TryDispatch(request,out var rejection),Is.False,rejection.ToString());Assert.That(sink.Behaviors.Count+sink.Messages.Count,Is.Zero);
        }
        [Test]public void E04_03_DefaultDenyDoesNotGrantDoorOrMechanismControl()
        {
            var registry=new TargetRegistry();var door=Target(registry,"door_01",null);var original=door.transform.position;var sink=new Sink();var dispatcher=new IntentDispatcher(registry,sink);
            Assert.That(dispatcher.TryDispatch(ScriptCommands.Investigate("door_01"),out var rejection),Is.False);Assert.That(rejection,Is.EqualTo(IntentRejection.ForbiddenTarget));
            Assert.That(dispatcher.TryDispatch(ScriptCommands.Warn("door_01"),out _),Is.False);Assert.That(door.transform.position,Is.EqualTo(original));Assert.That(sink.Behaviors.Count+sink.Messages.Count,Is.Zero);
            Target(registry,"safe_01",true);Assert.That(dispatcher.TryDispatch(ScriptCommands.Warn("safe_01"),out _),Is.False);Target(registry,"danger_01",false,true);Assert.That(dispatcher.TryDispatch(ScriptCommands.Investigate("danger_01"),out _),Is.False);Assert.That(dispatcher.TryDispatch(ScriptCommands.Warn("danger_01"),out _),Is.True);
        }
        [Test]public void E04_04_ExecutorRefusalDoesNotEmitPrematureSuccessSpeech()
        {
            var registry=new TargetRegistry();Target(registry,"clue_01",true);var sink=new Sink{Reject=true};var dispatcher=new IntentDispatcher(registry,sink);
            Assert.That(dispatcher.TryDispatch(ScriptCommands.Investigate("clue_01"),out var rejection),Is.False);Assert.That(rejection,Is.EqualTo(IntentRejection.ExecutorRejected));Assert.That(sink.Messages.Count,Is.Zero);
        }
        [UnityTest]public IEnumerator E04_05_NewRuntimeResetsUseLlmFalseAndScriptCommandStillWorks()
        {
            GameRuntimeOptions.UseLLM=true;yield return new EnterPlayMode();Assert.That(GameRuntimeOptions.UseLLM,Is.False);var sink=new Sink();var dispatcher=new IntentDispatcher(WorldTarget.Active,sink);Assert.That(dispatcher.TryDispatch(ScriptCommands.Follow(),out _),Is.True);Assert.That(sink.Behaviors.Count,Is.EqualTo(1));
            Assert.That(typeof(IntentDispatcher).Assembly.GetReferencedAssemblies().Any(a=>a.Name.Contains("Barracuda")||a.Name.Contains("Sentis")),Is.False);yield return new ExitPlayMode();
        }
        [Test]public void E04_06_SavedCabinPermissionsOnlyExposeInformationAndOneDanger()
        {
            var scene=EditorSceneManager.OpenPreviewScene(CabinGreyboxSetup.ScenePath);
            try{var targets=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<WorldTarget>()).ToArray();Assert.That(targets.Length,Is.EqualTo(5));foreach(var target in targets){var permissions=target.GetComponent<EchoTargetPermissions>();Assert.That(permissions.CanInvestigate,Is.True);Assert.That(permissions.IsDanger,Is.EqualTo(target.Id=="power_panel_01"));}}
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
        [UnityTearDown]public IEnumerator After()
        {foreach(var go in created)if(go)Object.DestroyImmediate(go);created.Clear();if(Application.isPlaying)yield return new ExitPlayMode();GameRuntimeOptions.UseLLM=previousLlm;}
    }
}
