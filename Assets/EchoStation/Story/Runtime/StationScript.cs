using System;
using System.Collections.Generic;
using System.Linq;
using EchoStation.Dialogue.Knowledge;
using EchoStation.Memory;

namespace EchoStation.Story
{
    public sealed class PuzzleSpec
    {
        public string Id { get; }
        public string Zone { get; }
        public string CompletionId => "progress_" + Id;
        public string Title { get; }
        public IReadOnlyList<string> Sources { get; }
        public IReadOnlyList<string> Previous { get; }
        internal PuzzleSpec(string id, string zone, string title, string[] sources, params string[] previous)
        { Id = id; Zone = zone; Title = title; Sources = Array.AsReadOnly((string[])sources.Clone()); Previous = Array.AsReadOnly((string[])previous.Clone()); }
    }
    public sealed class EndingSpec
    {
        public string Id { get; }
        public string Title { get; }
        public string Consequence { get; }
        public bool EchoStays { get; }
        public IReadOnlyList<string> Required { get; }
        internal EndingSpec(string id, string title, string consequence, bool stays, params string[] required)
        { Id = id; Title = title; Consequence = consequence; EchoStays = stays; Required = Array.AsReadOnly((string[])required.Clone()); }
    }
    public sealed class ScriptLine
    {
        public string Id { get; }
        public string Text { get; }
        public IReadOnlyList<string> KnownClues { get; }
        internal ScriptLine(string id, string text, params string[] known)
        { Id = id; Text = text; KnownClues = Array.AsReadOnly((string[])known.Clone()); }
    }
    public static class StationScript
    {
        public static IReadOnlyList<PuzzleSpec> Puzzles { get; } = Array.AsReadOnly(new[] {
            new PuzzleSpec("pz01", "valley", "时间线互证", new[]{"note_entry_inspection_01", "note_evacuation_01"}),
            new PuzzleSpec("pz02", "cabin", "小屋维护终端", Array.Empty<string>()),
            new PuzzleSpec("pz03", "lab_a", "通风供电", Array.Empty<string>(), "pz02"),
            new PuzzleSpec("pz04", "lab_a", "校准标记", new[]{"scan_mark_01", "scan_lab_calibration_01"}),
            new PuzzleSpec("pz05", "vent", "通道维护终端", Array.Empty<string>(), "pz03", "pz04"),
            new PuzzleSpec("pz06", "core", "事故归档互证", new[]{"data_lab_maintenance_01", "terminal_vent_data_01", "data_core_sensor_01"}, "pz05")
        });
        public static IReadOnlyList<EndingSpec> Endings { get; } = Array.AsReadOnly(new[] {
            new EndingSpec("seal_exit", "封存撤离", "切断剩余站内供电，封存现场并带走现有记录；Echo随你撤离。", false, "pz02", "pz03", "pz04", "pz05"),
            new EndingSpec("monitor_stay", "保留监测", "保持实验支路隔离，仅保留低功耗监测；你明确安排Echo留站值守。", true, "pz02", "pz03", "pz04", "pz05", "pz06"),
            new EndingSpec("archive_exit", "完整归档", "完整证据写入游戏内本地随身存储，实验支路保持隔离；Echo随你撤离。", false, "pz01", "pz02", "pz03", "pz04", "pz05", "pz06")
        });
        public static IReadOnlyList<ScriptLine> Lines { get; } = Array.AsReadOnly(new[] {
            new ScriptLine("enter_valley", "入口记录可能保留停站时间。我会跟随你检查。"),
            new ScriptLine("enter_cabin", "这里是地面观测小屋。先检查维护记录和终端。"),
            new ScriptLine("enter_lab_a", "已进入地下A区。请核对回路标签，避免恢复实验供电。"),
            new ScriptLine("enter_vent", "通道内能见度较低。维护终端由你操作，我负责记录。"),
            new ScriptLine("enter_core", "核心记录可用于最后核对。处置选择由你确认。"),
            new ScriptLine("revisit", "已返回前区。现有记录仍保留，可以继续补查。"),
            new ScriptLine("need_evidence", "目前证据不足。请在日志中核对尚未收集的来源。"),
            new ScriptLine("wrong_code", "维护码未通过。请核对当前终端对应的已归档记录。"),
            new ScriptLine("need_scans", "校准凭证不完整。需要储藏室与A区两处真实扫描记录。"),
            new ScriptLine("wrong_power", "当前回路不满足通风条件。请检查标签和回路说明。"),
            new ScriptLine("power_success", "排风已恢复。请等待局部雾气消退，再继续通行。"),
            new ScriptLine("already_done", "该项已完成。现有结果保持，不需要重复操作。"),
            new ScriptLine("cabin_unlocked", "地下通路已获授权。请由你现场开门。", "terminal_data_01"),
            new ScriptLine("vent_unlocked", "核心通路已获授权。请由你现场开门。", "terminal_vent_data_01"),
            new ScriptLine("scan_cabin", "标记指向地下A区校准架。我可以在那里复核。", "scan_mark_01"),
            new ScriptLine("scan_lab", "校准信息已归档。请在通道终端核对凭证并由你输入维护码。", "scan_lab_calibration_01"),
            new ScriptLine("scan_lab_needs_cabin", "校准架记录已归档。还需回小屋储藏室扫描配对标记，才能形成凭证。", "scan_lab_calibration_01"),
            new ScriptLine("scan_credential_complete", "两处校准记录已匹配。请由你在通道终端输入已归档的维护码。", "scan_mark_01", "scan_lab_calibration_01"),
            new ScriptLine("timeline_done", "两份记录一致：报警先于人工隔离。结论已记录。", "note_entry_inspection_01", "note_evacuation_01"),
            new ScriptLine("cause_done", "三份资料互证，支持校准偏差经错误维护判断引发回流。", "data_lab_maintenance_01", "terminal_vent_data_01", "data_core_sensor_01"),
            new ScriptLine("ending_confirm", "请核对处置后果。确认后结束本次复查；也可以取消，返回补查。"),
            new ScriptLine("ending_cancel", "已取消处置。你可以继续核对记录。"),
            new ScriptLine("refuse_device", "我不能替你开门、输入密码或切换回路。请由你操作。"),
            new ScriptLine("scan_failed", "扫描条件尚未满足。请检查距离、视线和目标位置。"),
            new ScriptLine("no_records", "我目前没有相关记录。"),
            new ScriptLine("unknown_command", "抱歉，我没能理解你的指令，请换一种说法。")
        });
        private static MemoryFactRequirement C(string id) => new MemoryFactRequirement(MemoryCategory.Clue, id);
        private static HintPlan H(string id, string zone, string done, string query, string[] sources, params string[] levels)
            => new HintPlan(id, zone, done, query, sources.Select(C).ToArray(), levels);
        public static IReadOnlyList<HintPlan> Hints { get; } = Array.AsReadOnly(new[] {
            H("hint_pz01", "valley", "progress_pz01", "报警 断电 时间", new[]{"note_entry_inspection_01", "note_evacuation_01"}, "可以对照入口的两份时间记录。", "请区分报警时间、人工隔离时间和撤离时间。", "记录表明02:13报警，02:16人工隔离，02:19撤离。请在日志确认先后关系。"),
            H("hint_pz02", "cabin", "progress_pz02", "小屋 维护 终端", new[]{"note_maintenance_01"}, "维护记录可用于地面终端。", "服务码仅属于地面终端，门仍需人工开启。", "请查看已归档维护记录中的服务码，并由你在小屋终端输入。需要复述时可明确问我密码。"),
            H("hint_pz04_cabin", "cabin", "progress_pz04", "储藏室 标记", new[]{"scan_mark_01"}, "储藏室标记不是门锁开关。", "已扫描的箭头指向地下A区校准架。", "请先取得地下通路授权，再带我到A区校准架调查另一处可疑标记。"),
            H("hint_pz03", "lab_a", "progress_pz03", "通风 回路", new[]{"note_lab_circuit_01"}, "供电面板旁的说明区分了三条支路。", "恢复排风并不需要恢复内循环或实验支路。", "已读说明要求排风开启、内循环关闭、实验支路关闭，再由你确认送电。"),
            H("hint_pz04_lab", "lab_a", "progress_pz04", "校准架 标记", new[]{"scan_mark_01"}, "可以在校准架复核储藏室定位序列。", "两处隐层标记需要实际扫描，不能只抄写维护码。", "请指示我调查A区校准架可疑点；遗漏小屋扫描时仍可沿原路补查。"),
            H("hint_pz05", "vent", "progress_pz05", "通道 校准 维护码", new[]{"scan_mark_01", "scan_lab_calibration_01"}, "通道终端使用校准架取得的信息。", "凭证来自两处真实扫描，维护码属于通道终端。", "两处记录齐全后，请由你输入A区已归档维护码。需要数字时请明确询问通道密码。"),
            H("hint_pz06", "core", "progress_pz06", "维护 回执 时序", new[]{"data_lab_maintenance_01", "terminal_vent_data_01", "data_core_sensor_01"}, "三份不同来源的事故资料已齐全。", "请对照维护指令、设备实际回执和独立传感器时序。", "证据支持校准偏差经错误维护判断导致回流。请在日志确认三份资料互证，再决定处置。"),
            H("explore_valley", "valley", null, "入口 记录", Array.Empty<string>(), "先检查入口周围的巡检与撤离资料。", "可以分别阅读两份记录，再打开日志核对。", "补齐入口资料有助于完整调查；也可先沿通路进入小屋。"),
            H("explore_cabin", "cabin", null, "小屋 维护", Array.Empty<string>(), "先查看小屋内可以读取的记录。", "检查维护终端附近与储藏室，再整理日志。", "明确目标后可让我调查可疑点；密码和开门需要你亲自操作。"),
            H("explore_lab", "lab_a", null, "实验室 设备", Array.Empty<string>(), "先查看供电面板说明和实验室记录。", "校准架与通风面板承担不同用途。", "可以先补齐纸面记录，再让我扫描可疑点。设备操作由你完成。"),
            H("explore_vent", "vent", null, "通道 终端", Array.Empty<string>(), "先检查通道终端与维护提示。", "如果缺少校准资料，可以沿原路补查。", "核对日志中的扫描来源，再操作终端；不需要重新切换通风回路。"),
            H("explore_core", "core", null, "核心 记录", Array.Empty<string>(), "先读取核心时序与处置说明。", "最终选择前可以取消并返回补查。", "请在日志核对证据，再在核心终端查看各选项条件。")
        });
        public static MemorySearch CreateSearch() => new MemorySearch(new[] {
            new MemoryKeywordGroup("线索", "记录", "资料", "整理", "总结"),
            new MemoryKeywordGroup("小屋", "地面", "服务码"), new MemoryKeywordGroup("通道", "校准", "维护码"), new MemoryKeywordGroup("密码", "服务码", "维护码", "终端"),
            new MemoryKeywordGroup("报警", "断电", "隔离", "撤离", "时间"), new MemoryKeywordGroup("通风", "回路", "排风", "内循环"),
            new MemoryKeywordGroup("事故", "回流", "原因", "时序"), new MemoryKeywordGroup("标记", "扫描", "储藏室", "校准架")
        }, StationClues.All.Select(c => new MemoryAlias(MemoryCategory.Clue, c.Id,
            c.Id == "note_maintenance_01" ? new[]{c.Title,"小屋终端","小屋维护终端","地面终端","小屋密码","地面密码","cabin_terminal_01"} :
            c.Id == "scan_lab_calibration_01" ? new[]{c.Title,"通道终端","通道维护终端","通道密码","通风通道密码","vent_terminal_01"} :
            c.Id == "note_core_shutdown_01" ? new[]{c.Title,"核心终端","核心处置终端","核心密码","核心通路门","core_terminal_01","core_door_01"} :
            c.Id == "note_lab_circuit_01" ? new[]{c.Title,"供电面板","A区通风供电面板","power_panel_01"} :
            c.Id == "terminal_data_01" ? new[]{c.Title,"地下通路门","cabin_door_01"} : new[]{c.Title})));
        public static string KnownLine(string id, MemorySnapshot known)
        {
            var line = Lines.FirstOrDefault(l => l.Id == id);
            if (line == null || known == null || line.KnownClues.Any(c => !known.Clues.Any(k => k.Id == c))) return "我目前没有相关记录。";
            return line.Text;
        }
    }
}
