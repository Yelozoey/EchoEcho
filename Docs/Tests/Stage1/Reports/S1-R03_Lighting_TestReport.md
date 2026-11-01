# S1-R03 静态烘焙与动态受光测试报告

日期：2026-10-25（Asia/Shanghai）。用户授权参数按推荐执行；参数v0.1。**本模块功能独立测试通过**：R03-A六项配置、R03-B真实烘焙/图像分别通过后进入R03-C；最终构建后28/28通过（R01十三项、R02八项、R03七项），Windows独立程序退出0。未作目标硬件或完整关卡60FPS验收，R04未开始。

## 1. 交付与参数

| 交付 | 工程相对路径 |
| --- | --- |
| LightingSettings | `Assets/EchoStation/Rendering/Lighting/ES_Interior_Prototype.asset` |
| 独立封闭测试室 | `Assets/EchoStation/Tests/Scenes/S1_R03_BakedLighting.unity` |
| 烘焙数据/颜色/方向贴图 | `Assets/EchoStation/Tests/Scenes/S1_R03_BakedLighting/` |
| 创建与安全异步烘焙工具 | `Assets/EchoStation/Rendering/Editor/BakedLightingSetup.cs`、`BakedLightingBake.cs` |
| 独立测试 | `Assets/EchoStation/Tests/Editor/BakedLightingConfigurationTests.cs`、`BakedLightingImageTests.cs` |
| 共享Editor/Player图像验证 | `Assets/EchoStation/Tests/Fixtures/R03LightingProbe.cs` |
| Windows验证程序 | `Builds/ModuleTests/S1-R03/EchoStation_R03.exe` |

实际参数：Progressive CPU，手动Baked GI，Realtime GI关闭；20texels/m，1024图集上限、Padding4，Directional、HighQuality压缩，Direct/Indirect/Environment=64/256/64，Min/Max Bounces=2/2，Gaussian Direct1/Indirect2，无烘焙AO；Probe采样倍率2。完整参数、隔离条件与复用边界见[参数记录](../../../Stage1/S1-R03_LightingParameters.md)。

测试室8×6×4m、墙厚0.25m；八个静态实体独立UV2，两盏Baked点光源；动态胶囊不ContributeGI，使用BlendProbes。93个探针覆盖可走体积、避开实体。该技术夹具不是正式小屋布局、美术或Echo模型。

原模板、R01/R02场景与Volume资产九份保护文件哈希未变；共享Pipeline用户输入字段无变化。SSAO仅在定量对照运行期暂时关闭，最终展示图及Dispose恢复；没有保存或调整其继承参数。原场景布局在烘焙后恢复，不保存/丢弃用户未保存场景。

## 2. 环境与硬件基准

| 项 | 实际/要求 |
| --- | --- |
| 引擎/管线/API | Unity6000.6.7f1 / URP17.6普通Forward / Direct3D11 |
| 输出 | 1920×1080；内部HDR，SDR显示输出，显示器HDR关闭 |
| 当前开发机 | Windows11 10.0.26300；i7-12650H；RTX4060 Laptop GPU，VRAM7956MB；RAM16122MB |
| 最低目标，未实测 | Windows10/11 x64；i5-8400 / GTX1060 6GB / RAM16GB；1080p中画质稳定60FPS |
| 推荐目标，未实测 | Windows10/11 x64；i5-12400 / RTX3060 6GB / RAM16GB；1080p高画质稳定60FPS |

本轮未实现三档画质、整关卡或正式性能预算。纹理内存数据不代表整个场景内存、Addressables卸载或完整游戏帧率达标。

## 3. 实际测试与证据

| 用例 | 实际结果 | 状态 |
| --- | --- | --- |
| A01 配置/引用 | 设置字段与推荐值、LightingSettings引用一致 | 通过 |
| A02 静态实体/UV2 | 八实体ContributeGI/ReceiveGI.Lightmaps，UV2完整有限且三角形非退化，边界封闭 | 通过 |
| A03 探针体积 | 93点分层覆盖，排除实体内部，覆盖移动路径 | 通过 |
| A04 动态/相机/光源 | 代理无Static标记、BlendProbes；两盏灯均Baked；相机HDR/SDR和Volume Mask正确；测量材质隔离反射 | 通过 |
| A05/A06 保护/幂等 | 原资产保护、重复创建不重写资产；原活动场景dirty状态不变 | 通过 |
| B01 实际烘焙 | CPU异步50.378秒；1组512×512，颜色BC6H、方向BC7；LightingData与八个Renderer绑定有效；布局恢复 | 通过 |
| B02 灯开关 | Editor及最终Player静态图像RGB平均绝对差均为0 | 通过 |
| B03 Lightmap移除 | 静态照明变黑，平均亮度0；恢复绑定后继续测试 | 通过 |
| B04 动态受光 | 49点SH非黑、最大相邻亮度变化/峰值=0.0185071；九处实际像素0/1/2倍SH严格单调增加，四种状态帧号严格递增 | 通过 |
| C01 Windows构建与运行 | Succeeded，0 errors、2 warnings；实际D3D11/1080p/HDR/SDR、1组贴图/93探针随包加载；退出0、无超时 | 通过，警告单列 |
| C02 回归 | 构建后28/28、0失败、0跳过，含R01/R02所有原契约；临时状态及哈希核验 | 通过 |

