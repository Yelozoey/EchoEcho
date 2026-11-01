# R4-R02a-1 房间静态网格合并

2026-10-30，Merge_B2/2（4.447秒）；落盘重开Saved_B12/12（23.942秒，含Lighting/Rendering/Layout）。SceneSafety/RenderSafety/Console门禁通过。

142个静态Renderer变37个，分区49→13、48→12、45→12。32,385个三角形角逐一核对材质、Atlas、位置50微米及UV5e-6容差，数量完全相等；CombineMesh应用原LightmapScaleOffset，输出identity变换，显式持久绑定Atlas。三区实际画面A/B差分别约2.98e-9、2.45e-9、7.09e-9，均远低于原0.002阈值。

新场景备份BeforeMerge.unity保留；输出37个独立Mesh资产，源Renderer/MeshFilter移除，空Prefab层次解包清除，碰撞与Nav不变。重开验证Atlas/全部角数，实际三个门洞与返回短路路径、七货物可达、探针/反射/手电/画质切换回归通过。F246舍入字符串误判、F247旧Renderer数量断言均留首失败证据并复测。

此处37是场景Renderer数量，不能当作GPU DrawCall或目标硬件性能结论；实际Windows绘制与Profiler在后续P01。未实现部分仍需逐模块验收。
