# PK01 实际结果

2026-10-27，Unity6000.6.7f1/D3D11，开发机RTX4060 Laptop；独立场景，不代表目标硬件或完整跑酷手感验收。

已实际由GameObject.CreatePrimitive生成184个Cube构件、五个屋顶、三坡道、开放跳口、护栏/大块矢量引导/外围塔楼。16个路线落点向下射线全部落在预期高度，四组跳口标记水平距离≤3.4m、向上差≤1.1m；五屋顶有实体碰撞。

通过：`Evidence/PK01/Geometry_C.json`，2/2；运行前SceneSafety/RenderSafety均通过、Console0错误。`Geometry.png`是实际编辑器相机图像，此时仍使用临时URP Lit与原S2管线，不称最终材质或照明已验收。

历史：首次编译F162已修；Geometry_A因忙碌门禁未启动；Geometry_B错误依赖Runner当前空场景0/2，F163修复后重跑2/2。用户旧场景未保存/覆盖。下一模块PK02。
