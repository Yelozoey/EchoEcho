using System;
using System.Linq;
using System.Text;
using EchoStation.AI.Context;
using EchoStation.AI.IntentJson;
using EchoStation.Memory;
using NUnit.Framework;

namespace EchoStation.Tests.AI
{
 public sealed class JsonConstraintTests
 {
  private const string Prefix="{\"intent\":\"answer\",\"content\":\"";
  private static byte[] Bytes(string text)=>Encoding.UTF8.GetBytes(text);
  private static JsonIntentConstraint Grammar(bool targets=true)
  {
   var items=targets?new[]{new TargetCandidate("panel_01","面板","cabin",true,true,false,true,1),new TargetCandidate("hazard_01","危险设备","cabin",true,false,true,true,2)}:Array.Empty<TargetCandidate>();
   return new JsonIntentConstraint(new InferenceSnapshot(new MemoryStore(),"调查",0,"cabin",items).Targets);
  }
  private static void Feed(JsonIntentConstraint g,string text){var bytes=Bytes(text);Assert.That(g.Allows(0,bytes),Is.True,"Rejected prefix: "+text);g.Accept(0,bytes);}
  [TestCase("{\"intent\":\"follow\"}")]
  [TestCase("{\"intent\":\"wait\"}")]
  [TestCase("{\"intent\":\"investigate\",\"targetName\":\"panel_01\",\"content\":\"我前去扫描。\"}")]
  [TestCase("{\"intent\":\"warn\",\"dangerTarget\":\"hazard_01\",\"content\":\"设备存在危险。\"}")]
  [TestCase("{\"intent\":\"answer\",\"content\":\"已有记录。\"}")]
  [TestCase("{\"intent\":\"refuse\",\"content\":\"我不能代为操作。\"}")]
  public void SixCanonicalObjectsAcceptEveryByteBoundaryAndStrictParser(string raw)
  {var g=Grammar();Assert.That(g.CanEnd,Is.False);foreach(var b in Bytes(raw)){Assert.That(g.Allows(0,new[]{b}),Is.True);g.Accept(0,new[]{b});}Assert.That(g.CanEnd,Is.True);Assert.That(g.Allows(0,Bytes("\n")),Is.False);Assert.That(IntentJsonParser.Parse(raw).Success,Is.True);}
  [Test]public void UnknownTargetsAndWrongPermissionNeverEnterOutputLanguage()
  {
   Assert.That(Grammar(false).Allows(0,Bytes("{\"intent\":\"investigate\"")),Is.False);Assert.That(Grammar(false).Allows(0,Bytes("{\"intent\":\"warn\"")),Is.False);
   Assert.That(Grammar().Allows(0,Bytes("{\"intent\":\"investigate\",\"targetName\":\"hazard_01\"")),Is.False);Assert.That(Grammar().Allows(0,Bytes("{\"intent\":\"warn\",\"dangerTarget\":\"panel_01\"")),Is.False);Assert.That(Grammar().Allows(0,Bytes("{\"intent\":\"investigate\",\"targetName\":\"unknown_01\"")),Is.False);
  }
  [Test]public void FailedAllowAndAcceptNeverMutateStateAndEmptyTokensAreRejected()
  {var g=Grammar();Assert.That(g.Allows(0,Bytes("```json")),Is.False);Assert.That(g.Allows(0,Array.Empty<byte>()),Is.False);Assert.Throws<InvalidOperationException>(()=>g.Accept(0,Bytes("[]")));Feed(g,"{\"intent\":\"follow\"}");Assert.That(g.CanEnd,Is.True);}
  [Test]public void Utf8AndEscapedSurrogatePairsCanSpanArbitraryTokens()
  {
   var g=Grammar();Feed(g,Prefix);g.Accept(0,new byte[]{0xe4,0xb8});Assert.That(g.CanEnd,Is.False);g.Accept(0,new byte[]{0xad,0xf0,0x9f});g.Accept(0,new byte[]{0x9a,0x80});Assert.That(g.ContentUtf16Length,Is.EqualTo(3));Feed(g,"\"}");Assert.That(g.CanEnd,Is.True);
   g=Grammar();Feed(g,Prefix+"\\uD8");Feed(g,"3D\\uDE");Feed(g,"80\"}");Assert.That(g.ContentUtf16Length,Is.EqualTo(2));Assert.That(g.CanEnd,Is.True);
  }
  [Test]public void InvalidUtf8PrefixesAreRejectedWhileStillExtendibleChoicesRemain()
  {
   var g=Grammar();Feed(g,Prefix);foreach(var bytes in new[]{new byte[]{0x80},new byte[]{0xc0},new byte[]{0xe0,0x80},new byte[]{0xed,0xa0},new byte[]{0xf4,0x90},new byte[]{0xf5}})Assert.That(g.Allows(0,bytes),Is.False);Feed(g,"x\"}");Assert.That(g.CanEnd,Is.True);
  }
  [Test]public void InvalidEscapesAndUnpairedSurrogatesAreRejectedBeforeDeadEnd()
  {
   var g=Grammar();Feed(g,Prefix);Assert.That(g.Allows(0,Bytes("\\x")),Is.False);Assert.That(g.Allows(0,Bytes("\\uDC")),Is.False);Feed(g,"\\uD800");Assert.That(g.Allows(0,Bytes("\"")),Is.False);Assert.That(g.Allows(0,Bytes("\\u0041")),Is.False);Feed(g,"\\uDC00\"}");Assert.That(g.ContentUtf16Length,Is.EqualTo(2));Assert.That(g.CanEnd,Is.True);
  }
  [Test]public void Utf16LimitRejectsHighSurrogateOrFourByteStartWhenOnlyOneUnitRemains()
  {
   var g=Grammar();Feed(g,Prefix);Feed(g,new string('a',279));Assert.That(g.Allows(0,new byte[]{0xf0}),Is.False);Assert.That(g.Allows(0,Bytes("\\uD8")),Is.False);Feed(g,"b");Assert.That(g.ContentUtf16Length,Is.EqualTo(280));Assert.That(g.Allows(0,Bytes("c")),Is.False);Assert.That(g.Allows(0,Bytes("\\")),Is.False);Feed(g,"\"}");Assert.That(g.CanEnd,Is.True);
  }
  [Test]public void ContentMustContainNonWhitespaceAndCannotFillAll280UnitsWithSpaces()
  {var g=Grammar();Feed(g,Prefix);Assert.That(g.Allows(0,Bytes("\"")),Is.False);Assert.That(g.Allows(0,Bytes(new string(' ',280))),Is.False);Feed(g,"   \\nx\"}");Assert.That(g.CanEnd,Is.True);}
  [Test]public void Token240ReserveRequiresNonWhitespaceThenClosesWithoutAddingFields()
  {
   var g=Grammar();Feed(g,Prefix);for(var i=0;i<239;i++)Feed(g," ");Assert.That(g.AcceptedTokens,Is.EqualTo(240));Assert.That(g.Allows(0,Bytes(" ")),Is.False);Feed(g,"x");Assert.That(g.Allows(0,Bytes("y")),Is.False);Feed(g,"\"}");Assert.That(g.CanEnd,Is.True);Assert.That(g.AcceptedTokens,Is.LessThan(256));Assert.That(IntentJsonParser.Parse(Prefix+new string(' ',239)+"x\"}").Success,Is.True);
  }
  [Test]public void TokenReserveSafelyFinishesPartlyEncodedSurrogateBeforeClosing()
  {var g=Grammar();Feed(g,Prefix);for(var i=0;i<237;i++)Feed(g,"\\n");Feed(g,"\\uD8");Feed(g,"00");Assert.That(g.AcceptedTokens,Is.EqualTo(240));Assert.That(g.Allows(0,Bytes("\\u0041")),Is.False);Feed(g,"\\uDC00");Feed(g,"\"}");Assert.That(g.CanEnd,Is.True);Assert.That(g.ContentUtf16Length,Is.EqualTo(239));}
  [Test]public void GrammarUsesCanonicalOrderButParserStillAcceptsOtherLegalOrder()
  {var other="{\"content\":\"记录。\",\"intent\":\"answer\"}";Assert.That(IntentJsonParser.Parse(other).Success,Is.True);Assert.That(Grammar().Allows(0,Bytes(other)),Is.False);Assert.That(Grammar().Allows(0,Bytes("{\"intent\":\"follow\",\"content\":\"x\"}")),Is.False);}
  [Test]public void RepeatedOrOverBudgetTargetListsAreRejected()
  {var targets=new InferenceSnapshot(new MemoryStore(),"调查",0,"cabin",new[]{new TargetCandidate("a","A","cabin",true,true,false,true,1)}).Targets;Assert.Throws<ArgumentException>(()=>new JsonIntentConstraint(new[]{targets[0],targets[0]}));Assert.Throws<ArgumentException>(()=>new JsonIntentConstraint(Enumerable.Repeat(targets[0],9).ToArray()));}
 }
}