子模块记录：[R03-A](S1-R03A_Configuration_TestReport.md)、[R03-B](S1-R03B_BakeAndLighting_TestReport.md)。最终原始证据：[NUnit](../Evidence/S1-R03/EditMode-Final.json)、[烘焙](../Evidence/S1-R03/BakeResult.json)、[Player](../Evidence/S1-R03/Player/PlayerProbe.json)、[退出](../Evidence/S1-R03/Player/Exit.json)、[指标汇总](../Evidence/S1-R03/MetricsSummary.json)。B04多项断言集中于同一实际图像用例，R03共七个NUnit案例，不能把内部采样数当成额外测试案例数。

## 4. 真实图像与内存

以下为最终Windows独立程序真实GPU截图，线性浮点读数计算，PNG单次sRGB编码。定量图关闭随机颗粒/色差/SSAO以隔离变量；最终展示图恢复原SSAO及R02批准的三效果。

| 图像 | 观察点 |
| --- | --- |
| [灯启用的烘焙室](../Evidence/S1-R03/Player/01_Baked_LightsEnabled.png) | 静态照明与阴影 |
| [所有灯关闭](../Evidence/S1-R03/Player/02_Baked_AllLightsDisabled.png) | 保留相同烘焙照明 |
| [移除Lightmap](../Evidence/S1-R03/Player/03_WithoutLightmaps.png) | 静态照明消失 |
| [动态代理左侧](../Evidence/S1-R03/Player/04_Proxy_Probes_0.png) / [右侧](../Evidence/S1-R03/Player/04_Proxy_Probes_8.png) | 原探针实际受光 |
| [零SH控制](../Evidence/S1-R03/Player/05_Proxy_ZeroBakedProbe.png) / [双倍SH控制](../Evidence/S1-R03/Player/07_Proxy_DoubleProbe.png) | 同一受光路径因果对照 |
| [完整效果室内图](../Evidence/S1-R03/Player/06_Room_ApprovedVolume.png) | 原Volume与SSAO恢复后的展示 |

最终Player中九处零SH控制像素均为0；原受光最小亮度0.00334643；原值减零值的最小增量0.00334643、双倍减原值的最小增量0.00631250。全部超过0.0001技术噪声阈值。49点相邻变化阈值为0.25，实测0.0185071；这验证夹具路径连续性，不等同于所有未来关卡不存在探针接缝。

颜色+方向两张纹理：Editor实测1,400,064字节；Player实测700,000字节，约0.67MiB（Profiler.GetRuntimeMemorySizeLong求和）。这是两张烘焙纹理的占用，不能推导总VRAM或性能。图集上限1024，实际自动打包为512，未强制扩展。

实际图检查未见明显UV缺失黑块或室外光穿入；封闭房间暗面与遮挡阴影保留。正式小屋材质、美术亮度与线索可读性尚未验收。

## 5. 故障、失败历史与限制

- A首轮5/6，枚举零值断言修复后6/6（F-023）。
- B阶段修正宽高比/ROI、Off环境回退假设、SH控制实验与真实帧等待（F-024至F-027）。未把“清零SH即全管线必须全黑”当成有效的通用假设；最终以数据清零、像素0/1/2倍单调响应和连续性证明贡献。
- Editor附加加载测试仍有零SH残余亮度0.00441721–0.09022047，来源没有完整归因；它不计入探针贡献，也不标记根因已修复。独立Player的同一控制为0。保留Editor限制，正式性能/画面判断以独立程序为准。
- C首Player因继承SSAO采样噪声造成静态平均差0.000037684而退出1；首轮数据保留在`PlayerFirstRun/`。量化对照临时停用SSAO、最终展示及Dispose恢复后，最终Player平均差0、退出0；未放宽1e-6静态断言（F-028）。
- 最终构建2条既有警告：Unity Services未关联、System.Windows.Forms平台兼容性；F-011仍跟踪。运行时有禁用DOF/Panini Shader剥离提示（F-019）；未为消提示打开这些效果或修改第三方包。烘焙后出现一次UnityTls网络提示，未导致本模块烘焙失败。

完整经过及失败JSON见[故障经验](../../../Troubleshooting.md)。未执行：Windows10、最低/推荐硬件、正式场景完整性能、动态实时阴影、Lightmap流送/卸载及美术验收。没有LLM、玩家或Echo行为开发。

## 6. 复现

1. Unity6000.6.7f1以D3D11启动。菜单 `Tools/EchoStation/Stage 1/R03/Create Lighting Template`；先运行`BakedLightingConfigurationTests`六项。
2. 确认没有未保存场景或Prefab Stage，再执行 `R03/Bake Isolated Lighting`。工具拒绝保存/关闭未保存内容；只加载本模块场景异步烘焙，完成后恢复原布局。
3. 运行`BakedLightingImageTests`。真实PlayMode、完整URP渲染、实际游戏帧等待；临时Profile/SH/Renderer状态恢复，不写回共享资产。
4. 执行 `R03/Build Validation Player`；仅构建该测试室，不改正式Build Settings场景列表。执行 `Tools/Rendering/RunR03Player.ps1`，45秒超时仅终止自己启动的测试进程。
5. 执行 `python Tools/Rendering/analyze_r03.py`；检查Player退出码、原始像素及采样帧号。构建后运行`EchoStation.Rendering.Editor.Tests`，本版本应28/28。

下一模块为S1-R04实时光与级联阴影；当前报告只结束R03，不代表R04已实现。
