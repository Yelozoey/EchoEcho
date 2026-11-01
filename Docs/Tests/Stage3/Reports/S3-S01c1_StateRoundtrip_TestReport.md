# S3-S01c1 全状态与独立原子单槽往返

2026-10-27，Unity6000.6.7f1 / Windows / D3D11普通Forward内部HDR/SDR统一门禁通过；开发机i7-12650H/RTX4060 Laptop/16GB。最低i5-8400/GTX1060 6GB/16GB中1080p60与推荐i5-12400/RTX3060 6GB/16GB高1080p60不由本存档测试证明。

Roundtrip_A作业27e4518ff18c4cdba6fd8d5f1f884bfb：1/1、0失败/跳过、6.3565245秒。实际S3专用场景读取维护记录、导航扫描储藏室、设置真实视角/手电与60条历史，然后S3临时目录保存两次、恢复初始状态、读主档。2条日志/对应摘要记忆、扫描凭证、视角215/12、位置、手电及最近50条历史准确恢复；主/备均有效且记忆一致、tmp不存在。

存档无UseLLM/TokenIds/Prepared/requestId/past_key_values字段，启动与恢复脚本模式无Owner创建。相邻临时S1/S2哨兵逐字节不变；只用Library/EchoStation/S3Tests/WorldSaves唯一目录，不写用户单槽；清理前验证绝对路径属于测试root，失败保留现场。

入口`Tools/MCP/module_tests.py start/poll --evidence Docs/Tests/Stage3/Evidence/S3-S01c1 --label Roundtrip_A --assembly EchoStation.AI.Execution.Editor.Tests --group '^EchoStation.Tests.AIExecution.S3StateRoundtripTests'`。regression_host Roundtrip_A原布局/Play选项/管线已还原；c1通过，接下来在途读档取消与回滚，不提前称整个S01完成。
