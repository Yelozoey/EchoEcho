# S1-E05 行为树生命周期验收

2026-10-25，v0.1。Lifecycle.json 9/9、0失败/跳过，.642秒；E05功能通过，允许E06。

根按四意图切换测试子树，Enter/Exit各一次；切换后旧树不再Tick，旧MovementLease的Move和Stop都不能调用底层端口。只有当前代次拥有移动权，四次合法移动对应3次切换清理；取消/完成/失败撤销并清路径，重复取消幂等。真实EchoNavigation实现同一IMovementPort接口，具体子树与实际Agent结合在后续模块验证。

未注册/返回null工厂拒绝并保留当前行为；answer/refuse只发消息不切树。Enter/Tick/Exit各自注入异常均停止/撤权、记录LastError、退出一次；Enter内部请求取消延迟到回调结束清理，没有遗留有效移动权。三种异常为预期隔离注入，未出现产品故障或Console报错。

测试使用计数节点/端口，不能声称已经实现follow/investigate/wait/warn具体子树；这些严格在E06/E07/E09/E10分别开发。没有LLM推理、门/机关API或玩家实现。源码Echo/Runtime/BehaviorLifecycle.cs，节点接口Enter/Tick/Exit可独立复用。

Unity6000.6.7f1/Windows Editor D3D11，当前开发机；纯生命周期/权限测试无独立Player或目标硬件FPS声明。新增接口未更改E02寻路实现，实际Agent适配接下来E06验收。沿用F-037测试前置门禁；没有新增故障。
