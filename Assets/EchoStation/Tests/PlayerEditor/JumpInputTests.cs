using EchoStation.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace EchoStation.Tests.Player
{
    public class JumpInputTests:InputTestFixture
    {
        private GameObject root;private PlayerInputReader reader;private Keyboard keyboard;
        [SetUp]public override void Setup(){base.Setup();keyboard=InputSystem.AddDevice<Keyboard>();root=new GameObject("JumpInputFixture");reader=root.AddComponent<PlayerInputReader>();reader.SetApplicationFocus(true);reader.SetPointerCaptured(true);}
        [TearDown]public override void TearDown(){Object.DestroyImmediate(root);base.TearDown();}
        [Test]public void P02_A04_SpaceIsEdgeTriggeredAndUiOrLostFocusCannotJump()
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space));InputSystem.Update();Assert.That(reader.JumpPressed,Is.True);reader.SetUiBlocked(true);Assert.That(reader.JumpPressed,Is.False);reader.SetUiBlocked(false);reader.SetPointerCaptured(true);InputSystem.Update();Assert.That(reader.JumpPressed,Is.False);InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space));InputSystem.Update();Assert.That(reader.JumpPressed,Is.True);reader.SetApplicationFocus(false);Assert.That(reader.JumpPressed,Is.False);
        }
    }
}
