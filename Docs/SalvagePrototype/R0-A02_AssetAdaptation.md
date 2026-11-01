# R0-A02：两件资源的隔离 Unity 适配

日期：2026-10-28；前置 R0-A01 已通过。用户“按计划继续”推进本模块。当前不实现拾取、载重、电量、任务或新 AI，不恢复模型训练。

## 拆分与顺序

R0-A02-I 导入与材质 → 独立测试/报告 → R0-A02-P 预制体与碰撞 → 独立测试/报告 → R0-A02-V 展示场景与实际图像 → 独立测试/报告。三项全通过才计总计划 R0-A02 通过；35个顶层模块不因内部细分重复计数。

## 选定资源与处理

- 地板：Quaternius Standard 中 `FBX/Platforms/Platform_Simple.fbx`。同名 glTF 的 POSITION 范围为 X/Z -2至2、Y=0，给出4m×4m平面尺度依据；实际FBX导入须再测，不能默认乘0.01修正。
- 地板材质：同名glTF明确引用 MI_Trim_03，对应原包 T_Trim_03_BaseColor/Normal/ORM 三张贴图。保留原始颜色，生成URP所需遮罩。
- 回收物：Poly Haven Metal Toolbox，作者Mateusz Sadek，1K FBX与原依赖/ARM。原始文件SHA/MD5依据R0-A01清单；模型实际尺寸需在Unity记录，不假装已核验动画。
- 新文件仅进入 `Assets/EchoStation/Salvage/`，测试进入 `Assets/EchoStation/Tests/SalvageEditor/`；保留原Docs来源文件与所有旧资产。
- FBX使用文件单位、globalScale=1，不导入相机/灯/动画/材质，不自动加MeshCollider。静态地板生成次级UV；工具箱自动UV2会丢失三件小部件，已通过真实开关对照定位（F194），动态可搬运道具改用探针、不生成UV2；不宣称该道具可直接静态烘焙。运行期模型/贴图不保留CPU可读副本。
- 只创建两份URP Lit材质。BaseColor使用sRGB，Normal按NormalMap导入，ORM/ARM及派生Mask使用线性数据；最大1024、MipMap、CompressedHQ，延续已下载1K与原PC低开销基准。
- ORM/ARM均按R=AO、G=Roughness、B=Metallic解释；派生Mask为R=Metallic、G=AO、B=0、A=1-Roughness，同时用于URP metallic与occlusion输入。_Smoothness/_BumpScale/_OcclusionStrength=1，保留源响应，后续美术风格调优另行测试。
- 预制体保留源模型变换并加identity包装根；地板使用静态薄盒碰撞壳，顶部与原Y=0表面对齐，厚0.1m；工具箱使用按实际渲染包围盒生成的BoxCollider。本模块不附加Rigidbody或决定游戏质量/载重。

## 测试边界

I：核对复制SHA、真实ModelImporter与导入Mesh、4m地板边界、UV/法线/切线、贴图色域与Mask采样、URP材质引用/关键词/Shader错误。

P：独立实例化预制体，核对根变换/材质/碰撞尺寸，真实Collider射线命中与边界外不命中，重复地板网格拼接；不冒充玩家手持/物理搬运测试。

V：独立拥有场景、1920×1080实际URP渲染，地板全景与工具箱近景非黑且无粉材质；正常/去掉Normal或Mask的真实图像对照，恢复原材料后再留截图。次级UV存在不等于已烘焙，LOD和目标硬件60FPS未在本模块验收。

继续用 module_tests.py 的SceneSafety/RenderSafety/Console门禁；保存原跑酷场景布局、渲染绑定、画质AA和播放选项，测试使用空宿主。最终还原原场景与设置，并逐文件SHA比较；未知变更不自动覆盖。

## 已见环境问题

本地MCP服务起初未启动，现用已有10.3.0离线缓存和回环HTTP恢复，Unity会话通过编辑器正常Connect建立。初次CodeDom只读诊断外部空失败，但历史记录证明实际成功（约85.7秒）；后续快照写入文件后成功返回。初始Console为既有IAP/UnityConnect/MCP联网启动失败，保留原始错误后再清理明确已解决的历史错误，不能隐藏新编译/模型/渲染错误。见F-189至F-191及本模块证据。
