# S3-I02 单请求与完成后冷却测试报告

2026-10-26。通过，S3第7/24模块。仅调度基础设施，尚未接入游戏动作或算模型JSON质量通过。

环境：Unity6000.6.7f1、Windows x64、D3D11、普通Forward内部HDR→SDR；开发机i7-12650H/RTX4060Laptop/16GB。最低基准i5-8400/GTX1060 6GB/16GB，1080p中档60FPS；推荐i5-12400/RTX3060 6GB/16GB，1080p高档60FPS，均未作目标实机验收。

先行I02a_A：5/5，2.068秒，报告见S3-I02a_Cooldown_TestReport.md。随后I02b_A真实模型加回归：6/6，19.083秒，job `5549f2ba2ce74377b29dc8fca1bb4e27`。守卫确认场景/Prefab干净、Console无错误和实际RenderSafety后启动。

独立入口：`python -X utf8 Tools/MCP/module_tests.py start --evidence Docs/Tests/Stage3/Evidence/S3-I02 --label 新标签 --assembly EchoStation.AI.Editor.Tests --group '^EchoStation.Tests.AI.RealSchedulerTests' --group '^EchoStation.Tests.AI.SchedulerTests'`，随后用同标签poll。

| 用例 | 操作与期望 | 实际 |
| --- | --- | --- |
| 精确边界 | 完成后0/299ms拒绝，300ms允许 | 通过；低频时钟向上取整，不提前放行 |
| 时序回读 | 相同/旧完成戳不得延长或回滚新冷却 | 通过 |
| 单请求 | 运行中及结果未消费时反复提交 | 均Busy，无排队 |
| 完整释放 | Owner停止后以实际Dispose完成戳更新共享门禁；新Owner不能绕过 | 通过 |
| 实际模型 | 两次真实请求，观察第一结果时未满300ms须拒绝，再等待后放行 | 96.7161ms观察时拒绝；第二次实际393.1731ms放行；两次成功、38次原生Run、Owner线程8282 |

证据：`Evidence/S3-I02/I02b_A-*`及`RealScheduler_*.json`原始完成戳/调用数。冷却从实际推理/KV清理完成计，Owner销毁时再延至完整释放点。取消和超时调用方不得另建Owner绕过门禁；具体逻辑超时/代次由后续I03/I04独立验收。

限制：该测试不覆盖完整JSON/权限、实际游戏并发性能或目标机60FPS；raw格式F120仍待J01/T01。只读源码查询曾误猜OwnerTests文件名，沿用F106，实际InferenceOwnerTests.cs已枚举后读取。
