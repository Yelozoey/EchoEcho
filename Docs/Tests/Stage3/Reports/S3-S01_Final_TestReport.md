# S3-S01 独立场景与全状态单槽验收

2026-10-27，**S01工程通过，S3为17/24**。S1/S2的767保护SHA一致；仅Qwen3-0.6B，不进入S4。

依次独立通过：a目录/标签及S2回归11/11；b1专用场景/导航静态1/1（F150夹具失败后C复测）；b2专用场景实际启动/脚本/导航2/2；c1实际记录+扫描+全状态主备往返1/1；c2不可中断在途读档取消1/1；c3故障原世界回滚1/1；c4新局确认/保留手动档/丢旧响应1/1；d正式Qwen3保存/恢复1/1。F151夹具编译访问级别已修复并保留日志。

最终Native_A作业dc658a6e7c9e4085a3e118f924d3a9d6，1/1、0失败/跳过、22.4839843秒。实际S3场景通过Tab按钮开启正式Qwen3，真实follow保存后恢复初始/读档，再真实wait；两次原始JSON和意图正确、传统执行接受。冷14.3922329秒；主线程1，Owner/Native1192，完成与资源释放均true。进度档无UseLLM/KV/请求状态：开启时读档保持true，关闭释放后读原来开启时保存的档仍false且不重新加载，源档字节保持。

入口`Tools/MCP/module_tests.py start/poll --evidence Docs/Tests/Stage3/Evidence/S3-S01d --label Native_A --assembly EchoStation.AI.Execution.Editor.Tests --group '^EchoStation.Tests.AIExecution.S3LiveSaveTests'`；NativeSave数字时间戳JSON含真实raw/线程/清理，Native_A.json为NUnit回执；regression_host Native_A原布局/Play选项/管线已还原。

交互入口：打开`Assets/EchoStation/AI/Scenes/ES_S3_AI_Demo.unity`后Play，默认脚本可探索；Tab右上角本地AI开关，T文字/Enter发送/Esc返回，首次开启后台加载；F5/F9为`Application.persistentDataPath/Saves/S3/slot1.json`完整单槽，不迁移S1/S2。五区灰盒及独立NavMeshData，渲染/美术资源只读共享；非最终Windows包。

Unity6000.6.7f1 / Windows开发机i7-12650H/RTX4060 Laptop/16GB / D3D11普通Forward内部HDR/SDR。最低i5-8400/GTX1060 6GB/16GB中1080p60及推荐i5-12400/RTX3060 6GB/16GB高1080p60仍须对应实机。最终T01新100条、T02对抗、T03压力、T04 on/off三结局和V01–V03仍待，不能称整个S3完成。
