# 回收探索工程说明

本轮目标是三区回收切片的Unity客户端交付。反馈02新入口为`Assets/EchoStation/SalvageNext/ArtV2/ES_ArtV2_Streamed.unity`；旧`Scenes/ES_Salvage_Streamed.unity`保留；单场景`ES_Salvage_Next`是保留的渲染/导航参考，R2灰盒与旧五区工程保留。Windows入口与操作见WindowsPlayGuide.md。

源码分为`SalvageNext/Domain`（纯规则/配置/校验/需求规划）、`Runtime`（Unity交互与呈现）、`Editor`（独立资产与构建工具）、`Tests/SalvageNextEditor`（模块用例）和`Tests/SalvageNextFixtures`（显式参数才启动的Windows验证）。原Salvage组件复用移动、携带、任务与传统Echo，不改旧源码。

`NextSession`管理新版独立原子槽、偏好与画质；`NextAdvice`只对已发现、World、≤8kg、可达目标排序，确认后执行一个目标。`NextCommandPanel`提供R/T入口；`NextDisplayPanel`经DisplayTransaction稳定观察/确认后写独立显示配置，失败回滚。新槽与R2分离。

`RoomDemandPlanner`取玩家/Echo/有效任务位置并集，10m预载、18m卸载滞回；World/任务票据拒绝过期应用。`RoomSceneOwner`持有真实Addressables句柄，迟到加载只清理；独立pump在主场景退出后继续释放与合并UnusedAssets请求。`RoomPresenter`绑定房间相机、全局Lightmap槽与实例灯/装饰，最后引用释放真实Mesh/贴图/Lightmap。碰撞、导航和逻辑目标常驻，视觉三区异步流送。

反馈02静态200→52 Mesh合并（旧版142→37保留），房间共享PVS、动态SRP Batcher、GPU装饰3级LOD/广告牌/Shader风、18个实例灯面、半分辨率16步局部雾、全屏扫描及16粒子池预算都有独立原生计数/图像/释放证据。管线固定普通Forward、内部HDR转SDR，三画质参数见对应R01/R02报告。

AI保持记忆→语义JSON→传统执行三层与六意图，不能改GameObject或代搬/改设备。本轮Qwen3三个候选原始任务正确率均未到90%门槛，已No-Go并停止模型实验；R3未完成，当前Windows版是脚本Echo，不能将客户端/安全守卫通过写成模型质量通过。旧S3与模型源只读保留，当前包移除模型副本。

独立模块结果以R3_R4_ModuleLedger.md和Docs/Tests/SalvagePrototype/Reports为准；原始日志、JSON、截图、Profiler和失败记录留存。module_tests.py统一使用新鲜场景/渲染/Console门禁。完整回归排除一次性模型实验入口和已完成的合并准备探针，不能重放它们来测产品。

资源：Quaternius免费Standard、Poly Haven与JamesWhite Mars Rover为CC0；Noto为OFL，JSON.NET/ONNX Runtime包含相应许可，Unity Addressables受Unity Companion License约束，随包ThirdPartyNotices保留。不会将付费Pro/Source包内容当免费资源。

性能证据是开发机D3D11/1080p静态观察与真实功能压力；最低i5-8400/GTX1060与推荐i5-12400/RTX3060、Windows10另机测试、外部新玩家体验尚待。反馈02额外准备FirstPlayTestForm.md供真实外部试玩；尚无外部玩家结论，不进入R6。

## 反馈02空间与界面

ArtV2/ES_ArtV2_Authoring用于独立创作/烘焙，ES_ArtV2_Merged_B为最终按房合并源，ArtV2/Rooms三区为es-v2-room-0/1/2地址。旧A合并只保留为本轮草稿。泊位/储藏4.5m，检修5m，原门3m，摄像机高度1.6m/FOV70未变。200静态接收器、2方向图集、234探针，流送各区独立复制图集并在退役时释放；新PVS静态图差0，原生绘制61→24。

NextHud仅从Mission/Carry/Delivery/Echo读取实际状态，0.15秒更新，CanvasGroup只隐藏HUD，不接管输入；Mission旧文本HUD仅在ArtV2运行时替换。NextUiTheme提供精选Kenney九宫格/按键/准星；NextPanelStyle分列任务板并统一现有面板呈现，仍由原组件决定交付/结算/取消。原Salvage源码不改，旧Next场景无主题时保持原界面。

NextWindowsBuild.Schedule默认仍可构建旧流送入口；传NextArtV2Stream.Main构建反馈02。所有构建设置只在作用域内生效并归档native/disk/dirty后置比较。Windows验证只由显式命令行参数启动，普通双击不自动测试。新增CC0许可证/来源哈希随ThirdPartyNotices/KenneyUI分发。
