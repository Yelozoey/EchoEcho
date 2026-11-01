# EchoEcho / EchoStation：Agent 开发交接

## 先读与当前状态

先读本文件、[AGENTS.md](AGENTS.md)、[开发总文档](Docs/DevelopmentPlan.md)、[故障经验](Docs/Troubleshooting.md)。总文档和AGENTS含大量按时间追加的历史记录，**最新用户决定优先**；旧连续授权、阶段验收与旧README不能自动恢复执行。

项目主线已改为 **3D第一人称科幻设施伪装潜入**，求职方向是Unity客户端，同时展示有实际伙伴协作价值的AI。当前交付的是可漫游的六区美术/路线样板，还没有潜入任务闭环、真实巡逻/检查或潜入场景的Echo LLM。不要把旧回收、旧六谜题/三结局当成本主线验收目标。

本轮工作：将用户已接受的浅灰半哑光涂层应用到适合涂装的墙板、设备等表面，保留结构金属/屏幕/机器人配色，独立重GI与反射、实测归档，整理交接和可重建Git工程。实施方案见[A01-F](Docs/InfiltrationPrototype/A01F_CoatedSceneAndRepositoryPlan.md)。新场景与旧场景隔离，不覆盖旧模型、构建、存档或历史失败证据。

## 打开项目与试玩

使用 **Unity 6000.6.7f1、Windows10/11 x64、D3D11** 打开仓库根目录；不要新建工程后只复制脚本。初次打开由UPM恢复Packages；运行时游戏与AI仍要求离线，开发期包恢复与运行时联网不是同一件事。不要随手升级Unity、URP或包锁。

当前主场景：`Assets/EchoStation/InfiltrationCoated/Scenes/ES_Infiltration_Coated.unity`。

按Play、点击Game；WASD移动、鼠标观察、Space跳跃、Esc释放鼠标。Tab显示/隐藏路线，F8切换中庭局部地面倒影。相机为第一人称，地图36×32m包络、净地面约1007.7㎡，六功能区、三主路线、双回环，层高以4.5/6m为主。四机器人为两检查岗/两未来巡逻岗的模型与Idle展示，Echo为轮式模型；当前没有可提交任务、自动开门、战斗或T对话入口。

场景的LevelPreview仅在Play时切到自己的渲染资产，退出恢复原绑定；编辑器默认/PC绑定可能仍为历史`VC_ForwardHDR`，不要把它误认成新场景Play用的管线，或为跑测试永久改全局绑定。历史Build Settings也不能当作当前主入口；本轮按用户要求只Editor测试，没有新Windows发行版。

## 最新用户决定与材质

- 墙板、已有储柜、终端/电脑和喷漆设备主体、桌椅非软质主体采用浅灰半哑光涂层；门框、结构边条、管线、风机和机械裸件保留金属。深色坐垫/橡胶、玻璃、屏幕、灯、标识、布料与机器人配色保留。当前地图无门扇，不为材质任务新增门机关。
- 用户已接受局部样板的亮度与材质，并明确授权全场应用、常规适配由代理决定。涂层是覆盖金属基底的非金属表面：Metallic0、Smoothness.42；派生512色图保留低对比细节，原法线/AO保持。不可只改Metallic滑条却让金属遮罩继续生效。
- 全场1125材质槽中327涂层、798原表现，60个自有变体；相同原Atlas按物体角色/槽分开处理。原地面不整片镜面；中庭只有一块4×4m/1024宽实时平面反射，主Renderer保留SSAO/雾，反射Renderer不重复跑这两项。
- 普通URP Forward、内部HDR缓冲、SDR显示输出；烘焙GI/Lightmap、8区域反射、局部半分辨率16步雾、ACES曝光.55。这里没有UE5、硬件光追、SSR或SSS，不要将平面反射描述成光追。

## 关键源码与资产

