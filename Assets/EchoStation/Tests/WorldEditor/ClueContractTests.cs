using System.Linq;
using EchoStation.World;
using EchoStation.World.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace EchoStation.Tests.World
{
    public class ClueContractTests
    {
        private static ClueDefinition Data(ClueKind kind=ClueKind.Note)
        {var data=ScriptableObject.CreateInstance<ClueDefinition>();data.Configure("clue_fixture","标题","这是一篇完整的测试原文，供玩家阅读。","测试内容摘要。",kind);return data;}
        private static ClueSource Source(ClueDefinition data)
        {var go=new GameObject("ClueFixture");go.SetActive(false);go.AddComponent<WorldTarget>().Configure("clue_fixture","夹具");var source=go.AddComponent<ClueSource>();source.Configure(data,null);return source;}
        [Test]public void W04_01_RepeatReadsKeepImmutableFullTextAndDistinctSummary()
        {
            var data=Data();var source=Source(data);try
            {Assert.That(source.TryRead(out var a),Is.True);Assert.That(source.TryRead(out var b),Is.True);Assert.That(a.Id,Is.EqualTo(b.Id));Assert.That(a.FullText,Is.EqualTo(b.FullText));Assert.That(a.Summary,Is.EqualTo(b.Summary));Assert.That(a.FullText,Is.Not.EqualTo(a.Summary));Assert.That(a.Kind,Is.EqualTo(ClueKind.Note));}
            finally{Object.DestroyImmediate(source.gameObject);Object.DestroyImmediate(data);}
        }
        [Test]public void W04_02_HiddenScanCannotReturnRecordBeforeEchoAuthorization()
        {
            var data=Data(ClueKind.Scan);var source=Source(data);try
            {Assert.That(source.IsReadable||source.IsRevealed,Is.False);Assert.That(source.TryRead(out var record),Is.False);Assert.That(record,Is.Null);}
            finally{Object.DestroyImmediate(source.gameObject);Object.DestroyImmediate(data);}
        }
        [Test]public void W04_03_InvalidOrIdenticalSummaryAndMismatchedTargetAreRejected()
        {
            var data=Data();var source=Source(data);try
            {
                Assert.Throws<System.ArgumentException>(()=>data.Configure("clue_fixture","标题","原文","原文",ClueKind.Note));
                Assert.Throws<System.ArgumentException>(()=>data.Configure("clue_fixture","标题","原文",new string('摘',121),ClueKind.Note));
                Assert.Throws<System.ArgumentException>(()=>data.Configure("clue_fixture","标题","","摘要",ClueKind.Note));
                var other=ScriptableObject.CreateInstance<ClueDefinition>();try{other.Configure("other_clue","标题","独立原文","摘要",ClueKind.Note);Assert.Throws<System.ArgumentException>(()=>source.Configure(other,null));}finally{Object.DestroyImmediate(other);}
                Assert.That(source.TryRead(out var record)&&record.Id=="clue_fixture",Is.True);
            }
            finally{Object.DestroyImmediate(source.gameObject);Object.DestroyImmediate(data);}
        }
        [Test]public void W04_04_SavedCabinCluesHaveActualPresentationCollidersAndReadProtection()
        {
            var scene=EditorSceneManager.OpenPreviewScene(CabinGreyboxSetup.ScenePath);
            try
            {
                var clues=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<ClueSource>()).ToArray();Assert.That(clues.Length,Is.EqualTo(4));
                foreach(var clue in clues)
                {
                    Assert.That(clue.ClueId,Is.EqualTo(clue.GetComponent<WorldTarget>().Id));Assert.That(clue.GetComponent<BoxCollider>().isTrigger,Is.True);
                    Assert.That(GameObjectUtility.GetStaticEditorFlags(clue.gameObject),Is.EqualTo((StaticEditorFlags)0));var renderer=clue.GetComponent<MeshRenderer>();
                    Assert.That(renderer.sharedMaterial.shader.name,Is.EqualTo("Universal Render Pipeline/Lit"));Assert.That(renderer.lightProbeUsage,Is.EqualTo(LightProbeUsage.BlendProbes));
                    if(clue.Kind==ClueKind.Scan){Assert.That(renderer.enabled||clue.TryRead(out _),Is.False);}
                    else{Assert.That(renderer.enabled,Is.True);Assert.That(clue.TryRead(out var record),Is.True);Assert.That(record.Summary.Length,Is.LessThanOrEqualTo(120));}
                }
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
    }
}
