# R3/R4最终工程交付与证据

日期：2026-10-30。结论：授权范围内R4客户端工程与Windows试玩版交付完成；R3模型质量No-Go，相关后续整合未执行。目标硬件性能与外部体验验收仍待，不将这些项目记为通过。

## 发行与入口

Windows x64 Development构建：`Builds/SalvageNext/EchoStationSalvageNext.exe`。最终原生构建9e6d0080a96146a98ac1963712c26d40，Succeeded、0 errors、32.576秒。包内无模型副本；源码模型、旧构建与旧场景保留。使用本地Addressables，不需要联网、登录或防火墙入站许可。

逐文件字节/SHA清单：`Evidence/R4-Final/DeliveryManifest_G.json`，484文件、259,756,498字节（247.723MiB），12份许可文件；发行前再次核对646项产品SHA。试玩操作：`Docs/SalvagePrototype/WindowsPlayGuide.md`，发行目录内另附README与ThirdPartyNotices。许可包括CC0资源、Noto OFL、JSON.NET/ORT及Unity Addressables许可；不把软件依赖称为CC0。

Unity正式入口：`Assets/EchoStation/SalvageNext/Scenes/ES_Salvage_Streamed.unity`。保留单场景参考、新版独立槽与R2槽隔离。手持上限8kg、六物品分数/重量、≥3件且≥12分主动结算、只读Echo权限保持已确认规则。

## 实际验证

| 项目 | 结果与边界 | 原始入口/报告 |
| --- | --- | --- |
| 完整模块回归 | FullNext_C 188/188，0失败/0跳过，36.685秒；统一新鲜Scene/Render/Console门禁 | Evidence/R4-Final/FullNext_C.json；R4-Final_Regression_TestReport.md |
| 构建一致性 | 646项产品源码/资产/游戏DLL/Bundle，玩法a7b6与最终9e6d实际SHA零差异；明确排除验证DLL/元数据 | Evidence/R4-P01c/Product_Final_F.json |
| Windows启动 | Windows_C startup exit0，真实独立Player | Evidence/R4-P01c/Windows |
| 跨进程存读档 | Gameplay_D write/read 44.028/46.029秒，F5/F9混合状态、原生坏独立槽拒绝且World不变、继续回收结算 | R4-P01c-2_WindowsSave_TestReport.md |
| 完整两路线 | Gameplay_D route/alternate 60.042/69.038秒，A+B+D与C+F+B实际步行/扫描/携带/交付/主动结算/重开；结果末尾清零是验证重开后的状态 | R4-P01c-3_WindowsRoutes_TestReport.md |
| 原生显示 | Display_F 34.039秒，实际按钮点击回执、稳定显示、720/900/1080、窗口/无边框/本机独占、确认/取消/15秒超时；失焦测试调用产品同一事件入口，非人工Alt-Tab证明 | R4-P01a-3d_WindowsDisplay_TestReport.md |
| 实际性能采样 | Perf_F 53.078秒、9组×180=1620条唯一非零CPU/GPU；前台可见1080p；仅测量作用域60FPS/vSync0后还原 | R4-P01b-3_Performance_TestReport.md；Evidence/R4-P01b-3/PerformanceSummary_F.json |

开发机Windows11家庭版26300、i7-12650H、RTX4060 Laptop 8GB、约16GB RAM、D3D11。三画质三区主线程P95最高4.687ms、GPU P95最高3.441ms、Unity已用内存最高599.1MiB。CPU整帧约16.8ms包含节奏等待；全帧GC约1.2KB/帧，不能称零GC。静态点采样不能代替移动/扫描所有峰值；计数器内存不等于进程工作集/显存。

最低i5-8400/GTX1060 6GB/16GB中画质1080p60、推荐i5-12400/RTX3060 6GB/16GB高画质1080p60仍是待实机证明目标。Windows10另机、人工首次体验/完成时长未执行。

## 渲染、流送与美术

