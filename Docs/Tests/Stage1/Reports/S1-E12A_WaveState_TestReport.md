# S1-E12-A 扫描波状态测试

2026-10-25，Unity6000.6.7f1编辑器，D3D11/Forward HDR/SDR。ScanWaveStateTests独立3/3通过（Evidence/S1-E12/A-WaveState.json）。检查闲置不推进、.6s半径3m、1.2s半径6m并结束、重复请求代次递增且覆盖单波、取消、NaN/Infinity/负时间拒绝且无状态污染。

纯状态测试不证明渲染/显存/GPU性能；B继续验证实际全屏深度。硬件基线沿用总文档的最低i5-8400/GTX1060 6GB/16GB与推荐i5-12400/RTX3060 6GB/16GB，1080p中/高60FPS未验收。
