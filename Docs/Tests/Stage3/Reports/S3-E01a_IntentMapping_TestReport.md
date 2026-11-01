# S3-E01a 六意图到传统接口映射

2026-10-27，Unity6000.6.7f1 / Windows / D3D11 / 普通Forward内部HDR/SDR，干净场景与Console门禁通过。开发机i7-12650H/RTX4060 Laptop/16GB；最低i5-8400/GTX1060 6GB/16GB中1080p60、推荐i5-12400/RTX3060 6GB/16GB高1080p60不由纯映射测试证明。

GuardedIntentMapper只接J02授权决定，将四行为映射到既有IntentRequest，使用已授权ID与可信短句；Answer/Refuse经可信回复，不传模型Content，无游戏对象/直接引擎引用/记忆写入。

Mapping_A作业2c641ae8a950422e8f4f843c5fbf6e68：9/9、0失败/跳过，0.0925692秒。入口`Tools/MCP/module_tests.py start/poll --evidence Docs/Tests/Stage3/Evidence/S3-E01a --label Mapping_A --assembly EchoStation.AI.Editor.Tests --group '^EchoStation.Tests.AI.GuardedIntentMapperTests'`。覆盖六意图字段/目标/可信台词、澄清无移动、无状态写与引擎引用、无授权不能映射。实际与预期一致，a独立通过。尚未调用游戏执行器，不将本结果算完整E01或真实模型游戏链通过。
