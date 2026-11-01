# S1-E08-A 扫描数据权限与幂等

2026-10-25，A-ScanLedger.json 6/6，0失败/跳过，.701秒。A通过，允许B真实扫描器；E08整体尚未完成。

未揭示隐藏线索TryRead=false且Renderer关闭；Echo内部提交后才可读并显现，目标Transform保持。重复提交ID/摘要稳定、首次IsNewDiscovery=true/第二次false、发现事件与账本条目都仅1份。Note和设备信息均只产出<=120字符摘要；EchoScanReceipt没有全文/ClueRecord/ClueDefinition属性，不把原文送入未来AI上下文。

无调查能力、目标停用、空目标不能揭示或入账。ClueSource.TryRevealFromEcho与账本TryCommit均非public，World只向Echo.Runtime开放内部能力，没有Player友元；扫描提交只能由传统Echo执行程序集调用。测试程序集被单独授予Echo内部可见性，仅Editor测试存在。

本轮直接调用内部账本验证数据门禁，没有证明距离/视线/探头/.6秒时序有效；这些严格在B验证后才允许产品扫描路径调用账本。没有门/机关状态API或自动解谜。Unity6000.6.7f1/Windows Editor，当前开发机，无新增故障/目标硬件性能声明。
