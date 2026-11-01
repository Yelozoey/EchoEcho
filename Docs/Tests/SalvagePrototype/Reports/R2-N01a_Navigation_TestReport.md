# R2-N01a 真实调查导航

2026-10-29，通过。Navigation_A真实PlayMode3/3、8.069秒，完整门禁。证据`../Evidence/R2-N01a/Navigation_A.json`。

真实NavMeshAgent从小屋西侧穿主门、移动超过6m，在B的approach水平0.25m内且稳定后才执行到达回调；回到原待命。无路approach拒绝到达/发现，玩家新Wait取消旧任务，拾取调查目标使旧任务失败且回跟随，暂停不扣电/不传送。测试用已知ID夹具隔离导航，未声称真实扫描通过。四分支IntentBehaviorTree与独立TargetRegistry、MovementLease已绑定R/T入口；旧行为程序集未改。

本步不含动态障碍、扫描发现/扣8、标记或自动危险恢复。普通Forward/HDR内部/SDR/D3D11保持；目标硬件性能待验收。接下来仅实施N01b。
