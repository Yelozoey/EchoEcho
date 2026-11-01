# R1-V01 回收闭环最终报告

2026-10-29；工程验收通过，R1八模块已完成。约5分钟首次体验与目标硬件60FPS仍未由本报告验收。

## 实际测试

- 当前源码回归Regression_E：56/56、14.383秒，无跳过；每案独立场景清理，module_tests新鲜SceneSafety/RenderSafety/空Console，6000.6.7f1/普通Forward/内部HDR→SDR/D3D11。
- Walk_C真实PlayMode主路/改道2/2；Cross_D实际写档→卸载拥有场景→全新实例读档继续完成1/1。后续让行/HUD改动已由56项回归和当前Windows四进程复测覆盖。
- WindowsBuild_D：Succeeded，0 errors，63.776秒。保留ServicesCore未关联项目、System.Windows.Forms受引用平台提示等构建警告；没有为消提示登录账号或改旧包。首ILPP锁失败及后续构建回执全部保留。
- Windows R1_C：write/read/route/alternate四个进程全部exit0，实际1920×1080、D3D11、ES_S2_ForwardHDR内部HDR、显示HDR=false。不是Editor画面冒充Windows。

| Windows场景 | 真实结果 | 驱动内墙钟/整进程墙钟 |
| --- | --- | --- |
| write | A已交付1件2分、B手持、C已放下；保存独立验证槽；电量预置37用于跨进程一致性验证 | 21.167s / 58.060s |
| read | 全新进程恢复混合状态，继续实际行走/交付到3件12分，主动确认Completed；未把夹具定位当正常行走 | 28.059s / 33.036s |
| route | 实际主路，取消交付、重开确认、D放下再拿，3件12分后主动完成，再确认新局归零 | 36.057s / 41.036s |
| alternate | 实际短路改道C/F/B，3件12分后主动完成，再确认新局 | 48.455s / 53.046s |

驱动以真实CharacterController.ApplyInput逐帧移动，正常路线不直接改物品交付状态；读档/新局才恢复位姿。报告route/alternate最终Completed=false、count0是已验证完成后“重开归零”，不能误读为未完成；actions含explicit_completion/restart_verified。自动熟练路线约36/48秒，不代表人类首次约5分钟，不能人为等待凑时长。

真实PNG已目视检查：中文可读、无粉材质/空管线黑图，手持模型与任务板显示正常；当前read_Complete中HUD与任务板都3件12分、空手3.5m/s，准星可见。早期截图HUD定时刷新短暂旧值已修复交易版本刷新并有真实UI用例。

## 故障与修复证据

F199嵌套EnterPlayMode→顶层切换；F200未保存宿主Main Camera→用户批准后另存恢复副本再重开；F201/F208实体Echo阻挡→基于玩家期望运动的有效路径让行、保留碰撞并测试门洞接触；F202未启动不得poll；F203早期路线撞批准物品→夹具绕行；F204/.NET API、F210 asmdef引用修正；F205构建排队未执行→update队列；F206 ILPP锁失败后只读确认可读/有限重试；F207夹具走回落物→侧走再返回；F209显式恢复AA与依赖执行码核对。所有失败/门禁拒绝留存，不并入通过率。

## 还原、预算与限制

FinalWorkspace_A实际恢复原ES_VectorCity_Parkour、VC默认/PC/实际绑定、AA4、PC质量、播放选项true/1、background=false、干净未播放。FinalAudit_A：2403份旧Assets/Packages/ProjectSettings逐文件SHA完全一致，联合S3与新增资产/构建/证据约13.86185GiB/20GiB。恢复副本Assets/EchoStation/Salvage/Recovery/Host_MainCamera_20261029_A.unity保留，原宿主未覆盖。

开发机Windows/RTX4060 Laptop与目标最低i5-8400/GTX1060 6GB/16GB、中1080p60及推荐i5-12400/RTX3060 6GB/16GB、高1080p60不同；本报告不是目标机性能证明。正式美术、LOD/烘焙、Addressables与外部首次试玩仍按R4/R5执行；R1没有LLM/调查扫描功能。

证据均在../Evidence/R1-V01：Regression_E.json、Cross_D.json、Walk_C.json、WindowsBuild_D_Succeeded.json、Windows/R1_C/Processes.json及PNG/Player.log、FinalWorkspace_A.json、FinalAudit_A.json。Windows入口Builds/Salvage/EchoStationSalvage.exe，操作见Docs/SalvagePrototype/PlayGuide.md。

后续按已确认R2连续授权推进，R2完成后停止，不进入R3。
