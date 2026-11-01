# 环境回收切片源码职责

入口场景：Assets/EchoStation/SalvageExpansion/Scenes/ES_EnvironmentalSalvage.unity。所有本轮产品代码/资产位于SalvageExpansion，Editor测试位于Tests/SalvageExpansionEditor；旧版源代码、场景与构建保留。默认UseLLM=false，未启动新模型实验。

## 数据与事务

Domain/InventoryCell、InventoryFootprint、InventoryBoard：固定6×4形状、旋转、真实占格，L形空角可用。InventoryItems/InventoryStore：七种定义与不可变位置记录；World/Backpack/Held/Installed/Delivered唯一归属，手持保留格子，安装电池才释放格子；失败不改Revision。InventoryDrag只预览，成功版本检查后一次提交，不自动挪动其他物品。

Runtime/ExpansionSnapshot、ExpansionSaveStore、ExpansionPersistence：独立版本1槽、128KiB、严格JSON/SHA256/原子替换/有效备份。验证全部库存、世界碰撞/支撑、角色位置、各模块段后提交，失败倒序回滚。IExpansionSaveSection是设备、危险、Echo、任务的prepare/commit/rollback接口；Restored事件仅在成功后取消交互/任务。正常玩家槽为persistentDataPath/SalvageExpansion，验证器仅写Library下独立测试槽。

## 玩家与设施执行

ExpansionWorld拥有七个实体视图、实际3米射线拾取、安全放下、单件装备、批量交付预览/确认与HUD；复用SalvageMotor/PlayerInputReader/PlayerFlashlight。世界状态的物品启用NavMeshObstacle carving，拾回/装备/安装/交付时禁用，已通过实际成孔与恢复测试。BackpackPanel为真实UGUI拖拽/旋转，不依赖自动整理。ExpansionPause使用嵌套租约：背包/任务/Echo/显示暂停时钟、移动和导航；阀门仅阻断输入，危险继续；最后租约才恢复原始状态。

FacilityController拥有便携电池插口、90秒供电、通风、可选8秒数据校验、隔离和三夹具。ExpansionTools只处理玩家实际工具输入与圆弧拖动，到位才调用执行层。EnvironmentHazards/HazardZone拥有蒸汽周期、漏电、压力/恢复、失败和重开确认，不自动发收益。ExpansionMission拥有任务时间、教程、可选评价、明确结算与初始完整快照重开。

## Echo边界

ExpansionEcho记录发现/短报告/状态戳/历史、电量、基础模式和位置；过时报告必须重扫。UI先创建既有IntentRequest结构，再由IntentDispatcher校验、IntentBehaviorTree取得MovementLease执行；调查节点只导航、扫描、标记与发台词，跟随节点有玩家回头时侧向让路。底层复用EchoNavigation、EchoPresentation、RoverAppearance；扫描波纹复用全屏ScanWaveEmitter/View。Echo没有Facility工具/供电/夹具写接口，不替玩家完成设施操作。

本轮语义层为脚本适配，R3模型No-Go没有被改成通过。Echo代码只读设施状态，没有设备操作意图，不会调用设施写入方法。未来模型只能输出既有结构化意图，经目标/权限/已知信息门禁进入此执行层；不能把脚本台词计为LLM推理效果。

## 渲染与流送

ExpansionStreaming复用RoomDemandPlanner/RoomSceneOwner/RoomPresenter，绑定独立存档Epoch与任务Generation；距离预加载/滞后卸载，加载缺失时取暂停租约，退休时移除Renderer/Lightmap引用并释放Addressables句柄。仅复用原三区已验证美术、烘焙、探针、LOD/Instancing/Impostor及PVS；新碰撞/设备使用独立NavMesh。

ExpansionGraphics拥有三档专用普通Forward管线、内部HDR/SDR、互动景深，以及独立显示配置与DisplayTransaction异步原生确认/回退。ExpansionFogField/View/Feature使用半分辨率16步局部RenderGraph雾，蒸汽状态或供风改变实际渲染，低档关闭。项目全局绑定由运行作用域恢复，不永久替换用户模板。

## 构建与证据

ExpansionSceneBuilder只创建/修改本轮自有场景、材质、Nav和管线。ExpansionWindowsBuild采用可恢复构建作用域、SDK保存拦截和精确临时link.XML清理；仅剥离新输出的模型副本。Validation/ExpansionWindowsProbe仅显式命令行启用，测试退出恢复虚拟输入/帧率状态；普通启动没有自动路线。Tools/SalvageExpansion提供统一module_tests包装、预算监测、原生设置/布局恢复及旧资产/二进制SHA核对。实际结果以各模块报告和V01最终报告为准。
