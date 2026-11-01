# S3-E03c1 主线程会话桥

2026-10-27，Unity6000.6.7f1 / Windows / D3D11普通Forward内部HDR/SDR，统一门禁通过。开发机i7-12650H/RTX4060 Laptop/16GB；最低i5-8400/GTX1060 6GB/16GB中1080p60与推荐i5-12400/RTX3060 6GB/16GB高1080p60仍待对应实机。

StationLlmSession串联唯一输入拦截、纯Snapshot、单Owner Host、J02权限/E01应用及可信记忆执行，默认无模型创建。忙/冷却拒绝不排队、不记故障；手动覆盖/取消作废旧响应；三次解析/推理故障关路由、仅后续新输入走脚本，不重放旧指令。创建/运行/释放由Owner处理，主线程只控制/Poll。

Session_A作业95869fd428564e35a8d42e0bbf3084bd，6/6、0失败/跳过、4.039707秒。入口`Tools/MCP/module_tests.py start/poll --evidence Docs/Tests/Stage3/Evidence/S3-E03c1 --label Session_A --assembly EchoStation.AI.Execution.Editor.Tests --group '^EchoStation.Tests.AIExecution.StationLlmSessionTests'`。

实际站点/对话UI+明确可控Factory夹具：默认脚本零创建、惰性开启单后台、错follow不替代wait；忙输入无队列；三坏JSON均无动作、第三次关闭后新输入脚本；第三故障与新输入同帧仍处理新输入；手动Wait作废阻塞内核迟到Follow；取消<100ms且内核返回前不释放，完成后无动作/无故障；每次异步释放线程等于创建线程。Factory不是真实模型，不计原始语义/性能或全链推理。原布局/Play选项/管线已还原，c1通过，下一叶c2自动生命周期/全局开关与实际工厂绑定。
