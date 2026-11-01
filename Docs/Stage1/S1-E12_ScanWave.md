# S1-E12 全屏蓝色扫描波纹

2026-10-25连续S1授权下参数决策。前置E08扫描、E11表现全部通过后实施。

A独立扫描波状态：单并发、重复请求重启同一个波，不生成对象；中心为扫描开始时探头世界位置；1.2秒由0扩至6m，宽.18m、蓝色(.08,.4,1)、强度.35，结束/取消完全跳过渲染。测试时间边界/非法数据/重启/取消。

B独立RendererFeature/Shader：URP17 RenderGraph的一个全屏三角形Pass，AfterRenderingTransparents/BeforePostProcessing，读取场景色与真实相机深度，重建世界位置，绘制球壳与可见表面相交波纹。天空不着色、遮挡面之后的表面不可透视。无粒子、无额外扫描相机/几何。

复制已验收管线/Renderer为ES_Gameplay_ForwardHDR与ES_Gameplay_ForwardRenderer，保持普通Forward、HDR/SDR/D3D11和原有参数；只新增波纹Feature，保留13份保护资产SHA。当前项目基线绑定不变，测试临时绑定再还原，最终P09演示场景启用此变体。相机ScanWaveView显式绑定发射源，避免多相机全局串台。

B测试实际1080p深度波纹开关/半径变化/天空/遮挡；Frame Debugger确认活动时全屏Pass、闲置不提交。C用实际EchoScanController的扫描代次触发与取消，Prefab固定接线。Windows普通帧CPU/GPU计时对照作为开发机诊断，不能冒充目标硬件性能验收。

模块预算：活动波最多1个全屏Pass，闲置0个波纹Pass，无GC实例化、无粒子。独立报告完成再进入E13。
