# S1-E03 动态障碍与实际重规划验收

2026-10-25，v0.1。最终Echo-Final.json 14/14、0失败/跳过（E01四项+E02七项+E03三项），25.701秒；E03功能通过，允许E04。

动态柜模板含实际BoxCollider/NavMeshObstacle一致的1.1×1.3×.8m包络、Carving/OnlyStationary、.05m移动阈值/.2秒静止期、动态URP Lit/BlendProbes/无Static。能力由实际原生组件与Agent自动重算实现，不添加门机关/推物谜题，产品小屋暂不额外摆放该模板。

机器人已在行进中时，前方主房新增障碍；.5游戏秒后原可采样地面在障碍中心不再可采样，证明真实剔孔。固定起终点路径最近净空.204859→1.09m，路径从直线变为绕行折点，实际机器人抵达；27683个真实游戏帧检查所有环境与障碍Collider，最大穿透0。Detour.json保存前后路径和实测值。

门斗门洞堵塞后路径非Complete、移动请求被拒绝，持续.4游戏秒位置变化<.01m；移除障碍后不会自动复活旧命令，新命令可穿门并抵达，DoorRecovery.json passed/recovered=true。此规则避免后续wait/取消被后台恢复覆盖，follow等行为后续可主动节流重发。

首轮嵌套EnterPlayMode卡顿/取消及MCP孤立状态清理见F-048；未保存用户场景/未重启Unity。原生运行器确认停止后才清遗留状态；最终Editor播放选项true/1恢复、13份既有资产SHA不变（FinalAudit.json）。第二轮2/3是旧NavMeshPath更新后被清空，F-049改为预先复制历史路径，原失败Detour-PathSnapshotFailure.json及Obstacles-Retest.json保留，没有降低净空/碰撞/抵达断言。

Unity6000.6.7f1/AI Navigation2.0.16/Windows Editor D3D11、当前开发机；无产品Windows动态障碍或最低/推荐硬件60FPS验收。E04意图权限/行为树等尚未实现。
