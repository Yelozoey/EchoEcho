# S3-S01b1 专用场景与导航资产

2026-10-27，统一Unity6000.6.7f1 / Windows / D3D11普通Forward内部HDR/SDR干净门禁后，菜单`Tools/EchoStation/Stage 3/S01/Create Dedicated S3 Scene`创建：场景Assets/EchoStation/AI/Scenes/ES_S3_AI_Demo.unity（446090B）、独立导航AI/Generated/ES_S3_NavMesh.asset（24816B）。保留五区及只读共享美术/渲染资源，唯一S3Bootstrap/Driver；未改全局BuildSettings。

Creation_A.json回执saved/originalLayoutRestored均true。原导航GUID7076a28f627fb964b88368ee1aad74df，新1c0c1015ffd316c499de6b45c1f378d4；复制通过Unity资产API完成，不要求序列化名称字节完全相同，实际导航运行待b2。保护脚本767/767 SHA未变。

Scene_A因新资产待刷新未启动；compile:none刷新后Scene_B为0/1，finally恢复NUnit内部空布局报错F150。改为项目既有OpenPreviewScene方式，Scene_C作业47b29181d35e4425baded3712b3331c9：1/1、0失败/跳过、0.0930688秒，检查新场景/导航存在、唯一Bootstrap/Driver、导航引用独立资产且GUID不同、五区齐全与资产无脏状态。入口`Tools/MCP/module_tests.py start/poll --evidence Docs/Tests/Stage3/Evidence/S3-S01b1 --label Scene_C --assembly EchoStation.AI.Execution.Editor.Tests --group '^EchoStation.Tests.AIExecution.S3SceneAssetTests'`。

regression_host Scene_A已还原原布局/Play选项/管线。开发机i7-12650H/RTX4060 Laptop/16GB；静态资产检查非最低i5-8400/GTX1060 6GB/16GB中1080p60或推荐i5-12400/RTX3060 6GB/16GB高1080p60实测。b1通过，下一叶实际启动绑定/导航；不称完整S01或S3已完成。
