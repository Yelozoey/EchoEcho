# S1-E02 导航拆分、参数与独立测试计划

2026-10-25，前置E01独立4/4通过。Unity AI Navigation2.0.16（已安装），使用Unity NavMesh内置寻路。A专用代理/小屋实际烘焙与配置测试 → B运行时完整路径/抵达/无效和断开目标测试；A报告后才写B，不接动态障碍/行为树。

A新增名为Echo的代理设置，不修改原Humanoid/Area：v0.2半径.54m、高.9m、步高.15m、坡度35度；小屋根NavMeshSurface收集Children PhysicsColliders，忽略Agent/Obstacle；Voxel .08m、Tile128、MinRegionArea.1，不生成自动Links。数据持久化到Echo/Navigation/ES_Cabin_NavMesh.asset，源为原30个实体Collider，4个线索触发器不作障碍。保存修改前NavMeshAreas原文供审计；不以整栋合并Renderer包围盒代替实体几何。

B给Echo预制体配置初始禁用NavMeshAgent；显式绑定到<=.6m内合法地面后才启用，不在无NavMesh的模型夹具启动时刷错。速度2.2m/s、加速度8、转速180度/s、停止.15m、Agent.radius.535/height.86/baseOffset0。只接受有限坐标和完整路径，拒绝部分/无效/远离表面的目标，不随意替换目标；取消会清理路径，实际抵达有状态反馈。

首次A半径.38m烘焙与3项路径测试通过，B前包络审查发现.75×.75m根Box绕Y转动的外接半径为.5303m，半宽不足以代表转弯净空。故修订.54/.535并增加包络断言，重烘焙A复验后才写B；不是忽略角落穿透风险。v0.1证据A-Bake.json保留。显式重建仅替换本模块生成的固定DataPath，构建成功后才替换，不删除其他资产。

A独立测试专用/原设置隔离、保存Surface/Data/体素参数、实际小屋门斗→主房→储物室完整路径。B测试实际Echo跨门到达与地面/墙体保持、非有限坐标/无NavMesh目标拒绝、独立双岛路径只能Partial时安全拒绝、取消后停止。测试目标是夹具，不提前实现玩家或E03障碍。当前开发机功能测试不代替两档目标硬件性能验收。
