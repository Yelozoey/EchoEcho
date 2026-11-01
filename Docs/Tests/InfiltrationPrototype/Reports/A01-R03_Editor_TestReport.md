# A01-R03 Editor交付实际记录

2026-11-01。R01_D3/3、R02_A3/3分别归档后做交付。范围限独立金属/平面反射样板，不改大地图/旧版、引擎、API、依赖或AI；Editor测试，不打包。

## 独立输入与Profiler

R03_A真实1/2：输入通过，240帧237有效GPU但有1帧Draw Counter为0，夹具要求所有帧Draw>0失败。原始帧/尖峰保留；修正为每模式至少100个正GPU+CPU/Draw组合样本、零值仍无效，不删除或伪造。**R03_B实际2/2、0跳过、11.185秒，所有output为空**。

真实W移动.44m；1两次切换地面反射，2环境反射、3直接高光，P暂停机器人、Esc释放鼠标均用真实InputSystem事件验证。输入只负责样板操作，不是LLM/守卫AI。

Profiler_85d7b912：i7-12650H/RTX4060 Laptop/D3D11，相机1920×1080，单块1024宽HDR反射，预分配240样本、每模式.75秒预热；236非零/去重GPU且CPU/Draw有效，4无效时序保留，零Counter0。本批开启/关闭反射GPU平均5.228/4.209ms、P95 6.738/4.759ms；Draw628/465。此为短顺序Editor采集，不能当固定反射成本；此前失败批关比开慢，也未据此宣称负开销。

Editor Main平均9.51/8.32ms、P95 24.98/15.40ms，有109.15ms尖峰；原失败批亦有95.27ms。含Editor/Runner/GUI，不宣称稳定60FPS或产品无卡顿。最低基准仍Win10/11x64、i5-8400/GTX1060 6GB/16GB、1080p中画质60FPS；推荐i5-12400/RTX3060 6GB/16GB、1080p高画质60FPS；本机数据不替代两套目标硬件。

本模块独立门禁与报告完成，随后执行受影响组合回归、原生Game预览、旧文件/设置精确还原及最终预算，再补记交付。本人视觉接受尚未执行。

## 组合与实际Game画面

Final_A实际**8/8、0跳过、11.464秒、全部output为空**，新鲜SceneSafety/RenderSafety通过。覆盖全部R01/R02/R03；先前超时、黑墙、单帧计数0和阻止启动的收据均保留。最终Profiler_e578b47c共240帧、237有效GPU+Counter，开启/关闭GPU平均4.925/3.889ms、P95 5.642/4.462ms，Main P95 6.661/6.656ms，Draw628/465。最终短批没有前述大尖峰，不能因此抹去独立批109ms或宣称全时稳定60FPS。

NativePreview_A实际进入自有Play并保存GameView_A.png，2560×1440、有中文HUD；RS_ForwardHDR、HDR/SDR、一个监听器和GameView焦点原生读回，非1080p性能图。代理检查机器人/箱体/灯条实时倒影与三球差异。finally退出自有Play并回空宿主，预览进入前的runInBackground=true先按捕获值恢复；随后BackgroundRestored_A依据上轮A01-D交付已验证false显式复原后台运行开关，不把本轮未捕获的App开关说成新鲜完整快照。

NativeRestore_Final_A、NativeVerify_BeforeLeave_A、NativeVerify_AfterLeave_A分别确认Graphics/Quality/EditorSettings的native JSON/磁盘字节/dirty精确一致，PC1/AA4/原VC默认与PC绑定，Reflection-WorkspaceRestored先还原原精修场景/选择/播放选项。原大地图未改，无未知场景保存/丢弃。旧SHA与打开小样后的最终保护结果在收尾补记。

保护审计补记：Protection_Final_A发现两个设置文件差异，三设置native检查通过并不意味着所有ProjectSettings字节已还原。ProjectSettings.asset唯一后台开关0→1；TimeManager从旧`Fixed Timestep: 0.02`格式自动序列化到v2 rational，简单改Count不能还原。逐个候选必须匹配本轮原始SHA才恢复；两个原字节文件均找回，原变更字节保留，SettingsRecovered_A再次验证匹配且原生后台false/时间倍率1/步长约.02。没有用猜测字节覆盖或改变原保护基线，见F-368。随后重新全量核对旧SHA与交付设置，不将前一轮失败标为通过。

## 最终交付与停止

**Protection_Final_B重新核对15,053个旧文件，changed=[]、passed=true**，保留失败A与原记录。ImplementationManifest_A静态记录94个本轮实现/工具文件，不替代测试。NativeVerify_Handoff_A在有意打开新小样后仍确认三设置native/disk/dirty精确；RecoveredSettings_Handoff_A另确认PlayerSettings/TimeManager字节依旧匹配本轮原始SHA。原大地图和所有旧资产/模型/构建/证据保留，未改包或下载资源。

Handoff_Inventory_A实际小样clean/Edit未播放、0缺失脚本、单平面/512探针B/RS_Camera、临时反射相机和RT均0；Console_Handoff_Final_A为0错误/0警告。Budget_Final_A19.958802/20GiB，相对前轮增加约21.7MiB，低于24MiB计划；最后少量文档另见Budget_Final_B。旧预算失败例外未启用，不删历史腾空间。

已打开Assets/EchoStation/ReflectionSample/Scenes/ES_ReflectionSample.unity交本人Editor检查。[操作指南](E:/Projects/Unity/EchoStation/Docs/InfiltrationPrototype/A01R_EditorPlayGuide.md)。仅工程/实景检查通过，本人审美与目标机性能未验收；不是硬件光追/SSR，不扩展任意曲面或自动铺回大地图。当前短任务完成并停止，等待反馈，不进入守卫行为、潜入流程或LLM。
