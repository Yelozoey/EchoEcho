# S3-E01 传统执行绑定验收

2026-10-27，**E01工程通过，S3为14/24**。a映射9/9、b应用门含回归28/28已独立归档；c真实场景作业a56b4bc9112346f3bfb2984223a6799f，Game_B 3/3通过、0失败/跳过、7.8999751秒。

执行`Tools/MCP/regression_host.py enter/leave --evidence Docs/Tests/Stage3/Evidence/S3-E01c --label Game_A`隔离并还原原布局，模块入口`Tools/MCP/module_tests.py start/poll --evidence Docs/Tests/Stage3/Evidence/S3-E01c --label Game_B --assembly EchoStation.AI.Execution.Editor.Tests --group '^EchoStation.Tests.AIExecution.TraditionalIntentExecutionTests'`。A测试尚未启动即被已知IAP错误拦截；原始记录保留，核对清理后B实测。

实际覆盖六种意图：跟随实际位移>0.15m，待命0.3秒位移<0.02m；调查经真实导航扫描，隐藏标记揭示且摘要进入已知记忆；警告仍受距离/角度感知约束，远处拒绝不记成功，近处成功记已知危险，目标姿态/权限不变。Answer/Refuse不改行为/记忆；模型夹具标记UNTRUSTED_MODEL_FACT_9999未显示或写入。只有可信执行接受记录命令摘要，扫描事实由真实游戏事件归档。

该叶使用手写结构化JSON隔离执行，不宣称新真实推理。模型→游戏完整链及LLM on三结局仍待T04；UI单消费者/开关及持久化待后续模块，不能称S3可玩完成。

Unity6000.6.7f1 / Windows / D3D11普通Forward内部HDR/SDR，开发机i7-12650H/RTX4060 Laptop/16GB。目标最低i5-8400/GTX1060 6GB/16GB中1080p60、推荐i5-12400/RTX3060 6GB/16GB高1080p60须实机；本测试无该性能结论。WorkspaceRestored回执确认原布局/Play选项/管线绑定还原；保护脚本767/767 SHA未变。当前进入E02，不进入S4。
