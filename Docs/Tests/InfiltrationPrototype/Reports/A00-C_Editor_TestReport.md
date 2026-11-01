# A00-C：编辑器漫游与交付实际报告

日期：2026-11-01。授权仅第一人称美术样板；本项结束后停下，交本人Editor亲测。AI、完整潜入、通行检查与物品任务未接入；未训练、推理、下载模型或打包。门卫传统行为树＋Echo LLM/行为树是后续方向，尚未实施。

入口：[ES_Infiltration_ArtSample.unity](E:/Projects/Unity/EchoStation/Assets/EchoStation/InfiltrationArt/Scenes/ES_Infiltration_ArtSample.unity)。[本人试玩指南](E:/Projects/Unity/EchoStation/Docs/InfiltrationPrototype/A00_EditorPlayGuide.md)。

## 环境和验收边界

Unity6000.6.7f1，Windows11、D3D11、普通URP Forward、内部HDR/SDR；测试使用ES_Gameplay_ForwardHDR，退出还原原VC管线。开发机i7-12650H、RTX4060 Laptop、约16GB RAM。

目标仍为Windows10/11 x64：最低i5-8400/GTX1060 6GB/16GB、1080p中画质60FPS；推荐i5-12400/RTX3060 6GB/16GB、1080p高画质60FPS。本项未在两套目标硬件运行、未做Profiler性能验收，不能用本机测试时长或画面帧号证明60FPS。

## 独立测试及修复

A00-A先完成资源独立8/8并归档，A00-B再完成实际烘焙、图像修正和独立5/5并归档，才进入C。对应A/B报告保留，早先失败和初版画面没有覆盖。

| 实际批次 | 结果 | 含义 |
| --- | --- | --- |
| Walk_A | 2/3 | 移动/碰撞/跳跃与光标门禁通过；门卫Idle过一轮后停止 |
| Final_A | 15/16 | 新复制FBX仅Idle启用循环后实际动作通过，但跳跃输入未触发 |
| Final_B/C | 各15/16 | 前置真实Grounded=false；仅设minMoveDistance=0仍不足以证明解决 |
| Walk_D | 3/3 | 等待真实帧、显示GameView/60FPS并还原原设置后通过，实际着地Below、真实Space跳起并落回地面 |
| Final_D | 16/16 | 全组合通过，但叠加测试出现双AudioListener警告，未作为最终干净结果 |
| Final_E | **16/16，0跳过，15.588秒** | 新鲜SceneSafety/RenderSafety及编译门禁；临时监听器隔离/还原后所有测试output为空 |

Final_E通过范围：资源8项（15角色实际网格来源、派生URP材质、作者Idle引用、门卫/椅子/门尺度、桌椅门贴图、工具箱与终端不同实物）；场景和烘焙5项（关键模型/4.5m顶/转角通行净空，实际Lightmap和探针，HDR/SDR/ACES，四个1080p实际相机视角，完成烘焙与宿主恢复）；漫游3项（实际InputAction W移动/鼠标观察/墙体阻挡/Space跳起超过0.35m并落地，作者Idle持续运动及尺度，Esc/点击/失焦输入所有权且无AI组件）。

失败原因与界限见F-340–F-342。旧SalvageMotor/PlayerVerticalMotion源码未修改；新样板控制器minMoveDistance设0。测试节奏调整与设置改变未逐项做A/B隔离，因此不把阈值单项说成唯一根因。监听器收据定位为测试运行器未命名场景Main Camera；作用域只临时关闭该Play实例，卸载样板后恢复，非删除场景对象。

## 原生画面和本人验收

NativePreview_A因MCP资源缓存停在旧transition而超时，finally实际退出并回宿主，没有截图，保留失败。B改读原生EditorApplication状态：实际Play帧147、GameView焦点、单AudioListener、HDR=true/SDR=true、无AI，生成**2560×1440**实际Game图。该尺寸来自当时编辑器Game设置；不冒称1080p或发行包。四视点另为1920×1080离屏实际渲染，来源有区别。

![原生Game画面](E:/Projects/Unity/EchoStation/Docs/Tests/InfiltrationPrototype/Evidence/A00/GameView_B.png)

代理已查看原生图和最终相机图：现场物体可见，字牌方向正常、UI有底色，工具箱/终端/桌椅/门框采用实体模型。门卫更简化、Rover与工具箱更写实，是否风格协调与满足用户视觉底线，仍须本人判断。本报告不将自动工程通过或代理看图记为用户美术验收。

## 保护、配置与停止点

原GraphicsSettings/QualitySettings/EditorSettings的native/disk/dirty已逐项恢复并验证全true，原VC默认/PC绑定、PC1、AA4、D3D11保留。regression_host leave已还原原环境回收场景/选中/播放选项，再次RestoredNative_B通过；之后有意只打开新样板供本人测试，留Edit模式，不自动播放，不写玩法存档。

所选11个源数据文件最终SHA/长度一致，见SelectedSourceVerification_Final.json；原14,426文件完整SHA校验**0差异**，见Protection_Final.json，旧场景/模型/构建/源码/配置字节保留。资源总量约19.76/20GiB，精确交付测量见Budget_Handoff.json；未使用22GiB打包例外。

EditorHandoff_A确认独立样板干净、未播放、VC默认/PC绑定；HandoffNative_A再次确认三项设置native/disk/dirty全true、PC1/AA4/D3D11。Console_Final实际0条错误。交付场景本来就是新自有场景，有意打开不等于覆盖旧版；原环境回收场景的布局恢复证据另有留存。没有用户美术反馈时仅记录“工程完成、本人接受待测”。

证据根目录：Docs/Tests/InfiltrationPrototype/Evidence/A00。关键收据：Walk_A/D、Final_A–E及每轮门禁；JumpPrecondition、GuardPlayback、RunnerListeners；NativePreview_A/B各阶段与GameView_B；RestoreNative_A、RestoredNative_A/B、ArtSample-WorkspaceRestored、EditorHandoff_A/HandoffNative_A；SelectedSourceVerification_Final、Protection_Final、Console_Final、Budget_Handoff。

当前停止在A00本人美术试玩；不恢复历史I00、不开始完整潜入或Echo模型实验。没有用户反馈前，不把样板通过当成整个新游戏路线已获验收。

## 本人反馈补记（2026-11-01）

用户实际答复原话：“需要调整美术（请说明位置）”。记录为视觉需修改，尚未提供位置、现象或期望；已询问1–3处具体问题，不能自行解释成某一种风格/亮度/比例问题。原话及边界见Editor_UserFeedback_A.json。此反馈不推翻工程16/16，也不等于美术接受；本轮仅归档，未操作Unity、改资产、追加测试或进入玩法/LLM。

后续已收到具体四点反馈，原话见Editor_UserFeedback_B.json：地图至少9倍、多路线、经典潜行设计、多定点/巡逻守卫与更好光影。超出旧样板范围的具体规格写入Feedback02_LevelAndLightingProposal.md并询问，不再等待笼统“指出位置”。A00仍视觉需改；本轮只读资源/反射结果不是新渲染效果通过证据。
