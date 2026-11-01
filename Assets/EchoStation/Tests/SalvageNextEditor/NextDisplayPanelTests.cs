using System;
using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.Player;
using EchoStation.Salvage;
using EchoStation.SalvageNext.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace EchoStation.SalvageNext.Tests
{
    public sealed class NextDisplayPanelTests
    {
        sealed class Backend:IDisplayBackend{public DisplaySelection Current=DisplaySelection.Default();public DisplaySelection Capture()=>Current;public bool TryApply(DisplaySelection data,out string error){Current=data;error="";return true;}}
        Scene old,main;bool background;NextDisplayPanel panel;NextRoomStreaming stream;PlayerInputReader input;Backend backend;RoomSceneOwner survivor;int width,height;FullScreenMode mode;
        static IEnumerator Wait(Func<bool> predicate){var watch=System.Diagnostics.Stopwatch.StartNew();while(!predicate()){Assert.That(watch.Elapsed.TotalSeconds,Is.LessThan(20));yield return null;}}
        IEnumerator Boot()
        {background=Application.runInBackground;Application.runInBackground=true;width=Screen.width;height=Screen.height;mode=Screen.fullScreenMode;old=SceneManager.GetActiveScene();main=EditorSceneManager.LoadSceneInPlayMode(NextRoomPartition.Main,new LoadSceneParameters(LoadSceneMode.Additive));while(!main.isLoaded)yield return null;SceneManager.SetActiveScene(main);stream=main.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<NextRoomStreaming>()).Single();panel=stream.GetComponent<NextDisplayPanel>();Assert.That(panel,Is.Not.Null);yield return Wait(()=>!stream.Blocking);input=stream.GetComponent<PlayerInputReader>();input.SetApplicationFocus(true);input.SetPointerCaptured(true);backend=new Backend();panel.ConfigureForTests(backend,Path.GetFullPath("Library/EchoStation/SalvageNext/DisplayUI_"+Guid.NewGuid().ToString("N")));stream.GetComponent<SalvageMotor>().enabled=false;}
        [UnityTest]public IEnumerator ButtonsConfirmAndCancelWithoutTouchingEditorDisplayOrLLM()
        {yield return new EnterPlayMode();yield return Boot();Assert.That(panel.Open(),Is.True);Assert.That(input.UiBlocked,Is.True);Assert.That(input.PointerCaptured,Is.False);Assert.That(stream.GetComponent<NextCommandPanel>().Open(false),Is.False);panel.QualityButtons[2].onClick.Invoke();panel.ResolutionButtons[1].onClick.Invoke();panel.ModeButtons[1].onClick.Invoke();Assert.That(panel.Draft,Is.EqualTo(new DisplaySelection(RecoveryQuality.High,1600,900,DisplayWindowMode.Borderless)));panel.ApplyButton.onClick.Invoke();yield return Wait(()=>panel.Transaction.Phase==DisplayPhase.Confirming);Assert.That(panel.ConfirmButton.interactable,Is.True);Assert.That(File.Exists(panel.Store.Primary),Is.False);panel.ConfirmButton.onClick.Invoke();Assert.That(panel.Store.TryRead(out var saved,out _),Is.True);Assert.That(saved,Is.EqualTo(panel.Draft));panel.QualityButtons[0].onClick.Invoke();panel.ApplyButton.onClick.Invoke();yield return Wait(()=>panel.Transaction.Phase==DisplayPhase.Confirming);panel.RevertButton.onClick.Invoke();yield return null;Assert.That(backend.Current,Is.EqualTo(saved));panel.RequestClose();yield return null;Assert.That(panel.IsOpen,Is.False);Assert.That(input.CanPlay,Is.True);Assert.That(panel.GetComponentsInChildren<Button>(true).Single(b=>b.name=="UseLLM").interactable,Is.False);Assert.That(EchoStation.Echo.GameRuntimeOptions.UseLLM,Is.False);}
        [UnityTest]public IEnumerator ExistingModalAndFocusLossPreserveInputOwnership()
        {yield return new EnterPlayMode();yield return Boot();input.SetUiBlocked(true);Assert.That(panel.Open(),Is.False);Assert.That(input.UiBlocked,Is.True);input.SetUiBlocked(false);input.SetPointerCaptured(true);Assert.That(panel.Open(),Is.True);panel.QualityButtons[0].onClick.Invoke();panel.ApplyButton.onClick.Invoke();yield return Wait(()=>panel.Transaction.Phase==DisplayPhase.Confirming);input.SetApplicationFocus(false);yield return null;yield return null;Assert.That(panel.IsOpen,Is.False);Assert.That(backend.Current,Is.EqualTo(DisplaySelection.Default()));Assert.That(input.PointerCaptured,Is.False);Assert.That(File.Exists(panel.Store.Primary),Is.False);input.SetApplicationFocus(true);Assert.That(panel.Open(),Is.True);panel.RequestClose();yield return null;Assert.That(input.CanPlay,Is.True);}
        [UnityTearDown]public IEnumerator Cleanup()
        {if(!Application.isPlaying)yield break;if(panel){panel.RequestClose();yield return null;}Assert.That(Screen.width,Is.EqualTo(width));Assert.That(Screen.height,Is.EqualTo(height));Assert.That(Screen.fullScreenMode,Is.EqualTo(mode));if(old.IsValid())SceneManager.SetActiveScene(old);if(main.IsValid()&&main.isLoaded){survivor=stream?stream.Owner:null;var unload=SceneManager.UnloadSceneAsync(main);while(!unload.isDone)yield return null;}if(survivor!=null)yield return Wait(()=>survivor.IsClosed&&Resources.FindObjectsOfTypeAll<RoomScenePump>().Length==0);Application.runInBackground=background;yield return new ExitPlayMode();}
    }
}
