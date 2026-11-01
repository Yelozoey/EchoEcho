> S1历史使用说明。当前S2入口与最新进度见项目根README。

# EchoStation · 阶段 1 基础原型

使用 Unity **6000.6.7f1** 打开项目。当前交付目标是地面观测小屋探索 Demo，Echo 使用传统 C# AI，`UseLLM=false`。**S1全部31模块独立通过，工程回归244/244，Windows完整探索与同版本跨进程恢复通过；开发机900帧全部0B GC。** 实际结果见 [开发总文档](../../Docs/DevelopmentPlan.md) 和 [模块报告](../../Docs/Tests/Stage1/Reports/)。

## 运行

源码已进入S2逐模块开发，记忆M01–M07已独立通过；[S2模块进度](../../Docs/Stage2/ModuleBreakdown.md)和[已确认玩法](../../Docs/Stage2/GameplayProposal.md)记录当前状态。下面的可运行构建仍是S1；S2完整五区/对话/三个结局完成并验收后另行交付，不将后端测试当成已可游玩的新版本。

Windows 10/11 64 位运行 `Builds/S1/EchoStation.exe`，保留整个 `Builds/S1` 文件夹及其中 Data/MonoBleedingEdge 等依赖，不能只复制 exe。此为 S1 Development 构建；Steam 发布构建在阶段 4。编辑器场景：`Assets/EchoStation/World/Scenes/ES_Cabin_Demo.unity`。重建使用菜单 `Tools/EchoStation/Stage 1/P09/Build Windows Demo`，构建前需场景与Prefab无未保存内容。

| 操作 | 输入 |
| --- | --- |
| 移动 / 视角 / 跳跃 | WASD / 鼠标 / 空格 |
| 读取近处线索 / 手电 | E / F |
| 日志（分类、全文、滚轮） | J；J 或 Esc 关闭 |
| Echo 脚本控制与显示设置 | Tab；Tab 或 Esc 关闭 |
| 跟随 / 待命 / 调查选定位置 / 警告供电面板 | 1 / 2 / 3 / 4 |
| 单槽保存 / 读取 | F5 / F9；控制面板也有按钮 |
| 释放 / 重新捕获鼠标 | Esc / 点击画面 |

Echo 调查可在 Tab 面板中选择位置；警告需要设备处于 Echo 真实感知范围与机身视角内。指令经既有意图验证/行为树执行，机器人不会开门、改供电状态或代替玩家解谜。站位可能影响动态避让，若提示无法扫描，先让出路线再重新指定位置。蓝灯平静、黄灯思考/扫描、红灯警戒。

从门厅进入主室，读取桌前维护记录和终端；经右侧通道进入储物间，收集值班记录，再请 Echo 调查可疑位置以显现隐藏标记。日志自动归档全文，另有摘要接口供未来记忆层入库。Tab 提供窗口分辨率与无边全屏选项；完整高/中/低画质设置属于阶段 4。

存档在 `Application.persistentDataPath/Saves/slot1.json`，带版本/校验、原子替换和上次有效备份。不自动读档、不在退出时覆盖：继续上次进度请按 F9。读取后鼠标释放，点击继续。损坏文件保留；有有效备份时回退并提示，未来版本或双损坏拒绝覆盖。测试使用项目内隔离目录，不触及玩家真实存档。

## 项目与记录

- [阶段与模块拆分](../../Docs/Stage1/ModuleBreakdown.md) / [持续授权账本](../../Docs/Stage1/ContinuousExecutionAuthorization.md)
- [源码职责](../../Docs/SourceArchitecture.md) / [渲染配置](../../Docs/Rendering/S1_RenderConfiguration.md) / [性能实测](../../Docs/Performance/S1_Performance.md)
- [资源与预制体规范](../../Docs/Stage1/PrefabAndAssetConventions.md) / [资源许可](../../Docs/Assets/ThirdPartyLicenses.md)
- [故障经验](../../Docs/Troubleshooting.md)：开发前先查；失败记录和复测均保留。
- [MCP 操作工具](../../Tools/MCP)：Unity MCP 包已配置；测试用 `module_tests.py`，不直接调用会自动保存场景的原始测试接口。

内部 HDR、ACES 转 SDR，显示器 HDR 关闭，普通 URP Forward、D3D11、1080p。开发机测试不能代替最低 i5-8400/GTX1060 6GB/16GB 中画质，或推荐 i5-12400/RTX3060 6GB/16GB 高画质的 1080p 稳定 60FPS 验收。

S1 不含 LLM、完整对话/记忆、五区完整流程、25–40 分钟时长或三个结局。用户已确认S2完整方案并授权逐模块连续完成；阶段 2 使用五区灰盒闭环固定谜题/分支，完成后停止；阶段 3 再接本地 Barracuda；阶段 4 完成正式场景与性能/打包。
