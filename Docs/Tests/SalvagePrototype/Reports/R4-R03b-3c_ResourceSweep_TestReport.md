# R4-R03b-3c 产品侧资源回收与即时Ready撤销

2026-10-30，Sweep_A8/8、12.987秒，完整module_tests门禁和Console0。

产品pump在真正场景卸载完成后合并启动异步Resources.UnloadUnusedAssets；持续过渡最多推迟2秒、重复扫限频1秒，已关闭场景owner不再持句柄后pump继续到原生回收完成才销毁。新测试不自行调用UnusedAssets，真实加载三房、卸载第一区，确认其独有Texture/Mesh销毁、其他区共享材质保留；最后两区卸载后共享材质销毁、Bundle归0、LiveHandles0、pump销毁。产品实际执行2轮原生回收。

World刷新同帧，旧Loaded的IsReady立即false；新代次重载提交后才true。原6项所有权异常/取消/线程/晚完成测试全部回归通过；共8项。退出测试现等待产品清理完成，不以固定两帧假定原生释放。

此项尚未与Main距离驱动整合，不是整段产品压力/目标机帧率验收。后续先独立验证加载输入租约，再绑定主场景。
