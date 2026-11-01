# EchoStation · 环境回收原型

最新独立灰盒（2026-10-31）：运行 **`Builds/SalvageExpansion/EchoStationSalvageExpansion.exe`**，保留整个文件夹；Unity入口`Assets/EchoStation/SalvageExpansion/Scenes/ES_EnvironmentalSalvage.unity`。

取回数据核心，在6×4形状背包中取舍物资，亲手操作阀门、隔离供电和解除夹具，再回泊位确认交付/主动结算。Echo只做信息协作与扫描，不代操作设备。当前**UseLLM=false/脚本模式**，R3模型质量No-Go保持。

I背包、背包R旋转、E交互、G放下、探索R Echo、Tab任务与引导、F手电、F5/F9存读、F10显示设置。详见[新手指南](Docs/SalvagePrototype/Expansion/PlayGuide.md)、[交付说明](Docs/SalvagePrototype/Expansion/Delivery.md)、[源码职责](Docs/SalvagePrototype/Expansion/SourceArchitecture.md)。

最终90/90回归、八类Windows验证与810有效GPU帧；[实际报告](Docs/Tests/SalvagePrototype/Reports/Expansion_V01_Windows_TestReport.md)、[开发机性能](Docs/SalvagePrototype/Expansion/Performance.md)。目标硬件与真人体验尚待；旧版本/存档保留，编辑器原场景及设置已恢复。本轮停在Windows灰盒。

## 历史版本

上一回收版：[空间与HUD反馈版](Docs/SalvagePrototype/Feedback02_Delivery.md)，运行`Builds/SalvageNext/EchoStationSalvageNext.exe`。下面S2描述为历史版本记录，不代表当前环境回收切片的任务或按键。

### S2 五区探索原型

Unity **6000.6.7f1** / 普通URP Forward / 内部HDR转SDR / D3D11 / Windows10或11 x64。S2工程模块30/30已逐项开发、测试、归档；Echo是本地脚本机器人，**UseLLM=false**。五区灰盒跑通六个环境谜题和三个玩家选择结局，无战斗。

运行 **`Builds/S2/EchoStation.exe`**，保留整个Builds/S2文件夹。编辑器打开`Assets/EchoStation/Station/Scenes/ES_Station_Greybox.unity`。此为S2非Development可玩构建，Steam发布准备属于S4。

WASD/鼠标/空格移动视角跳跃，E交互、F手电、J日志、T文字、Tab指令/显示，1跟随/2待命/3调查眼前目标，F5保存/F9读取。Esc释放鼠标，单击恢复。文字可用“调查储藏室可疑点”“调查校准架”“给我提示”；脚本识别预设中文，已知密码须明确问。Echo只提供信息/移动/扫描/警告，终端/门/回路由玩家操作。

小屋扫描后蓝色箭头指向地下A区校准架，继续下到A区调查；它只是跨区线索，完整流程延续到供电、通道终端和核心选择。路线、操作和后续提示见[无剧透玩法说明](Docs/Stage2/HowToPlay.md)，需要答案时看[完整攻略](Docs/Stage2/Walkthrough.md)。

S2独立手动单槽`Application.persistentDataPath/Saves/S2/slot1.json`，不自动保存/读取、不在结局或新局覆盖旧手动档。建议最终选择前F5保存；新局有确认，读档后单击恢复鼠标。S1程序仍在Builds/S1，与S2存档分离；[S1历史指南](Docs/Stage1/S1_UserGuide.md)保留。

- [开发总文档](Docs/DevelopmentPlan.md) / [30模块报告索引](Docs/Tests/Stage2/Index.md) / [连续授权](Docs/Stage2/ContinuousExecutionAuthorization.md)
- [源码架构](Docs/SourceArchitecture.md) / [S2渲染参数](Docs/Rendering/S2_RenderConfiguration.md) / [实际性能](Docs/Performance/S2_Performance.md)
- [预制体与命名](Docs/Stage2/PrefabAndAssetConventions.md) / [资源许可](Docs/Assets/ThirdPartyLicenses.md) / [故障经验](Docs/Troubleshooting.md) / [DevLog](Docs/DevLog.md)
- [Unity MCP工具](Tools/MCP)：使用module_tests.py安全门禁，不直接调用会保存用户场景的原始测试接口。

新局三结局路线3/3、数据207/207、场景/导航55/55；最终Windows完整归档/新进程读档/无测试参数启动均通过。开发机五区1500有效帧全部0B GC，PVS受控Draw Calls43→38且图差0，原始证据保留。

目标最低i5-8400/GTX1060 6GB/16GB中画质1080p60、推荐i5-12400/RTX3060 6GB/16GB高画质1080p60尚待实机；人类首次25–40分钟、系统IME候选窗体验及长期压力也未验收。当前开发机数据不能替代这些结论。S3本地Barracuda模型/推理、S4四区正式美术/Addressables流送/三档画质尚未开始；本次停在S2。
