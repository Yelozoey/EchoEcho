# A01-M03 Editor交付实际记录

2026-11-01本人视觉反馈已归档：对金属感、亮度与中庭倒影的Editor试玩问题，用户回答“效果合适”。本项视觉验收通过，原话见Evidence/A01M/Editor_UserAcceptance.json。未提供逐步操作细节，不扩写为新增自动测试、目标硬件性能验收或后续开发授权。本次仅更新文档，没有操作Unity或新跑测试；下方“等待反馈”为此前交付状态。

2026-11-01。M01_A21/21、M02_A11/11分别实测归档后实施本项。仅增加本项独立输入/性能夹具和交付工具，没有改变已通过的材质、灯光、布局、反射产品代码。

**M03_A实际2/2、0跳过、22.505秒**，失败为0。统一module_tests.py的新鲜场景/Prefab、Console、RenderSafety门禁通过；实际Space跳跃落地、Tab路线、四机器人Idle与F8开关通过。不是只调用开关方法的静态测试。输出逐项另查原始M03_A回执。

Profiler_518e04fc：实际1920×1080、Direct3D11、i7-12650H/RTX4060 Laptop、含Editor开销，预热每档.75秒，预分配240条原始样本全部保留。232条GPU及正CPU/Draw组合有效，开/关各116条，其余8条不计有效GPU；无零计数器样本。开启GPU均值8.935ms/p95 12.102ms，关闭5.980ms/p95 7.457ms；主线程均值9.878/6.820ms，Draw均值5421/4174。此短批固定视角观察到约2.955ms GPU均值差，只说明局部反射有成本，不代表完整路线/稳定60FPS或目标硬件验收。保持单块4×4m、1024宽和F8选择，不扩大反射范围。

统一目标仍是Windows10/11 x64、D3D11、1920×1080；最低i5-8400/GTX1060 6GB/16GB、中画质60FPS，推荐i5-12400/RTX3060 6GB/16GB、高画质60FPS。两套目标硬件均未实测，本次不打包。

本独立结果先归档，随后执行受影响组合回归、一次原生Game预览、5设置native/disk/dirty与运行时还原、15,148旧文件SHA/预算核验，再打开新场景；最终结果另补，不将待执行项记为通过。

## 组合与原生预览

Final_A实际22/22、0跳过、11.411秒：布局9、源模型/窗口/岗位5、装饰3、材质3、输入1、性能1；所有用例output为空。M02的光照/雾/动态像素11项已独立通过，之后未改受影响产品实现；未为凑全套数量重复生成多份大图。Final_A的Profiler_b6562d3f另保留240行/238有效GPU（每档119）：开/关GPU均值7.874/5.882ms、p95 9.026/7.023ms，Draw5421/4174；Editor短批波动不支持把两轮时长差当优化成果。

NativePreview_A是代理自建的一次实际Play会话，GameView_A.png为2560×1440原生Game图（非1080p基准测试）；主相机内部HDR/显示SDR、MM自有管线、单AudioListener、AI未接入。已查看入口墙壳/检查台/终端与中庭反射区域；退出只属于本次的Play并回到空宿主，未操作用户Play会话。M02行政/中庭及Plane_On/Off图也已查看。本人视觉接受仍待反馈。

NativeRestored_A及NativeVerified_PreLayout_A/PostLayout_A证明5设置native/disk/dirty、运行时background/fps/scale精确匹配原快照；原ReflectionSample场景、布局/播放选项和PC1/AA4先恢复后再交新场景。旧文件全SHA与交付新场景状态以随后收尾结果为准。

## 收尾实际结果

Protection_Final_A对原15,148文件全部重算SHA，changed/differences均为空，原基线完整保留；最终回执只存基线SHA与总数，避免重复保存数MiB相同清单耗用预算。不是排除发生变化的旧文件。NativeVerified_Handoff_A在打开新地图后再次验证5设置native/disk/dirty及运行时全部精确一致，D3D11/PC1/AA4。

EditorHandoff_A与EditorInventory_A：新场景clean/Edit未播放、0 Missing Script；72地块、净1007.70074㎡、78装饰、4原岗位；2自有Lightmap、8自有Cubemap（2×256/6×128），主相机HDR/SDR、主Renderer两特效/辅助Renderer无特效。Edit中临时反射相机0/无RT分配；只在本人进入Play并看见反射面时产生。旧地图/小样/模型/存档/构建保留，未下载/打包或接入任何新AI。

预算交付前实测Budget_PreHandoff_A为19.990425/20GiB，本轮较起点新增约32.37MiB（原生Game PNG约2.21MiB），低于本轮34MiB计划；最后文档写入后再以Budget_Final_A记录精确值。工程到此完成并停止，等待本人视觉反馈，不把未获得的审美反馈、潜入闭环或目标硬件稳定60FPS写成通过。

最终Console_Final_A错误0；Budget_Final_A为19.990451/20GiB，仍低于总量及本轮34MiB计划。没有启动后续开发或代替本人进行视觉验收。
