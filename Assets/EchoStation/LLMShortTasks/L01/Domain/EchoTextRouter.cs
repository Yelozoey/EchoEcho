using System;
using System.Collections.Generic;
using System.Linq;

namespace EchoStation.L01
{
    public enum EchoTextKind { Follow, Wait, Cancel, Investigate, Battery, Report, Clarify, Reject }
    public sealed class EchoTextTarget
    {
        public string Id { get; } public string Name { get; } public bool Available { get; } public string Report { get; } public bool Stale { get; }
        public EchoTextTarget(string id,string name,bool available,string report=null,bool stale=false)
        {if(string.IsNullOrWhiteSpace(id)||string.IsNullOrWhiteSpace(name))throw new ArgumentException("Known target identity required");Id=id;Name=name;Available=available;Report=report;Stale=stale;}
    }
    public sealed class EchoTextContext
    {
        public IReadOnlyList<EchoTextTarget> Targets { get; } public string PointedId { get; } public int Battery { get; }
        public EchoTextContext(IEnumerable<EchoTextTarget> targets,string pointedId,int battery)
        {if(targets==null)throw new ArgumentNullException(nameof(targets));var copy=targets.ToArray();if(copy.Any(t=>t==null)||copy.Select(t=>t.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=copy.Length||battery<0||battery>100)throw new ArgumentException("Invalid known facts");Targets=Array.AsReadOnly(copy);PointedId=pointedId;Battery=battery;}
    }
    public sealed class EchoTextResult
    {
        public EchoTextKind Kind { get; } public EchoTextKind ChoiceKind { get; } public string TargetId { get; } public string Text { get; } public IReadOnlyList<string> Choices { get; }
        internal EchoTextResult(EchoTextKind kind,string text,string target=null,IEnumerable<string> choices=null,EchoTextKind choiceKind=EchoTextKind.Reject)
        {Kind=kind;Text=EchoTextRouter.LimitReply(text);TargetId=target;Choices=Array.AsReadOnly((choices??Array.Empty<string>()).ToArray());ChoiceKind=choiceKind;}
    }
    /// <summary>Explicit script grammar. Only the supplied discovered facts can be addressed.</summary>
    public static class EchoTextRouter
    {
        public const int InputLimit=200,ReplyLimit=280;
        public static string LimitReply(string value)
        {value=value??"";if(value.Length<=ReplyLimit)return value;int count=ReplyLimit-1;if(char.IsHighSurrogate(value[count-1]))count--;return value.Substring(0,count)+"…";}
        public static EchoTextResult Parse(string input,EchoTextContext context)
        {
            if(context==null)throw new ArgumentNullException(nameof(context));
            if(string.IsNullOrWhiteSpace(input))return Reject("请输入一条指令或问题。");if(input.Length>InputLimit)return Reject("输入最多200个字符，请缩短后重试。");
            var text=input.Trim().TrimEnd('。','！','!','？','?');
            if(Matches(text,"跟随我","跟着我","跟随","follow"))return Result(EchoTextKind.Follow,"已收到跟随指令。");
            if(Matches(text,"待命","原地待命","等在这里","停止跟随","停下","wait"))return Result(EchoTextKind.Wait,"已收到待命指令。");
            if(Matches(text,"取消","取消调查","取消当前任务","cancel"))return Result(EchoTextKind.Cancel,"已收到取消指令。");
            if(Matches(text,"电量","查看电量","你还有多少电","剩余电量"))return Result(EchoTextKind.Battery,"当前电量为"+context.Battery+"%。");
            // Whole-command matching above and explicit target matching below prevent partial execution.
            foreach(var forbidden in new[]{"替我","帮我拿","帮我搬","开门","解锁","关闭阀门","给设备通电","解除夹具","隔离供电"})if(text.Contains(forbidden))return Reject("我只提供跟随、扫描与信息。设备操作和物品搬运需要由你完成。");
            foreach(var prefix in new[]{"查看扫描报告","查看报告","扫描报告","扫描结果","报告"})if(text.StartsWith(prefix,StringComparison.Ordinal))return Resolve(EchoTextKind.Report,text.Substring(prefix.Length),context);
            foreach(var prefix in new[]{"调查","扫描"})if(text.StartsWith(prefix,StringComparison.Ordinal))return Resolve(EchoTextKind.Investigate,text.Substring(prefix.Length),context);
            return Reject("脚本模式可用：跟随我、原地待命、取消、调查这个、电量、报告＋目标名称。LLM尚未接入。");
        }
        static bool Matches(string value,params string[] choices)=>choices.Any(s=>string.Equals(s,value,StringComparison.OrdinalIgnoreCase));
        static EchoTextResult Result(EchoTextKind kind,string text,string target=null)=>new EchoTextResult(kind,text,target);
        static EchoTextResult Reject(string text)=>Result(EchoTextKind.Reject,text);
        static EchoTextResult Clarify(EchoTextKind operation,IEnumerable<EchoTextTarget> choices,string text)
        {return new EchoTextResult(EchoTextKind.Clarify,text,choices:choices.OrderBy(t=>t.Id,StringComparer.Ordinal).Select(t=>t.Id),choiceKind:operation);}
        static EchoTextResult Resolve(EchoTextKind operation,string raw,EchoTextContext context)
        {
            var query=raw.Trim().TrimStart(':','：').Trim();
            if(Matches(query,"这个","这里","准星目标","它"))
            {if(string.IsNullOrEmpty(context.PointedId)||!context.Targets.Any(t=>t.Id==context.PointedId))return Clarify(operation,context.Targets,"准星没有指向已发现目标。请靠近观察，或选择一个已发现目标。");return Select(operation,context.PointedId,context);}
            if(query.Length==0)return Clarify(operation,context.Targets,"请指定一个已发现目标。");
            var id=context.Targets.FirstOrDefault(t=>string.Equals(t.Id,query,StringComparison.OrdinalIgnoreCase));if(id!=null)return Select(operation,id.Id,context);
            var names=context.Targets.Where(t=>string.Equals(t.Name,query,StringComparison.Ordinal)).ToArray();
            if(names.Length==1)return Select(operation,names[0].Id,context);
            return Clarify(operation,names.Length>1?names:context.Targets,names.Length>1?"存在同名目标，请选择一个。":"未找到这个已发现目标，请从列表选择。");
        }
        public static EchoTextResult Select(EchoTextKind operation,string id,EchoTextContext context)
        {
            if(context==null)throw new ArgumentNullException(nameof(context));if(operation!=EchoTextKind.Investigate&&operation!=EchoTextKind.Report)return Reject("此选择不对应可执行请求。");
            var target=context.Targets.FirstOrDefault(t=>string.Equals(t.Id,id,StringComparison.Ordinal));if(target==null)return Reject("该目标尚未发现，或已不属于当前场景。");
            if(operation==EchoTextKind.Investigate)return target.Available?Result(operation,"准备调查"+target.Name+"。",target.Id):Reject("目标当前不在可调查位置。");
            if(string.IsNullOrEmpty(target.Report))return Reject("尚无该目标的扫描报告，请先让Echo调查。");
            return Result(EchoTextKind.Report,(target.Stale?"（报告已过时，请重新扫描）":"")+target.Report,target.Id);
        }
    }
}
