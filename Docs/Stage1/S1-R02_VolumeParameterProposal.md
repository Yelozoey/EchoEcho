# S1-R02：Volume 参数提案 v0.1

提案日期：2026-10-24（Asia/Shanghai）；用户确认日期：2026-10-25。状态：**v0.1参数与资产组织已获确认并实现，独立功能测试通过**。实际图像、本机CPU/GPU诊断和限制见 [测试报告](../Tests/Stage1/Reports/S1-R02_Volume_TestReport.md)；不代表正式场景视觉或目标硬件性能已验收。

本文件保留获批方案；实际实现和测试结果另行归档。数值是本项目的视觉起始值，不是 Unity 官方推荐值，也不是目标硬件性能达标结论。

## 1. 建议参数

| 类别 | 参数 | 建议值 | 意图 |
| --- | --- | --- | --- |
| Tonemapping | Mode | ACES | 落实用户固定要求，作为后续材质/照明校准的统一基线 |
| Film Grain | Type | Thin1，URP 内置纹理 | 使用细颗粒，不增加自绘/外部噪声纹理依赖 |
| Film Grain | Intensity | 0.10（范围 0–1） | 轻度颗粒，优先保证线索和细节可辨认 |
| Film Grain | Response | 0.80（范围 0–1） | 减弱亮区颗粒；实际效果通过对比图确认 |
| Chromatic Aberration | Intensity | 0.02（范围 0–1） | 极轻色差；通过高对比边缘图卡检查可读性 |
| Pipeline | Color Grading Mode | High Dynamic Range | 采用 HDR 调色流程；与已经确认的内部 HDR/SDR 显示输出配合 |
| Pipeline | Color Grading LUT Size | 32 | 保持现有 LUT 尺寸，本轮不额外提高精度 |
| Global Volume | Is Global / Weight / Priority | true / 1.0 / 0 | 作为可复用的全局基础效果；未来局部区块效果另行确认 |
| 基础色彩 | Post Exposure / Contrast / Saturation | 0 EV / 0 / 0 | 基础模板保持中性，便于分别验收光照和后处理 |
| 基础色彩 | Color Filter / White Balance | 白色 / Temperature 0、Tint 0 | 冷蓝灰及橙红危险光在光照与场景模块定稿 |
| 相机 | Render Post Processing | 在新的 R02 测试相机开启 | 实际验证 Volume 栈，而非只生成资产 |
| 输出 | 内部 HDR / 显示器 HDR | 开启 / 关闭 | 延续已确认的 D3D11、Windows x64、1080p、SDR 输出基线 |

所有本提案要生效的 Volume 参数均显式设置 Override 状态，不依靠模板的偶然继承值。仅对自定义纹理有意义的 Film Grain Texture 不设置；HDR 显示器专用 nits/paper-white 参数不启用。

Bloom、Vignette、Motion Blur、Depth of Field、Lens Distortion、Panini Projection 在 R02 基础模板中禁用；其他色彩修饰使用中性值，并验证没有旧模板效果混入。交互对焦景深属于 S1-P06；局部体积雾/光属于对应场景模块，本次不提前实现。

## 2. 建议资产与变更范围

1. 新建项目专用中性默认 Profile 与探索效果 Profile。中性默认 Profile 显式清除旧模板效果；探索 Profile 只启用 ACES、Film Grain、Chromatic Aberration 三项视觉效果。
2. 新建 Global Volume 预制体和独立 R02 测试场景，保留原 SampleScene 与 R01 测试场景内容。
3. 新建专用 `PostProcessing` Layer，测试相机的 Volume Mask 只包含该层；避免其他已加载场景中的 Global Volume 干扰独立测试。
4. 共享专用 Pipeline 的必要变化为：`m_ColorGradingMode` 从 LDR 改为 HDR、`m_VolumeProfile` 指向项目专用中性默认 Profile。LUT 维持 32；Forward、内部 HDR、SDR 输出和 D3D11 契约继续回归检查。
5. 对 R01 的继承参数检查追加本次经用户批准的两项变更及基线版本记录，不关闭其他参数漂移、模板保护或图像断言。

上述资产组织和 Pipeline 变更同样属于本提案，须随参数一起确认后才实施。未列出的光照、阴影、SSAO、分辨率缩放、抗锯齿参数不纳入本模块调优。

## 3. 确认后执行的独立测试

| 用例 | 步骤 | 预期 |
| --- | --- | --- |
| R02-T01 参数与生效栈 | 检查 Profile、Override、相机开关、Volume Layer/Mask 和运行中的 Volume Stack | 有效参数与用户批准值一致，不只检查磁盘资产 |
| R02-T02 ACES | 使用中性灰阶、HDR 亮度和彩色图卡，分别捕获 ACES 关闭/开启结果 | ACES 实际生效，灰阶/高亮变化可复现；不启用显示器 HDR |
| R02-T03 颗粒 | 固定相机与曝光，分别采集强度 0 与 0.10 的多帧图像 | 颗粒实际生效；亮区 Response 行为可观察；不能只比较两个随机噪声帧就下结论 |
| R02-T04 色差 | 使用中心/边缘高对比图卡，对比强度 0 与 0.02 | 色差存在且符合批准的轻微程度，检查细线和字形可辨认性 |
| R02-T05 中性与隔离 | 移除效果 Volume 或将权重设为 0；并加载含旧 Volume 的其他场景 | 回到中性默认值；独立相机不受旧 Volume 干扰，无意外 Bloom/暗角/运动模糊 |
| R02-T06 持久性与回归 | 保存重开、重复运行，执行 R01 对应回归和 Windows x64 验证 | 参数、引用、内部 HDR/SDR/D3D11 契约保持，原模板未改 |
| R02-T07 开销记录 | 在相同分辨率、相机、图卡、构建和设备上对比关闭/开启后处理 | 记录 CPU/GPU 增量与方法，不凭低强度推断低开销；两档目标硬件未实测则明确标记 |

较低的颗粒/色差强度主要控制视觉表现，不代表 GPU 成本同比降低。本提案不提前承诺毫秒预算或 60 FPS；用户既定最低/推荐硬件和 1080p 基准继续有效，稳定帧率统计口径仍按总文档等待确认。

## 4. 确认项

请确认整套 v0.1，或指出要修改的参数/资产组织。确认仅授权 S1-R02 的实现与独立测试；不代表后续光照、小屋、Echo、玩家模块的未决需求已经确认。实现后提交实际对比图和测试报告，再根据用户反馈调整视觉强度。

## 5. 核对来源

具体字段、范围及默认值以当前已安装 URP 17.6.0 源码为准：FilmGrain.cs、ChromaticAberration.cs、Tonemapping.cs、ColorGradingLutPass.cs。Unity 6.0 手册仅用于概念参考，不能替代 Unity 6000.6.7f1 的实际运行验证。

- [Film Grain：参数与 Response 的含义](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/Post-Processing-Film-Grain.html)
- [Chromatic Aberration：强度范围](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/post-processing-chromatic-aberration.html)
- [Tonemapping：ACES](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/post-processing-tonemapping.html)
