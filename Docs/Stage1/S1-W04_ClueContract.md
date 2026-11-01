# S1-W04 线索数据与表现契约、独立测试计划

2026-10-25。前置W03独立4/4通过。临时原型文本由连续授权委托代理编写，仅用于S1读/扫/存档，不确定S2最终谜题与结局。

四项：维护笔记note_maintenance_01、值班记录note_roster_01、终端观测数据terminal_data_01、隐藏校准标记scan_mark_01。ScriptableObject保存ID/标题/原文/独立人工摘要/Note或Data或Scan分类；ID必须匹配WorldTarget，摘要<=120字符且不能与原文相同，全文<=4000字符。原文只供玩家日志；未来AI接口只取摘要，禁止以全文填上下文。

ClueSource只公开TryRead和ClueId/分类/可见状态；未揭示Scan返回false且不泄露Record。扫描授权由后续E08实现，本模块不提供公开“玩家解锁”入口。E08之前隐藏标记保持不可读/Renderer关闭。Note/Data可重复读取相同不可变记录，记录与收集分离，暂不实现E输入、日志去重或谜题状态。

表现为动态独立薄纸片/小数据片/墙面标记（不破坏8个已烘焙房间合并体），URP Lit/无静态标记/BlendProbes、各自BoxCollider和WorldTarget在同GO上；隐藏标记Collider保持供Echo后续感知，但玩家读取被拒绝。独立测试数据校验拒绝空/相同摘要/不匹配ID；反复读稳定；隐藏返回false；场景4项实际数据和表现、Collider/无错误静态合并。后续E08增加只面向Echo执行层的扫描权限路径并独立测试。
