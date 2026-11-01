using System;
using System.Collections.Generic;
using System.Linq;
using EchoStation.Dialogue.Commands;
using EchoStation.Dialogue.Knowledge;
using EchoStation.Echo;
using EchoStation.Memory;
using EchoStation.Story;
using NUnit.Framework;

namespace EchoStation.Tests.Story
{
    public sealed class StationStateAnswerTests
    {
        private static void Add(MemorySession m, string id) { StationClues.TryGet(id,out var c); m.Store.AddClue(new ClueMemory(c.Id,c.Title,c.Summary,c.Kind,c.Password,c.KeyMarker)); }
        private static void Zone(MemorySession m,string zone) {
            var c=new CharacterMemory(ScriptKnowledgeResponder.CurrentZoneMemoryId,CharacterMemoryKind.PlayerKnowledge,"玩家当前区域。",zone);
            if(m.Store.TryGetCharacter(c.Id,out _)) m.Store.UpdateCharacter(c); else m.Store.AddCharacter(c);
        }
        private static ScriptParseResult Q(string text)=>new ScriptCommandParser(Array.Empty<ScriptTarget>()).Parse(text);
        private static ScriptKnowledgeResponder R(MemorySession m,IEnumerable<HintPlan> hints=null)=>new ScriptKnowledgeResponder(m,StationScript.CreateSearch(),hints??StationScript.Hints,new[]{"7316","0428"});
        private static IEnumerable<HintPlan> Plans()=>StationScript.Hints;
        [TestCase("核心终端的密码是多少",false,false)]
        [TestCase("核心处置终端的密码是多少",true,true)]
        [TestCase("core_terminal_01密码",true,false)]
        [TestCase("供电面板的密码是什么",false,false)]
        [TestCase("power_panel_01密码",true,false)]
        [TestCase("core_door_01密码",true,false)]
        [TestCase("cabin_door_01密码",true,false)]
        [TestCase("未登记终端的密码是多少",true,false)]
        [TestCase("mystery_device_01密码",true,false)]
        [TestCase("实验支路的维护码是什么",false,false)]
        public void D06_C05_NonPasswordDevicesCannotReciteAnotherKnownDevicesCode(string query,bool both,bool coreNote)
        {
            var m=new MemorySession();Add(m,"note_maintenance_01");if(both)Add(m,"scan_lab_calibration_01");if(coreNote)Add(m,"note_core_shutdown_01");var before=m.CaptureJson();
            var responder=R(m);Assert.That(responder.Answer(Q(query)).Content,Is.EqualTo(ScriptKnowledgeResponder.NoRecords));Assert.That(responder.LastContext.Text,Does.Not.Contain("7316"));Assert.That(responder.LastContext.Text,Does.Not.Contain("0428"));Assert.That(m.CaptureJson(),Is.EqualTo(before));
        }
        [TestCase("cabin_terminal_01密码是多少","7316","0428")]
        [TestCase("vent_terminal_01密码是多少","0428","7316")]
        public void D06_C06_TerminalIdsResolveOnlyTheirKnownSource(string query,string expected,string other){var m=new MemorySession();Add(m,"note_maintenance_01");Add(m,"scan_lab_calibration_01");var r=R(m);Assert.That(r.Answer(Q(query)).Content,Does.Contain(expected));Assert.That(r.Answer(Q(query)).Content,Does.Not.Contain(other));}
        [TestCase("密码是多少")][TestCase("请告诉我维护码是什么")][TestCase("终端的密码是多少")][TestCase("请问服务码呢")]
        public void D06_C07_GenericExplicitRequestsStillUseOnlyKnownMetadata(string query){var m=new MemorySession();Add(m,"note_maintenance_01");Assert.That(R(m).Answer(Q(query)).Content,Does.Contain("7316"));}
        [TestCaseSource(nameof(Plans))] public void D06_C01_HintRequiresEveryKnownSourceAndCorrectZoneAndResets(HintPlan plan)
        {
            var m=new MemorySession();var r=R(m,new[]{plan});Zone(m,plan.ZoneId);
            foreach(var required in plan.Required){ Assert.That(r.Answer(Q("给我提示")).Content,Does.Contain("没有足够资料")); Add(m,required.Id); }
            var before=m.CaptureJson();
            for(var i=0;i<4;i++)Assert.That(r.Answer(Q("给我提示")).Content,Is.EqualTo(plan.Levels[Math.Min(i,2)]));
            Assert.That(m.CaptureJson(),Is.EqualTo(before)); Zone(m,"other_zone");Assert.That(r.Answer(Q("给我提示")).Content,Does.Contain("没有足够资料"));
            Assert.That(m.TryRestore(before,out _),Is.True);Assert.That(r.Answer(Q("给我提示")).Content,Is.EqualTo(plan.Levels[0]));
            if(plan.CompletedEventId!=null){m.Store.AddEvent(plan.CompletedEventId,0,"已完成。");Assert.That(r.Answer(Q("给我提示")).Content,Does.Contain("没有足够资料"));}
            m.Reset();Assert.That(r.Answer(Q("给我提示")).Content,Does.Contain("没有足够资料"));
        }
        [Test] public void D06_C02_UnknownOneKnownBothKnownPasswordsAreSpecificAndNeverInSummary()
        {
            var m=new MemorySession();var r=R(m);
            Assert.That(r.Answer(Q("小屋终端的密码是多少")).Content,Is.EqualTo(ScriptKnowledgeResponder.NoRecords));
            Add(m,"note_maintenance_01");Assert.That(r.Answer(Q("小屋终端的密码是多少")).Content,Does.Contain("7316"));
            Assert.That(r.Answer(Q("通道终端的密码是多少")).Content,Is.EqualTo(ScriptKnowledgeResponder.NoRecords));
            Add(m,"scan_lab_calibration_01");
            var cabin=r.Answer(Q("小屋终端的密码是多少")).Content;Assert.That(cabin,Does.Contain("7316"));Assert.That(cabin,Does.Not.Contain("0428"));
            var vent=r.Answer(Q("通道终端的密码是多少")).Content;Assert.That(vent,Does.Contain("0428"));Assert.That(vent,Does.Not.Contain("7316"));
            foreach(var input in new[]{"整理线索","给我提示","小屋记录是什么？"}){var text=r.Answer(Q(input)).Content;Assert.That(text,Does.Not.Contain("7316"));Assert.That(text,Does.Not.Contain("0428"));}
        }
        [Test] public void D06_C03_StorySpecificLinesGateAllSourceSubsetsAndRestoreHasNoReplay()
        {
            foreach(var line in StationScript.Lines.Where(l=>l.KnownClues.Count>0)){
                for(var bits=0;bits<(1<<line.KnownClues.Count);bits++){
                    var m=new MemorySession();for(var i=0;i<line.KnownClues.Count;i++)if((bits&(1<<i))!=0)Add(m,line.KnownClues[i]);
                    var expected=bits==(1<<line.KnownClues.Count)-1?line.Text:ScriptKnowledgeResponder.NoRecords;
                    Assert.That(StationScript.KnownLine(line.Id,m.Store.CaptureSnapshot()),Is.EqualTo(expected));
                    var json=m.CaptureJson();Assert.That(m.TryRestore(json,out _),Is.True);Assert.That(StationScript.KnownLine(line.Id,m.Store.CaptureSnapshot()),Is.EqualTo(expected));
                }
            }
        }
        [Test] public void D06_C04_AllKnownRecordsStaySummariesUnderContextBudgetAndNoProgressMutation()
        {
            var m=new MemorySession();foreach(var c in StationClues.All)Add(m,c.Id);Zone(m,"core");var r=R(m);var before=m.CaptureJson();
            foreach(var input in new[]{"整理线索","为什么会发生事故","密码是多少","给我提示"}){
                var a=r.Answer(Q(input));Assert.That(a.Kind,Is.EqualTo(IntentKind.Answer));Assert.That(a.Content.Length,Is.LessThanOrEqualTo(280));
                Assert.That(r.LastContext.Utf8Bytes,Is.LessThanOrEqualTo(1200));Assert.That(r.LastContext.Entries.Count,Is.LessThanOrEqualTo(8));
                foreach(var c in StationClues.All)Assert.That(r.LastContext.Text,Does.Not.Contain(c.FullText));
            }
            Assert.That(m.CaptureJson(),Is.EqualTo(before));
        }
    }
}
