# S3-E03c2 自动生命周期与全局开关

2026-10-27，Unity6000.6.7f1 / Windows / D3D11普通Forward内部HDR/SDR，统一门禁通过；开发机i7-12650H/RTX4060 Laptop/16GB。最低i5-8400/GTX1060 6GB/16GB中1080p60及推荐i5-12400/RTX3060 6GB/16GB高1080p60仍需对应实机。

S3DialogueDriver默认false、惰性正式Qwen3工厂，仅主线程Poll。接记忆Changed/Replaced、区域/进度/设备交互、加载/新局、结束选择/确认、禁用/销毁；可信应用自身记忆更新不自取消。全局UseLLM在自动三故障后同帧镜像false，防旧true下一帧重新启用。组件禁用保留同一Host的异步排空/冷却，重新启用不重叠Owner；销毁才Dispose且不主线程等待。

Lifecycle_A作业3d43ce90712b442cba414da892dbcb5a：12/12、0失败/跳过、7.7384809秒（新生命周期6项+会话桥6项回归）。入口`Tools/MCP/module_tests.py start/poll --evidence Docs/Tests/Stage3/Evidence/S3-E03c2 --label Lifecycle_A --assembly EchoStation.AI.Execution.Editor.Tests --group '^EchoStation.Tests.AIExecution.(S3DialogueDriverTests|StationLlmSessionTests)'`。regression_host Lifecycle_A工作区/Play选项/管线已还原。

明确Editor-only控制Factory/时钟验证：全局开关启动/自己接受的命令记忆不自取消、记忆变更/替换丢旧结果并解除旧Store监听、三故障后不自行重开且后续脚本、组件禁用重开PeakOwner=1/旧结果0、结束待选作废/确认关闭、加载取消<100ms且清理恰一次。结束条件用夹具设置，不当真实结局路线。正式Player不编译测试依赖入口；真实模型→游戏及Tab开关仍待下一叶，工程总模块仍15/24，不能据此声称E03完成。
