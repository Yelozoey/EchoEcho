# EchoStation 源码职责与边界

项目代码位于 `Assets/EchoStation`，按 asmdef 划分 Runtime、Editor 与测试夹具。Editor 构建/烘焙入口在 Unity 的 Tools/EchoStation/Stage 1；测试统一通过 `Tools/MCP/module_tests.py` 的新鲜场景/Prefab/渲染绑定/Console 门禁。

| 目录 | 主要职责与类 |
| --- | --- |
| Rendering/Runtime | DistanceShadowCaster、FaultLightFlicker；ScanWaveState/Emitter/View/Feature 与全屏 Shader；ParticleEffectPool/Budget/Lease |
| Rendering/Editor | 项目专用普通 Forward、HDR/SDR、Volume/光照/实时光/合并与独立测试场景；SceneSafetyGuard/RenderBindingValidation；FrameDebuggerEvidence |
| World/Runtime | WorldTarget/TargetRegistry 唯一 ID；ClueDefinition/ClueRecord/ClueSource 原文/摘要及隐藏读取门禁 |
| World/Editor | 灰盒/PBR/合并体、Lightmap/PVS 烘焙和验证；Demo 烘焙写入专用目录，保留原小屋 |
| Memory/Runtime | S2三类不可变记忆/MemoryStore、严格MemoryJson/MemoryFileStore、关键词MemorySearch/时间预算MemoryContextBuilder、MemorySession版本与整体替换；独立无引擎引用，M01–M07已验收 |
| Memory/Integration | JournalMemoryBridge只接已归档摘要、显式已知事实、日志替换冻结与匹配恢复；无日志原文入口 |
| Dialogue/Runtime | 无引擎DialogueSession/不可变消息；玩家200/Echo280字符、100条显示/50条保存、会话版本和整体替换 |
| Player/Runtime 的对话UI | GameTextInput/GameTextBaseInput连接InputSystem字符/IME与uGUI，DialoguePanel只显示/提交文本并管理模态；经D03/D04解析/适配到既有意图执行 |
| Dialogue/Commands、Knowledge、Integration | 预设中文六意图/澄清/代操作拒绝；已知摘要/分层提示与设备范围隔离；ScriptDialogueController只通过EchoBrain/IntentDispatcher执行，快捷键共用同一入口 |
| Story/Runtime | StationClues的14份原文/独立摘要/密码元数据；StationScript六谜题、三结局、区域台词/手工别名/提示目录；纯数据，不写机关 |
| Puzzles/Runtime | PuzzleProgress与Evidence/Terminal/Power/Scan规则、三选一EndingChoice、ProgressAudit跨系统一致性与12个保留关键事件位；无Unity依赖 |
| Persistence/Runtime | StationSaveData/StrictJson/SHA256/2MiB与StationFileStore原子单槽/备份/未来版本保护；纯数据文件组件，不执行存档文字或对象命令 |
| Station/Runtime | 五区Session、玩家终端/回路/证据/结局面板、独立Lifecycle；StationRestorePlan只读准备、StationPersistence完整事务/导航重绑/失败回滚；Echo没有这些面板写入口 |
| Station/Editor、Scenes、Generated | S2专用灰盒/导航/管线/合并网格/烘焙/PVS；独立Windows测试与交付构建，不覆盖S1 |
| Tests/MemoryEditor、MemoryIntegrationEditor | 记忆纯数据/真实文件/检索/预算/生命周期与真实小屋E/扫描；75项不同用例已通过 |
| Tests/DialogueEditor、DialogueUiEditor | 会话数据、实际文字编辑/IME事件与真实Demo模态/消息复用；PlayerFixtures另有明确CLI的Windows对话诊断 |
| Echo/Runtime | EchoRig/Navigation、IntentRequest/Dispatcher/ScriptCommands、代次移动控制与行为树、Follow/Investigation/InvestigateScan/Wait/Warn、感知/扫描/三色与机械动画 |
| Echo/Runtime/EchoPersistence | 仅持久化程序集可访问的内部恢复桥；不向意图或未来 LLM 暴露显现/清理接口 |
| Player/Runtime | Input System、CharacterController 移动/跳跃、手电、射线交互；JournalStore/PlayerJournal/JournalPanel；InteractionFocus；SaveCodec/LocalSaveStore/SaveGameAdapter；CabinDemo/Controls |
| Player/Editor | 按已验证模块扩展专用玩家 Prefab；Demo 场景接线与 Windows 构建 |
| Tests/Editor、WorldEditor、EchoEditor、PlayerEditor | 配置、边界、实际物理/导航/图像与整合测试 |
| Tests/Fixtures、PlayerFixtures | 专用实际渲染/Windows 合成输入/Profiler 诊断；CLI 未启用时不执行测试流程 |
| ThirdParty | 经许可与哈希核验的 PBR/字体，运行时本地引用 |

游戏执行方向为 ScriptCommands → IntentDispatcher → 行为树/传感器/导航。S1 无模型、Prompt 拼装或推理实例；未来固定记忆→LLM JSON→传统执行层，现有公开意图权限保持。answer/refuse 只发台词，四行为只移动、扫描、待命或警告；隐藏标记只由授权扫描或合法持久化恢复显现。

日志全文仅供玩家阅读。ExportSummaries不含FullText；S2只索引摘要与规范元数据，按相关性/时间降权取最多8条、1200UTF-8字节，S3模型确定后才做精确Token预算。存档保ID/姿态/稳定状态及摘要记忆/最近50对话，按规范重建日志；原始对话历史不作为检索上下文，不执行外部文件台词或GameObject指令。

LocalSaveStore 在持久化目录执行小文件单槽原子提交，不承诺多进程同时写入。SaveGameAdapter 在应用前核对当前场景/身份/扫描授权/地面碰撞/NavMesh；恢复清在途行为、速度与旧扫描代次，重建 follow/wait。进度不会通过保存继续执行旧调查。

上述LocalSaveStore/SaveGameAdapter属于保留的S1。S2另用Saves/S2/slot1.json，全状态一次File.Replace提交；场景事务先验证规范资料/访问权限/碰撞与导航，再冻结输入/生产者、准备候选世界，导航绑定成功才整体替换日志/扫描/记忆/进度/历史。失败恢复原世界与最多100条可见历史，不发布新发现，不迁移或覆盖S1。当前无LLM推理实例；未来必须保持记忆→LLM结构化JSON→传统执行层，不能绕过现有权限。

源码故障先查 Docs/Troubleshooting.md。不能以仅编译、配置字段或测试夹具成功代替 Windows 实际行为及目标硬件验收。后续阶段按 DevelopmentPlan 顺序推进。
