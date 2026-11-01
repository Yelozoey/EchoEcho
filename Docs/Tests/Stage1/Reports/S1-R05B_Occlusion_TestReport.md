# S1-R05-B 视锥与房间遮挡独立测试

2026-10-25；2/2通过，4.077秒，0失败/跳过；`Evidence/S1-R05/B-FirstRun.json`。Unity6000.6.7f1、URP17.6、D3D11、1920×1080、内部HDR→SDR；Windows11/i7-12650H/RTX4060 Laptop/16GB开发机。非最低/推荐硬件60FPS验收。

实际烘焙双房间夹具：7面实墙/地顶、8个近室/12个远室静态对象；Smallest Occluder2、Hole.25、Backface100。0.350秒，实际OcclusionCullingData文件12881字节，保存PVS5568字节，场景GUID引用有效，原场景布局恢复。native umbraDataSize仍0，按F-033记录统计接口限制；未将0值冒充不存在数据，也未单凭非空文件判定功能通过。

实际功能：固定近室相机关闭遮挡为2个不透明SRPBatch绘制提交，开启为1；画面差0。相机反转时为1，证明视锥外那组不再提交；进入远室为2，远室物体实际显隐对照图像差0.083635，证明未被错误永久剔除。真实事件与4张1080p截图、`OcclusionReport.json`保存Editor目录。

为在不依赖不可用单draw详情的情况下分辨房间，远室隔离夹具使用URP Unlit AlphaClip不透明变体（alpha始终1，形状不变），与近室常规变体区分提交；这是测量夹具，不是正式场景强制材质策略。2→1是提交批次变化，不是底层draw数。SSAO只在量化期间临时停用，最终恢复共享状态。

A12项已先归档；B通过允许开始C动态SRP Batcher。Windows正常帧Draw Calls与正式同室合并降绘制证据尚未执行，归D；R05整体未完成。用户目标基准仍为i5-8400/GTX1060 6GB/16GB中档和i5-12400/RTX3060 6GB/16GB高档1080p60FPS。
