# P01a-2b1 灯具派生几何与材质门禁

2026-10-30，LampAssets_A2/2、1.478秒，完整module_tests/Console0。CC0 Wide Light派生Mesh保留960个三角形角点、两材质槽与全部UV，中心归零、2.2×.16×.4m尺寸、向下发光面通过；源Prefab SHA前后f0d3934591d09d0d4183d31c89441d7fb5b33e7153bb8327b596a2eef7926523一致。

两个自有URP Lit材质实际保留Emission Map/关键字/非Black flags和实例化，发光色原生读回(.45,1.5,3)；SDK保存范围/项目设置字节保护通过。此项仅验证生成资产，未证明灯具GPU呈现或场景包绑定，下一b2原生图像与FrameDebugger，再b3房间流送。
