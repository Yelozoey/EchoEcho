# S1-R05-C 动态SRP Batcher独立测试

2026-10-25；2/2通过、2.776秒、0失败/跳过；`Evidence/S1-R05/C-FirstRun.json`。前置A12/12、B2/2已分别归档。

Unity6000.6.7f1/URP17.6普通Forward/D3D11/内部HDR→SDR/1080p，开发机Windows11/i7-12650H/RTX4060 Laptop/16GB。不是最低/推荐硬件性能验收，目标仍为i5-8400 GTX1060 6GB 16GB中档与i5-12400 RTX3060 6GB 16GB高档1080p60FPS。

检查3个动态代理无Static标记、无MPB、URP Lit材质、未启用Instancing、未加入静态合并。实际从隐藏→出现→位移(.75,0,.35)m，Frame Debugger不透明SRPBatch提交1→2→2；物体显隐图像差0.004668，移动图像差0.009640，证明不是只有配置标签而没有绘制/移动。截图和原始事件保存在Dynamic目录。

此处2代表SRPBatch提交批次，不是底层draw或实例化批次。独立夹具用发光材质保证无光源情况下可见，正式场景依照既有烘焙/动态光规范接入。共享Pipeline/Renderer没有改动；实际目标硬件性能未测。

C通过允许D Windows正常帧计数与全回归。尚不代表R05整体完成。

D阶段稳定化修订：动态测试分离为S1_R05_DynamicBatcher.unity（无PVS依赖），相同几何/动态对象、相同严格图像和SRPBatch断言；最终隐藏/出现/移动为2→3→3。实际完整63/63与设置恢复见R05总报告，初测1→2→2原始数据保留。
