# Feedback02 V01：层高几何独立测试

2026-10-30；Unity6000.6.7f1，Windows11开发机i7-12650H/RTX4060 Laptop/16GB，D3D11。最低i5-8400/GTX1060 6GB与推荐i5-12400/RTX3060 6GB均未在本模块测试；本模块不是性能验收。

结果：**Height_B 1/1 Passed，1.451秒**，零失败/跳过。前置空测试宿主、SceneSafety/RenderSafety通过、Console错误为空；运行入口Tools/MCP/module_tests.py。原S3布局与全局设置已独立快照，最终集成后恢复。

- 实际打开新ArtV2创作场景，对每房18片屋顶逐片测量4.5/4.5/5m；平面4×4m保持。
- 上墙与门上补板实际bounds下缘3m，上缘与屋顶一致；原3m门框保持，三房新屋顶碰撞下缘与屋顶一致。
- 对照原正式参考场景，七个货物物件的位置/比例、Echo比例、相机FOV与本地高度全部相同。六件可回收物及原超重测试件不变。
- 当前发行版484文件已逐项SHA验证归档Feedback02/Feedback01_Windows.zip，源发行目录未删除。新创作场景源自只读BeforeMerge证据；原合并场景和光照资产不写入。

Height_A被外部资产待刷新门禁拒绝，未启动测试；刷新后新标签Height_B实际通过。首次刷新等待超时但随后Console0与实际编译类型可用，没有重复创建；F288记录编辑器后台延迟问题。

证据：`Evidence/Feedback02/V01/Height_B.json`、同前缀SceneSafety/RenderSafety、`Geometry_*.json`、`Created_A.json`、`CreateSaveScope_A.json`。本报告只接受几何模块；光照烘焙、流送包、导航回归、截图观感和Windows均待后续独立模块，不能据此宣称产品已更新。
