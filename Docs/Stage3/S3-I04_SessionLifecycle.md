# S3-I04 世界/会话代次与生命周期

2026-10-26，I03已归档后实施。先a纯代次围栏，再b单Owner主线程宿主；每叶独立测试归档后进入下一叶，不提前整合游戏。

a：SessionFence发出不可变(session, revision, request)戳。新局/读档开始新session；手动行为覆盖、世界变更、结局、场景卸载及开关变更递增revision；新的请求号使已取走但未应用的旧结果也失效。只有三个值均为当前，应用前才能放行；结果被取走不视为已应用。

b：纯数据InferenceSessionHost集中持有至多一个RequestSupervisor，保留CompletionCooldown。关闭开关/场景卸载/Dispose作废结果并异步停止；重新开启先等待旧Owner实际CleanupComplete并观察最终释放戳，才可创建新Owner；禁止新旧原生实例重叠。新局/读档/手动覆盖取消当前请求并丢弃通知，若继续启用则可复用已加载权重，KV由每请求清空。主线程只Poll，不等待后台。

实际游戏生命周期/目标权限和思考UI接线分别留E01–S01，本模块用独立可控宿主场景协议与实际模型复测验证，没有GameObject执行权限。
