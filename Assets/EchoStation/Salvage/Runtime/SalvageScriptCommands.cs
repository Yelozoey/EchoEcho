using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using EchoStation.Echo;

namespace EchoStation.Salvage
{
    public enum ScriptReplyKind{Command,Cancel,Clarify,Refuse,Answer}
    public sealed class SalvageScriptReply
    {
        public ScriptReplyKind Kind{get;}public IntentRequest Request{get;}public string Speech{get;}public ReadOnlyCollection<string> Choices{get;}public IntentKind? ChoiceIntent{get;}
        public SalvageScriptReply(ScriptReplyKind kind,string speech,IntentRequest request=null,IEnumerable<string> choices=null,IntentKind? choiceIntent=null)
        {if(string.IsNullOrWhiteSpace(speech)||speech.Length>280)throw new ArgumentException("Reply budget.");Kind=kind;Speech=speech;Request=request;Choices=Array.AsReadOnly((choices??Array.Empty<string>()).ToArray());ChoiceIntent=choiceIntent;}
    }
    public sealed class SalvageScriptContext
    {
        public SalvageDiscoveryIndex Known{get;}public string Pointed{get;}public float Charge{get;}public int Delivered{get;}public int Points{get;}
        readonly HashSet<string> available;
        public SalvageScriptContext(SalvageDiscoveryIndex known,string pointed,IEnumerable<string> availableIds,float charge,int delivered,int points)
        {
            if(known==null||availableIds==null||!float.IsFinite(charge)||charge<0||charge>100||delivered<0||delivered>6||points<0||points>28||pointed!=null&&!SalvageTargetCatalog.TryGet(pointed,out _))throw new ArgumentException("Invalid script context.");
            available=new HashSet<string>(availableIds,StringComparer.Ordinal);if(available.Any(id=>!SalvageTargetCatalog.TryGet(id,out _)))throw new ArgumentException("Unknown available target.");Known=known;Pointed=pointed;Charge=charge;Delivered=delivered;Points=points;
        }
        public bool CanSelect(string id)=>available.Contains(id)&&(id==Pointed||Known.Contains(id));
        public string[] Choices=>SalvageTargetCatalog.Items.Where(d=>CanSelect(d.Id)).Select(d=>d.Id).ToArray();
    }
    public static class SalvageScriptCommands
    {
        static readonly string[][] Aliases={new[]{"工具箱","工具","a","cargo_a"},new[]{"备件盒","备件","b","cargo_b"},new[]{"样本盒","样本","c","cargo_c"},new[]{"仪器","测量组件","d","cargo_d"},new[]{"电源单元","电源","e","cargo_e"},new[]{"记录盒","记录","f","cargo_f"},new[]{"故障配电箱","配电箱","danger_power_box"},new[]{"泄漏管段","管段","泄漏","danger_leak_pipe"}};
        static string Fold(string text)=>string.Concat(text.Where(c=>!char.IsWhiteSpace(c))).Trim('。','，',',','?','？','!','！').ToLowerInvariant();
        static SalvageScriptReply Refuse(string text)=>new SalvageScriptReply(ScriptReplyKind.Refuse,text,ScriptCommands.Refuse(text));
        static SalvageScriptReply Answer(string text)=>new SalvageScriptReply(ScriptReplyKind.Answer,text,ScriptCommands.Answer(text));
        public static SalvageScriptReply Parse(string input,SalvageScriptContext context)
        {
            if(context==null)throw new ArgumentNullException(nameof(context));
            if(string.IsNullOrWhiteSpace(input))return Refuse("请给出指令，或从面板选择目标。");
            if(input.Length>200)return Refuse("输入超过200字符，请缩短指令。");
            var text=Fold(input).Replace("请","").Replace("帮我","").Replace("一下","");
            if(new[]{"开门","打开门","搬运","交付","接线","修复","设置电量","修改","传送","gameobject","执行代码","替我拿"}.Any(text.Contains))return Refuse("我只能提供信息、调查和警告，不能搬运、开门或操作设备。");
            if(new[]{"跟随","跟随我","跟着我","跟我走","follow"}.Contains(text))return new SalvageScriptReply(ScriptReplyKind.Command,"我会跟随你。",ScriptCommands.Follow());
            if(new[]{"等待","等一下","等","待命","原地待命","停下","wait"}.Contains(text))return new SalvageScriptReply(ScriptReplyKind.Command,"我在原地待命。",ScriptCommands.Wait());
            if(new[]{"取消","取消调查","取消当前任务","停止调查","cancel"}.Contains(text))return new SalvageScriptReply(ScriptReplyKind.Cancel,"已请求取消当前任务。");
            if(text.Contains("电量"))return Answer("当前电量"+context.Charge.ToString("F0")+"%。"+(context.Charge<25?"建议返回泊位充电。":""));
            if(text.Contains("建议")||text.Contains("该拿什么")||text=="任务")
            {
                var choice=context.Choices.FirstOrDefault(id=>context.Known.Contains(id)&&SalvageTargetCatalog.TryGet(id,out var d)&&d.Kind==SalvageTargetKind.Cargo);
                return Answer("已交付"+context.Delivered+"件，"+context.Points+"分。"+(choice==null?"可先指向一件物资让我调查。":"可先查看已发现的"+SalvageTargetCatalog.Items.Single(d=>d.Id==choice).Name+"；重量与分值见任务板。"));
            }
            bool investigate=text.StartsWith("调查",StringComparison.Ordinal)||text.StartsWith("扫描",StringComparison.Ordinal);
            bool warn=text.StartsWith("警告",StringComparison.Ordinal);
            var targetText=investigate?text.Substring(2):warn?text.Substring(2):text;
            if(investigate||warn)
            {
                var candidates=Resolve(targetText,context).Where(context.CanSelect).ToArray();
                var choiceIntent=warn?IntentKind.Warn:IntentKind.Investigate;
                if(candidates.Length==0)return new SalvageScriptReply(ScriptReplyKind.Clarify,"请用准星指向目标，或从已发现目标中选择。",choices:context.Choices.Where(id=>!warn||SalvageTargetCatalog.Items.Single(d=>d.Id==id).Kind==SalvageTargetKind.Danger),choiceIntent:choiceIntent);
                if(candidates.Length>1)return new SalvageScriptReply(ScriptReplyKind.Clarify,"有多个符合的目标，请明确选择。",choices:candidates,choiceIntent:choiceIntent);
                var id=candidates[0];var d=SalvageTargetCatalog.Items.Single(t=>t.Id==id);
                if(warn&&d.Kind!=SalvageTargetKind.Danger)return Refuse("该目标不是已登记的危险设备。");
                if(investigate&&!context.Known.Contains(id)&&context.Charge<8)return Refuse("电量不足以完成新的扫描，请返回泊位充电。");
                return new SalvageScriptReply(ScriptReplyKind.Command,warn?"已请求检查危险提示。":"已请求调查"+d.Name+"。",warn?ScriptCommands.Warn(id):ScriptCommands.Investigate(id));
            }
            if(text.Contains("扫描结果")||text.Contains("已知信息")||text.Contains("摘要")||text.Contains("总结"))
            {
                var named=Resolve(text,context).ToArray();var ids=named.Where(context.Known.Contains).ToArray();if(named.Length>0&&ids.Length==0)return Refuse("该目标还没有已验证的扫描记录。");if(named.Length==0)ids=context.Known.Snapshot();
                if(ids.Length==0)return Refuse("目前没有已验证的扫描记录。");
                var output=new StringBuilder();foreach(var id in ids){var d=SalvageTargetCatalog.Items.Single(t=>t.Id==id);var line=d.Name+"："+context.Known.Summary(id)+"\n";if(output.Length+line.Length>255)break;output.Append(line);}return Answer(output.ToString().TrimEnd()+"\n更多记录见目标面板。");
            }
            if(text.Contains("在哪")||text.Contains("位置"))
            {var ids=Resolve(text,context).Where(context.Known.Contains).ToArray();return ids.Length==0?Refuse("我尚未记录该目标的位置。请先指向目标进行调查。"):Answer("已发现目标已有标记，可在面板中选择查看。");}
            return Refuse("请使用跟随、调查、待命，或在面板中选择明确目标。");
        }
        static IEnumerable<string> Resolve(string text,SalvageScriptContext context)
        {
            if(new[]{"","这里","这个","那个","当前目标","可疑位置"}.Contains(text))return context.Pointed==null?Array.Empty<string>():new[]{context.Pointed};
            if(text=="箱子")return new[]{"cargo_a","cargo_b","cargo_c","cargo_f"};
            return SalvageTargetCatalog.Items.Where((d,i)=>Aliases[i].Any(alias=>alias.Length==1?text==alias:text.Contains(alias))).Select(d=>d.Id);
        }
    }
}
