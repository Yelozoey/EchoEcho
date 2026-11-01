# S1-R03 静态光照模板与推荐参数 v0.1

日期：2026-10-25（Asia/Shanghai）。用户授权“继续，参数按照你的推荐来”，本模块按下表推荐值实施，不再重复申请参数确认。授权覆盖R03静态烘焙与动态受光夹具，不改变阶段/玩法或提前实施R04实时阴影。

状态：已实现并独立测试通过；A六项、B实际烘焙/图像、C构建后28项回归及Windows独立程序完成，详见[实际报告](../Tests/Stage1/Reports/S1-R03_Lighting_TestReport.md)。目标硬件性能与正式场景美术未验收。

## 模块内拆分与执行顺序

| 子模块 | 交付 | 独立测试与推进条件 |
| --- | --- | --- |
| R03-A 配置与几何 | LightingSettings资产；封闭8×6×4m室内夹具；静态几何UV2；动态代理；Light Probe Group | 参数/UV/Static标记/探针位置/原资产保护测试通过并归档后再烘焙 |
| R03-B 实际烘焙 | 场景Lightmap、方向贴图、LightingData和探针SH | 异步烘焙成功；有效贴图、UV变换、非零探针；关闭灯照明保留、取消Lightmap后照明消失；动态受光图像/插值连续性测试 |
| R03-C 持久化与回归 | Windows独立验证场景、截图、原始数据及报告 | 保存重开/Player验证；R01/R02回归；保护哈希和临时状态恢复；完成前不进入R04 |

## 参数

| 配置 | 推荐值 | 理由/范围 |
| --- | --- | --- |
| Lightmapper | Progressive CPU | 本机Windows可用；不占用渲染GPU烘焙；不改D3D11 |
| Baked GI / Realtime GI / Auto Generate | true / false / false | 显式手动烘焙与可重现记录 |
| Lightmap Resolution | 20 texels/m | 小尺度原型起始密度；正式资源按尺寸/细节单独验收 |
| Max Atlas / Padding | 1024 / 4 texels | 控制原型图集尺寸并留出UV岛间距，不承诺所有场景仅一个图集 |
| Directionality / Compression | CombinedDirectional / HighQuality | 开启压缩、保留表面方向信息；实际内存与纹理数量实测记录 |
| Direct / Indirect / Environment Samples | 64 / 256 / 64 | 原型质量与烘焙时间折中；不是最终高画质预设 |
| Min/Max Bounces | 2 / 2 | 有限室内间接照明，固定验证基线 |
| Filtering | Advanced；Denoiser None；Gaussian Direct=1、Indirect=2 | 不依赖GPU专属降噪器；AO关闭，不烘焙额外接触暗化 |
| Indirect Scale / Albedo Boost | 1 / 1 | 中性输入，不用非物理增强补照明错误 |
| Probe Samples Multiplier | 2 | 原型探针采样预算；实际SH和插值验证 |
| 动态受光 | 传统Light Probe Group + BlendProbes | 当前Pipeline已使用该探针系统，保持已验收共享管线不变 |
| 环境 | Ambient黑色、强度0、无Skybox、Reflection强度0 | 隔离烘焙来源，避免天空/反射掩盖Lightmap/探针缺失；仅测试室 |
| 光源 | 两盏Baked Point，强度3、Range=8；冷蓝与少量暖橙 | 仅烘焙，测试中可完全关闭；实时光/频闪/手电属于R04及后续 |
| 材质/几何 | 原生URP Lit中性灰、低Smoothness；程序盒体+UV2 | 技术夹具不引入美术资源或手绘纹理，不代替正式PBR美术 |
| 动态受光测量材质 | 独立URP Lit，Environment Reflections关闭 | 排除默认反射Cube，只测漫反射探针贡献；静态房间材质保留，不据此改变正式场景PBR反射方案 |
| 后处理 | 复用R02；定量A/B临时关闭随机颗粒与色差，保留ACES | 消除随机噪声干扰；展示图另存获批三效果版本；不修改共享Profile |
| 定量对照的SSAO | 仅运行期暂时停用，结束恢复原active状态 | 去除继承SSAO的噪声；不变更其参数或共享资产，最终展示图恢复原Feature |

测试室墙厚0.25m；探针分层覆盖可走体积并排除实体内部；动态胶囊不ContributeGI、不占用Lightmap。场景设置与资产全部项目专用。烘焙期间只加载本模块场景；先检查所有打开场景无未保存修改，保存并恢复原场景布局，禁止保存/丢弃用户未保存内容。

API以当前Unity6000.6.7f1本地XML及URP17.6源码为准。概念参考：[Unity Progressive Lightmapper](https://docs.unity3d.com/6000.0/Documentation/Manual/progressive-lightmapper.html)、[Lightmap Resolution](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/LightingSettings-lightmapResolution.html)。上述数值是本项目原型推荐，不是官方统一性能参数。
