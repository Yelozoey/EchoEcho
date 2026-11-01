# R4-R03b-3b 主线程句柄所有者与晚完成清理

2026-10-30，Owner_A6/6、4.908秒，完整module_tests门禁/Console0；全部真实Addressables场景操作，非假提供者。

普通C# RoomSceneOwner拥有3槽与load/unload句柄；独立私有DontDestroyOnLoad pump在主场景消失后继续轮询。需求撤回卸载、Loading同帧Dispose、World变更废弃旧票据后仅提交当前结果、上下文销毁晚完成、后台Dispose拒绝、绑定回调抛错的部分提交回滚均通过。

晚完成只卸载初始inactive根，不激活/调用旧提交；已提交槽先调用解除再隐藏/卸载。SDK消费load句柄后仅释放unload句柄，所有权标志防双释放。六例最终liveHandles=0、Bundle回基线0、pump销毁；World例旧lateRelease1/当前commit1，立即Dispose及上下文销毁例commit0。绑定异常保留LastError且仍retire/释放，无Unity错误被吞作通过；后台防护在访问Unity前拒绝。

测试使用本轮新空上下文对象，销毁用例立即销毁该对象以确定模拟加载中失效，不接触用户场景。此项尚未把Presenter/光图/距离驱动装入正式主场景，视觉仍inactive；非完整流送试玩/性能验收。
