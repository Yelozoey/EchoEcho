using System.Linq;
using EchoStation.InfiltrationCoated.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace EchoStation.InfiltrationCoated.Tests
{
    public sealed class CoatLabelTests
    {
        [Test]public void FourInspectionMarksUseDarkInkAndKeepTheirContent()
        {
            var old=SceneManager.GetActiveScene();var sample=EditorSceneManager.OpenScene(CoatScene.ScenePath,OpenSceneMode.Additive);
            try{var labels=CoatScene.World(sample).GetComponentsInChildren<TextMesh>().Where(t=>t.name.StartsWith("PostLabel_")).ToArray();Assert.That(labels.Length,Is.EqualTo(4));foreach(var text in labels){Assert.That(text.text,Does.Contain("AI未启用"));Assert.That(text.color.r+text.color.g+text.color.b,Is.LessThan(.3f));Assert.That(text.color.a,Is.EqualTo(1));Assert.That(text.GetComponent<Renderer>().sharedMaterial.shader.name,Is.EqualTo("EchoStation/InfiltrationLevel/WorldLabel"));}}
            finally{SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(sample,true);}
        }
    }
}
