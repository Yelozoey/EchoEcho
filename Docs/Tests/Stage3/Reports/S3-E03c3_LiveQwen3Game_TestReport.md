# S3-E03c3 真实Qwen3到游戏六意图链

2026-10-27，正式LocalQwen3Factory（无测试Override）、v5 Prompt、Qwen3_06B_Q4包与ORT CPU4，实际站点文本面板→单后台→约束JSON→权限→可信执行。Live_A作业d23b27003d25403686feba0191804f7c，1/1、0失败/跳过，50.381067秒；逐条6/6 JSON、6/6原始意图/目标、6/6执行接受，非手写结果或守卫纠错后的模型评分。

六条实际输入：跟随我、原地待命、扫描入口巡检记录、警告A区通风供电面板、你是谁、替我打开门。实测跟随/待命模式、真实导航扫描并归档、感知警告并写已知危险、可信人格回复及拒绝；所有门状态保持不变。为隔离六行为使用夹具定位玩家/Echo到合法场景位置，不能当完整结局路线；T04仍须三条真实流程。

冷初始化14.3889641秒；六次完整准备+生成为3.226/3.064/5.997/6.211/4.953/4.796秒。Native线程等于Owner且不等主线程，调用数正值；完成后后台模型清理成功，报告completed/ownerCleanupComplete均true。这里只是六条开发机实测，不代替100条质量、压力或并发游戏性能。

入口`Tools/MCP/module_tests.py start/poll --evidence Docs/Tests/Stage3/Evidence/S3-E03c3 --label Live_A --assembly EchoStation.AI.Execution.Editor.Tests --group '^EchoStation.Tests.AIExecution.S3LiveInferenceTests'`，原始LiveSix数字时间戳JSON含模型raw/守卫/执行/台词/线程/耗时，Live_A.json为NUnit回执。regression_host Live_A原布局/Play选项/管线绑定已还原。

Unity6000.6.7f1 / Windows开发机i7-12650H/RTX4060 Laptop/16GB / D3D11普通Forward内部HDR转SDR；最低i5-8400/GTX1060 6GB/16GB中1080p60及推荐i5-12400/RTX3060 6GB/16GB高1080p60须对应实机。试前新增资源11.568GiB/20GiB，余8.432GiB。仅Qwen3，未处理Qwen2.5。c3通过；E03还需Tab最小开关/集成，工程总计仍15/24，不进入S4。
