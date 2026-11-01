# P01c-1 D目标路线独立复测

2026-10-30，最终DRoute_E 2/2、22.780秒，完整module_tests门禁通过。真实主场景本地Bundle、NavMeshAgent和传感器，初始/有效读档后两种状态分别7.656/7.965秒完成调查；原生phase=Completed、PaidDiscoveries=1、Known=true、错误None。货物仍World，未代搬/交付或改设备。

玩家先按实际窄Collider中心瞄准下达指令，再让出门口和Approach，机器人由储藏区真正走到检修间扫描。Editor夹具的玩家位置设置不等于完整步行流程；Windows夹具须实际Walk让路和返回取物，另行验收。原产品源码与权限未修改。

A0/2为F048/F199嵌套EnterPlayMode重犯，原防火墙归因已更正；B0/2、C0/1、D0/1均未扫到，最终D诊断证明玩家堵门导致3秒无进展回Wait，传感器根本未开始。所有失败保留，不提升超时、不绕过射线/导航/付费门槛。后台标志原生恢复与磁盘SHA见BackgroundRecovery_C。
