# A01 扩图与光影：最终Editor交付记录

2026-11-01。用户确认反馈02 Q1–Q3，仅A01；逐项A→B→C1→C2→C3独立测试、报告后整合。不进入A02传统守卫行为、潜入闭环或Echo LLM，不打包、下载、训练或改全机网络。旧版和既有No-Go保持。

## 实际交付

独立场景 `Assets/EchoStation/InfiltrationLevel/Scenes/ES_Infiltration_Routes.unity`。36×32m/六区，4.5m常规层高与6m中庭；真实模块/桌椅/终端/货箱/检修设备、观察窗、4个机器人岗位（2固定/2将来巡逻）及静态Echo。三条实际可通路线、两个回环，Tab可隐藏5条导向/巡逻预览线。四守卫仅源Idle展示，无巡逻/感知/权限检查/任务，现场HUD和标牌均说明。

最终地面并集1152m²；扣除实际墙/设备碰撞投影并集后的净地面保守下界1021.735m²，为旧96m²的10.643倍，≥864m²。门框空跨也按包围矩形扣除，会低估地面；这不是角色中心可放置位置的精确测量。最终地面NavMesh面积另为730.673m²，包含烘焙代理半径侵蚀/导航规则，未拿它替代净地面。所有六功能区连通，三路与双回环分别NavMesh完整，另由真实CharacterController按拐点Move遍历，无中途穿障碍传送。

普通URP Forward、内部HDR/SDR、D3D11，独立IL管线/Renderer/Volume。实际2张方向性Lightmap、216光照探针、8个区域离线反射；36烘焙灯/1 Mixed主光/2实时灯；SSAO/ACES、局部半分辨率16步主光阴影散射雾。SSR/SSS未实现，非UE5/Lumen或完整多灯物理体积系统。灯壳仍为源模型外观，最终审美由用户判断。

## 测试与证据

| 范围 | 实际结果 | 报告/收据 |
| --- | --- | --- |
| A空间/连通 | Layout_A8/9后，修夹具时长；Layout_B9/9 | A01-A_Layout_TestReport.md |
| B源模型/岗位/遮挡/图像 | Art_A6/6但实图有缺陷；修文字/窗/上墙/双面共面条纹，Art_B15/15含A回归、Art_C6/6 | A01-B_Art_TestReport.md / F-346 |
| C1正式光照 | 两次真实烘焙，Lighting_A6/6、修灯/窗后Lighting_C6/6 | A01-C1_Lighting_TestReport.md |
| C2局部薄雾 | Fog_A3/3，实际960×540/16步/主阴影依赖与散射衰减 | A01-C2_LocalFog_TestReport.md |
| C3输入/采集 | Delivery_A1/2夹具Clip名错误，Delivery_B2/2，预分配采样后Delivery_C2/2 | A01-C3_InputAndProfiler_TestReport.md / F-350–351 |
| 最终组合 | **Final_A25/25，0跳过，30.981秒；全部test output为空** | Evidence/A01/Final_A.json与新鲜SceneSafety/RenderSafety/Console门禁 |

最终4视角1672b9c5为真实PlayMode相机1920×1080离屏图，无临时检查补光。代理复查入口/观察窗，先前顶板条纹和镜像文字已修。NativePreview_A另为真实Game画面2560×1440、有HUD，非1080p性能样本；IL管线/HDR/SDR/1监听器/GameView焦点读回，截图GameView_A.png，结束退出自有Play并还原背景运行/空宿主。本人走图尚未执行，不记为接受。

逐效果像素开关对照在C1/C2报告：AO、反射、总烘焙贡献非零；薄雾带阴影散射少于无阴影。没有把总烘焙贡献当纯间接/逐区溢色的定量证明。源动画四实例首周期后实际运动、Space跳跃/落地、真实W/鼠标/Esc/点击/失焦/Tab均有用例。

## 性能的实际范围

独立采集与最终组合均为i7-12650H/RTX4060 Laptop/约16GB、D3D11、相机1920×1080，包含Editor/Runner/GUI开销。最终EditorProfiler_dac410e5为3视角×4效果组合×60=720帧，其中712有效GPU时序（非零并去重），8无效保留；默认全开GPU平均5.72/5.06/5.50ms，p95 6.11/5.57/5.87ms；Main p95 6.36/6.60/6.59ms。

独立批次仍有约19ms尖峰，旧逐帧JSON批次约110ms尖峰留存；预分配样本减少测试分配，但未隔离所有Editor干扰或证明无卡顿。约3千级Draw、Editor全进程内存/GC也留存，不能说产品零GC/全场合批完成。短顺序采样的微小差值不能当某特效固定耗时。

**目标机未验收**：Windows10/11x64，最低i5-8400/GTX1060 6GB/16GB、1080p中画质稳定60FPS；推荐i5-12400/RTX3060 6GB/16GB、1080p高画质稳定60FPS。没有这两套硬件/发行版数据，不将本机Editor当达标证明。

## 还原与保护

NativeRestore_Final_A、NativeVerify_BeforeLeave、NativeVerify_AfterLeave、NativeVerify_Handoff_A均确认Graphics/Quality/EditorSettings native JSON、磁盘字节、dirty逐项精确；PC1/AA4/原VC默认及PC绑定恢复。Level-WorkspaceRestored先恢复原A00场景/选择/播放选项，再按本轮交付有意打开新A01；Handoff_Open_A新场景clean、Edit未播放，Console_Final_A与收尾Console_Handoff_Final均为0错误。没有保存/丢弃未知用户场景。

收尾Budget_Final_B实际19.852849/20GiB（最后报告补记仅文本小量）；未删历史证据/模型/构建。Protection_Final实际核对14,591个旧Assets/Packages/ProjectSettings/Builds/Tools清单文件，changed=[]、passed=true，零差异。新A01实现另以ImplementationManifest_A记录163文件SHA；该静态快照不替代上述真实Unity测试。Handoff_Inventory_A另确认417源模型实例、0丢失脚本、四唯一岗位、SSAO/局部雾均启用、2张Lightmap，场景clean/Edit。最终工程保护门禁完成；本人视觉接受待测。

## 本人测试与停止边界

[走图指南](E:/Projects/Unity/EchoStation/Docs/InfiltrationPrototype/A01_EditorPlayGuide.md)：Play→点击Game，WASD/鼠标/空格/Esc/Tab。建议正面蓝线到核心、左行政换线、后侧检修观察窗绕到右仓区返回，再隐藏线看路线辨识。重点反馈空间规模/层高、三路回环、遮挡观察点与光影。只交本人测试；视觉接受、真实守卫、潜入闭环、Echo伙伴均未记完成。
