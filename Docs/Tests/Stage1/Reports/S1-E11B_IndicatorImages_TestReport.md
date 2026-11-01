# S1-E11-B 实际指示灯图像与SRP提交测试

2026-10-25，Unity6000.6.7f1/URP17.6普通Forward、D3D11、1080p内部HDR/SDR；当前Win11/i7-12650H/RTX4060 Laptop/16GB开发机。最低i5-8400/GTX1060 6GB/16GB中画质和推荐i5-12400/RTX3060 6GB/16GB高画质1080p60FPS未在目标硬件测试。

独立运行EchoPresentationImageTests，B-IndicatorImages.json：1/1通过。隔离实际相机渲染四张1080p图像，读取灯中心7×7线性像素：蓝(.139,.734,2.493)、黄亮(2.636,1.525,.061)、黄灭(.183,.229,.262)、红(2.636,.077,.046)。三色分量占优及灭灯亮度阈值均通过；YellowOff实际Renderer.enabled=false。

RobotFrameDebugger.json记录实际DrawOpaqueObjects的SRPBatch事件2个，兼容动态Echo材质/网格，不能把事件数当作Draw Calls。原图与原始像素见Evidence/S1-E11/Blue.png、YellowOn.png、YellowOff.png、Red.png、IndicatorImages.json。这里只验证灯/机械表达，不包含E12扫描波纹、最终场景或目标GPU预算。
