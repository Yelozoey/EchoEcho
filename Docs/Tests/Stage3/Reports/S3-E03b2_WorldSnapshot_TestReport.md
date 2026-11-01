# S3-E03b2 实际站点可信快照来源

2026-10-27，Unity6000.6.7f1 / Windows / D3D11普通Forward内部HDR/SDR，统一门禁通过。开发机i7-12650H/RTX4060 Laptop/16GB；最低i5-8400/GTX1060 6GB/16GB中1080p60及推荐i5-12400/RTX3060 6GB/16GB高1080p60不由快照测试证明。

StationInferenceSnapshotSource仅主线程读取当前站点、相机视锥、同PhysicsScene墙体遮挡、注册目标/权限及可信别名；生成纯Snapshot与只读目录，仅Snapshot交后台。最多128候选/8许可。现有小屋维护记录ID明确指向储藏室扫描点，作为KnownReference；原KeyMarker/已知危险规则保持，不读取或发送日志全文，普通文字提及不授权。

World_A作业bfe88c01e0a549c9a2f7bd7c1dd17db4：4/4、0失败/跳过、2.580968秒。入口`Tools/MCP/module_tests.py start/poll --evidence Docs/Tests/Stage3/Evidence/S3-E03b2 --label World_A --assembly EchoStation.AI.Execution.Editor.Tests --group '^EchoStation.Tests.AIExecution.StationSnapshotSourceTests'`。实际相机转向/临时墙遮挡及移除还原、未知跨区拒绝/已知记录唯一指向、目标禁用后新快照去除而旧快照稳定、后台调用先于任何Unity读取被拒绝均通过。

regression_host World_A原布局/Play选项/管线绑定已还原，场景修改仅Play副本，未写S1/S2资产。b2通过；后端会话桥c仍待实施，此非完整自然语言游戏验收。
