# S1-W03 唯一目标身份验收

2026-10-25，v0.1。前置W02通过；Identity.json中4/4通过、0失败/跳过。独立用例覆盖两个ID精确查找、重复ID保留原对象、冒名注销拒绝、失效ID/空引用拒绝、边界1/64/65字符、实际PlayMode启用/禁用/销毁、产品场景5个保存ID与显示名。

ID使用Ordinal和小写ASCII规则，不做trim/大小写修正/模糊查找；启用时注册、禁用/销毁注销、SubsystemRegistration重置静态服务。Configure只允许禁用时执行，非法参数不修改既有身份。重复注册返回DuplicateId，不覆盖/错误注销原对象。WorldTarget只提供身份，不包含门、机关、解谜或玩家输入方法。

小屋5个身份已保存：note_maintenance_01、note_roster_01、terminal_data_01、scan_mark_01、power_panel_01。仅挂点身份，无收集/扫描行为；下一W04提供数据与表现契约。源码World/Runtime，场景接线World/Editor/CabinTargetSetup.cs。

环境Unity6000.6.7f1/Windows Editor D3D11、现有开发机；本模块无独立Player/性能验收要求，实际游戏整合留P09。目标硬件两档1080p60FPS未测。无本模块故障；执行前查阅F-030/F-037，并通过只读场景安全预检。W03功能通过，允许W04。
