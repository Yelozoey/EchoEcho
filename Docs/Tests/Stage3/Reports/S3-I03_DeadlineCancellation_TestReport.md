# S3-I03 超时、取消与恢复实际报告

2026-10-26，通过，S3第8/24模块。a协议17/17，b1截止/回归25/25，均先通过归档后进入下一叶。最终b2真实ORT/截止回归9/9，33.707秒，job `25cd94ee54c743df96ee87336baf99dc`。证据`Evidence/S3-I03/I03b2_A*`与`RealDeadline_*.json`。

环境Unity6000.6.7f1/Windows x64/D3D11/1080p/普通Forward内部HDR→SDR，开发机i7-12650H/RTX4060Laptop/16GB。最低i5-8400/GTX1060 6GB/16GB中档1080p60及推荐i5-12400/RTX3060 6GB/16GB高档1080p60仍未验收。已确认实际RenderSafety与干净场景/Console门禁。

独立入口module_tests.py，assembly EchoStation.AI.Editor.Tests，group `^EchoStation.Tests.AI.RealDeadlineTests`及`^EchoStation.Tests.AI.RequestSupervisorTests`。协议完整入口见a、b1报告。

| 真实用例 | 期望 | 实际 |
| --- | --- | --- |
| 冷加载 | 后台完成且不超30秒 | 11.183秒，主线程1/Owner8387 |
| 原生Run期间取消 | 主线程<100ms；只一次Cancelled；资源未释放前Busy | 0.8003ms，Cancelled无Generation，后台7.484秒安全返回；迟到输出丢弃 |
| 原生Run期间逻辑超时 | 提前推进测试时钟越过20秒；快速通知；不可并发重发 | 0.0153ms返回RequestTimeout，后台7.962秒返回、无第二通知 |
| 取消/超时后复用 | 两次各等待真实完成后300ms门禁再发；KV不沿用；正常生成 | 第5号请求实际EOS，1.687秒；同一Owner总21次Run，最后资源安全释放 |

测试时钟只在该测试替身Clock推进20秒，生产常量仍20秒，请求生成器独立实际时钟不变。本次不是模型自然耗时20秒的证明；20秒精确边界、300ms、不可中断阶段及晚Poll语义由b1虚拟时钟独立覆盖。没有主线程Join/Abort/native Dispose，单次原生调用可暂不可中断，逻辑结果先作废、后台实际返回后清理。

新增资源盘点9951034921字节≈9.27GiB<20GiB，细分ResourceBudget.json；模型内容仍<1GiB。完整游戏并发内存/时延分位数待V02，JSON质量F120待J01/T01；不算本模块通过内容。取消和后端异常重叠时后端仍被标故障并销毁，不因玩家取消掩盖损坏会话。
