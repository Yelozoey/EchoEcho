using EchoStation.L01;
using NUnit.Framework;
namespace EchoStation.L02.Tests
{
    public class SceneQuestionTests
    {
        static EchoTextContext Known(string report="扳手可关闭阀门；操作期间危险仍运行。",bool stale=false,bool available=true)=>new EchoTextContext(new[]{new EchoTextTarget("valve","阀门",available,report,stale)},"valve",90);
        [Test]public void CaptureContainsOnlySelectedSummaryAndNoChatOrHiddenFacts(){var r=SceneQuestion.Select("这个怎么处理？",Known(),"valve","1");Assert.That(r.Success,Is.True);Assert.That(r.Capture.Snapshot.Known.Clues.Count,Is.EqualTo(1));Assert.That(r.Capture.Snapshot.Known.Clues[0].Summary,Is.EqualTo(r.Capture.Source.Report));Assert.That(r.Capture.Snapshot.Known.Events.Count,Is.Zero);Assert.That(r.Capture.Snapshot.Targets[0].Id,Is.EqualTo("valve"));}
        [Test]public void NoTargetDoesNotChooseTheOnlyEntry(){Assert.That(SceneQuestion.Select("怎么处理？",Known(),null,"1").Success,Is.False);}
        [TestCase(null,false,true)] [TestCase("",false,true)] [TestCase("已有报告",true,true)] [TestCase("已有报告",false,false)]
        public void MissingStaleOrUnavailableCannotReachInference(string report,bool stale,bool available){Assert.That(SceneQuestion.Select("这个怎么处理",Known(report,stale,available),"valve","1").Success,Is.False);}
        [Test]public void ExplicitTargetResolvesWithoutPointing(){Assert.That(SceneQuestion.Select("阀门应该怎么处理",Known(),null,"1").Success,Is.True);}
        [Test]public void DuplicateNamesAndMultipleTargetsRequireClarification(){var c=new EchoTextContext(new[]{new EchoTextTarget("valve","阀门",true,"摘要"),new EchoTextTarget("valve_b","阀门",true,"摘要")},null,1);Assert.That(SceneQuestion.Select("阀门怎么处理",c,null,"1").Success,Is.False);}
        [Test]public void UnknownTargetCannotBeSelected(){Assert.That(SceneQuestion.Select("怎么处理",Known(),"unknown","1").Success,Is.False);}
        [TestCase(1)] [TestCase(201)]public void QuestionBudgetRejected(int count){Assert.That(SceneQuestion.Select(new string('字',count),Known(),"valve","1").Success,Is.False);}
        [Test]public void Full200CharactersPreserved(){var text=new string('字',200);Assert.That(SceneQuestion.Select(text,Known(),"valve","1").Capture.Snapshot.Input,Is.EqualTo(text));}
        [Test]public void VersionReportAndAvailabilityInvalidate(){var capture=SceneQuestion.Select("这个怎么处理",Known(),"valve","1").Capture;Assert.That(SceneQuestion.IsFresh(capture,Known(),"1"),Is.True);Assert.That(SceneQuestion.IsFresh(capture,Known(),"2"),Is.False);Assert.That(SceneQuestion.IsFresh(capture,Known("新摘要"),"1"),Is.False);Assert.That(SceneQuestion.IsFresh(capture,Known(stale:true),"1"),Is.False);Assert.That(SceneQuestion.IsFresh(capture,Known(available:false),"1"),Is.False);}
        [TestCase("{\"intent\":\"follow\"}")] [TestCase("{\"intent\":\"investigate\",\"targetName\":\"valve\",\"content\":\"扫描\"}")] [TestCase("{\"intent\":\"answer\",\"content\":\"回复\",\"extra\":1}")] [TestCase("text")]
        public void ActionsAndMalformedJsonNeverBecomeReply(string raw){Assert.That(SceneQuestion.TryReadReply(raw,out _,out _),Is.False);}
        [TestCase("answer")] [TestCase("refuse")]public void ReadOnlyReplyAllowed(string intent){Assert.That(SceneQuestion.TryReadReply("{\"intent\":\""+intent+"\",\"content\":\"中文解释\"}",out var content,out _),Is.True);Assert.That(content,Is.EqualTo("中文解释"));}
        [Test]public void LongReplyRejectedRatherThanTruncated(){Assert.That(SceneQuestion.TryReadReply("{\"intent\":\"answer\",\"content\":\""+new string('字',281)+"\"}",out _,out _),Is.False);}
    }
}