已独立实测：普通Forward、内部HDR转SDR；静态Lightmap与独立180探针；局部半分辨率16步雾、ACES/颗粒/轻色差/交互DOF；142→37静态Mesh合并、GPU装饰3级LOD与远广告牌/Shader风、动态SRP Batcher；多场景PVS、全屏扫描与16粒子池预算。

官方Addressables2.11.2/SBP3.0.3本地Bundle；10m预载/18m卸载滞回，玩家/Echo/有效任务需求并集，过期World/任务票据拒绝、主线程句柄所有者和晚完成清理。实际16次跨区、5次读档及隔离坏包/恢复，最终Bundle/句柄归零；房间Mesh/贴图/Lightmap最后引用释放，有原生证据。详情按R3_R4_ModuleLedger.md逐项查阅。

Standard完整ZIP 48,559,316字节，SHA256 `6fae60cf5189e44dff0bd91097f094a765acc6d57d64a85a0cc0dd56e03035e3`；Mars Rover ZIP 210,646字节，SHA256 `474e8e6b29a7dd80304687300f6a8bf16dba54a7963912808011c9f9303d12c4`。设施与道具、六轮Echo/探头/三灯、危险设备与可见灯具均已适配。Standard是模块包，1195项中无可直接运行的完整Unity场景。

## 保护、恢复与归档

最终保护/预算：`Evidence/R3R4/Audit_Final_H.json`。3610旧文件仅3项审查例外：Packages/manifest.json、packages-lock.json（授权官方AA依赖）、EditorBuildSettings.asset（AA必要映射，原场景列表不变）；73个旧包版本/来源/哈希/依赖保留。总量17.737949GiB，未超20GiB。最终Console错误/警告0，FinalConsole_H.json留存。包清单专项证据见R4-R03a-1_Packages_TestReport.md。

保留你接续期间后来打开的`ES_S3_AI_Demo`干净场景；未保存或丢弃用户场景。原全局VC_ForwardHDR、PC等级1、AA4精确恢复，Graphics/Quality整体SHA与任务前一致；EditorSettings播放选项true/flags1、dirty=false，后台运行false，非播放。证据：Evidence/R4-Final/FinalWorkspace_G.json、GlobalNativeRestore_F.json及OriginalSettingsCandidates目录内恢复收据。

FinalRenderSafety_G的allowed=false如实保留：该门禁只允许S1/Gameplay/S2三条测试基线资产路径，恢复的原用户VC资产不在白名单。该记录HDR/Forward/D3D11/SDR均正确，FinalWorkspace_G也逐项确认；这是结束测试后的原上下文恢复，不能冒充仍处于模块测试基线。所有实际模块执行前使用allowed=true的新鲜门禁，未放宽白名单或空绑定测试。

原运行验证目录与构建符号已同卷归档，映射见Evidence/R4-Final/DeliveryArtifactRelocations.json。1620帧及359,712,800字节原始Profiler保留在RuntimeValidation/Perf_F；Perf_E不足GPU帧的失败原始Profiler已CRC/SHA验证压缩保留于Evidence/R4-P01b-3/FailedProfiler_E.zip。失败轮次不删除、不混入最终通过结果。完整故障根因/修复/复测见Docs/Troubleshooting.md F267–F281。

## 未完成与停止边界

R3三候选原始任务48/60、46/60、47/60，均未达54/60；H100未启动，未训练/换模型/第四候选。包内UseLLM=false，界面不能绕过No-Go。39项上下文/权限通过与188项工程回归不等于模型质量通过；R3未完成，旧S3仍17/24。

构建保留已知警告：未关联Unity Services、验证夹具DEVELOPMENT_BUILD过时警告、旧程序集System.Windows.Forms依赖警告；本轮实际执行路径通过，未声称其他未覆盖路径均无警告。该包为Development试玩版，未当Steam正式发行验收。

本轮停于R4客户端/Windows交付。R5外部试玩、R6求职材料、目标硬件性能、Windows10另机与LLM质量仍未验收。
