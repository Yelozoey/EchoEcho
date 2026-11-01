# R0-A02-P 预制体与碰撞独立报告

2026-10-28；前置I报告已通过后实施。真实Unity EditMode Prefab_A 5/5、0.764秒，无跳过；module_tests.py新鲜SceneSafety/RenderSafety/空Console门禁通过，普通Forward/HDR→SDR/D3D11。

两个已保存预制体独立实例化，根identity，源视觉子变换保留、材质槽全部指向新URP材质，无Rigidbody。地板BoxCollider厚0.1m、顶面对齐Y0，板内3射线命中Y0，边界外射线不中。工具箱按实际bounds生成单盒，六方向实际Collider.Raycast命中、外部不命中。两个地板按4米网格实例化边界相接。测试在自有preview scene运行并finally销毁；没有把几何碰撞验证说成玩家搬运/动态物理验收。

证据：../Evidence/R0-A02-P/Prefab_A.json及同标签SceneSafety/RenderSafety。测试类SalvagePrefabTests，命令入口Tools/MCP/module_tests.py，程序集EchoStation.Salvage.Editor.Tests。

尚未验收：实际截图、Lightmap烘焙、LOD、手持、目标硬件60FPS。硬件基准仍为Win10/11 x64 1080p，i5-8400/GTX1060 6GB/16GB中画质60，i5-12400/RTX3060 6GB/16GB高画质60；当前EditMode不是性能证据。
