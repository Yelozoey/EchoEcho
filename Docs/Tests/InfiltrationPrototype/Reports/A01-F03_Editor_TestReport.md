# F03 全场涂装Editor交付实际记录

F01_A21/21、F02_A11/11分别测试/报告后进入本项；只给新场景四个岗位Unlit文字加深色以适配浅墙，原材质/几何/灯光保持。F03_A被外部资源变更门禁挡住，无Job；refresh compile:none后F03_B实际3/3、0跳过、25.633秒、output为空：实际Space/Tab/F8、四个进口Idle循环/失焦、四文字内容与对比配置、有效GPU+正CPU/Draw采样流程。这里的性能测试通过表示采样有效，不是60FPS通过。

Profiler_69ac0648：1920×1080/D3D11、i7-12650H/RTX4060 Laptop，含Editor开销，240原始样本保留，222有效组合（倒影开114/关108）。开/关GPU均值36.869/33.718ms、p95 38.382/35.864ms；Draw5421/4174。明显高于16.7ms，不能写性能达标。

因此在相同测试方式/视角下追加未改旧金属场景控制，MetalControl_A实际1/1、12.388秒，原始Profiler保留在A01M/Profiler_84b82779.json：224有效（开110/关114），开/关GPU35.995/34.370ms，Draw仍5421/4174。两版都高、差异方向不一致，当前没有充分证据将高耗时归因为本次涂层；根因未定位，不假定为后台限速。原早先金属版低耗时证据也保留，不用它替换本轮高值。后续需要独立Profiler任务/目标硬件复核。

最低i5-8400/GTX1060 6GB/16GB中1080p60、推荐i5-12400/RTX3060 6GB/16GB高1080p60均未实测。当前不是可宣称稳定60FPS的最终作品集版本。

上述独立结果先归档，再运行受影响组合/三视角回归、恢复5设置/运行时/布局和旧文件SHA，完成交接与仓库审计。最终结果随后追加；不将未执行项记为通过。

最终组合Final_A实际25/25、0跳过、22.245秒、output为空；含原布局/模型/装饰/材质、岗位文字、实际输入/Idle、性能采样与三视角。新ArtView_0/1/2_84c2550c重新渲染，入口及中庭深色岗位文字已查看可辨。Final性能原始Profiler_f931753e保留218有效组合，开/关GPU35.348/34.365ms，仍未过16.7ms；未因有效采样通过而改写性能限制。

NativeRestored_A及NativeVerified_PreLayout_A验证Graphics/Quality/Editor/PlayerSettings/TimeManager的native/disk/dirty、background/fps/timeScale精确一致；原局部样板布局先恢复。Protection_Final_A对15,419旧文件全量SHA，changed为空。随后EditorHandoff_A仅打开新ES_Infiltration_Coated，FinalState_A再次验证5设置/运行时一致、clean/Edit、Missing Script0、72地块/净1007.70074㎡、四岗位/78装饰、两自有Lightmap/八自有区域反射；单平面/主两特效/辅助0、HDR/SDR/D3D11，Edit临时反射相机0。Console_PrePublish_A错误0。

本轮美术工程与交接完成后进入已授权G01可重建仓库交付，不开始守卫或LLM。新机完整Unity导入/目标硬件/真人潜入流程均未验收。仓库排除项与复现限制必须随HANDOFF/README交付。
