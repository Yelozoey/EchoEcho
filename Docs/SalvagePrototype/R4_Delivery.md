# R4 Windows试玩交付

2026-10-31当前发行已更新为[空间/HUD反馈版](Feedback02_Delivery.md)：4.5/4.5/5m、三区地标、HUD/任务板，208/208及新Windows验证通过。下文保留此前发行的历史记录。

2026-10-30后续试玩反馈：当前同一路径已更新为亮度修正版32dea，曝光+1.0EV/对比度2；7项针对性测试和Windows启动/完整回收路线通过。见[亮度报告](../Tests/SalvagePrototype/Reports/R4_Feedback01_Visibility_TestReport.md)及[资源与下一步](Feedback01_ResearchAndNextSteps.md)。下文原R4构建/清单保留为历史，旧包完整ZIP已归档；当前包清单为Evidence/R4-Feedback01/DeliveryManifest_F.json。

2026-10-30。本轮授权的R3有界验证及R4工程/Windows交付已经执行完毕，停止于R4。R3模型质量未通过，依已确认的例外继续完成脚本版；不将R3或旧S3记为完成。

运行`Builds/SalvageNext/EchoStationSalvageNext.exe`，保留整个目录。当前约248MiB；最终逐文件SHA和精确体积见`Docs/Tests/SalvagePrototype/Evidence/R4-Final/DeliveryManifest_G.json`。原始下载已成功接收并校验，无需继续重试资源下载。

玩法是亲自进入三区、回收并返回泊位确认交付；至少3件且12分后打开任务板主动结算。WASD/鼠标移动观察，E拾取和申请交付，G放下，R指挥Echo，T可选文字，Tab任务板，F手电，F5/F9新版独立存读档，F10画质/分辨率/窗口设置。具体物品、充电和操作见[试玩说明](WindowsPlayGuide.md)。

美术已使用Quaternius模块化设施与Mars Rover适配Echo，包含六轮、探头和三色灯。三区含返回短路；原R2灰盒、旧五区与模型源保留。正式Unity入口是`Assets/EchoStation/SalvageNext/Scenes/ES_Salvage_Streamed.unity`，单场景`ES_Salvage_Next`保留作参考。当前编辑器保留你后来打开的S3场景，不自动切场景。

完成了真实静态烘焙、三画质后处理/局部雾、静态合并、GPU实例/LOD/远广告牌/Shader风、PVS、全屏扫描与粒子池，以及本地Addressables三区距离异步预载/卸载和Mesh/贴图/Lightmap引用释放。代码职责与参数入口见[工程说明](TechnicalGuide.md)。

最终188/188回归通过；Windows实际启动、混合状态跨进程读档/坏槽拒绝、两条完整操作路线、显示确认/取消/超时，以及三画质三区1620条有效CPU/GPU样本均留存。完整事实、失败修复和证据入口见[最终报告](../Tests/SalvagePrototype/Reports/R4_Final_Delivery_TestReport.md)。

当前限制：Qwen3三个候选均未过90%任务质量门槛，UseLLM关闭，Echo使用脚本协作。性能采样来自i7-12650H/RTX4060 Laptop开发机，不能证明GTX1060最低配置或RTX3060推荐配置稳定60FPS。Windows10另机、首次外部玩家体验和5分钟时长验收仍待；没有进入R5外部试玩或R6求职材料。
