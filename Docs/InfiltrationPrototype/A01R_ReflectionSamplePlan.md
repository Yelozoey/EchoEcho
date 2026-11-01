# A01-R 金属反射小样：已确认执行计划

最终（2026-11-01）：R01_D3/3→R02_A3/3→R03_B2/2分别归档，Final_A8/8、0跳过/输出为空；实际512探针/1024宽动态地面反射与三材质对照、1/2/3/P/移动按键完成。原生Game图与有效GPU237/240采样留存；收尾发现两设置写回，经原始SHA唯一匹配恢复后Protection_Final_B15,053旧文件零差异，三设置native/disk/dirty与原布局恢复，再有意打开小样clean/Edit、Console0，约19.96/20GiB。见[最终实际报告](E:/Projects/Unity/EchoStation/Docs/Tests/InfiltrationPrototype/Reports/A01-R03_Editor_TestReport.md)、[Editor指南](E:/Projects/Unity/EchoStation/Docs/InfiltrationPrototype/A01R_EditorPlayGuide.md)。工程完成后停止等本人看反射效果，不自动铺回地图，非硬件光追/SSR。以下待确认/未实施为历史。

用户已选择“金属对照＋实时平面倒影（推荐）”，授权R01→R02→R03逐项测试并交Editor体验后停止。独立ReflectionSample目录，普通样板布置/参数依本方案实施；下方待确认文字为历史。

2026-11-01。用户对A01-D反馈：大多金属物体缺少明显镜面/光照反射，希望类似光线追踪，先做简单小样。此请求授权准备独立反射样板；尚需确认展示技术范围，不能把“类似光追”自动解释为更换引擎/图形API/管线。本轮目前仅只读核对和记录，未创建样板或更改Unity/旧资产。

## 实际核对

当前精修场景clean/Edit。8个实际Custom反射探针为128分辨率、强度.7、Box Projection、指向A01-D自己的烘焙Cubemap；反射并非全局关闭。抽查墙地材质Smoothness .20–.40、使用涂装遮罩，多数Lit的Environment Reflections与Specular Highlights为1；玻璃例外是之前有意关闭。标量Metallic与贴图最终组合不同，不能仅凭标量宣称每个像素金属度。上述使反射偏模糊/弱，但尚未做受控因果A/B，不认定只有单一原因。

本机当前API=Direct3D11，SystemInfo.supportsRayTracing=false。URP17.6本地SSR源码被URP_SCREEN_SPACE_REFLECTION保护，实际加载的ScreenSpaceReflection类型仍为0。官方SSR说明属于预览，有屏幕外信息缺失与探针回退；不把搜到源码当已启用，不能承诺全表面/全视角硬件光追。

项目已有可复用的VectorCityPlanarReflection及实际像素/资源释放测试。其采用反射相机、斜切近裁剪、临时反转剔除、HDR目标和finally还原；适合独立平面样板，但不能据旧测试宣称新小样已通过，也不能把平面倒影推广为任意曲面反射。复用或隔离适配仅在确认后做，旧跑酷源码不改。

## 推荐小样

独立约6×6m房间，复用已有科幻模块、灯和机器人。三个同形测试球/金属板显示粗糙金属、抛光金属与原涂装材质的差异；这些标准测试几何有明确标签，不充当正式场景美术。固定光照/曝光并列对照，避免把单纯抬亮误当反射改善。

推荐增加**一块实时平面反射地面**：机器人演示移动、彩色物体/灯条与视角变化的倒影；可以一键关闭动态反射，对照探针。小样明确标注“平面反射＋PBR/探针，非硬件光追”，只证明该平面。曲面金属使用探针和直接高光，不用平面投影伪造球面镜像。反射初值1024宽HDR/单平面/仅小样主相机，现场测开销；按结果调优，不要求目标机未实测即宣称60FPS。

另一可选范围：先仅做PBR金属与更清晰静态探针对照，不做动态平面倒影。前者更贴近用户要直观看到的清晰实时倒影，后者范围更小但不会反映移动机器人。

## 独立实施顺序与实际门禁

| 子模块 | 内容 | 必须实际测试 |
| --- | --- | --- |
| R01 材质与探针对照 | 自有场景/管线/材质/512探针；原涂装与金属光泽对照 | 实际Cubemap保存与来源、材质/遮罩、同相机反射/高光开关像素差、非黑图和人工看图；独立报告后进入R02 |
| R02 实时平面反射（推荐范围） | 单平面、演示机器人移动、反射开关 | 倒影实际更新而非静态贴图、镜面变换/裁剪/视角、非平面不冒用、禁用后RT/相机/全局剔除状态还原、开关图像与有效GPU采样；独立报告 |
| R03 Editor交付 | 实际输入与组合回归、原配置/布局保护，打开小样 | module_tests.py新鲜SceneSafety/RenderSafety、Console、旧资产/设置还原、预算，交本人Play检查并停止 |

Unity6000.6.7f1/普通Forward/内部HDR转SDR/D3D11保持，只Editor、不打包。当前正式潜入场景与旧版/模型/构建/证据保留，不把小样自动铺回大地图；不升级Unity/URP、不修改包缓存或启用实验SSR宏，不新增云/训练/下载。总≤20GiB；上次19.937636GiB是历史测量，实施前重新测，新增计划≤24MiB，若预计超限先确认，不删历史腾空间。未知未保存场景/用户Play保持，原设置native/disk/dirty精确恢复。

## 来源与待确认

[Unity Lit材质参考](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/lit-shader.html)：Smoothness、Specular Highlights、Environment Reflections各自职责；[Unity官方SSR预览](https://discussions.unity.com/t/preview-of-screen-space-reflections-for-urp/1721494/1)：只使用屏幕信息，缺失时回退。本地实际加载结果优先于对未来版本的推测。

待确认：采用推荐的“金属材质对照＋一块实时平面倒影”，还是仅PBR/静态探针对照。真实硬件光追涉及当前硬约束之外的另一个技术范围，未授权更改。项目AGENTS.md要求未明确的方案选择先询问，故此处只问一次展示范围，已明确的Editor测试/旧版保护不重复索取权限。
