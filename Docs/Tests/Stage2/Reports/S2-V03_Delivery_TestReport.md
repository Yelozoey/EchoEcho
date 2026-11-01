# S2-V03 性能与最终交付

2026-10-26。A/B性能已分别归档：原始Profiler定位区域闭包，既有功能2/2后最终五区1500/1500唯一有效CPU/GPU帧、全部0B GC；PVS两轮实际Draw Calls43→38、受控图像差0。完整数据、尖峰和局限见Docs/Performance/S2_Performance.md；原始01/02/03、失败构建及F107–F109均保留。

## C：交付构建与实际程序

Build_20261025_174050.json记录Win64/BuildOptions.None（非Development），Succeeded/55.874秒/0错误/4警告。显式仅ES_Station_Greybox场景，输出`Builds/S2/EchoStation.exe`；FrameTimingStats内存和磁盘恢复false、选择恢复=true。四警告为既有Services未关联、MCPForUnity两条UAC0005及Windows.Forms依赖，按F011跟踪，没有为消提示关联云服务或改第三方。运行的禁用Panini剥离提示F019仍保留。

| 原生进程 | 独立步骤与期望 | 实际 |
| --- | --- | --- |
| Delivery_A01 | 新局经W/E/中文/实际指针走六谜题完整归档；拒绝代操作/未知码，错密码/错回路不提交，两个实际隐藏扫描；结局前手动档保留 | exit0/144.670秒，归档路线111.734秒、73段W、10次E阅读、4条命令、14目录来源，全部通过；中文/200字符/100行/滚轮/模态/鼠标/手电/分辨率/全屏也通过 |
| Delivery_B01 | 新进程空局经F9整体读上例隔离档，逐字段比位置/视角/手电、Progress、记忆JSON、历史；文件不能改变 | exit0/6.048秒，玩家/Echo位置偏差0、时钟差0、数据一致；读前后SHA256均9A48B7A92DC49A647C988BC083DEC1E889CC98D0C880F52F3FA02CA7A9FC2D6B |
| Delivery_Normal01 | 没有任何S2测试参数的普通程序启动；出现真实窗口并持续运行，再关闭自有窗口 | exit0/28.172秒、seenWindow/CloseMainWindow=true、无超时/Exception；仅证明普通启动存活/关闭，玩法由上两独立步骤验证 |

运行实际Unity6000.6.7f1/D3D11/1920×1080/ES_S2_ForwardHDR，内部HDR=true/显示HDR=false/UseLLM=false。交付程序SHA256为9806ABDE4A406ED68647E9505852C49F6AA72A372F4F084E2AA2821AFFEA57A2。结局与读档日志实际截图已查看，字体/布局清楚，无Development水印。测试仅用Evidence隔离槽，默认S1/S2玩家档未由探针访问。日志未发现Exception/Crash/Shader Error/双监听器错误。

## 文档、资产与收尾

源码架构、S2渲染参数、玩法/剧透攻略、命名规范、性能、DevLog、30模块索引和故障文档已整理；Builds/S2附玩法说明、许可及逐文件SHA256清单。Noto许可证/版权原文随包复制，PBR来源与CC0记录保留。S1的475保护文件最终核验未变。SettingsAuditFinal：Editor空闲、原场景干净、Profiler关闭/空帧、FrameTiming内存/磁盘均0；Console后置0错误。仅中文字体运行时Font Texture有Unity内部dirty状态，未批量保存资产。

V03工程与可用环境验收通过，S2的30个工程模块开发/独立测试/归档结束。外部验收仍未完成：最低i5-8400/GTX1060 6GB/16GB中画质1080p60、推荐i5-12400/RTX3060 6GB/16GB高画质1080p60；人类首次25–40分钟/系统IME候选窗人工体验；最终正式画质/流送长期压力。这些不能由开发机固定视点或已知答案自动路线替代。S3/S4未开始。
