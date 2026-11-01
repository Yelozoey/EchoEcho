# S3-E01b 完成结果应用门

2026-10-27，Unity6000.6.7f1 / Windows / D3D11普通Forward内部HDR/SDR，统一安全门禁通过。开发机i7-12650H/RTX4060 Laptop/16GB；最低i5-8400/GTX1060 6GB/16GB中1080p60与推荐i5-12400/RTX3060 6GB/16GB高1080p60仍须对应实测。

LlmIntentApplication仅接真实Host已交付代次：成功结果+同一提交Snapshot的Prepared；严格JSON/EOS、J02授权、可信映射后，立即TryClaim再调用既有执行委托。坏JSON/TokenLimit仅固定台词；过期、重复、缺上下文/错快照零执行。返回应用/执行拒绝/解析故障分态，不自动脚本重试，不把拒绝算执行成功。

Application_A作业b74cbc3559184224a2f9363878afe87b：28/28、0失败/跳过、0.134545秒。入口`Tools/MCP/module_tests.py start/poll --evidence Docs/Tests/Stage3/Evidence/S3-E01b --label Application_A --assembly EchoStation.AI.Editor.Tests --group '^EchoStation.Tests.AI.(IntentApplicationTests|GuardedIntentMapperTests|SessionFenceTests)'`。

新8项控制Owner完成夹具覆盖单次claim、读档后丢弃、缺Prepared、同文本异Snapshot、坏JSON精确兜底、部分生成、执行器拒绝无重试、越权误follow改拒绝；每项异步Owner清理恰一次。另映射9项/Fence11项回归通过。控制夹具无真实推理，不计模型质量或游戏动作验收。b通过，下一叶c验证实际游戏执行及记忆。
