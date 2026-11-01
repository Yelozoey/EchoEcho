# Rover源拆分子步骤（c1尚待Unity验证）

2026-10-30。文件级4/4，0.198秒，exit0。原始OBJ／贴图哈希与CC0来源保持；8个派生部件（身体、探头、6轮）覆盖全部704源多边形，逐面角点位置反变换与UV匹配；无漏面／重复分配，6轮半径约0.054m、三轴位置对称。缩放0.25是已委托外观适配，整机约0.58×0.54×0.77m，不改变能力。

Unity实际独立坐标探针证实OBJ导入镜像X、保持Y／Z，因此派生顶点由原生Importer处理，轮轴／探头位置元数据应用同一[-1,1,1]轴变换，避免对称模型掩盖左右不一致。ObjAxisProbe_A.json留存。

工具split_rover.py／test_split_rover.py；原始源保留Original，派生Derived，所有参数／源面索引／SHA在ThirdParty/MarsRover/RoverParts.json。此步骤只证明源数据守恒，不证明Unity材质、动画、导航或产品可玩。下一步c1视觉Prefab独立测试通过后再c2驱动整合。
