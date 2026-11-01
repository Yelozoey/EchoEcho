# R1-G01 三区灰盒与玩家基线

2026-10-28，通过。module_tests.py World_A真实EditMode 5/5、0.814秒；新鲜干净场景/Console/普通Forward内部HDR→SDR/D3D11门禁通过。原始Evidence/R1-G01/World_A.json与同标签安全文件。

36×24m地板、3区、x12处主门z18/短路z6、x24处检修门z12已保存独立ES_Salvage_Demo。复用原输入、视角公式、垂直运动和手电；新SalvageMotor单独支持3.5m/s，不改旧FirstPersonMotor。真实CharacterController逐步走通主路与短路到检修间；墙体阻挡、真实跳跃离地>0.35m、视角和实际手电Light开关均通过。

灰盒几何/碰撞与开发机EditMode走测，不代表正式美术、烘焙、导航、流送、5分钟时长或目标硬件60FPS通过。旧文件最终统一在R1退出宿主后再做保护对照。
