using System;
using System.Collections;
using System.Text;
using EchoStation.Player;
using EchoStation.Player.Editor;
using EchoStation.Tests.Fixtures;
using EchoStation.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace EchoStation.Tests.Player
{
    public class JournalPanelTests
    {
        private Scene scene,previous;private bool background;private SyntheticInputScope inputs;private GameObject actor;private PlayerInputReader reader;private PlayerJournal journal;private JournalPanel panel;
        private void Setup(bool move=false)
        {
            background=Application.runInBackground;Application.runInBackground=true;previous=SceneManager.GetActiveScene();scene=SceneManager.CreateScene("JournalPanelFixture",new CreateSceneParameters(LocalPhysicsMode.Physics3D));SceneManager.SetActiveScene(scene);inputs=new SyntheticInputScope();var floor=new GameObject("Floor").AddComponent<BoxCollider>();floor.transform.position=new Vector3(0,-.1f,0);floor.size=new Vector3(20,.2f,20);actor=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabSetup.PrefabPath));actor.transform.position=new Vector3(0,.03f,0);actor.GetComponentInChildren<Camera>().enabled=false;actor.GetComponent<FirstPersonMotor>().enabled=move;reader=actor.GetComponent<PlayerInputReader>();reader.SetApplicationFocus(true);reader.SetPointerCaptured(true);journal=actor.GetComponent<PlayerJournal>();panel=actor.GetComponent<JournalPanel>();scene.GetPhysicsScene().Simulate(.02f);
        }
        private IEnumerator Press(Key key){InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState());yield return R04ShadowProbe.Frame();InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState(key));yield return R04ShadowProbe.Frame();}
        private static IEnumerator Observe(float seconds){var start=Time.time;var timer=System.Diagnostics.Stopwatch.StartNew();while(Time.time-start<seconds){if(timer.Elapsed.TotalSeconds>5)Assert.Fail("Journal UI clock stalled.");yield return R04ShadowProbe.Frame();}}
        [UnityTest]public IEnumerator P05_C01_ActualJAndEscapeOwnFocusAndSuppressGameplayUntilFreshClick()
        {
            yield return new EnterPlayMode();Setup(true);yield return Observe(.15f);yield return Press(Key.J);Assert.That(panel.IsOpen&&reader.UiBlocked,Is.True);Assert.That(reader.PointerCaptured,Is.False);var held=actor.transform.position;InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState(Key.W,Key.F,Key.E,Key.Space));yield return Observe(.3f);Assert.That(Vector3.Distance(held,actor.transform.position),Is.LessThan(.001f));Assert.That(actor.GetComponent<PlayerFlashlight>().IsOn,Is.False);Assert.That(reader.Move,Is.EqualTo(Vector2.zero));Assert.That(reader.JumpPressed||reader.InteractPressed,Is.False);yield return Press(Key.Escape);Assert.That(panel.IsOpen||reader.UiBlocked,Is.False);Assert.That(reader.PointerCaptured,Is.False);InputSystem.QueueStateEvent(inputs.Mouse,new MouseState{buttons=1});yield return R04ShadowProbe.Frame();Assert.That(reader.CanPlay,Is.True);
        }
        [UnityTest]public IEnumerator P05_C02_FilterButtonsSelectLiteralFullTextAndMouseWheelScrollsWithoutRowGrowth()
        {
            yield return new EnterPlayMode();Setup();var text=new StringBuilder("<color=red>标签按原文显示</color>\n");for(var i=0;i<80;i++)text.Append("观测记录").Append(i).Append("：微粒浓度变化需要与设备记录交叉核对。\n");text.Append("末尾记录：读到这里。");var full=text.ToString();journal.Store.Add(new JournalEntry("ui_long_note","长篇观测笔记",full,"记录多次环境观测。",ClueKind.Note));journal.Store.Add(new JournalEntry("ui_short_note","简短笔记","完整的短篇原文。","短篇摘要。",ClueKind.Note));journal.Store.Add(new JournalEntry("ui_data","采样数据","完整采样数据内容。","数据摘要。",ClueKind.Data));journal.Store.Add(new JournalEntry("ui_scan","扫描结果","完整扫描记录正文。","扫描摘要。",ClueKind.Scan));Assert.That(panel.SetOpen(true),Is.True);Assert.That(panel.VisibleCount,Is.EqualTo(4));var allocated=panel.AllocatedRows;panel.FilterButton(ClueKind.Data).onClick.Invoke();Assert.That(panel.VisibleCount,Is.EqualTo(1));Assert.That(panel.DisplayedTitle,Is.EqualTo("采样数据"));panel.FilterButton(ClueKind.Note).onClick.Invoke();Assert.That(panel.VisibleCount,Is.EqualTo(2));panel.EntryButton("ui_long_note").onClick.Invoke();Assert.That(panel.DisplayedFullText,Is.EqualTo(full));Assert.That(panel.BodyScroll.content.GetComponent<Text>().supportRichText,Is.False);Assert.That(panel.BodyScroll.content.rect.height,Is.GreaterThan(panel.BodyScroll.viewport.rect.height+200));
            var point=RectTransformUtility.WorldToScreenPoint(null,panel.BodyScroll.viewport.TransformPoint(panel.BodyScroll.viewport.rect.center));InputSystem.QueueStateEvent(inputs.Mouse,new MouseState{position=point});yield return R04ShadowProbe.Frame();InputSystem.QueueStateEvent(inputs.Mouse,new MouseState{position=point,scroll=new Vector2(0,-120)});yield return R04ShadowProbe.Frame();yield return R04ShadowProbe.Frame();Assert.That(panel.BodyScroll.verticalNormalizedPosition,Is.LessThan(.99f),"Actual UI mouse-wheel route must scroll the reading viewport.");for(var i=0;i<10;i++){panel.FilterButton(ClueKind.Scan).onClick.Invoke();panel.FilterButton(null).onClick.Invoke();}Assert.That(panel.AllocatedRows,Is.EqualTo(allocated));Assert.That(panel.Canvas.renderMode,Is.EqualTo(RenderMode.ScreenSpaceOverlay));
        }
        [UnityTest]public IEnumerator P05_C03_DisablingAndDestroyingPanelReleaseFocusCanvasAndOwnedEventSystem()
        {
            yield return new EnterPlayMode();Setup();panel.SetOpen(true);panel.enabled=false;Assert.That(reader.UiBlocked,Is.False);Assert.That(panel.Canvas.gameObject.activeSelf,Is.False);panel.enabled=true;Assert.That(panel.IsOpen,Is.False);Assert.That(panel.SetOpen(true),Is.True);var ownedCanvas=panel.Canvas.gameObject;var eventSystem=EventSystem.current;Object.Destroy(panel);yield return R04ShadowProbe.Frame();yield return R04ShadowProbe.Frame();Assert.That(!ownedCanvas,Is.True);Assert.That(!eventSystem,Is.True);Assert.That(reader.UiBlocked,Is.False);Assert.DoesNotThrow(()=>journal.Store.Add(new JournalEntry("after_ui_destroy","日志仍可归档","面板销毁后正常保存到日志数据。","销毁后摘要。",ClueKind.Note)));
        }
        [UnityTearDown]public IEnumerator After()
        {
            if(inputs!=null){inputs.Dispose();Assert.That(inputs.IsRestored,Is.True);inputs=null;}if(Application.isPlaying){if(previous.IsValid())SceneManager.SetActiveScene(previous);var operation=scene.IsValid()?SceneManager.UnloadSceneAsync(scene):null;var timer=System.Diagnostics.Stopwatch.StartNew();while(operation!=null&&!operation.isDone){if(timer.Elapsed.TotalSeconds>5)throw new TimeoutException("Journal panel fixture unload timeout.");yield return null;}Application.runInBackground=background;yield return new ExitPlayMode();}
        }
    }
}
