using EchoStation.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace EchoStation.Tests.Player
{
    public class PlayerInputTests:InputTestFixture
    {
        private GameObject root;private PlayerInputReader reader;private Keyboard keyboard;private Mouse mouse;
        [SetUp]public override void Setup(){base.Setup();keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();root=new GameObject("InputReaderFixture");reader=root.AddComponent<PlayerInputReader>();reader.SetApplicationFocus(true);reader.SetPointerCaptured(true);}
        [TearDown]public override void TearDown(){Object.DestroyImmediate(root);base.TearDown();}
        [Test]public void P01_A01_ActualWasdActionsAndDiagonalAreNormalized()
        {
            foreach(var pair in new[]{(Key.W,Vector2.up),(Key.S,Vector2.down),(Key.A,Vector2.left),(Key.D,Vector2.right)}){InputSystem.QueueStateEvent(keyboard,new KeyboardState(pair.Item1));InputSystem.Update();Assert.That(Vector2.Distance(reader.Move,pair.Item2),Is.LessThan(.0001f));}
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.D));InputSystem.Update();Assert.That(reader.Move.magnitude,Is.EqualTo(1).Within(.0001f));Assert.That(reader.Move.x,Is.GreaterThan(.7f));Assert.That(reader.Move.y,Is.GreaterThan(.7f));
        }
        [Test]public void P01_A02_MouseUsesFrameDeltaAndUiOrLostFocusBlocksHeldKeys()
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));InputSystem.QueueStateEvent(mouse,new MouseState{delta=new Vector2(50,20)});InputSystem.Update();Assert.That(reader.Look,Is.EqualTo(new Vector2(50,20)));Assert.That(reader.Move,Is.EqualTo(Vector2.up));reader.SetUiBlocked(true);Assert.That(reader.Move,Is.EqualTo(Vector2.zero));Assert.That(reader.Look,Is.EqualTo(Vector2.zero));Assert.That(reader.PointerCaptured,Is.False);reader.SetUiBlocked(false);Assert.That(reader.CanPlay,Is.False);reader.SetPointerCaptured(true);Assert.That(reader.CanPlay,Is.True);
            reader.SetApplicationFocus(false);Assert.That(reader.Move,Is.EqualTo(Vector2.zero));Assert.That(reader.Look,Is.EqualTo(Vector2.zero));reader.SetApplicationFocus(true);Assert.That(reader.CanPlay,Is.False);reader.SetPointerCaptured(true);Assert.That(reader.CanPlay,Is.True);
        }
        [Test]public void P01_A03_DisabledReaderCannotResumeInputAndRestoresCursor()
        {
            reader.enabled=false;reader.SetPointerCaptured(true);Assert.That(reader.CanPlay,Is.False);Assert.That(reader.PointerCaptured,Is.False);Assert.That(reader.Move,Is.EqualTo(Vector2.zero));
        }
    }
}
