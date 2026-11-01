# S3-I03a 请求取消与类型化故障

2026-10-26，通过17/17，6.644秒，job `276a013df9ab403f9897529d62db705a`，I03a_A证据目录`Evidence/S3-I03`。仅叶模块通过，I03整体尚未完成。

环境与最低/推荐性能基准沿用I02报告：Unity6000.6.7f1、Windows x64、D3D11、1080p普通Forward内部HDR-SDR，开发机i7-12650H/RTX4060Laptop/16GB；不能替代i5-8400/GTX1060 6GB中档60FPS或i5-12400/RTX3060 6GB高档60FPS验收。

入口：module_tests.py，assembly EchoStation.AI.Editor.Tests，group OwnerCancellationTests、InferenceOwnerTests、SchedulerTests三组全名正则；实际RenderSafety与干净场景/Console门禁通过。

新增4项：不可中断假后端取消调用<100ms、未返回时仍Running/无结果/不释放，返回后只Cancelled并可复用；四种GenerationFault分别返回类型化Timeout/Generation，下一请求成功、最终只释放一次；取消同时发生后端异常时结果作废且Owner仍Faulted并销毁；取消结果完成戳后299ms不放行、300ms允许。另8项Owner和5项调度回归通过。

Owner使用单请求CTS，发布锁内再次核验取消，避免Generate最后检查与发布之间的竞态；CTS仅Owner释放，主线程不接触native对象。GenerationException已在生成finally清KV后可恢复，其他后端错误使Owner终止，后续生命周期层再重建。替身测试不计为真实LLM质量或真实20秒超时；b继续实现逻辑截止与真实原生调用取消。
