# S1-R02：Volume 模板独立测试报告

日期：2026-10-25（Asia/Shanghai）；方案v0.1，用户已确认。**本模块功能独立测试通过**：最终21/21（R02八项、R01回归十三项），Windows x64验证程序退出0；实际图像与本机CPU/GPU开关对比已归档。未进行两档目标硬件或完整关卡60FPS验收，未开始R03。

## 1. 交付与范围

- 中性默认Profile、探索Profile、Global Volume预制体、PostProcessing Layer 8和独立R02图卡场景。
- ACES；Thin1颗粒Intensity=0.10、Response=0.80；CA=0.02。曝光/白平衡/对比度/饱和度中性；关闭Bloom、暗角、运动模糊、景深、畸变和Panini。
- 共享Pipeline仅两项获批用户输入变化：HDR调色和默认Volume Profile引用；R01回归契约v0.2逐项断言批准值，保留其他漂移检查。三个原模板和R01场景SHA256均未变。
- 参数、路径与复用入口见 [Volume参数记录](../../../Stage1/S1-R02_VolumeParameters.md)。测试代码和图卡仅为验证夹具，不代表小屋、玩家、Echo、景深或正式UI已实现。

## 2. 环境及性能基准

| 项 | 实测/要求 |
| --- | --- |
| Unity / URP | 6000.6.7f1 / 17.6.0 |
| 本机系统 | Windows 11，10.0.26300 |
| 本机硬件 | i7-12650H / RTX 4060 Laptop GPU，驱动32.0.16.1714 / RAM16122MB、VRAM7956MB |
| 实际API / 分辨率 | Direct3D11 / 1920×1080 |
| 内部缓冲 / 输出 | R16G16B16A16_SFloat；HDR启用；显示器HDR关闭，SDR输出 |
| 最低目标（尚未实测） | Windows10/11 x64，i5-8400 / GTX1060 6GB / RAM16GB，1080p中画质稳定60FPS |
| 推荐目标（尚未实测） | Windows10/11 x64，i5-12400 / RTX3060 6GB / RAM16GB，1080p高画质稳定60FPS |

本模块没有制作三档画质；当前PC档的其他渲染参数仍为继承未验收值。稳定帧率的采样时长、百分位与允许尖峰阈值仍待后续确认。

## 3. 实际测试结果

| 计划用例 | 实际执行与证据 | 结果 |
| --- | --- | --- |
| R02-T01 参数/绑定/生效栈 | 配置测试T01–T03；Player实际栈ACES/0.10/0.80/0.02；相机Post=true、HDR=true、显示器HDR=false；Layer Mask=256；Global/1/0 | 通过 |
| R02-T02 中性/零权重 | 配置T03/T04；其他效果强度0、DOF Off，权重0回到中性默认值 | 通过 |
| R02-T03 ACES实际图像 | 1080p真实URP渲染，灰阶ROI平均RGB绝对差2.086536；指定亮部R从3.525391变0.895020 | 通过 |
| R02-T04 颗粒多帧/Response | Editor和Player均采8个不同frameCount；每帧Response0和0.8使用相同随机偏移，均与无颗粒ACES参考比较；Player亮区差异比均值0.240253 | 通过 |
| R02-T05 色差/字形图卡 | CA=0/0.02的棋盘边缘ROI平均RGB绝对差0.00399881；28/21/14像素高字形截图留存，供视觉复核 | 功能通过；不代替正式字体/UI可读性验收 |
| R02-T06 外部Volume隔离 | 配置T05及图像测试；Default层高优先级Neutral+Bloom10注入；目标栈未变，整幅图像差异0 | 通过 |
| R02-T07 持久性/保护/回归 | 保存重开、PlayMode、独立Player、重复工具GUID稳定；T06/T07哈希与dirty状态；R01十三项包含错误配置和真实renderScale漂移负向测试 | 通过 |
| R02-T08 Windows Player | 最终构建Succeeded、0 errors、2 warnings；显示窗口运行后自动退出0，无超时；实际D3D11/1080p/HDR/SDR及图像检查通过 | 通过，已知警告单列 |
| R02-T09 开销记录 | 4组正常帧OFF/ON/OFF/ON；279个有效CPU/GPU样本，81个无效样本剔除，0重复；原始值和方法留档 | 记录完成；未作目标性能验收 |

NUnit最终结果：[EditMode-Final.json](../Evidence/S1-R02/EditMode-Final.json)，21/21、0失败、0跳过。R02八项由配置七项及实际图像一项组成；图像测试逐项断言ACES、颗粒、Response、CA、有效栈与隔离。所有测试通过后才归档本报告，没有进入依赖模块。

图像使用完整URP StandardRequest，在PlayMode真实采样；线性浮点目标读数用于计算，PNG做一次sRGB编码。指标仅判断技术效果存在与隔离正确，不能推导美术效果或真实游戏UI已获用户验收。

## 4. 实际图像

以下全部来自最终Windows Player真实GPU渲染，无生成式图像或额外美化。上部HDR灰阶，中部灰/冷蓝/橙色块与棋盘，下部测试字形。

| 图像 | 用途 |
| --- | --- |
| [01 无Tonemapping](../Evidence/S1-R02/PlayerFinal/01_NoTonemapping.png) | 与ACES比较；PNG的SDR编码会裁剪超1值，原始浮点读数保留在JSON |
| [02 仅ACES](../Evidence/S1-R02/PlayerFinal/02_ACES_Only.png) | 无颗粒、无CA参考 |
| [03 ACES+颗粒](../Evidence/S1-R02/PlayerFinal/03_ACES_Grain.png) | 单张展示；实际颗粒结论依据8帧统计 |
| [04 ACES+色差](../Evidence/S1-R02/PlayerFinal/04_ACES_CA.png) | 对比棋盘边缘及字形 |
| [05 获批三效果](../Evidence/S1-R02/PlayerFinal/05_Approved.png) | 当前模板的完整效果，供后续视觉反馈 |

