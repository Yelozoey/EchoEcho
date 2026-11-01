# VECTOR CITY / 城市屋顶跑酷美术场景

2026-10-27，Unity **6000.6.7f1**、URP **17.6.0**。本轮按用户明确委托建立独立场景；EchoStation S1–S3的玩法、AI、原场景和渲染资产保留。

## 打开与查看

打开 `Assets/EchoStation/ParkourArt/Scenes/ES_VectorCity_Parkour.unity`。

默认已经放入第一人称Player：在Unity按Play，点一下Game窗口即可操作。WASD移动、鼠标转向、Shift加速、Space跳跃、R返回起跑台；Esc释放鼠标、左击重新捕获。掉落至屋顶下方自动返回起点。出生在白色起跑台，眼高1.6m、FOV70，只有Player相机启用。

Unity菜单 `Tools > EchoStation > Parkour Art > Views` 可切换First Person Player与Hero Overview、Runner Departure、Reflective Atrium展示视点；导出菜单写入 `Docs/Tests/ParkourArt/Final/`。Scene View也可自由移动查看，全部实体有基础碰撞。已提供基础走/跑/跳查看，完整蹬墙、攀爬和翻越系统不在本次范围。

所有实体通过 `GameObject.CreatePrimitive(PrimitiveType.Cube)` 在Unity中创建，没有导入外部模型。箭头、区域数字也由薄板组成。184个构件，五个主屋顶高度12/13/15/18/20m，外围10座塔楼；整体含背景约120×120m。路线从白色起跑台出发，跨红色加速台，向东进入青蓝庭院，再向北上高台、向西转终点。

## 实际设置清单

| 项目 | 最终值/资产 |
| --- | --- |
| Pipeline | `Settings/VC_ForwardHDR.asset`；普通URP Forward，内部HDR，SDR输出，D3D11 |
| Renderer | `Settings/VC_ForwardRenderer.asset`；RenderingMode=0，无Renderer Feature，SSAO关闭 |
| 分辨率/AA | 交付截图1920×1080、RenderScale1、管线MSAA4；相机FXAA辅助边缘，无TAA抖动 |
| 阴影 | 8192主阴影图、4级CSM、110m，分割.12/.32/.60，边界.12；深度Bias.15/NormalBias.4；High PCF，无时间噪声 |
| 日光 | Directional，旋转(48,-32,0)、强度1.4，色(1,.965,.92)，阴影强度.72 |
| 环境 | 明亮Trilight，干净渐变天空，无雾、无环境遮蔽贴图 |
| 反射探针 | 五区域512px HDR Cube，真正离线烘焙EXR；Custom模式持久化绑定离线结果，boxProjection+3m blend |
| 平面反射 | 庭院地面+玻璃墙，分别2048宽HDR RT、可见性门禁、斜裁剪、反射面自排除、递归守卫、粗糙度Mip |
| Volume | `Settings/VC_ColorOnly.asset`，仅ColorAdjustments/WhiteBalance |
| 颜色参数 | 曝光-.15EV、对比7、饱和-2、色温2、色调-1 |
| 已禁用 | SSAO、FilmGrain、ChromaticAberration、Vignette、DOF、Bloom、镜头瑕疵、颗粒噪声；无轮廓描边/光照分段 |
| 显示HDR | 项目原关闭状态保留，不修改Windows/显示器HDR |

管线由根节点 `VectorCityPipelineScope` 限定在该场景使用；关闭场景时恢复原S2的默认/PC引用。需要保存全局资产时，应先切回原场景/VC_TestHost，再SaveAssets；临时绑定的内存还原与磁盘SHA分别验证，见F173。原PC/Gameplay/S2管线不修改。

## 材质与Shader源码

源码 `Assets/EchoStation/ParkourArt/Shaders/VectorCityPBR.shader`。ForwardLit调用URP的 `UniversalFragmentPBR`，保留GGX高光、阴影、SH环境填充与反射探针；只有 `_BaseColor`、常量 `_Roughness`、`_Metallic`、`_PlanarStrength`。没有Albedo/Normal/Detail/AO/roughness贴图。ShadowCaster与DepthOnly共用一致材质常量布局。

