# Q3-R03a 调度/代次纯回归通过

2026-10-27，正式100条通过后，回归共用Owner/取消/300ms节流/会话代次与Host。仅选择已核对的InferenceOwnerTests、OwnerCancellationTests、SchedulerTests、SessionFenceTests、SessionHostTests，全部纯夹具，无旧模型权重加载；不运行旧RealOwner/RealScheduler/RealSessionHost。

Unity6000.6.7f1，作业d2294c3aff974372bde47955f1e28f80实际35/35通过，0.192411秒。覆盖单Owner/无队列、未读取结果拒新请求、不可中断阶段取消后丢弃结果、typed异常/故障、0/299/300ms与舍入边界、加载/停止/清理间隔、所有生命周期代次失效与旧结果不可执行。安全/Console门禁通过。

证据Evidence/S3-Q3-R03a/Coordination_A-Job/Coordination_A.json及安全记录。纯夹具用于调度边界，不算真实LLM生成或游戏质量；下一叶新LocalQwen3Factory配真实InferenceOwner/Scheduler，实测Native活动中的取消、恢复和停止/Owner释放。
