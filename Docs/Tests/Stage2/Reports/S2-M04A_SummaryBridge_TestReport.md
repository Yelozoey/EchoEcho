# S2 M04A 摘要适配测试报告

2026-10-25，6/6通过、0失败/跳过，框架0.3550429秒。Unity6000.6.7f1、Windows11开发机，普通Forward/HDR到SDR/D3D11及原干净场景门禁通过。硬件目标沿用总文档，本数据适配测试不证明1080p60FPS。

独立运行 Memory.Integration.Editor.Tests 中 JournalMemoryBridgeTests，A-SummaryBridge 标签。实际验证：未收集时工厂不调用、已知摘要与显式元数据入库且不含原文；首次重放与重复不再通知；工厂不能偷换ID/标题/摘要/分类；Dispose解绑且幂等；Journal.Replaced立即禁止旧知识，匹配新库后恢复；冲突或多余未知线索在任何补写前拒绝。

测试触发Journal.Replace使用反射调用已有内部方法，只在测试夹具中重现原事件；未扩大产品权限。真实E/扫描未在本子步骤执行，下一步M04B单独验证。

证据：[实际结果](../Evidence/S2-M04/A-SummaryBridge.json)和对应新鲜门禁。A通过并归档，允许进入B实际交互测试。