`VectorCityPlanarReflection.cs`用镜像相机实际渲染，替换间接镜面辐射项，再乘URP的Fresnel/环境BRDF；不把镜像作为Emission直接叠加。beginContextRendering预渲染、beginCameraRendering绑定缓存，兼容本版本后处理相机栈。地面平面点用Cube顶面，墙面用左表面；D3D11纹理方向已核对。

| 材质 | 纯色RGB（编辑值） | 粗糙度 | 金属度 | 平面反射混合 |
| --- | --- | --- | --- | --- |
| White_Porcelain | .86/.89/.90 | .48 | 0 | 0 |
| White_Chalk | .72/.78/.81 | .48 | 0 | 0 |
| Runner_Red | .80/.018/.032 | .31 | 0 | 0 |
| Atrium_Cyan | .015/.51/.62 | .33 | 0 | 0 |
| Structural_Grey | .29/.36/.40 | .48 | 0 | 0 |
| Glass_Cyan | .08/.32/.41 | .10 | .24 | .85 |

Glass_Cyan是干净的着色反射建筑面，采用不透明渲染，不包含折射/透射模拟。其余屋顶为粗糙纯色实体，保留探针IBL。真实连续光照和硬边几何造成的明暗面，不是三角面色块或赛璐璐阴影。

## 场景结构

`VECTOR CITY — Rooftop Run` 为持久化根节点，下含：

1. `01 — WHITE / Departure`：起跑台、门架、红色引导带、护栏。
2. `02 — RED / Momentum`：红色屋顶、上升坡道、发射跳台。
3. `03 — CYAN / Atrium`：青蓝楼体、抛光庭院地面、玻璃反射墙、柱廊与坡道。
4. `04 — WHITE / Ascent`：高处落点、横向坡道、青蓝大块墙面。
5. `05 — WHITE / Arrival`：终点屋顶、引导带、终点门架。
6. `06 — Skyline / Quiet architecture`：外围几何塔楼与城市底板。
7. `07 — Route / Jump landing markers`：16个不渲染的路线标记。
8. `08 — Presentation`：主相机与日光。
9. `09_Reflections_BakedRegions`：五个区域探针。
10. `10_ColorOnly_Volume`：颜色后处理。
11. `11_Player_FirstPerson`：CharacterController、复用PlayerInputReader、本场景VectorCityFirstPerson；子节点Camera_FirstPerson为主相机并带唯一AudioListener。

`VC_PlanarCamera_*`和`VC_PlanarHDR_*`为隐藏临时资源，不保存到场景，关闭反射组件释放。使用自己的材质、Shader、Pipeline、Renderer、天空、Volume与EXR，不覆写模板。

## 后续微调指令

可以直接说：

- “把红色改成橙红，其他配色保持。”
- “把第二块红色平台向前拉长4米。”
- “庭院玻璃反射调亮一点。”
- “把天际线降低，让终点更明显。”
- “阴影再软一些，保留现在的明亮感。”
- “切到玩家起跑视角。”

修改颜色会更新共享纯色材质；修改平台会同步屋顶/碰撞、护栏、路线标记与跳口；影响反射的改动会重烘焙对应探针，并重跑受影响测试/截图。不会重新生成覆盖手工编辑的现有场景。

第一人称模块计划见`ParkourArt_PlayerPlan.md`，实际报告在`Docs/Tests/ParkourArt/PK06_Player_Report.md`。原S1 Player脚本/Prefab保持，只有输入与鼠标逻辑复用；独立移动参数为4.5/8m/s、1.6m跳高、18m/s²重力，适应屋顶间距。独立模块报告在 `Docs/Tests/ParkourArt/`，实际失败与修复在 `Docs/Troubleshooting.md`。编辑器图像/测试不等于Windows Player、完整跑酷手感或GTX1060/RTX3060目标硬件60FPS验收。

实现依据为本机URP17.6源码与Unity反射验证；官方接口说明：[Camera.SubmitRenderRequest](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Camera.SubmitRenderRequest.html)、[URP反射探针](https://docs.unity.com/en-us/engine/6000.6/manual/lighting-overview/lighting/reflection-probes)。
