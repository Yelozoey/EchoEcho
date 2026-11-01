# S1-R01：URP Forward + 内部 HDR 测试计划

状态：测试设计，非通过报告。建立日期：2026-10-24。

## 范围与环境

- Unity 6000.6.7f1；当前 URP 17.6.0；Windows 10/11 x64；仅 Direct3D 11。
- 新建项目专用 Pipeline、Renderer、独立测试场景；复制 PC 模板，原模板和 SampleScene 内容保留。
- 本模块只修改普通 Forward、内部 HDR、SDR 输出和管线绑定。其余复制参数必须逐项记录为“继承、未验收”，不擅自调优。
- 内部 HDR 开启；Player 显示器 HDR 支持/默认使用关闭，相机 HDR 显示输出路径关闭。不能为了关闭显示器 HDR 而关闭内部 HDR。
- 独立夹具不依赖玩家、Echo、线索、日志、LLM 或正式关卡。测试辅助代码不算产品系统。

## 所有后续性能测试共同采用的基准

| 级别 | 硬件 | 分辨率/画质 | 性能目标 |
| --- | --- | --- | --- |
| 最低 | i5-8400；GTX 1060 6GB；16GB RAM | 1920×1080，中等画质 | 稳定 60 FPS |
| 推荐 | i5-12400；RTX 3060 6GB；16GB RAM | 1920×1080，高画质 | 稳定 60 FPS |

游戏须支持窗口缩放、全屏和分辨率设置；设置 UI 属于后续模块，本模块不提前制作。每次 Profiler 测试必须记录真实 CPU/GPU/显存/RAM、OS/驱动/API、分辨率、窗口模式、画质和构建类型。没有对应硬件实测不得写“最低/推荐配置达标”。稳定帧率采样口径尚待确认，本模块只做功能/渲染基线验证和当前机记录。

## 夹具要求

1. 独立测试场景使用项目专用 Renderer，包含测试相机、简单几何体、受控照明及已知线性输出大于 1 的 HDR 测试区域。
2. HDR 检查在色调映射前采样，或使用独立浮点 RenderTexture 获取相机渲染结果；最后输出 SDR 不能被误判为内部 HDR 关闭。
3. 配置测试检查实际有效的 Graphics/Quality 管线和相机覆盖，不能只读某个没有生效的资产。
4. 负向测试只改临时复制资产/测试相机，结束后清理，不能破坏正式设置或原模板。
5. 若测试需要关闭后处理来隔离 HDR 样本，仅作用于测试相机；ACES 的正式验收属于 R02。
6. 继承参数检查比较有效的用户输入属性；URP 构建自动重写的 `m_Prefilter*`、`m_Light2DPrefilteringMode` 和 `m_Light2DKeptVariantCombos` 属于派生数据，单独记录而不要求与未构建模板字节相同。增加修改 renderScale 的负向对照，保证检查仍能检出真正参数漂移。依据为已安装 URP 的 ShaderBuildPreprocessor 和 UpdateShaderKeywordPrefiltering。

## 独立测试用例

| ID | 执行方式与具体步骤 | 预期结果 |
| --- | --- | --- |
| R01-T01 | 读取版本文件和实际运行版本、URP 包版本；打开独立场景 | 引擎严格 6000.6.7f1；记录 URP 版本；场景无缺失脚本或编译错误 |
| R01-T02 | 配置检查器读取 Graphics 默认、当前 PC 画质覆盖、Renderer 列表及相机 Renderer 索引；解析最终有效资产 | 全局默认及 PC 有效配置均指向专用资产；相机最终使用专用 Renderer；无失效引用 |
| R01-T03 | 读取专用 Renderer 类型和运行渲染路径；结合 Frame Debugger/运行断言检查 | 普通 Forward；不是 Forward+、Deferred 或自定义回退路径 |
| R01-T04 | 读取 Pipeline 的 supportsHDR 和测试相机 allowHDR；进入 Play Mode | 两者均开启；配置保存后仍有效 |
| R01-T05 | 关闭测试相机色调映射/后处理干扰，在支持浮点输出的测试目标渲染已知 HDR 区域；读取色调映射前像素和格式 | 格式支持 HDR，HDR 区域至少一颜色通道大于 1；不是仅靠资产勾选得出结论 |
| R01-T06 | 检查 PlayerSettings HDR 支持/使用开关、相机额外 HDR 输出开关；记录显示输出状态 | 显示器 HDR 输出路径关闭，内部 HDR 仍开启；最终为 SDR，不要求 HDR 显示器 |
| R01-T07 | 检查 Windows Graphics API 列表、Auto API、运行 SystemInfo.graphicsDeviceType；确认目标平台 | Windows x64；Auto API 关闭且仅 D3D11；当前渲染测试实际运行 D3D11，不以 Player 设置代替运行证据 |
| R01-T08 | 在临时 Renderer/相机副本分别置 Forward+、关闭 Pipeline HDR、关闭相机 HDR，并运行检查器 | 三个错误分别被检出；恢复后通过；原模板/正式配置未被负向测试污染 |
| R01-T09 | 保存资产、重新加载独立场景，再重复配置与 HDR 检查；同一验证连续执行两次 | 引用与设置持久保存；重复执行不产生重复资产、额外相机或结果相互污染 |
| R01-T10 | 对比原 PC Pipeline/Renderer/SampleScene 修改前后内容哈希；检查专用资产继承参数清单 | 原模板内容未改；专用资产只在本模块授权字段/引用上不同；其他参数完整标记为继承、未验收 |
| R01-T11 | 收集运行日志、渲染器/相机状态、截图与当前机硬件记录；检查夹具依赖 | 无本模块引入的 Error/Exception/粉色材质；不依赖 LLM/玩家/Echo；没有把空场景记录写成整机性能达标 |
| R01-T12 | 若当前 Windows 构建支持已安装，生成此独立测试场景的 x64 验证 Player，在 1920×1080/D3D11 运行并检查上述状态 | Player 正常渲染、内部 HDR/SDR 路径与编辑器一致；这是模块验证构建，不是阶段 4 正式交付；无法执行需标记受阻并说明原因 |

测试实现：`Assets/EchoStation/Tests/Editor/ForwardHdrBaselineTests.cs`，Test Runner 程序集 `EchoStation.Rendering.Editor.Tests`。当前 13 项 EditMode 案例包含真实 URP 图像采样；独立 Player 探针和构建入口已提供。具体复现命令与实际结果见 [测试报告](Reports/S1-R01_URPForwardHDR_TestReport.md)。Frame Debugger 在后续需要它的模块单独留证，本轮不伪造截图。

## 验收与报告

配置测试与真实 HDR 渲染检查必须通过；原模板保持不变；独立夹具可重复运行；实际结果归档后才允许推进 R02。如 Player 验证或图形诊断受阻，报告必须明确未执行项，不得隐去或写全通过，并先与用户确认后续验收安排。

实际报告路径：`Docs/Tests/Stage1/Reports/S1-R01_URPForwardHDR_TestReport.md`；实际原始证据放对应报告引用的目录。当前计划不包含任何“通过”结论。