| 路径 | 职责 |
| --- | --- |
| `Assets/EchoStation/InfiltrationCoated/Runtime` | 当前独立平面反射、F8反馈 |
| `Assets/EchoStation/InfiltrationCoated/Editor` | 当前场景生成、分类涂层、独立GI/探针烘焙与保存作用域 |
| `Assets/EchoStation/InfiltrationCoated/Generated` | 当前材质/512色图/主与辅助Renderer/Volume/LightingSettings/区域Cubemap |
| `Assets/EchoStation/InfiltrationCoated/Scenes` | 主场景与实际Lightmap/LightingData |
| `Assets/EchoStation/InfiltrationCoated/Tests` | 当前布局、材质保护、烘焙、像素、实际输入/动画与性能测试 |
| `Assets/EchoStation/InfiltrationLevel` | 共享地图布局、世界标记、路线预览、局部雾、岗位数据 |
| `Assets/EchoStation/InfiltrationArt`、`InfiltrationRefinement` | 共享模型/装饰与既有资源适配；不批量改源资源 |
| `Assets/EchoStation/Player`、`Salvage/Runtime/SalvageMotor.cs` | 当前复用输入与运动；复用Motor不代表启用回收玩法 |
| `Assets/EchoStation/AI` | 旧三层AI、意图/权限、tokenizer、ORT执行、探针和旧场景集成 |
| `Assets/EchoStation/LLMShortTasks` | 编辑器中文IME、脚本对话与只读场景报告问答实验 |
| `Tools/MCP`、`Tools/InfiltrationPrototype/A01F` | 安全测试入口、快照/还原/预算和本轮生成操作 |

旧金属图：`InfiltrationMetal/Scenes/ES_Infiltration_Metal.unity`；旧局部涂层样板：`InfiltrationWallPreview/Scenes/ES_WallBrightness_Preview.unity`。`ReflectionSample`是独立反射演示。`Station`是S2五区解谜，`SalvageNext`/`SalvageExpansion`是旧回收/环境工具玩法，均保留作参考，不能和当前入口混淆。

## 测试方式与可信边界

必须使用`Tools/MCP/module_tests.py`，先检查干净场景/Prefab、Console与新鲜RenderSafety。工具是Python标准库客户端，默认连接`http://localhost:8080/mcp`；Unity的MCP插件已嵌入Packages。服务器/客户端连接配置属于机器配置，不随Git上传；新机器需按官方Unity MCP安装说明建立本地连接。

在干净已保存场景中准备快照与临时ES_Gameplay绑定；完整示例见`Tools/InfiltrationPrototype/A01F`和`regression_host.py`。**不要在用户正在Play或有未保存内容时直接运行下列命令。**

```powershell
python Tools/MCP/module_tests.py start --evidence Docs/Tests/InfiltrationPrototype/Evidence/AgentRun --label AgentSmoke_A --group EchoStation.InfiltrationCoated.Tests.CoatMaterialTests
python Tools/MCP/module_tests.py poll --evidence Docs/Tests/InfiltrationPrototype/Evidence/AgentRun --label AgentSmoke_A
```

必须先确认start返回真正Job ID再poll；`external_changes_dirty`先refresh且compile:none，新标签重试，不能覆盖旧回执。EnterPlayMode/ExitPlayMode必须由顶层UnityTest/UnityTearDown yield，不可嵌套。GPU测试要求Game视图可渲染与有效GPU+CPU/Draw样本，0计数不是性能优秀。测试写入的原始回执默认Git忽略，本机仍保留。

本轮实际报告依次见[F01材质/布局](Docs/Tests/InfiltrationPrototype/Reports/A01-F01_CoatingAssets_TestReport.md)、[F02光照/反射](Docs/Tests/InfiltrationPrototype/Reports/A01-F02_Lighting_TestReport.md)、[F03 Editor交付](Docs/Tests/InfiltrationPrototype/Reports/A01-F03_Editor_TestReport.md)。F01实际21/21；F02实际11/11，自己ProgressiveGPU烘焙2 Lightmap/216探针/8反射。最终输入、性能、组合和保护结果以F03实际报告为准，不将测试计划当通过。

F03独立3/3、最终组合25/25（均无跳过/空output）；原5设置、运行时和布局恢复后15,419旧文件SHA一致，再打开新场景clean/Edit、Console0。最终性能有效218样本，但开/关GPU35.348/34.365ms，性能限制依旧存在。

目标：i5-8400/GTX1060 6GB/16GB，中画质1080p稳定60FPS；推荐i5-12400/RTX3060 6GB/16GB，高画质1080p稳定60FPS。当前开发机Editor采样只作工程与局部开销证据；没有这两台目标设备验收，也没有新机完整导入或外部真人潜入流程验收。

