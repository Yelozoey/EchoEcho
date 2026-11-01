# R2 最终交付与实际测试报告

2026-10-29，通过。用户确认范围为R1收尾＋完整R2后停止；R1已独立交付8/8，当前R2六模块全部完成。原S3仍17/24，本轮未进入R3–R6，未实验模型/训练/下载权重或调用云推理。授权见`Docs/SalvagePrototype/R2_ContinuousExecutionAuthorization.md`。

## 交付

- Windows：`Builds/Salvage/R2/EchoStationSalvage.exe`，同目录Data/Mono/许可文件须一起保留。原R1程序仍在上一级。
- 场景：`Assets/EchoStation/Salvage/Scenes/ES_Salvage_Demo.unity`。
- 操作：`Docs/SalvagePrototype/R2_PlayGuide.md`；源码导航：Salvage Runtime下的CommandHub/CommandPanel、EchoCoordinator、Scanner/ScanFeedback/DiscoveryMarkers、Persistence/SaveStore。
- 恢复副本：`Assets/EchoStation/Salvage/Recovery/Host_MainCamera_20261029_A.unity`，按用户单独批准保留；不覆盖原宿主或用户场景。

## 独立模块门禁

| 模块 | 实际证据 | 结论 |
| --- | --- | --- |
| C01 目标权限 | Contract_A9/9、Targets_A3/3 | 六货物/两只读危险，未知/失效/越权目标隔离 |
| U01 指令与UI | Commands_B16/16、UI_B3/3 | R/任务板与可选T、明确选择、200/280/100/50预算 |
| N01 实体导航 | Navigation_A3/3、Obstacles_A5/5，Save_B受影响回归 | 真实穿门/绕行、取消与无路反馈；最终动态障碍采用局部避让 |
| P01 可信扫描 | Sensor_C7/7、Feedback_B10/10，Lifecycle_A回归 | 八目标真实扫描、首成功扣8、幂等/取消/遮挡/标记/灯与波环像素 |
| B01 生命周期/存档 | Lifecycle_A14/14、Save_B28/28 | 危险恢复、玩家覆盖、新局、v2/原R1备份/迁移/坏档/回滚 |
| V01 整合 | Regression_B123/123；Windows五进程exit0 | 正常/改道/取消/耗尽/恢复/结算与交付 |

每个分步报告均已在依赖实施前归档，详见`R2_ModuleLedger.md`与本目录R2-C01a至R2-V01a报告。部分组包含前置回归，不把重复数量相加为独立覆盖率。历史失败、拒绝启动和修复保留，F211–F220已记录到独立Troubleshooting。

## 运行环境与渲染

开发机实际：Windows11 10.0.26300、i7-12650H/16逻辑线程、16122MB RAM、RTX4060 Laptop/7956MB显存。Unity6000.6.7f1、D3D11，普通URP Forward、内部HDR→SDR、显示器HDR关闭。PreBuild_A.json记录硬件；每次module_tests均有新鲜安全/渲染/Console前置门禁。

目标基准仍为Win10/11x64、1080p：最低i5-8400/GTX1060 6GB/16GB中画质60FPS，推荐i5-12400/RTX3060 6GB/16GB高画质60FPS。本开发机结果不替代目标硬件验收。

## Editor与Windows实际结果

Editor Regression_B：123/123、406.239秒、0失败/跳过。正常A/B/D与改道C/F/B均走通实际角色移动、R/MouseState按钮、扫描、E拾取、亲自交付、明确结束及新局；耗尽路线0扫描仍完成回收。跨场景write→卸载→全新实例read恢复发现/Wait/历史和混合物品，继续至3件12分。输入临时设置均恢复。Editor640×480记录不用于1080p性能声明。

Windows构建：`WindowsBuild_20261029_033219_b120d12725fd42a38130a74c6eaa1914.json`，Succeeded、0errors、41.821秒、5warnings。保留未关联UnityServices、三条DEVELOPMENT_BUILD弃用及System.Windows.Forms引用警告；未为消警告改旧包/联网服务。实际构建与五进程验证证明本次路径可运行，不代表所有旧依赖路径均已验收。

Windows/R2_A五进程全部exit0，无超时，实际1920×1080/D3D11/ES_S2_ForwardHDR、HDR=true/displayHdr=false、UseLLM=false、inputRestored=true：

| 模式 | 实际结果 |
| --- | --- |
| startup | 不注入设备、不改玩家位姿/组件启用状态，0物品/0发现/默认Follow，正常画面 |
| write | 真实InputSystem R/鼠标/T中文事件/Enter/E/F5；A交付、B手持、C落地，3发现/14历史/Wait写入隔离测试槽 |
| read | 另一进程F9恢复，继续到3件12分、4发现/19历史、Completed=true；重复D扫描不重复扣8 |
| route | A/B/D、取消调查/交付、D放下再取、显式结束/新局全部验证 |
| alternate | 短路及C/F/B另一合法顺序、显式结束/新局全部验证 |

原始日志、Result、Processes.json与实际PNG均在`../Evidence/R2-V01b/Windows/R2_A/`。已目视检查Startup、Text_Charge、read_Complete：场景有内容、中文/已知摘要/不可调查状态可读，HUD与任务板一致为3件12分/空手。实际相机波环A/B像素与零噪声对照在P01b报告；本轮无粒子堆叠。Windows输入为注入真实InputSystem事件穿过实际UI/游戏路径，未冒充人工原生中文输入法验收。

## 保护与还原

FinalWorkspace_A核对原干净ES_VectorCity_Parkour场景、默认/PC/实际VC管线、PC索引1、AA4、原播放选项true/1、选择空、background=false，未播放/编译/更新且无用户脏资产。F220中不可用于persistent设置对象的预览序列化错误已完整归档；随后仅对确认的两设置字段生成候选，候选全SHA等于最初基线后才原子还原，未批量保存其他脏资产。Console归档该两条自有诊断后清理，最终错误0。

FinalAudit_B：2403旧文件SHA全部一致；S1/S2/S3/ParkourArt及原包/配置不变。资源按原S3所有权范围加新原型源/资产/构建/测试/工具去重，约15.90GiB/20GiB；共享Unity导入/编译缓存仍按既有口径单独排除。SourceAndPlayerManifest_A提供本版源码/场景与保留R1、新R2构建摘要。

## 尚未验收及停止边界

当前为三区灰盒脚本协作版，扫描只给信息，玩家搬运/交付；Echo无开门、搬运或机关写入能力。不把约38–62秒的代理熟悉路线当首次5分钟体验达标，也不声称目标硬件60FPS、人工IME、正式美术、Addressables流送或回收任务LLM质量已通过。原模型E100的88/100失败结论保持。本轮按授权在R2完成后停止，后续R3只保留六意图/单步与有界实验偏好。
