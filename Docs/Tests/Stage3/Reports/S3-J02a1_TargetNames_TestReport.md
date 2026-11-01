# S3-J02a1 可信目标名称与别名

2026-10-27；Unity6000.6.7f1，Windows开发机i7-12650H/RTX4060 Laptop/16GB；D3D11、普通Forward、内部HDR/SDR由统一门禁确认。本模块纯数据测试不证明最低硬件1080p中画质60FPS（i5-8400/GTX1060 6GB/16GB）或推荐硬件1080p高画质60FPS（i5-12400/RTX3060 6GB/16GB）。

实现：独立无引擎授权程序集，128项有界可信目录、每项16别名、NFKC匹配、ASCII ID完整边界，返回只读唯一/未知/多目标结果。保留储藏室可疑位置等既有中文别名，不查GameObject、不执行动作。

执行：`Tools/MCP/module_tests.py start/poll --evidence Docs/Tests/Stage3/Evidence/S3-J02a1 --label Names_A --assembly EchoStation.AI.Editor.Tests --group '^EchoStation.Tests.AI.TargetNameMatcherTests'`。作业b6718e666f324a8e9a09a997d0831f09，14/14通过、0失败/跳过，0.1138841秒。证据为同目录Names_A.json及门禁/作业记录。

覆盖：中英文/全角ID、ID前后缀拒绝、中文别名、共享名和多ID歧义、未知目标无默认、重复/null/超容量目录、非法ID/长度/UTF16、只读列表、输入上限及128项目录。预期与实际一致，独立叶验收通过；不能据此称完整J02行动/事实权限已通过，后续进入a2。