**已知性能限制：**本轮涂层版GPU开/关倒影均值36.869/33.718ms；同条件未改旧金属控制35.995/34.370ms，Draw5421/4174。有效采样不等于性能达标，两版均高于16.7ms，原因未定位，不能说只是材质或后台节流。建议后续独立Profiler任务定位并复核目标硬件；不要把旧低帧时或测试条数当60FPS证明。

## LLM现状与禁止误读

当前潜入场景没有接上LLM。旧Qwen3-0.6B/ORT CPU离线链路能运行，但旧S3/R3及最新L02-Q01质量仍No-Go：只读问答v2已见20条诊断15/20，未达18/20，独立集未启动。用户说流程正常/个别回答正确，不代表模型可靠性通过。不要用脚本修正后结果冒充模型原始正确率。

原Barracuda路线已有不兼容证据，用户已允许切ORT CPU；三层仍为记忆 → LLM语义 → 传统执行。LLM只产结构化意图，不直接操作GameObject。Echo不得代开门、改机关或代替玩家解题；`UseLLM=false`的旧完整玩法保持，未来新玩法也需要脚本后备。实际推理必须后台、单请求、完成后300ms冷却/20秒超时等边界详见Stage3方案。六意图为follow/investigate/wait/warn/answer/refuse。

Qwen2.5处理暂停；本地LoRA仅提案，用户选择保留，未授权现在启动训练。过去Prompt实验各轮有明确候选/用例上限并已耗尽，不能借旧长期授权无限追加实验、下载新模型或上云。权重不在Git；恢复见[模型与仓库说明](Docs/Repository/RebuildAndModels.md)。默认脚本/实验开关限制继续保留。

## 下一步如何继续

建议下一个短任务为 **A02：一名定点门卫的传统行为树**，先验证发现玩家、怀疑、检查的可见反馈，再扩巡逻。这里尚无已确认的怀疑阈值、伪装/凭证规则、失败/恢复规则、守卫权限与测试验收，先将最小方案和独立用例交用户确认，再实施；不要同时堆四个守卫、潜入闭环和LLM。

用户偏好一次短任务，交本人在Editor实际测试；不主动打包。后续Echo应作为LLM＋行为树伙伴，让自然表达、情境与临时约定形成可见协作；寻路、侦察、警告、取消等动作由传统系统可靠执行，不能把新加的全部伙伴动作算作LLM贡献。守卫使用传统BT；旧“LLM门卫”方案已取消。

## 保护、预算与故障

原用户资产/未保存场景/Play会话不动；不得擅自保存、丢弃或停止。必要重启只有在获授权且新鲜确认无未保存内容时执行。完成时要还原Graphics/Quality/Editor/PlayerSettings/TimeManager的native JSON、磁盘原字节、dirty及background/fps/timeScale，恢复布局，做全量旧文件SHA；仅还原管线路径不够。

本轮明确上限 **21GiB，包含.git**，旧20GiB为历史。本机大模型/旧构建/历史原始证据保留，当前资源计账入口`A01F/task.py budget`；未来超预算、删除旧物、付费、云服务或玩法权限变化仍须确认。

先查[故障文档](Docs/Troubleshooting.md)：F-368设置格式/后台开关回写；F-369旧IAP Editor网络empty response（不是渲染源码错误，不能默认无视）；F-372/374重载MCP失联，先确认新DLL/原生状态，未知写结果不盲重放；F-376 UTF-8与类名替换不可误改_Metallic；F-377浅墙上的岗位文字需要深色。任何新故障留原失败回执、修复后重跑受影响范围。

## 仓库交付范围

目标仓库`https://github.com/Yelozoey/EchoEcho`，分支main，使用正常Git推送。Assets及.meta、Packages、ProjectSettings、C#、工具、MD报告与精选本轮图进入Git；Library/Temp/Logs/UserSettings、Builds、模型权重、原始Profiler、下载包与机器私有配置被忽略且保留本机。模型清单/tokenizer/许可保留，第三方通知见[仓库许可索引](Docs/Repository/ThirdPartyNotices.md)。当前自己的代码未新增统一开源许可证，不要擅自给第三方资产统一改许可。

历史报告可能指向只在原机保留的完整Evidence/原包或绝对路径，这是可重建工程交付策略，不是声称那些数据不存在。本轮必要烘焙/材质收据和精选图保留在仓库，完整原始保护快照不公开。接手时先`git status`检查用户变化、核对主场景，不要重置工作树或复活旧原型任务。
