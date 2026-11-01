# R4-R03b-4b 多场景PVS与实际剔除

2026-10-30，Visibility_B6/6、23.327秒，module_tests门禁/Console0。四项分区/独立探针回归、共享PVS依赖检查、真正Addressable房间的原生绘制与图像对照均通过。

主场景＋三房同载临时启用视觉，真实重新烘焙2.609秒；新PVS资产53,299字节，PVS数据25,344字节，四Scene同引用、各自SceneGUID保留，之后房间保存为inactive，原布局恢复。保存scope仅写四个自有Scene，旧Graphics/Quality保存被拦截，原dirty保持、项目设置磁盘无变化。PVS进入独立本地包12,279字节；实际八包重建Build_8d9…passed22.594秒，光照flag与SDK两个scope精确还原。

1920×1080/D3D11，真正三区Bundle：Native FrameDebugger DrawOpaqueObjects绘制36→20、背向0，Off/On平均线性图差0，Off/Off稳定差0、亮度.009119。计数来自原生事件实际draw字段，非UnityStats静态推算；动态图形/时变后处理在本因果对照中隔离，非最终产品可读性或目标硬件性能验收。

Main依赖仅新增自有共享PVS，不包含房间独有Mesh/Lightmap或旧LightingData；所有118碰撞、7货物、独立Nav、180探针保护回归通过。Visibility_A为5/6，旧探针夹具过早ExitPlayMode引起SDK晚回调异常，已按F260等Owner/NativeSweep完全清理后才退出；不修改SDK、不忽略异常，B全量重测通过。旧单场景/PVS不改。

R03距离/资源/视觉子项均已独立通过；下一步R03c整合压力，再进入P01与Windows交付。
