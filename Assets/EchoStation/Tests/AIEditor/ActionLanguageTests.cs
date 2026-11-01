using System;
using EchoStation.AI.Authorization;
using EchoStation.AI.Context;
using EchoStation.AI.IntentJson;
using NUnit.Framework;

namespace EchoStation.Tests.AI
{
 public sealed class ActionLanguageTests
 {
  [TestCase("Echo，走在我的后面。","follow")][TestCase("请陪我一起往前走。","follow")][TestCase("停止等候，改为跟我走。","follow")][TestCase("一直跟在我身边。","follow")][TestCase("Echo，陪着我移动。","follow")][TestCase("与我同行。","follow")][TestCase("别留在原处，跟我离开。","follow")][TestCase("请继续随行。","follow")]
  [TestCase("这次不要跟过来，在原地等候。","wait")][TestCase("暂停跟随，原地等待。","wait")][TestCase("请停在这里。","wait")][TestCase("Echo，停止前进。","wait")][TestCase("不要移动，等我的下一条指令。","wait")][TestCase("在这个位置保持静止。","wait")][TestCase("在这里候着我。","wait")][TestCase("暂时停止跟随。","wait")]
  public void PositiveAndExplicitStopCommandsKeepTheirIntendedMovement(string input,string kind)
  {var intent=IntentJsonParser.Parse("{\"intent\":\""+kind+"\"}").Intent;var result=IntentActionGuard.Evaluate(intent,input,Array.Empty<AllowedTarget>(),Array.Empty<AllowedTarget>(),Array.Empty<TrustedTargetIdentity>());Assert.That(result.Disposition,Is.EqualTo(GuardDisposition.Action));Assert.That(result.Kind.ToString().ToLowerInvariant(),Is.EqualTo(kind));}
  [TestCase("不要跟着我，然后跟随我")][TestCase("跟随我然后原地等候")][TestCase("别去扫描供电面板")]
  public void ConflictingOrUnrecognizedNegativeActionsStillClarify(string input)
  {var result=IntentActionGuard.Evaluate(IntentJsonParser.Parse("{\"intent\":\"follow\"}").Intent,input,Array.Empty<AllowedTarget>(),Array.Empty<AllowedTarget>(),Array.Empty<TrustedTargetIdentity>());Assert.That(result.Disposition,Is.EqualTo(GuardDisposition.Clarify));}
 }
}
