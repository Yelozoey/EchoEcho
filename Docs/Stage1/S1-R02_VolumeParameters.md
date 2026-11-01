# S1-R02 Volume 模板参数与使用

版本：v0.1；用户确认及实现日期：2026-10-25。依据 [获批提案](S1-R02_VolumeParameterProposal.md)。实际测试结论另见模块测试报告。

## 资产与引用

| 资产 | 路径（工程相对） | 用途 |
| --- | --- | --- |
| 中性默认 Profile | `Assets/EchoStation/Rendering/Settings/ES_Volume_Defaults.asset` | 绑定共享 Pipeline，清除旧模板后处理影响 |
| 探索 Profile | `Assets/EchoStation/Rendering/Settings/ES_Volume_Exploration.asset` | 仅ACES、颗粒、色差三组件 |
| 全局 Volume 预制体 | `Assets/EchoStation/Rendering/Prefabs/ES_GlobalVolume.prefab` | Global=true、Weight=1、Priority=0、PostProcessing Layer |
| 独立测试场景 | `Assets/EchoStation/Tests/Scenes/S1_R02_Volume.unity` | HDR灰阶、色块、棋盘与字形技术图卡，非产品关卡 |
| 测试图卡 Shader | `Assets/EchoStation/Tests/Fixtures/VolumeChart.shader` | 程序生成测试图卡，无外部贴图依赖 |

`PostProcessing` 占用用户 Layer 8；相机 Volume Mask 仅为 `1 << 8`（256）。只隔离Volume，几何体仍使用独立测试场景与相机。未来正式场景是否叠加局部Volume必须另行确认，不擅自扩大Mask或覆盖基础Profile。

## 已批准且实施的值

| 项 | 值 | Override |
| --- | --- | --- |
| Tonemapping Mode | ACES | true |
| Film Grain Type | Thin1 | true |
| Film Grain Intensity | 0.10 | true |
| Film Grain Response | 0.80 | true |
| Film Grain Custom Texture | 无 | false，不用于Thin1 |
| Chromatic Aberration Intensity | 0.02 | true |
| Pipeline Color Grading Mode / LUT | HighDynamicRange / 32 | HDR为本模块批准变更；32维持继承值 |
| Pipeline Volume Profile | ES_Volume_Defaults | 本模块批准变更 |
| 相机 Post Processing / HDR / HDR Display Output | true / true / false | 相机级配置 |

默认Profile显式使用引擎中性参数：Tone=None、Grain/CA/Bloom/Vignette/MotionBlur/LensDistortion强度0、DOF=Off、Panini Distance=0；曝光/对比度/饱和度/白平衡均0、Filter白色；ColorLookup、Curves、ChannelMixer、LiftGammaGain、ShadowsMidtonesHighlights、SplitToning、ScreenSpaceLensFlare使用中性默认值。Tone的HDR显示器专用字段不Override。它不改SSAO、GI、光照或阴影。

共享Pipeline用户输入变化仅有 `m_ColorGradingMode` 和 `m_VolumeProfile`。原模板三文件与R01测试场景保留。R01回归契约升级至v0.2，对这两个字段断言准确批准值；其他真实输入字段继续检查，构建派生字段按F-012记录。

## 复用与复现

执行 `Tools/EchoStation/Stage 1/R02/Create Volume Template` 可幂等创建。已有Profile、预制体和测试场景不会被重写；工具只重新绑定已批准的两个Pipeline字段。创建前检查图卡Shader无编译错误，不覆盖已打开场景。新场景中使用预制体时，相机必须开启Post Processing、内部HDR，关闭HDR Display Output，并显式设置Volume Layer Mask。

运行Unity Test Runner的 `EchoStation.Rendering.Editor.Tests`。图像测试临时进入PlayMode，使用真实完整URP渲染请求；结束清理运行期Profile克隆，不写回资产。独立构建菜单为 `Tools/EchoStation/Stage 1/R02/Build Validation Player`，只构建该测试场景，不改正式Build Settings场景列表。

景深焦点、体积雾/光、光照色调、画质预设不在本模块实施范围。图卡的技术效果通过后，正式场景效果仍需随场景制作评审。
