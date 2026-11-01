# PK02 实际结果

2026-10-27，Unity6000.6.7f1/D3D11，RTX4060 Laptop开发机。

七种材质均已替换为`EchoStation/Parkour/Vector City PBR`，纯色Albedo、常量粗糙度、金属参数，材质无表面贴图。ForwardLit/ShadowCaster/DepthOnly有效，实际Shader支持且编译错误0。实际场景1080p渲染非黑；可见玻璃庭院材质粗糙度.06/.85对照平均绝对像素差.01808377，使用URP连续GGX/IBL，不量化光照。

最终`Evidence/PK02/Materials_C.json`：2/2通过，`PBR_Hero.png`与两张粗糙度对照留档。F164首轮1/2因编辑模式销毁错误，独立截图工具修复后重跑通过；Materials_B被编译错误门禁拦截，未启动。新增源文件应使用全Assets刷新后再编译（F165），不改共用夹具。

此时尚未配置最终照明/反射/Volume，不声称这些功能通过。下一模块PK03。
