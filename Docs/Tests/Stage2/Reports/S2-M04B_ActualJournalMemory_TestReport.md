# S2 M04B 实际读取与扫描入库测试报告

2026-10-25，3/3通过，0失败/跳过；框架duration7.1502179秒为跨模式汇总值，不作为加载/播放/工具总耗时。Unity6000.6.7f1、Windows11开发机（i7-12650H/RTX4060Laptop/约16GB），普通Forward/HDR到SDR/D3D11；目标硬件性能没有在此验收。

M04A独立6项报告归档后才加入本步骤。为避免已有小屋重复ID和外部场景污染，regression_host记录原布局/选择/播放选项/管线后临时打开既有空测试场景；实际测试在PlayMode以独立PhysicsScene加载真实小屋和玩家/Echo预制体。结束已恢复原ES_Cabin_Greybox场景和播放选项，检查Gameplay绑定一致，没有保存/丢弃用户内容。

| 用例 | 操作与实际结果 | 状态 |
| --- | --- | --- |
| B01 | 真实InputAction E读取维护记录与终端；重复E不再通知；记忆只有ID/标题/独立摘要和分类 | 通过 |
| B02 | 隐藏标记开始不可读且记忆未知；实际取消扫描不显现不入库；随后真实传感器到位扫描完成才显现并入库 | 通过 |
| B03 | 真实扫描供电面板、重复扫描及重绑定仅入库一次；摘要等于已授权ScanFact；设备矩阵/活动状态保持，UseLLM=false | 通过 |

证据：[测试结果](../Evidence/S2-M04/B-ActualReadAndScan.json)、ActualE-Memory/ActualHiddenScan-Memory/ActualEquipmentScan-Memory 三个实际记忆JSON、B-ActualHost-WorkspaceBefore/Entered/Restored。首编译夹具引用遗漏保留B-CompilationErrors，按F069已有经验定位修复；门禁此前拒绝进入测试工作区，没有把失败当通过。

没有宣称验证S2供电机关状态，因为该机关尚未制作；B03只验证当前真实设备的可观察状态和只读摘要路径。供电写权限仍须在P03/F模块实际再验。
