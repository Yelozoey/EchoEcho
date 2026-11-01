# R0-A02-I 导入与材质独立报告

2026-10-28；结论：通过，真实Unity EditMode Import_C 9/9、2.273秒，无跳过。Unity6000.6.7f1/WindowsEditor/D3D11，PC/S2普通Forward内部HDR/SDR，RenderSafety/SceneSafety/空Console均由module_tests.py新鲜导出。

首次Import_A因外部变更门禁未启动；Import_B真实8/9，自动UV2使工具箱3个Mesh变0顶点。真实关闭UV2对照恢复，修正动态道具导入策略后Import_C 9/9，未删除空Mesh断言，源12份字节SHA全一致。见F193/F194。

地板实际bounds为4×0×4m、12顶点/8三角；工具箱约0.40002×0.34710×0.29232m（修复前可见壳bounds；最终修复后精确值见Import_C原始output），不使用0.01补丁。所有实际Mesh非空、UV0/法线/切线完整；地板有UV2；动态工具箱不生成UV2，不能据此宣称可静态烘焙。

检查实际ModelImporter/TextureImporter、源12文件SHA、两份URP Lit的Shader错误/引用/关键词，以及派生Mask的全部像素通道。BaseMap sRGB、Normal为NormalMap、数据图线性，max1024/MipMap/HQ压缩、非CPU可读。粗糙度转smoothness，没有把ORM直接当URP metallic贴图。

原始证据：../Evidence/R0-A02-I/Import_C.json、Import_C-SceneSafety.json、Import_C-RenderSafety.json；首次失败Import_B.json与ToolboxMeshInventory_A/ToolboxWithoutUv2Probe_A.json保留。命令：`Tools/MCP/module_tests.py start/poll --evidence Docs/Tests/SalvagePrototype/Evidence/R0-A02-I --label Import_C --assembly EchoStation.Salvage.Editor.Tests --group '^EchoStation.Salvage.Tests.SalvageImportTests'`。

本子模块不验收碰撞、场景观感、烘焙、LOD、玩法或硬件帧率。性能基准沿用Win10/11 x64、1080p，最低i5-8400/GTX1060 6GB/16GB中画质60、推荐i5-12400/RTX3060 6GB/16GB高画质60；当前开发机测试不替代目标硬件。
