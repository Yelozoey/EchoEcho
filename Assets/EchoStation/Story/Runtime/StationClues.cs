using System;
using System.Collections.Generic;
using System.Linq;
using EchoStation.Memory;

namespace EchoStation.Story
{
    public sealed class StationClue
    {
        public string Id { get; }
        public string ZoneId { get; }
        public string Title { get; }
        public string FullText { get; }
        public string Summary { get; }
        public MemoryClueKind Kind { get; }
        public string Password { get; }
        public string KeyMarker { get; }
        public StationClue(string id, string zone, string title, string full, string summary, MemoryClueKind kind, string password = null, string marker = null)
        {
            var validated = new ClueMemory(id, title, summary, kind, password, marker);
            if (!new[] { "valley", "cabin", "lab_a", "vent", "core" }.Contains(zone) || string.IsNullOrWhiteSpace(full) || full.Length < 120 || full.Length > 240 || full == summary || password != null && (!full.Contains(password) || summary.Contains(password))) throw new ArgumentException("Invalid station source text or metadata.");
            Id = validated.Id; ZoneId = zone; Title = validated.Title; FullText = full; Summary = validated.Summary; Kind = kind; Password = validated.Password; KeyMarker = validated.KeyMarker;
        }
    }

    public static class StationClues
    {
        private static readonly StationClue[] items = {
            new StationClue("note_entry_inspection_01", "valley", "入口巡检记录",
                "入口巡检留档：02:13，地下A区首先出现排风异常报警。山谷气象设备仍在连续采样，入口照明与通信指示均正常。值班员从小屋请求核对实验室回路，没有立即切断全站电源。02:16之后，入口才收到人工隔离实验支路的通知。请保留这份本地时间记录，设备上的断电时间不能单独用来判断事故起点。下一班复查人员应将此记录与撤离通知对照，不要把先后相隔的两次事件合并为一次故障。",
                "入口记录显示02:13先报警，02:16后才收到人工隔离实验支路通知。", MemoryClueKind.Note),
            new StationClue("note_evacuation_01", "valley", "撤离通知副本",
                "撤离通知：值班组于02:16执行实验支路隔离，于02:19组织人员离站。隔离的目的是限制异常回流，不是启动新的测试。低功耗生态监测支路保持供电，资料应留在本地终端等待复查。通知签发前，实验室已经报告排风异常；现场不再安排人员进入核对。若入口巡检时间与本通知一致，可确认报警发生在人工断电之前。复查人员应先从地面小屋取得维护授权，再进入地下区域，禁止未经确认恢复实验支路。",
                "撤离通知把02:16断电标为报警后的隔离措施，02:19人员撤离。", MemoryClueKind.Note),
            new StationClue("note_maintenance_01", "cabin", "小屋维护记录",
                "地面维护备忘：返站复查人员可使用服务码7316登录小屋维护终端，读取地下通路授权和停站后的工作交接。这个服务码只用于地面终端，不能代替通风通道的校准凭证。储藏室东墙留有旧校准定位标记，普通照明下不易辨认，可让Echo靠近扫描。终端读取后，地下入口仍需由复查人员亲自确认开启。请勿让辅助机器人代输密码或切换设备；它只负责检查、记录与提供线索。旧值班记录也应一并带走核对。",
                "小屋维护记录写有地面终端服务码，并提示储藏室东墙存在可扫描定位标记。", MemoryClueKind.Note, "7316"),
            new StationClue("terminal_data_01", "cabin", "小屋终端交接数据",
                "维护终端交接：地下通路授权已恢复，开门操作仍需现场人员确认。A区的排风支路与实验供电不是同一回路，返站检查时应先恢复必要通风，再判断其余设备状态。储藏室定位标记指向地下校准架，不是门锁开关。通风通道另有维护终端，它需要当前校准记录，不能重复使用地面服务码。原始事故日志分散在实验室、通道和核心控制室；只有对照来源和时间，才能区分测试、报警、人工隔离与人员撤离。",
                "地下通路已授权；实验室、通道和核心分别保存可互证的事故资料。", MemoryClueKind.Data),
            new StationClue("scan_mark_01", "cabin", "储藏室定位扫描",
                "Echo扫描结果：墙面标记由旧扫描涂层构成，完整轮廓呈向下的路径箭头，文字指向地下A区校准架。标记中的定位序列可用于核对同组设备，但不具有解锁或送电权限。另一半校准信息仍在A区，需要抵达对应位置再次扫描。普通照片无法确认涂层中的校准关系，因此这条记录仅在有效近距扫描后归档。若地下入口尚未开放，应回看小屋维护终端；进入A区后，请明确指示Echo调查校准架附近的可疑标记。",
                "储藏室扫描标记指向地下A区校准架，需要在那里继续扫描校准信息。", MemoryClueKind.Scan, marker: "scan_lab_calibration_01"),
            new StationClue("note_roster_01", "cabin", "值班交接备忘",
                "值班交接备忘：我们撤离时保留了基础监测，没有保留实验运行条件。地下设备的标签可能相似，复查时请逐项确认排风、内循环和实验支路，不能只凭指示灯亮灭判断用途。完整回路说明放在A区供电面板旁，任何送电都应以那份说明为准。Echo可以识别异常设备并提醒保持距离，但没有人工确认权限。若扫描记录与纸面文档出现不同表述，请把两者记入日志，保留原始来源，再到核心核对传感器时序。",
                "交接要求在A区查看完整回路说明，区分通风与实验供电，不凭指示灯猜用途。", MemoryClueKind.Note),
            new StationClue("note_lab_circuit_01", "lab_a", "A区通风回路说明",
                "A区供电说明：复查通风只需要独立排风支路。正确状态为排风开启、内循环关闭、实验支路关闭，完成检查后由现场人员按下确认送电。内循环重新接入会把测试区域的水汽和残余雾气带回通道，实验支路也不应随通风一起恢复。错误组合不会执行送电，请核对标签后重试。通风恢复后，入口附近的局部雾气会逐渐消退；这并不代表实验设备可以重新运行。Echo的扫描结果只是信息，不能代替回路确认。",
                "通风要求排风开启、内循环关闭、实验支路关闭，再由玩家确认送电。", MemoryClueKind.Note),
            new StationClue("data_lab_maintenance_01", "lab_a", "A区维护指令记录",
                "维护指令记录：停站前的测试使用了尚未复核的排风传感器校准值。指令把异常读数解释为支路未接通，随后将实验区错误接入内循环。记录中能看到测试请求与回路切换先于报警，不能据此推断报警是人工隔离引起的。请将本记录与通道维护回执、核心传感器时序对照，区分指令发出与实际反馈。低功耗生态监测在独立支路上，原始指令没有要求停用它；能否保留运行仍须在核心确认隔离条件。",
                "未复核的排风校准值导致错误接入内循环；应与通道回执和核心时序互证。", MemoryClueKind.Data),
            new StationClue("scan_lab_calibration_01", "lab_a", "A区校准架扫描",
                "Echo扫描结果：校准架的隐层标记注明，配对位置为小屋储藏室。两处记录都取得后才能复核定位序列。通道维护码为0428，供人工在通风通道终端输入。通道校准凭证需要两处真实扫描记录共同构成，单独抄写数字不能代替验证链。该标记只证明维护序列对应，不表示任何危险支路已经安全。扫描不会打开门、修改回路或替你提交终端指令。请在通风恢复、通路可达后前往维护终端，核对本记录，再由你输入维护码完成访问确认。",
                "A区扫描取得通道维护码；凭证需要小屋与A区两处真实扫描记录共同验证。", MemoryClueKind.Scan, "0428", "vent_calibration_valid"),
            new StationClue("note_lab_sensor_01", "lab_a", "传感器复核便签",
                "传感器复核便签：排风读数的校准偏差不会自动改变回路，真正的支路切换来自后续维护指令。复查时要把测量、判断和操作分别记录，避免把它们合成一个原因。生态监测使用独立采样与低功耗供电，不应与实验控制一同重启。局部雾气主要是停站后的水汽和残余测试介质，恢复排风可改善通行，但不能据此判断全部设备已修复。需要完整事故结论时，请到通道与核心补齐实际反馈记录。",
                "应区分传感器测量、维护判断和回路操作；局部通风恢复不等于实验设备修复。", MemoryClueKind.Note),
            new StationClue("terminal_vent_data_01", "vent", "通道维护回执",
                "通道维护回执：校准记录已由现场人员确认。停站日志显示实验区域先接入内循环，随后监测到回流并触发排风异常报警；人工隔离操作发生在其后。该回执记录实际设备反馈，与A区维护指令是不同来源。核心控制室还保留独立传感器时序，可用于最终核对。核心通路授权已经开放，门仍需由你现场确认开启。请保留此回执，不要把通风暂时恢复解释为允许恢复实验；低功耗监测是否可保留要按核心说明处理。",
                "通道反馈证实内循环接入与回流先于报警，人工隔离在后；核心通路获授权。", MemoryClueKind.Data),
            new StationClue("note_vent_route_01", "vent", "通风通道维护提示",
                "通道维护提示：入口雾气消退后方可继续复查，不要从停站设备的缝隙越过隔离边界。这里的终端接收A区校准维护码，同时检查储藏室和A区的扫描凭证。若只有数字而验证链不完整，应返回补齐扫描；若两处记录齐全，请检查是否输入了通道维护码，而不是小屋服务码。终端只向现场人员提供访问确认，Echo可以整理已有记录但不能替你提交。原路始终保留，可回看遗漏的资料，不需要反复切换已经确认的通风回路。",
                "通道终端检查两处扫描凭证及通道维护码；可原路返回补齐记录。", MemoryClueKind.Note),
            new StationClue("data_core_sensor_01", "core", "核心传感器时序",
                "核心传感器时序：实验支路测试请求之后，排风读数首先偏离复核范围，内循环切换使异常空气回流，随后才出现排风报警和人工隔离。该时序与A区维护指令、通道设备回执可以相互核对，支持校准偏差经错误维护判断引发回流的结论。生态监测支路在整个过程中保持独立，隔离实验供电后仍可低功耗采样。是否保留监测由返站人员确认。请在日志中完成三份资料的核对，不要仅凭一条时间线替代完整证据链。",
                "核心时序支持校准偏差与错误回路判断造成回流，并证明低功耗监测可独立保留。", MemoryClueKind.Data),
            new StationClue("note_core_shutdown_01", "core", "核心处置说明",
                "核心处置说明：现场人员可选择封存撤离，保持危险实验支路停止并带走现有记录；若已完成事故资料互证，可选择仅保留独立低功耗监测，由Echo按明确约定留站值守。完整归档还需要入口时间线与核心事故证据均核对完成，之后可将全套记录写入本地随身存储并与Echo撤离。这些选择都必须由你在核心终端确认，机器人不会代选。证据不足时可以取消并沿原路补查，最终确认也不会自动覆盖之前的手动存档。",
                "核心有封存撤离、保留监测、完整归档三种人工选择，条件不足可返回补查。", MemoryClueKind.Note)
        };
        private static readonly IReadOnlyList<StationClue> view = Array.AsReadOnly(items);
        private static readonly Dictionary<string, StationClue> byId = items.ToDictionary(c => c.Id, StringComparer.Ordinal);
        public static IReadOnlyList<StationClue> All => view;
        public static bool TryGet(string id, out StationClue clue) { clue = null; return id != null && byId.TryGetValue(id, out clue); }
    }
}
