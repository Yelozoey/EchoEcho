# S1-R01：URP Forward + 内部 HDR 独立测试报告

日期：2026-10-24（Asia/Shanghai）。

结论：**本模块功能验收通过**。2026-10-24最终EditMode13/13通过；Windows x64验证Player构建成功、运行退出码0，内部HDR与SDR输出检查通过。当时构建4条警告，单独跟踪；未进行目标硬件60FPS验收。以下保留首模块当日证据。

2026-10-25补充：S1-R02已获用户确认并实现。R01回归契约v0.2仅允许共享Pipeline的 `m_ColorGradingMode=HighDynamicRange`、`m_VolumeProfile=ES_Volume_Defaults` 两项批准变化，且断言准确值；其余输入漂移检查不变。R01场景与三份原模板哈希未变。R02构建后最终21/21包含本类全部13项，见 [R02报告](S1-R02_Volume_TestReport.md)。

## 1. 模块交付与边界

| 交付 | 实际文件 |
| --- | --- |
| 项目专用 Pipeline | `Assets/EchoStation/Rendering/Settings/ES_PC_ForwardHDR.asset` |
| 项目专用 Renderer | `Assets/EchoStation/Rendering/Settings/ES_PC_ForwardRenderer.asset` |
| 独立场景 | `Assets/EchoStation/Tests/Scenes/S1_R01_ForwardHDR.unity` |
| 创建/配置工具 | `Assets/EchoStation/Rendering/Editor/ForwardHdrBaselineSetup.cs` |
| 独立配置与图像测试 | `Assets/EchoStation/Tests/Editor/ForwardHdrBaselineTests.cs` |
| Player 验证探针 | `Assets/EchoStation/Tests/Fixtures/R01PlayerProbe.cs` |
| Player 构建工具 | `Assets/EchoStation/Rendering/Editor/R01PlayerValidationBuild.cs` |
| 参数逐项清单 | [S1-R01_RenderParameters.md](../../../Stage1/S1-R01_RenderParameters.md) |
| 可运行验证程序 | `Builds/ModuleTests/S1-R01/EchoStation_R01.exe` |

原 `PC_RPAsset`、`PC_Renderer`、`SampleScene` 内容哈希保持不变。全局 Graphics 与当前 PC Quality 绑定到专用 Pipeline；模板资产保留。相机及测试 Shader/Probe 仅用于独立夹具，不代表正式游戏场景或玩家功能已经完成。

本模块没有开发后处理模板、小屋、Echo、玩家、日志、存档或 LLM。未调优继承的光照、阴影、SSAO、批处理等参数；不能把模板中已经开启的开关算作这些模块验收完成。

## 2. 测试环境与目标基准

实际验证环境（来自 Player 的 SystemInfo）：

| 项目 | 实测值 |
| --- | --- |
| Unity / URP | 6000.6.7f1 / 17.6.0 |
| OS | Windows 11，10.0.26300 |
| CPU | Intel Core i7-12650H |
| GPU | NVIDIA GeForce RTX 4060 Laptop GPU |
| 显存 / RAM 报告值 | 7956 MB / 16122 MB |
| 图形 API | Direct3D11；Editor 与 Player 均已验证 |
| Player 渲染尺寸 | 1920×1080 |
| 构建 | Windows x64 Development；独立测试场景；batchmode 图形验证，未使用 nographics |

用户确认的后续 Profiler 与调优基准保持如下，不以本机替代：

| 目标档位 | 硬件 | 目标 |
| --- | --- | --- |
| 最低 | i5-8400 / GTX 1060 6GB / 16GB RAM | 1080p 中等画质稳定 60 FPS；本轮未实测 |
| 推荐 | i5-12400 / RTX 3060 6GB / 16GB RAM | 1080p 高画质稳定 60 FPS；本轮未实测 |

稳定帧率统计口径和各项性能预算待确认。以下测试耗时是测试执行耗时，不能换算为产品帧率。本模块尚未制作三档画质、窗口/全屏/分辨率设置 UI。

## 3. 实际执行记录

| 轮次 | 结果 | 原因与处理 | 原始证据 |
| --- | --- | --- | --- |
| 首轮 | 12 项，11 通过、1 失败 | 当前 Editor 仍为 D3D12；配置列表改变不等于运行设备改变；用户授权干净场景重启 | [EditMode-FirstRun.json](../Evidence/S1-R01/EditMode-FirstRun.json) |
| D3D11 重启后 | 12/12 通过 | 新 Editor PID 28716；实际 D3D11 | [EditMode-D3D11.json](../Evidence/S1-R01/EditMode-D3D11.json) |
| Windows 验证构建 | 成功，0 errors、4 warnings；107.653 秒 | 仅构建本模块场景，不改正式 Build Settings 场景清单 | [PlayerBuild.json](../Evidence/S1-R01/PlayerBuild.json) |
| 独立 Player | 通过，退出码 0 | D3D11、1080p、内部 HDR 与关闭 HDR 显示输出均满足 | [PlayerProbe.json](../Evidence/S1-R01/PlayerProbe.json)、[Player.log](../Evidence/S1-R01/Player.log) |
| 构建后首次回归 | 12 项，11 通过、1 失败 | T08 把 URP 自动重写的 Shader 变体过滤派生字段当成用户参数；据本地 URP 源码纠正测试边界 | [EditMode-Final.json](../Evidence/S1-R01/EditMode-Final.json) |
| 最终回归 | **13/13 通过，0 失败/跳过** | 新增 renderScale 负向对照，确保真正参数漂移仍被拒绝；耗时约 0.331 秒 | [EditMode-Acceptance.json](../Evidence/S1-R01/EditMode-Acceptance.json) |

