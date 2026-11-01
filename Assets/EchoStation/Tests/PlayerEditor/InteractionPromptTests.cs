using System;
using System.Collections;
using EchoStation.Player;
using EchoStation.Player.Editor;
using EchoStation.Tests.Fixtures;
using EchoStation.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace EchoStation.Tests.Player
{
    public class InteractionPromptTests
    {
        private Scene scene,previous;private bool background;private SyntheticInputScope inputs;
        [Test]public void P04_C02_BundledDynamicFontIncludesRequiredChineseGlyphs()
        {
            var font=Resources.Load<Font>("NotoSansCJKsc-Regular");Assert.That(font,Is.Not.Null);Assert.That(font.dynamic,Is.True);font.RequestCharactersInTexture("[E]读取测试线索设备",22,FontStyle.Normal);foreach(var character in "[E]读取测试线索设备")Assert.That(font.HasCharacter(character),Is.True,character.ToString());var importer=(TrueTypeFontImporter)AssetImporter.GetAtPath(UiFontSetup.FontPath);Assert.That(importer.includeFontData,Is.True);Assert.That(importer.fontTextureCase,Is.EqualTo(FontTextureCase.Dynamic));
        }
        [UnityTest]public IEnumerator P04_C01_RealOverlayShowsRegisteredTargetHidesForUiAndDestroysOwnedCanvas()
        {
            yield return new EnterPlayMode();background=Application.runInBackground;Application.runInBackground=true;previous=SceneManager.GetActiveScene();scene=SceneManager.CreateScene("InteractionPromptFixture",new CreateSceneParameters(LocalPhysicsMode.Physics3D));SceneManager.SetActiveScene(scene);inputs=new SyntheticInputScope();var actor=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabSetup.PrefabPath));actor.GetComponent<FirstPersonMotor>().enabled=false;actor.GetComponentInChildren<Camera>().enabled=false;var input=actor.GetComponent<PlayerInputReader>();input.SetApplicationFocus(true);input.SetPointerCaptured(true);var target=new GameObject("PromptTarget");target.SetActive(false);target.transform.position=new Vector3(0,1.6f,2);target.AddComponent<BoxCollider>().size=Vector3.one*.2f;target.AddComponent<WorldTarget>().Configure("prompt_fixture","测试线索");target.SetActive(true);scene.GetPhysicsScene().Simulate(.02f);yield return R04ShadowProbe.Frame();yield return R04ShadowProbe.Frame();var prompt=actor.GetComponent<InteractionPrompt>();Assert.That(prompt.IsVisible,Is.True);Assert.That(prompt.CurrentText,Does.Contain("测试线索"));Assert.That(prompt.Canvas.renderMode,Is.EqualTo(RenderMode.ScreenSpaceOverlay));Assert.That(prompt.Canvas.sortingOrder,Is.EqualTo(20));Assert.That(prompt.Canvas.GetComponent<CanvasScaler>().referenceResolution,Is.EqualTo(new Vector2(1920,1080)));Assert.That(prompt.Canvas.GetComponentInChildren<Text>().supportRichText,Is.False);input.SetUiBlocked(true);yield return R04ShadowProbe.Frame();yield return R04ShadowProbe.Frame();Assert.That(prompt.IsVisible,Is.False);var owned=prompt.Canvas.gameObject;Object.Destroy(prompt);yield return R04ShadowProbe.Frame();yield return R04ShadowProbe.Frame();Assert.That(!owned,Is.True);
        }
        [UnityTearDown]public IEnumerator After()
        {
            if(inputs!=null){inputs.Dispose();Assert.That(inputs.IsRestored,Is.True);inputs=null;}if(Application.isPlaying){if(previous.IsValid())SceneManager.SetActiveScene(previous);var operation=scene.IsValid()?SceneManager.UnloadSceneAsync(scene):null;var timer=System.Diagnostics.Stopwatch.StartNew();while(operation!=null&&!operation.isDone){if(timer.Elapsed.TotalSeconds>5)throw new TimeoutException("Prompt fixture unload timeout.");yield return null;}Application.runInBackground=background;yield return new ExitPlayMode();}
        }
    }
}
