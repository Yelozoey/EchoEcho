# R03房间流送细分工作笔记（尚未实现）

R03c再拆：c-1真实Main有效任务目标预载/取消、16次快速往返、手持物独立槽保存/读回及原生稳定资源计数；c-2隔离坏本地Bundle失败释放/重试恢复。各项单独测试/报告后才P01。c-1用有效已发现cargo_f的存档夹具验证任务租约，不把它当实际扫描验收；cargo_a仍通过实际视线拾取，夹具不调用UnusedAssets帮助产品过关。

Presentation_D已通过后追加所有权收尾子项：IsReady必须同时检查当前World票据，刷新World的同帧立即失效；pump在Unload完成后合并请求Resources.UnloadUnusedAssets（主线程启动、异步完成），拥有者关闭后仍存活直到清理完成，避免测试手动清理冒充产品实现。正常过渡稳定时立即触发，连续过渡最多推迟2秒并限频1秒；资源持有句柄仍保护共享引用。针对真实Bundle无夹具手动UnusedAssets验证资源销毁；该子项测试/报告后才整合Main。

2026-10-30 b4实施补充：RoomLightingRegistry/RoomPresenter已写；固定原有图之后6槽，sceneLoaded/unloaded重申，探针同样重申；MergedRoomMesh运行时externalManaged抑制Start覆盖。Presentation_A/B正确拦截Shader被剔除的黑图，见F258；BuildFlags_A2/2通过后正在真实重建，不代表光照验收。

下一b4距离驱动的加载门禁采用独立的PlayerInputReader.enabled租约，不写共享UiBlocked布尔。记录之前enabled/pointer，当前房间无视觉时禁止玩家游戏输入，恢复时先还原enabled，只有原先捕获或首次正常开始、具有焦点且UiBlocked=false才重新捕获。R/T面板、任务板/交付自己的UiBlocked归属不被加载完成清除；若原输入已禁用，不擅自启用。碰撞/Nav持续常驻，Echo活动目标加入当前World/Generation租约。初始加载/读档瞬移中显示加载状态，错误保留并限频重试，不能吞错误视作ready。

多场景PVS须在Main＋三区全载、临时启用视觉状态后重烘；完成保存新数据后恢复根initial inactive；三个Addressable Room序列化改变后重新构建。PVS字段断言需升级为实际新路径而非原先null，保持Main不依赖房间Mesh/Lightmap/原LightingData。旧reference/PVS不修改；真实Bundle中Off/On图差与Draw计数独立验收。

2026-10-30，前置Bundle_A2/2通过。这里只记录下一步选择，不能当作运行证据。

保持ES_Salvage_Next.unity作为R4单场景参考/已有测试基线；新正式入口克隆为ES_Salvage_Streamed.unity，使用相同新SalvageNext槽（相对R2仍独立）。为避免F077多Surface共享数据清空，复制自有Streamed_Nav资产后绑定，不动参考场景/Nav。正式Windows仅构建Streamed入口。

建议继续细分：b-1a独立探针资产持久化及SH对照；b-1b三区视觉场景/引用拆分及主场景常驻物理结构；b-2纯调度距离/滞回/任务代次测试；b-3实际Addressables Scene句柄生命周期；b-4光图/反射/探针及重建多场景PVS集成；c压力/保存恢复/退出/资源释放。逐步门禁，不一次写完。

b-1a先实测Instantiate(LightmapSettings.lightProbes)＋CreateAsset能否保留180位置/系数/四面体，重开单独资产并设置为全局后用插值对照。不能只因API可写就当可独立保存。如果不可用，先保留证据再采用自有GI数据仅保留探针的可验证方案；不要把原LightingData同时引用全部Atlas留在Main又声称逐区释放。

b-1b三区RoomVisual移入自有Addressable场景；主保留碰撞/Nav/逻辑货物/玩家/Echo/实时灯/Volume/局部雾。Stream Room无Camera/Light/Collider/Nav，仅视觉/反射/元数据。清除InstancedRoomDecor.view与DistanceShadowCaster的跨Scene相机引用，运行时由主驱动绑定。原2Atlas跨区共享，给每区派生自己的色/方向图（512px轻量，明确复制成本），保留原UV、每个MergedRoomMesh逻辑Atlas索引；反射原本每区唯一。主探针独立后清除原LightingData引用，Inspector/API/依赖图与实际运行均验证。

视觉根初始inactive，只有当前代次且仍需要的LoadSceneAsync成功才绑定相机/光图并启用；不使用长期activateOnLoad=false阻塞Unity场景队列。过期成功也必须Unload，不能呈现或提交旧结果。必要共享材质/贴图单独公共Bundle，真实唯一Mesh/光图/反射清理与常驻/共享最后引用分别记录；主动态货物可能仍引用公共贴图，不假称所有贴图均能随单一区释放。

b-2边界距离10m预载、18m卸载，取玩家/Echo/当前有效调查目标并集。不同加载状态需滞回；取消/新指令/读档代次变化废弃旧回执，保留正在使用的最新任务。纯调度测试后才接真实Scene提供者。

b-3主驱动退出时仍须清理未完成加载。独立main-thread owner可在DontDestroyOnLoad短暂继续轮询，只处理晚回执/卸载，全部空闲后自毁；不能依赖随场景停掉的coroutine。严格读SDK实际UnloadSceneAsync对load/unload句柄的释放语义，禁止双Release或失去句柄。初始化Bootstrap阻塞需尊重现有面板/任务板输入所有权，不能Loaded时粗暴清除其他UI阻塞。

b-4原生PVS在拆分后失效，需主＋三个视觉Scene一起加载烘焙，暂时启用视觉参与，完成后恢复初始inactive。场景ID/Renderer引用与保存数据核对，真实Bundle播放中再做绘制/图像对照；不可沿用旧36→20结论。不得写全局用户脏资产，SDK保存继续using NextSdkSaveScope。

后续P01仍须真实修复世界TextMesh穿墙/朝向/大小，并适配两个警告方块的外观、补可见灯面/可读性；前面的静态对照隐藏不能当产品修复。设置高中低/分辨率窗口全屏、实际WindowsProfiler/完整路线/跨进程存档、最终保护/原VC-AA4恢复均未完成。

新增实际代码审查点：MergedRoomMesh当前OnEnable和Start都会Apply旧逻辑Atlas0/1。未来Registry给多房间分配全局槽后，必须明确禁用流送房间内该自动覆盖（独立monolithic仍保留），否则Start会把第二/三区的槽重置；在b-4实际帧/多区光图测试验证，不能仅看初次赋值。