构建验证辅助代码曾发生 CS0266，已修复并重新编译。相关失败过程及重启工具问题均见 [故障经验 F-007 至 F-012](../../../Troubleshooting.md)。没有删除失败结果或放宽 Forward/HDR/D3D11 断言。

## 4. 测试计划对应结果

| 计划用例 | 执行证据 | 实际结果 |
| --- | --- | --- |
| R01-T01 版本/资产 | 自动化 T01 | 6000.6.7f1；专用资源与 Shader 存在并受支持，通过 |
| R01-T02 实际管线/相机覆盖 | 自动化 T02 | Graphics、PC Quality、当前有效 Pipeline、相机 Renderer 一致，通过 |
| R01-T03 普通 Forward | 自动化 T03，专用 Renderer 绑定与本地 URP 渲染路径定义 | RenderingMode.Forward（0），不是 Forward+，通过 |
| R01-T04 HDR 开关 | 自动化 T03/T04 | Pipeline 与相机内部 HDR 均开启，通过 |
| R01-T05 实际 HDR 图像 | 自动化 T10 + PlayerProbe | 1920×1080、R16G16B16A16_SFloat；中心像素 RGB=(4,2,1)，大于 1 的值保留，通过 |
| R01-T06 SDR 输出 | 自动化 T04 + PlayerProbe | HDR Display Support/Use 关闭、相机 HDR 输出关闭；Player displayHdrActive=false，通过 |
| R01-T07 API/平台 | 自动化 T05 + PlayerProbe | Windows x64、仅 D3D11，Editor/Player 实际 D3D11，默认 1920×1080，通过 |
| R01-T08 错误配置 | 自动化 T06 三个案例 + T11 | Forward+、Pipeline HDR 关闭、Camera HDR 关闭均被拒绝；renderScale=0.5 的真实漂移被明确检出，通过 |
| R01-T09 持久性/重复运行 | 自动化 T09、重启后与构建后多轮执行 | GUID 稳定、不重复创建资产；保存和重开后配置有效，通过 |
| R01-T10 模板保护/继承 | 自动化 T07/T08、SHA256、参数清单 | 三份原文件未改；实际用户输入参数保持继承，派生字段单独归档，通过 |
| R01-T11 日志/隔离 | 编译检查、独立场景、运行日志 | 当前模块无编译 Error/运行 Exception；夹具无玩家/Echo/LLM 依赖；兼容性警告另记 |
| R01-T12 Windows Player | BuildReport + 独立进程 Probe | 构建成功，进程正常退出 0，实际图形验证通过 |

实际 HDR 检查使用 URP SingleCameraRequest 渲染到浮点 RenderTexture 并读取像素；没有额外留存 Frame Debugger 截图，本轮不宣称已完成后续阴影、遮挡或合批模块所需的帧调试验证。测试相机关闭后处理以隔离 HDR，ACES 在 S1-R02 验收。

## 5. 复现入口

1. 使用 Unity 6000.6.7f1 打开项目，以 `-force-d3d11` 启动；检查当前 PC 档。不要在存在未保存场景时直接退出重启。
2. 执行菜单 `Tools/EchoStation/Stage 1/R01/Create Baseline`。工具幂等；不会重复创建资产或重写原模板。
3. 在Unity Test Runner运行 `ForwardHdrBaselineTests`，应发现13项案例；整个 `EchoStation.Rendering.Editor.Tests` 程序集在R02后为21项。
4. 执行 `Tools/EchoStation/Stage 1/R01/Build Validation Player`，查看 `PlayerBuild.json`。
5. 独立验证命令（下列路径为本工程实际位置；不加 `-nographics`）：

```powershell
& 'E:\Projects\Unity\EchoStation\Builds\ModuleTests\S1-R01\EchoStation_R01.exe' -batchmode -force-d3d11 -screen-width 1920 -screen-height 1080 -screen-fullscreen 0 --r01-report 'E:\Projects\Unity\EchoStation\Docs\Tests\Stage1\Evidence\S1-R01\PlayerProbe.json' --r01-exit
```

6. 生成当前参数记录：`python Tools/Rendering/write_r01_inventory.py`。

后续模块经用户确认而修改参数时，须追加批准的差异清单与基线版本，再调整相应回归契约；不得为了通过测试而任意忽略参数或回滚后续已经验收的功能。

## 6. 未结事项与下一模块

- 构建汇总 4 条 warning；控制台观测包括两条第三方 MCP UAC0005、一条 Unity Services 未关联项目提示，以及 System.Windows.Forms.dll 平台兼容性警告。确切依赖引入根因尚未查明；见 F-011。本轮未为消除提示关联云服务、修改第三方包或切换 .NET 配置。
- Windows 10 与两档目标硬件的功能/性能测试尚未执行；没有完整关卡、画质预设或性能达标结论。
- 当日下一模块为S1-R02。该模块后来于2026-10-25获批并完成，当前进度见总开发文档和R02报告；本报告的13项首轮证据仍保持历史记录。
