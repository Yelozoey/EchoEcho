# PK03 实际结果

2026-10-27，Unity6000.6.7f1、URP17.6.0、D3D11，RTX4060 Laptop开发机。

`VC_ForwardHDR`/`VC_ForwardRenderer`实际普通Forward、内部HDR/显示HDR关闭、MSAA4、RenderScale1、4096主阴影/4CSM/110m、.12/.32/.60分割。独立Renderer无任何Feature，SSAO关闭。日光1.4、阴影强度.72、Trilight明亮填充、无雾、无噪声天空渐变。旧S2/PC资产不改。

`Evidence/PK03/Lighting_D.json`实际2/2通过；阴影On/Off像素平均差.02480207，非黑图像；`Daylight_Preview.png`为实际1080p。场景关闭后默认/PC均按原S2资产路径还原，`BindingsRestored.json`。不等于目标硬件60FPS通过。

F166只读属性编译已修；Lighting_A未启动。F167路径守卫拒绝Lighting_B，改从原S2绑定的拥有空场景启动，不放宽公共守卫；Lighting_C因刚保存场景外部变更未刷新而被拦截，刷新后D通过。Light.shadowResolution的Built-In限定警告已从配置代码移除，URP实际分辨率由Pipeline设置生效。后续最终全套重跑。下一模块PK04。

最终QA：阴影提高至8192。`Lighting_Final_B.json` 3/3通过，新增“启动绑定已经为美术管线”的关闭恢复验证通过，原S2引用有效；`Lighting_Final`因外部资产待刷新而未启动。8192/4CSM/110m是最终配置，早期4096是历史证据。
