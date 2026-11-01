# S3-S01b2 专用场景启动与脚本基线

2026-10-27，Unity6000.6.7f1 / Windows / D3D11普通Forward内部HDR/SDR统一门禁通过；开发机i7-12650H/RTX4060 Laptop/16GB。最低i5-8400/GTX1060 6GB/16GB中1080p60与推荐i5-12400/RTX3060 6GB/16GB高1080p60仍待对应实机。

Startup_A作业44e396e378a94586a0548ab44206cfe9：2/2、0失败/跳过、1.9142606秒。实际加载S3Bootstrap.ScenePath：S3标签和Saves/S3/slot1.json路径、唯一Bootstrap/Driver/模式UI、默认UseLLM=false且OwnerThreadId=0、真实Echo导航绑定、标题说明S3与离线AI/脚本、内部HDR/SDR均符合预期；合成真实W键位移>0.1m，文字跟随/待命脚本动作通过，仍无模型加载。

入口`Tools/MCP/module_tests.py start/poll --evidence Docs/Tests/Stage3/Evidence/S3-S01b2 --label Startup_A --assembly EchoStation.AI.Execution.Editor.Tests --group '^EchoStation.Tests.AIExecution.S3StartupTests'`。regression_host Startup_A原布局/Play选项/管线已还原；默认目录只绑定未写用户单槽。b2通过，下一叶独立临时目录实际全状态保存/恢复与在途取消，不提前称整个S01完成。
