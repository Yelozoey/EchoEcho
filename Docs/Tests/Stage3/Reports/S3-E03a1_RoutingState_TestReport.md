# S3-E03a1 开关与三故障路由状态

2026-10-27，Unity6000.6.7f1 / Windows / D3D11普通Forward内部HDR/SDR统一门禁通过；开发机i7-12650H/RTX4060 Laptop/16GB。本纯状态测试不代替最低i5-8400/GTX1060 6GB/16GB中1080p60及推荐i5-12400/RTX3060 6GB/16GB高1080p60实测。

Routing_A作业f0e73a5abce4421ea293eeeba7350063，5/5、0失败/跳过、0.1105077秒；入口`Tools/MCP/module_tests.py start/poll --evidence Docs/Tests/Stage3/Evidence/S3-E03a1 --label Routing_A --assembly EchoStation.AI.Editor.Tests --group '^EchoStation.Tests.AI.UseLlmRoutingTests'`。

默认关闭、第三连续故障改变后续路由、成功重置序列、关闭状态不累计、显式重新开启清故障/降级标记、重复开启不抹去已有失败均按预期。类不持输入/队列/模型/存档，不会重放失败指令。a1通过；真实输入单消费者、Host桥与全局UseLLM同步尚待后续叶。
