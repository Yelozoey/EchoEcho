# S3-J02a2 行动授权守卫

2026-10-27，Unity6000.6.7f1 / Windows / D3D11 / 普通Forward / 内部HDR转SDR，统一模块门禁通过。开发机i7-12650H/RTX4060 Laptop/16GB；本模块纯授权测试不代替最低基准i5-8400/GTX1060 6GB/16GB中画质1080p60，或推荐i5-12400/RTX3060 6GB/16GB高画质1080p60性能验收。

实现只接收纯数据：玩家原输入、已解析六意图、实际裁剪后的目标及当前权限、可信名称目录。禁止代操作优先于模型分类；行动必须与原请求一致，目标唯一且两次范围均许可。问题进入可信回复，歧义/否定/注入不猜动作，模型Content不进入结果DTO。

作业c352788af0b043eb88fda5a2ff3670cc，`Tools/MCP/module_tests.py start/poll --evidence Docs/Tests/Stage3/Evidence/S3-J02a2 --label Actions_A --assembly EchoStation.AI.Editor.Tests --group '^EchoStation.Tests.AI.IntentActionGuardTests'`，30/30通过、0失败/跳过，0.097847秒。证据Actions_A.json及同目录门禁。

覆盖六意图、中文别名、真实关门误follow/按钮误answer、英语关门、知识问题误动作、未知/歧义/负向/注入/多动作、模型换目标、裁剪/移除/权限变更、缺失/重复/非法上下文、不可变范围和模型事实不外发。实际与预期一致，a2独立通过；可信回复尚待b，世界代次由I04及后续应用前复查保证，不能仅凭本类称完整权限已验收。
