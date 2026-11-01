# PK04 实际结果

2026-10-27，Unity6000.6.7f1/URP17.6/D3D11、RTX4060 Laptop开发机。

实际烘焙五个区域，每个512px HDR Cube，EXR离线资源以Custom模式持久化引用。`ActualProbeBakes.json`记载每个BakeReflectionProbe实际成功/文件字节；最终五张SHA均不同，重开场景后维度Cube/512/引用/boxProjection全部通过。Custom是引用真实离线烘焙结果的模式，不是实时伪烘焙。

庭院地面与玻璃墙各有独立镜像相机、斜裁剪、1024宽HDR RenderTexture、粗糙度Mip/Fresnel和URP IBL替换。递归守卫、可见面剔除、镜面自排除与GL.invertCulling finally还原。开启组件才创建临时资源，关闭后相机与RT计数0。

`Evidence/PK04/Reflections_C.json`：2/2；实际像素差总.05346126、仅地面.02821287、仅墙面.005438064。`Planar_On/Off/FloorOnly/WallOnly.png`包含实际1080p对照，已人工查看地面倒影与墙面镜像。Console0错误。

首轮A 1/2（F170天空/引用问题）；B 1/2（F171斜杠名称引起重复组）；均保留原始回执，最终C严格五个区域通过。最后重烘焙回执曾空失败，但实际文件时间/场景持久化/重开测试证实已执行；未因空回执重复写入。非目标硬件性能或最终Player验收。下一模块PK05。

最终QA：F172改用context预渲染后Reflections_D 2/2；主阴影8192并重烘焙五区域、两平面反射宽2048后，`Reflections_Final.json`再次2/2，Console无错误。最终配置2048宽；上文1024为历史初版。
