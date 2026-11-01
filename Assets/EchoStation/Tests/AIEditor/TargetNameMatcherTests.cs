using System;
using System.Collections.Generic;
using System.Linq;
using EchoStation.AI.Authorization;
using NUnit.Framework;

namespace EchoStation.Tests.AI
{
 public sealed class TargetNameMatcherTests
 {
  private static TrustedTargetIdentity Panel()=>new TrustedTargetIdentity("panel_01","供电面板","电源面板");
  [TestCase("调查panel_01。")][TestCase("扫描PANEL_01")][TestCase("检查ｐａｎｅｌ＿０１")]
  public void ExactIdCaseAndCompatibilityTextResolveUniquely(string input)
  {var result=TargetNameMatcher.Match(input,new[]{Panel()});Assert.That(result.UniqueId,Is.EqualTo("panel_01"));Assert.That(result.IsAmbiguous,Is.False);}
  [TestCase("扫描panel_010")][TestCase("调查xpanel_01")][TestCase("扫描panel_01_extra")]
  public void IdPrefixAndSuffixCannotMatchAnotherIdentity(string input)
  {Assert.That(TargetNameMatcher.Match(input,new[]{Panel()}).Ids,Is.Empty);}
  [Test]public void ExistingChineseStorageAliasResolvesWithoutGuessing()
  {var target=new TrustedTargetIdentity("scan_mark_01","储藏室可疑点","储藏室可疑位置","储物间可疑位置");Assert.That(TargetNameMatcher.Match("Echo，请去调查储藏室可疑位置。",new[]{target}).UniqueId,Is.EqualTo(target.Id));}
  [Test]public void SharedNamesOrTwoMentionedIdsRemainAmbiguous()
  {
   var targets=new[]{new TrustedTargetIdentity("point_a","可疑点"),new TrustedTargetIdentity("point_b","可疑点")};
   var first=TargetNameMatcher.Match("扫描可疑点",targets);Assert.That(first.IsAmbiguous,Is.True);Assert.That(first.UniqueId,Is.Null);
   Assert.That(TargetNameMatcher.Match("调查point_a和point_b",targets).IsAmbiguous,Is.True);
  }
  [Test]public void UnknownObjectDoesNotDefaultToKnownPanel()
  {Assert.That(TargetNameMatcher.Match("调查不存在的摄像头",new[]{Panel()}).UniqueId,Is.Null);}
  [Test]public void DuplicateNullAndOversizedCatalogsRejected()
  {
   Assert.Throws<ArgumentException>(()=>TargetNameMatcher.Match("扫描",null));Assert.Throws<ArgumentException>(()=>TargetNameMatcher.Match("扫描",new TrustedTargetIdentity[]{null}));Assert.Throws<ArgumentException>(()=>TargetNameMatcher.Match("扫描",new[]{Panel(),Panel()}));
   Assert.Throws<ArgumentException>(()=>TargetNameMatcher.Match("扫描",Enumerable.Range(0,129).Select(i=>new TrustedTargetIdentity("target_"+i,"设备"+i))));
  }
  [Test]public void InvalidNamesAliasesIdsAndUtf16Rejected()
  {
   Assert.Throws<ArgumentException>(()=>new TrustedTargetIdentity("../bad","设备"));Assert.Throws<ArgumentException>(()=>new TrustedTargetIdentity("target_a",new string('中',81)));
   Assert.Throws<ArgumentException>(()=>new TrustedTargetIdentity("target_a","设备",Enumerable.Repeat("别名",17).ToArray()));Assert.Throws<ArgumentException>(()=>new TrustedTargetIdentity("target_a","\ud800"));
  }
  [Test]public void ReturnedAliasAndMatchListsAreReadOnly()
  {var target=Panel();Assert.Throws<NotSupportedException>(()=>((IList<string>)target.Aliases)[0]="changed");var result=TargetNameMatcher.Match("供电面板",new[]{target});Assert.Throws<NotSupportedException>(()=>((IList<string>)result.Ids)[0]="changed");}
  [Test]public void InputLimitsAndBadUtf16Rejected()
  {Assert.Throws<ArgumentException>(()=>TargetNameMatcher.Match(null,new[]{Panel()}));Assert.Throws<ArgumentException>(()=>TargetNameMatcher.Match(new string('中',201),new[]{Panel()}));Assert.Throws<ArgumentException>(()=>TargetNameMatcher.Match("\udc00",new[]{Panel()}));Assert.That(TargetNameMatcher.Match(new string('中',200),new[]{Panel()}).Ids,Is.Empty);}
  [Test]public void Maximum128TargetsRemainBoundedAndExact()
  {var targets=Enumerable.Range(0,128).Select(i=>new TrustedTargetIdentity("target_"+i,"设备"+i)).ToArray();Assert.That(TargetNameMatcher.Match("调查target_127",targets).UniqueId,Is.EqualTo("target_127"));}
 }
}
