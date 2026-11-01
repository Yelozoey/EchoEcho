using System;
using EchoStation.Dialogue.Commands;
using EchoStation.Dialogue.Knowledge;
using EchoStation.Echo;
using EchoStation.Memory;
using NUnit.Framework;

namespace EchoStation.Tests.DialogueKnowledge
{
 public sealed class KnownQuestionApiTests
 {
  private static ScriptKnowledgeResponder Create(MemorySession session)=>new ScriptKnowledgeResponder(session,new MemorySearch(new[]{new MemoryKeywordGroup("密码","维护码","终端"),new MemoryKeywordGroup("资料","记录","线索")}));
  [TestCase(null)][TestCase("")][TestCase(" ")]
  public void InvalidInputCannotReachKnowledge(string input)
  {Assert.That(Create(new MemorySession()).AnswerKnown(input,ScriptQuestionKind.Fact).Kind,Is.EqualTo(IntentKind.Refuse));}
  [Test]public void InvalidSurrogateIsConstructedAtRuntime()
  {Assert.That(Create(new MemorySession()).AnswerKnown(new string((char)0xd800,1),ScriptQuestionKind.Fact).Kind,Is.EqualTo(IntentKind.Refuse));}
  [Test]public void InputAndQuestionKindAreBounded()
  {
   var responder=Create(new MemorySession());
   Assert.That(responder.AnswerKnown(new string('中',201),ScriptQuestionKind.Fact).Kind,Is.EqualTo(IntentKind.Refuse));
   Assert.That(responder.AnswerKnown(new string('中',200),ScriptQuestionKind.Fact).Kind,Is.EqualTo(IntentKind.Answer));
   foreach(var kind in new[]{ScriptQuestionKind.None,(ScriptQuestionKind)5,(ScriptQuestionKind)(-1)})Assert.That(responder.AnswerKnown("记录",kind).Kind,Is.EqualTo(IntentKind.Refuse));
  }
  [Test]public void SemanticApiAndOldParserUseSameKnownPasswordAndRedaction()
  {
   var session=new MemorySession();session.Store.AddClue(new ClueMemory("note_a","终端记录","终端维护码7316已归档。",MemoryClueKind.Note,"7316"));var responder=Create(session);
   var parsed=new ScriptCommandParser(Array.Empty<ScriptTarget>()).Parse("密码是多少");
   Assert.That(responder.AnswerKnown("密码是多少",ScriptQuestionKind.Password).Content,Is.EqualTo(responder.Answer(parsed).Content).And.Contains("7316"));
   Assert.That(responder.AnswerKnown("记录",ScriptQuestionKind.Fact).Content,Does.Not.Contain("7316"));
  }
  [Test]public void NoQuestionCategoryCanExecuteAnActionOrWriteMemory()
  {
   var session=new MemorySession();var responder=Create(session);var before=session.CaptureJson();
   foreach(var kind in new[]{ScriptQuestionKind.Hint,ScriptQuestionKind.Summary,ScriptQuestionKind.Password,ScriptQuestionKind.Fact})
    Assert.That(responder.AnswerKnown("跟随我并开门",kind).Kind,Is.EqualTo(IntentKind.Answer));
   Assert.That(session.CaptureJson(),Is.EqualTo(before));
  }
 }
}