## 5. 本机开销诊断

最终Player为Development构建，临时启用Frame Timing Stats，构建finally已恢复工程原值false。用户明确允许短暂显示1080p测试窗口；测试后已自动关闭。计时阶段用正常相机帧，不执行额外RenderRequest、ReadPixels、PNG编码；每组预热30帧后取90个候选帧。读取延迟由预热覆盖；只统计返回的唯一非零时间戳且CPU/GPU均大于0的样本。未取得的GPU值不以0计入统计。

| 正常帧指标（ms） | 关闭后处理 | 开启后处理 | 开启减关闭 |
| --- | --- | --- | --- |
| 有效样本数 | 137 | 142 | 总279；另81无效 |
| CPU帧时间中位数 | 0.387300 | 0.423200 | +0.035900 |
| GPU帧时间中位数 | 0.253952 | 0.299008 | +0.045056 |
| CPU帧时间均值 | 0.486027 | 0.554749 | +0.068722 |
| GPU帧时间均值 | 0.334422 | 0.401134 | +0.066712 |

组别GPU中位数依次0.253952 / 0.300032 / 0.253952 / 0.299008ms。CPU帧时间包含等待，不能当作后处理CPU函数耗时；GPU帧时间是整帧指标，两状态之差仅为这个极简图卡夹具的短时诊断。场景无正式几何/光照负载，当前机笔记本功耗状态与Development开销也会影响数据；不从这些数字计算或宣称完整游戏可稳定60FPS。

另有16次显式请求（4次交替预热后关闭/开启各8次），Player CPU提交中位数0.454100/0.475750ms，同步读取12.867450/13.044950ms。读取会等待GPU并复制整幅图像，不能标为GPU渲染耗时或正常游戏帧时间。

原始数据：[PlayerProbe.json](../Evidence/S1-R02/PlayerFinal/PlayerProbe.json)、[指标汇总](../Evidence/S1-R02/MetricsSummary.json)。汇总脚本 `Tools/Rendering/analyze_r02.py` 从真实原始数据计算，无效和缺失数据不补造。批处理/隐藏进程曾得到360个GPU无效值，历史证据保留，不与最终可见窗口数据混合。

## 6. 故障、修复与限制

首轮配置失败7项，原因是VolumeManager未初始化（F-015）；配置复测20/20。首图像失败因SingleCameraRequest未更新Volume及EditMode帧号冻结（F-016）；改为StandardRequest+PlayMode后21/21。构建后进一步修正预热残留请求（F-020）和失焦后台帧冻结（F-022），最终构建后21/21通过。自有Shader保留字错误和构建警告均已修复并复测（F-014/F-017）。完整过程见 [故障经验](../../../Troubleshooting.md)，失败JSON保留在同一证据目录。

最终构建：[PlayerBuild.json](../Evidence/S1-R02/PlayerBuild.json)，Succeeded、0 errors、2 warnings（Unity Services未关联；System.Windows.Forms平台兼容性）。增量构建间警告数可能不同，不据此标记F-011依赖问题已修复。本轮没有关联云项目、更改.NET兼容性或修改第三方代码。

最终运行日志：[Player.log](../Evidence/S1-R02/PlayerFinal/Player.log)；有四条禁用DOF/Panini Shader已剥离的URP Warning，实际启用的三效果已由图像验证。未通过开启禁用效果或关闭剥离来消提示；后续景深模块须重建与验证对应Shader。退出证据：[Exit.json](../Evidence/S1-R02/PlayerFinal/Exit.json)，exitCode=0、timedOut=false。本轮不宣称完全无警告。

尚未执行：Windows10、最低/推荐硬件、完整场景性能、正式游戏字体/中文UI、三档画质、光照烘焙和交互景深。下一模块S1-R03静态Lightmap模板的具体参数/技术选择仍需用户确认，本报告不授权默认实施。

## 7. 复现

1. Unity6000.6.7f1，以D3D11启动；当前PC档；保留用户未保存场景。
2. 菜单 `Tools/EchoStation/Stage 1/R02/Create Volume Template`；运行Test Runner中 `EchoStation.Rendering.Editor.Tests`，应21/21。图像测试自动进入/退出PlayMode，临时允许后台运行并恢复。
3. 菜单 `Tools/EchoStation/Stage 1/R02/Build Validation Player`；检查构建JSON，工程Frame Timing Stats恢复false。
4. 已获显示窗口授权后执行 `Tools/Rendering/RunR02Player.ps1 -EvidenceSubdirectory PlayerFinal -Visible`。程序只包含独立图卡场景；45秒超时助手只终止自身启动的测试进程。默认不加Visible时隐藏运行，可做图像功能检查，但本机隐藏GPU计时无效。
5. 执行 `python Tools/Rendering/analyze_r02.py`，再检查Player退出JSON、图像、原始样本、有效/无效数量与指标汇总。
6. 构建后再运行一次上述Test Runner检查管线重建与配置保护；本报告最终证据正是这次21/21。

计时含义参考 [Unity FrameTiming API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/FrameTiming.html)；具体URP请求流程与颗粒随机种子已按本地URP17.6源码核验，不能用其他版本手册代替当前运行结果。
