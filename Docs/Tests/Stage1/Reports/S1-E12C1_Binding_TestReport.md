# S1-E12-C1 实际扫描事件绑定

2026-10-25，Unity6000.6.7f1/Forward HDR→SDR/D3D11。ScanWaveBindingTests独立1/1通过（C1-ActualBinding.json、ActualWaveBinding.json）。产品Echo Prefab已添加ScanWaveEmitter/EchoScanWaveBinding，真实隐藏目标扫描Aiming事件从探头世界坐标触发单波，实际时钟扩散；取消立即停波且不显现线索，再次扫描生成第2代波，真实完成后隐藏标记显现并归档摘要，波1.2秒自然结束。

无LLM、无粒子、未控制机关。纯事件/实际时钟测试不等同GPU预算验收。目标硬件基线及开发机沿用B报告，C2另测Windows普通帧开销。
