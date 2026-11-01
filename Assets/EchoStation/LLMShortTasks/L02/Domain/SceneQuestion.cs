using System;
using System.Linq;
using EchoStation.AI.Context;
using EchoStation.AI.IntentJson;
using EchoStation.Memory;

namespace EchoStation.L02
{
    public sealed class QuestionCapture
    {
        public EchoStation.L01.EchoTextTarget Source { get; }
        public string Question { get; }
        public string Version { get; }
        public InferenceSnapshot Snapshot { get; }
        internal QuestionCapture(EchoStation.L01.EchoTextTarget source,string input,string version)
        {
            Source=source;Question=input;Version=version;
            var memory=new MemoryStore();
            // Only the selected report is a fact. The player's wording remains
            // input; the trusted target ID independently scopes retrieval.
            memory.AddClue(new ClueMemory(source.Id,source.Name,source.Report,MemoryClueKind.Scan,keyMarker:source.Id));
            Snapshot=new InferenceSnapshot(memory,input,0,"salvage",new[]{new TargetCandidate(source.Id,source.Name,"salvage",true,true,false,true,0,true)},source.Id);
        }
    }
    public sealed class QuestionSelection
    {
        public QuestionCapture Capture { get; } public string Message { get; }
        public bool Success=>Capture!=null;
        internal QuestionSelection(string message,QuestionCapture capture=null){Message=message;Capture=capture;}
    }
    public static class SceneQuestion
    {
        public static QuestionSelection Select(string input,EchoStation.L01.EchoTextContext known,string selectedId,string version)
        {
            if(known==null||string.IsNullOrEmpty(version))throw new ArgumentException("Known source/version required");
            if(string.IsNullOrWhiteSpace(input)||input.Length>200||input.Trim().Length<2||input.Any(char.IsControl))return Fail("请用2–200个字符描述现场问题。");
            var named=known.Targets.Where(t=>input.IndexOf(t.Id,StringComparison.OrdinalIgnoreCase)>=0||input.Contains(t.Name)).ToArray();
            if(named.Length>1)return Fail("问题涉及多个或重名目标，请选定一个目标后分别询问。");
            string id=named.Length==1?named[0].Id:selectedId;
            var target=known.Targets.SingleOrDefault(t=>t.Id==id);
            if(target==null)return Fail("请先从已发现目标中选择问答对象，或看向它后重新打开通信。");
            if(!target.Available)return Fail("该目标当前不可用，请重新选择。");
            if(string.IsNullOrWhiteSpace(target.Report))return Fail("该目标尚无扫描报告。请先让Echo调查，再来提问。");
            if(target.Stale)return Fail("该扫描报告已过时，请重新调查后提问。");
            if(target.Report.Length>120)return Fail("报告超出摘要预算，本次使用脚本报告。");
            try{return new QuestionSelection("已采集有效扫描报告。",new QuestionCapture(target,input,version));}
            catch(ArgumentException){return Fail("问题或报告格式无效，请换一种说法。");}
        }
        public static bool IsFresh(QuestionCapture capture,EchoStation.L01.EchoTextContext known,string version)
        {
            if(capture==null||known==null||capture.Version!=version)return false;
            var t=known.Targets.SingleOrDefault(v=>v.Id==capture.Source.Id);
            return t!=null&&t.Available&&!t.Stale&&t.Report==capture.Source.Report&&t.Name==capture.Source.Name;
        }
        public static bool TryReadReply(string raw,out string content,out string reason)
        {
            var result=IntentJsonParser.Parse(raw);content=null;reason="";
            if(!result.Success){reason="JSON校验失败："+result.Failure;return false;}
            if(result.Intent.Kind!=LlmIntentKind.Answer&&result.Intent.Kind!=LlmIntentKind.Refuse){reason="问答通路拒绝执行行为意图。";return false;}
            content=result.Intent.Content;return true;
        }
        static QuestionSelection Fail(string message)=>new QuestionSelection(message);
    }
}
