# S1-E08-C1 调查后扫描真实流程

2026-10-25，C1-Retest.json 2/2，0失败/跳过，28.258秒。原生MonoBehaviour Update按真实Time.deltaTime驱动Scanner，复用实际NavMeshAgent/E04入口/E05根/E07调查，独立PhysicsScene隔离旧夹具碰撞。

实际依次调查扫描5个目标，5份摘要事件，隐藏标记在有效扫描后显现。重扫隐藏标记成功但IsNewDiscovery=false，事件仍5份；所有目标位置保持、无门/机关操作。ActualScans.json逐项记录阶段/结果/摘要/游戏耗时，HiddenMarkerRevealed.png经视觉复查确认墙面蓝色标记已出现，尚无E12波纹。真实扫描阶段取消后观察.8游戏秒，标记仍锁、台账无记录、移动Idle，第二项通过。

首轮2项1通过，维护笔记被Main_DeskTop遮挡，原失败C1-ActualScans.json/ActualScans-DeskOcclusion.json保留；F-050按授权修正临时纸片位置到桌前侧，未忽略桌面Collider或降低视线约束。复测全部6次扫描请求成功，证明改摆放解决了实际几何遮挡。

C1通过允许C2自主扫描；E08整体还需自主扫描和受影响回归。Unity6000.6.7f1/Windows Editor D3D11/1920×1080，当前开发机，不作最低/推荐目标硬件性能声明。
