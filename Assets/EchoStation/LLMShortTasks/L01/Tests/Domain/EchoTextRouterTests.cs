using System;
using System.Linq;
using NUnit.Framework;
namespace EchoStation.L01.Tests
{
    public sealed class EchoTextRouterTests
    {
        static EchoTextContext Context(string pointed="valve",int battery=73)=>new EchoTextContext(new[]{new EchoTextTarget("valve","蒸汽阀门",true,"阀门尚未关闭。"),new EchoTextTarget("fan","通风接口",true),new EchoTextTarget("cargo","样本箱",false,"样本箱占4格。",true)},pointed,battery);
        [TestCase("跟随我",EchoTextKind.Follow)][TestCase("follow",EchoTextKind.Follow)][TestCase("原地待命",EchoTextKind.Wait)][TestCase("停止跟随",EchoTextKind.Wait)][TestCase("取消调查",EchoTextKind.Cancel)]
        public void ExactBehaviorAliases(string input,EchoTextKind kind){var r=EchoTextRouter.Parse(input,Context());Assert.That(r.Kind,Is.EqualTo(kind));Assert.That(r.Choices,Is.Empty);}
        [Test]public void PointedRequestResolvesOnlyDiscoveredTarget(){Assert.That(EchoTextRouter.Parse("调查这个",Context()).TargetId,Is.EqualTo("valve"));Assert.That(EchoTextRouter.Parse("调查这个",Context("unknown")).Kind,Is.EqualTo(EchoTextKind.Clarify));}
        [Test]public void ExplicitIdAndNameMapToSameTarget(){Assert.That(EchoTextRouter.Parse("调查 VALVE",Context()).TargetId,Is.EqualTo("valve"));Assert.That(EchoTextRouter.Parse("调查蒸汽阀门",Context()).TargetId,Is.EqualTo("valve"));}
        [Test]public void UnknownAndMissingNeverPickFirst(){foreach(var s in new[]{"调查","调查不存在的箱子","查看报告未知位置"}){var r=EchoTextRouter.Parse(s,Context());Assert.That(r.Kind,Is.EqualTo(EchoTextKind.Clarify));Assert.That(r.TargetId,Is.Null);}}
        [Test]public void DuplicateNamesRequireExplicitSelection(){var c=new EchoTextContext(new[]{new EchoTextTarget("b","面板",true),new EchoTextTarget("a","面板",true)},null,10);var r=EchoTextRouter.Parse("调查面板",c);Assert.That(r.Kind,Is.EqualTo(EchoTextKind.Clarify));Assert.That(r.Choices,Is.EqualTo(new[]{"a","b"}));Assert.That(EchoTextRouter.Select(r.ChoiceKind,"b",c).TargetId,Is.EqualTo("b"));}
        [Test]public void ChoiceRevalidatesAgainstFreshContext(){Assert.That(EchoTextRouter.Select(EchoTextKind.Investigate,"valve",new EchoTextContext(Array.Empty<EchoTextTarget>(),null,10)).Kind,Is.EqualTo(EchoTextKind.Reject));Assert.That(EchoTextRouter.Select(EchoTextKind.Follow,"valve",Context()).Kind,Is.EqualTo(EchoTextKind.Reject));}
        [Test]public void UnscannedDoesNotInventSummary(){var r=EchoTextRouter.Parse("报告通风接口",Context());Assert.That(r.Kind,Is.EqualTo(EchoTextKind.Reject));Assert.That(r.Text,Does.Contain("尚无"));}
        [Test]public void KnownReportIsReadonlyAndStaleIsVisible(){Assert.That(EchoTextRouter.Parse("报告蒸汽阀门",Context()).Text,Is.EqualTo("阀门尚未关闭。"));Assert.That(EchoTextRouter.Parse("报告样本箱",Context()).Text,Does.StartWith("（报告已过时"));Assert.That(EchoTextRouter.Parse("调查样本箱",Context()).Kind,Is.EqualTo(EchoTextKind.Reject));}
        [TestCase(0)][TestCase(100)]public void BatteryUsesCurrentSnapshot(int value){Assert.That(EchoTextRouter.Parse("电量",Context(battery:value)).Text,Is.EqualTo("当前电量为"+value+"%。"));}
        [TestCase("跟随我然后调查阀门")][TestCase("跟随我并替我开门")][TestCase("调查阀门然后给设备通电")][TestCase("关闭阀门")]
        public void CompoundAndUnauthorizedNeverExecuteSafeHalf(string input){var r=EchoTextRouter.Parse(input,Context());Assert.That(r.Kind==EchoTextKind.Reject||r.Kind==EchoTextKind.Clarify,Is.True);Assert.That(r.TargetId,Is.Null);}
        [Test]public void InputAndReplyBoundsAreIndependent(){Assert.That(EchoTextRouter.Parse("",Context()).Kind,Is.EqualTo(EchoTextKind.Reject));Assert.That(EchoTextRouter.Parse(new string('查',201),Context()).Kind,Is.EqualTo(EchoTextKind.Reject));var c=new EchoTextContext(new[]{new EchoTextTarget("a","对象",true,new string('字',400),true)},"a",1);var r=EchoTextRouter.Parse("报告这个",c);Assert.That(r.Text.Length,Is.LessThanOrEqualTo(280));Assert.That(r.Text,Does.StartWith("（报告已过时"));}
        [Test]public void ContextCopiesAndRejectsDuplicateIdentity(){var source=new[]{new EchoTextTarget("a","对象",true)};var c=new EchoTextContext(source,null,1);source[0]=new EchoTextTarget("b","另一个",true);Assert.That(c.Targets.Single().Id,Is.EqualTo("a"));Assert.Throws<ArgumentException>(()=>new EchoTextContext(new[]{source[0],source[0]},null,1));}
        [Test]public void NoTargetsStillClarifiesWithoutInventingOptions(){var r=EchoTextRouter.Parse("调查这个",new EchoTextContext(Array.Empty<EchoTextTarget>(),null,1));Assert.That(r.Kind,Is.EqualTo(EchoTextKind.Clarify));Assert.That(r.Choices,Is.Empty);}
    }
}
