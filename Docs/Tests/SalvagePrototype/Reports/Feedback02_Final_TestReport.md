# 反馈02最终报告：提高空间、三区地标、HUD与Windows交付

2026-10-31。用户授权提高层高并继续视觉识别、UI与首次试玩准备；按V01→V02→V03→V04→U01→U02→P02顺序逐模块实测归档。最终产品为ArtV2独立场景，原R2/旧五区/旧Next场景与旧发行包保留。R3模型No-Go不变，本版为脚本Echo。

## 实际变更

- 泊位/储藏屋顶4.5m，检修间5m；原3m门、1.6m相机、FOV70、玩家/Echo/货物比例与玩法位置保留。
- 高位墙板/货架/箱体、检修风机/线缆与三区标识；仅装饰变化，未增加交互机制、伤害或Echo执行权限。
- 重烘焙24个区域灯、2方向性图集、234探针与三区反射；200静态接收器合并为52。独立碰撞/Nav/分区光图/Addressables与PVS同步，旧资产未被覆盖。
- HUD分为回收进度、Echo电量/模式、手持信息、准星提示和按键栏；任务板七行物品列表，R/T、交付、F10统一主题。精选Kenney三套CC0包中的9张原PNG，源哈希与许可随包保存。

## 模块与集成结果

| 门禁 | 实际结果 |
| --- | --- |
| V01/V02 | Height_B1/1、Dressing_B2/2；净空错误修复保留初次失败 |
| V03 | Lighting_C4/4；实际Bake0c8910与合并图像差通过 |
| V04 | 初次Streaming_A5/6；导航夹具修复后Navigation_B1/1；真实资源释放与PVS通过 |
| U01 | 最终Hud_D2/2；1080/720明暗渲染、准星、真实进度和低电/模态 |
| U02 | Panels_A8/8；全量顺序发现呈现夹具依赖小GameView，修复后Panels_B1/1 |
| 完整回归 | FullNext_A207/208失败保留；最终FullNext_B **208/208**、无跳过，实际墙钟298.992秒 |
| Windows构建 | 19e0fe12451e4c9696623a4f177e70e7，成功、0错误/5记录警告，58.680秒 |
| Windows实际过程 | Feedback02_A：启动、原生显示、写档、跨进程读档、主路线、替代路线全通过，退出码0 |
| Windows性能 | Feedback02_Perf_A：三画质×三区共1620/1620有效非零GPU样本，退出码0 |

所有模块测试经Tools/MCP/module_tests.py新鲜场景/Console/实际Forward-HDR-SDR-D3D11门禁。未执行一次性模型实验/合并准备探针。测试期间的失败与修复见F289–F296，不删除历史、不降低产品质量门槛。

Windows确认为ES_ArtV2_Streamed、Unity6000.6.7f1、D3D11、1920×1080、最终中画质专用管线、UseLLM=false、输入恢复。实际原生点击/按键覆盖720p确认、900p取消、无边框确认、独占请求安全回滚、15秒超时与失焦还原。坏槽拒绝没有改变世界；手持/落地/已交付/扫描/历史/电量跨进程恢复。主/替代路线主动结算和重开通过，扫描不代交付。

## 画面与性能证据

真实Windows Startup.png已查看：中文HUD清晰，层高已进入发行包。Display_720.png确认原生720p下设置文字与按钮；其他流程/画质/三区图像及原始日志保留。

开发机为i7-12650H、RTX4060 Laptop、16GB、Windows11。1620有效样本中，各组GPU P95最高3.389ms（全样本最大4.261ms），主线程P95最高3.209ms（最大7.116ms），总使用内存采样峰值628.574MiB，整帧GC采样最大538B；不宣称整帧零分配。样本在可见前台Development Player中按60FPS/vSync0测试设置采集，含调试测量开销和UI。

实际PVS FrameDebugger绘制61→24、背向0，可见图差0；实际区块加载0.0539秒，小于10m/3.5m每秒的预载缓冲2.857秒。退役区独占Mesh/Lightmap贴图销毁，其他房共享材质和动态探针保留。Echo实际穿过各门并经短路返回。

这些结果**不代替**最低i5-8400/GTX1060 6GB/16GB中画质1080p60FPS，或推荐i5-12400/RTX3060 6GB/16GB高画质1080p60FPS的实机验收。Windows10另机、首次真实玩家用时/理解度、人工IME体验仍待；自动路线时间不能作首次约5分钟体验证明。

## 保留、恢复与证据

- 原亮度版484文件完整SHA归档为Feedback02/Feedback01_Windows.zip；846项旧Next资产核对仅11个已授权代码/包注册改动，旧场景无修改。
- 构建后、两次回归后1007项产品源码/资产/DLL/本地Bundle签名一致；测试修复只在Editor测试夹具，已测Windows包未改变。
- Graphics/Quality/EditorSettings完整native JSON、磁盘原字节、dirty标记与NativeBefore_A精确相同。已还原原S3干净场景、VC管线、PC1/AA4、后台false、播放选项true/1；未保存或丢弃用户场景。最终Console错误0。还原后的VC属于用户原管线，不在模块测试白名单，FinalRenderSafety的allowed=false如实保留；实际普通Forward/内部HDR/SDR/D3D11均正确，未修改白名单或在该绑定下重新运行模块测试。
- 旧文件保护与最终资源预算见R3R4/Audit_Feedback02_Final_A.json；构建期间预算采样估计最高19.928GiB，低于20GiB。模型源与旧证据保留，仅移除新发行包的模型副本。
- 当前发行目录的完整SHA/体积/许可清单见Feedback02/P02/DeliveryManifest_A.json。运行验证44文件与符号1文件已逐SHA验证移入P02，原始Profiler.raw仍在RuntimeValidation/Feedback02_Perf_A。

主要证据：Feedback02/P02/FullNext_B.json、NativeRestored_A.json、FinalWorkspace_A.json、PerformanceSummary_A.json；R4-P01c/Windows/Feedback02_A及Feedback02_Perf_A；R4-P01c/Product_Feedback02_Final_A.json；构建19e0fe的save/player/lightmap scope。原失败和每模块报告继续保留。

## 交付与下一步

运行Builds/SalvageNext/EchoStationSalvageNext.exe，保留整个目录。新Unity入口Assets/EchoStation/SalvageNext/ArtV2/ES_ArtV2_Streamed.unity；编辑器保持用户原S3布局，不自动切到新场景。

FirstPlayTestForm.md与更新后的WindowsPlayGuide.md已准备并随包复制。下一步由真实首次玩家试玩，记录空间/三区辨认、HUD、Echo帮助与交付结算卡点；尚无外部玩家结果，也未进入R6求职材料或重启模型实验。
