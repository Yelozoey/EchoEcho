# V01 Windows灰盒交付实际报告

2026-10-31。最终构建f3ce690cf8ff428fa3fed169eb7701d5，BuildPlayer成功、0 errors；开发版本警告原样保留。入口Builds/SalvageExpansion/EchoStationSalvageExpansion.exe，独立ES_EnvironmentalSalvage场景与SalvageExpansion存档，不覆盖旧版。默认脚本Echo，R3模型No-Go不变。

## 已执行结果

- 最终产品回归FinalRegression_B **90/90**，113.830秒，Job247ba1807ab74e8c984a7f13a3a35885，10完整场景＋80细分模块；准星真实像素、热点GC优化及动态carving随后在最终Windows版本复核。
- 最终Windows八进程全部exit0：Startup_D、Fan_D、Valve_D、Cross_D write/read、Failure_D、Display_D2、Performance_D。逐项原始Process/Result/Quit和实际actions见Evidence/Expansion/V01/FinalWindowsChecks_A.json。D套件Display_D失焦失败保留，D2为同包实际前台复测，不覆盖旧失败。
- 启动实际1920×1080/D3D11/Unity6000.6.7f1、普通Forward专用管线、内部HDR/SDR，HUD与44准星像素可见；普通启动没有自动路线。
- 两路线以虚拟按键驱动真实玩家移动/射线/工具交互，圆弧使用真实指针状态；隔离/三夹具/取核心/交付取消再确认/主动结算/全局重开通过。只有显式自动路线模式控制模拟焦点，普通启动、显示失焦与GPU真实焦点不覆盖。
- 跨进程完整物品/插口/设备/危险/记忆/任务恢复继续完成；失败不覆盖有效手动档，F9与N/Enter恢复通过；虚拟输入、帧率设置与退出背景状态均有恢复收据。
- 原生720p/900p/1080p、窗口/无边框/独占、15秒超时、失焦回退、重新捕获鼠标通过；实际已用UberPost支持，后处理开关全屏平均图差约0.0381994。未启用Panini的剥离警告按F019/F271保留，不能当全部后处理失败或隐藏它。
- **810/810**有效非零GPU帧，三画质×三区。开发机i7-12650H/RTX4060 Laptop；总体Main/GPU P95 **2.682/2.495ms**，GC中位 **0B/帧**、P95 3274B，Profiler总使用内存峰值531.49MiB，Draw Calls峰值185。具体分组/前后对照/边界见Expansion/Performance.md，不宣称所有帧零GC。
- 实际距离卸载房间0和2、Renderer/Lightmap引用清理且无流送错误；物品成孔/拾回恢复已在真实NavMesh测试通过。

## 预算与保护

四次构建独立峰值监测均低于批准22GiB；最高BuildPeak_D约20.883GiB。每次仅剥离新输出1,902,692,739字节模型副本；源模型保留。交付后最终20GiB审计见Budget_Final_A.json。

最终既定资源根合计 **19.187671GiB / 20GiB**；干净发行目录 **501文件 / 240,546,877字节（229.40MiB）**，无模型负载、无Validation副本。发行SHA清单DeliveryManifest_A.json；原始Windows证据归档AllPlayerEvidence_A，最终没有试玩进程残留。

原8554模型/旧构建文件（10,011,253,792字节）SHA256全部一致；原3767代码/YAML/meta/配置文件SHA256一致。SDK临时link.XML/meta归档后按准确路径清理，未删除整个Addressables配置目录。所有Windows副本诊断87文件/83,980,910字节先SHA核对归档再从新发行目录清理，旧构建和失败证据保留。

已恢复原S3 AI Demo干净布局、VC默认/PC/实际管线、PC1/AA4和播放选项；Graphics/Quality/EditorSettings完整native/disk/dirty均与Run01初始一致。最终FinalWorkspace_A、SceneSafety、Console0与保护收据在Run01。AA恢复门禁曾准确拒绝临时URP同步遗留0，F317只对精确记录字段/字节修复，未关闭完整门禁。VC不在测试白名单属于恢复后的已知状态，未在其上继续测试。

## 失败与实际边界

F301–F317及各轮原始回执保留：夹具/旧程序集筛选、自遮挡、通路摆点、机器人让路、API/直接程序集引用、显示指针、准星、动态障碍、自动测试后台焦点/退出和AA恢复等，均以对应修复后实际结果为准。Windows旧C路线失败/超时不计通过。

这是Windows11 x64开发机上的自动与图像验证，不能当Windows10另机、最低i5-8400/GTX1060 6GB/16GB中画质1080p60、推荐i5-12400/RTX3060 6GB/16GB高画质1080p60的实机证明。首次真人5–8分钟与趣味性尚待试玩。LLM、LoRA、云服务、战斗及下一阶段均未启动。本轮交付后停止。
