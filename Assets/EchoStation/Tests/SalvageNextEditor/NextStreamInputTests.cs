using System.Collections;
using EchoStation.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace EchoStation.SalvageNext.Tests
{
    public sealed class NextStreamInputTests
    {
        GameObject context;PlayerInputReader input;StreamInputLease lease;
        void Begin(){context=new GameObject("StreamInputLeaseTest");input=context.AddComponent<PlayerInputReader>();input.SetApplicationFocus(true);input.SetPointerCaptured(true);}
        [UnityTest]public IEnumerator CapturedGameplayInputResumesWithoutChangingMenuOwnership()
        {yield return new EnterPlayMode();Begin();Assert.That(input.CanPlay,Is.True);lease=new StreamInputLease(input);Assert.That(input.CanPlay,Is.False);Assert.That(input.enabled,Is.False);Assert.That(input.UiBlocked,Is.False);lease.Dispose();Assert.That(input.CanPlay,Is.True);lease.Dispose();Assert.That(input.CanPlay,Is.True);}
        [UnityTest]public IEnumerator MenuOpenedDuringLoadRemainsBlockedAndDoesNotCapturePointerOnReady()
        {yield return new EnterPlayMode();Begin();lease=new StreamInputLease(input);input.SetUiBlocked(true);lease.Dispose();Assert.That(input.enabled,Is.True);Assert.That(input.UiBlocked,Is.True);Assert.That(input.PointerCaptured,Is.False);Assert.That(input.CanPlay,Is.False);input.SetUiBlocked(false);input.SetPointerCaptured(true);Assert.That(input.CanPlay,Is.True);}
        [UnityTest]public IEnumerator PreviouslyDisabledInputIsNotEnabledByLoadingCompletion()
        {yield return new EnterPlayMode();Begin();input.enabled=false;lease=new StreamInputLease(input);lease.Dispose();Assert.That(input.enabled,Is.False);Assert.That(input.CanPlay,Is.False);}
        [UnityTest]public IEnumerator LostFocusOrDestroyedPlayerDoesNotCaptureOrTouchOldReader()
        {yield return new EnterPlayMode();Begin();lease=new StreamInputLease(input);input.SetApplicationFocus(false);lease.Dispose();Assert.That(input.enabled,Is.True);Assert.That(input.PointerCaptured,Is.False);lease=new StreamInputLease(input);Object.DestroyImmediate(context);Assert.DoesNotThrow(()=>lease.Dispose());}
        [UnityTearDown]public IEnumerator Cleanup(){if(!Application.isPlaying)yield break;lease?.Dispose();if(context)Object.Destroy(context);yield return new ExitPlayMode();}
    }
}
