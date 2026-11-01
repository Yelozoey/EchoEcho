# S3-I02 单请求与完成后冷却

2026-10-26，前置I01已归档。保留一个请求/结果槽，不加队列；新增纯CompletionCooldown与InferenceScheduler，状态分别Accepted/Busy/NotReady/CoolingDown。所有时钟为单调Stopwatch ticks，冷却为ceil(0.3×Frequency)，不是从请求开始或主线程取结果时计时。

正常请求的结束点在生成finally清KV之后；停止/故障时Owner最终释放Session后再刷新LastActualCompletionTick，冷却不能提前从旧完成点结束。主线程Observe仅读取原子/锁内数据，不等待Native；同一完成戳反复观察不续期，旧戳不能回退。Cooldown对象可跨Owner替换保留，生命周期I04会在清理完成前拒绝新Owner。

I02a用虚拟tick和明确协议替身独立验证0/299/300ms、长任务/忙/结果未消费、停止后资源释放点及共享gate；通过后I02b真实模型两次请求与实际等待间隔。测试替身不计LLM质量。
