# S3-I02a 节流门禁叶模块测试报告

2026-10-26。前置I01归档。**5/5通过**，job `c2d85cafad794026bbe4e2e9a6f75119`，2.068秒；允许进入I02b真实间隔，I02整体未完成。

Unity6000.6.7f1/Windows11 x64/D3D11/1080p/普通Forward内部HDR-SDR，i7-12650H/RTX4060Laptop/16GB；最低i5-8400/GTX1060 6GB/16GB中档1080p60及推荐i5-12400/RTX3060 6GB/16GB高档1080p60不在本叶验证。

CompletionCooldown用单调tick及ceil(300ms)计算，反复/旧完成戳不能续期或回退；InferenceScheduler不排队，忙/结果未消费优先Busy，未Ready不启动。正常完成戳位于finally清KV之后，停止/故障后的最终资源Dispose再刷新LastActualCompletionTick，shared gate跨Owner保留。

独立5项覆盖：0/299/300ms边界与剩余1ms；重复/旧戳及倒退时钟保守拒绝；低频tick向上取整；运行中无论虚拟时间多长仍Busy，未消费结果不排队，完成后299ms拒绝/300ms接受；受控资源释放前Cleanup=false，释放后新戳被观察并在新Owner仍冷却。替身只验证协议，不计真实LLM。场景/Console/RenderSafety门禁通过。下一叶使用真实模型两次完成并记录实际≥300ms间隔。
