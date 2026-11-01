# A01-D03 Editor交付实际记录

2026-11-01。D01_D18/18、D02_A11/11各自独立归档后，进行本模块。独立场景Assets/EchoStation/InfiltrationRefinement/Scenes/ES_Infiltration_Dressed.unity；仅Editor，不打包/下载/训练或加入真实守卫行为。

## 独立输入与渲染采集

**D03_A实际2/2、0跳过、21.469秒，两项output为空**。真实Space跳跃最高.883m并落地，Tab两次切换5条导向线，失焦停止/释放鼠标；四个守卫实际源Clip RobotArmature|Robot_Idle循环首周期后仍有10.505°局部运动。无AI执行器。收据DeliveryInput_fb1aea87。

EditorProfiler_8bc2ac84：i7-12650H、RTX4060 Laptop、16122MB、D3D11，相机1920×1080 ARGBHalf；3视角×AO/雾四组合×60帧，共720帧，709有效GPU（非零、时间戳去重），11无效保留不算有效。所有6种Recorder有效，预分配样本、每组合.75秒预热，结束后再组JSON。

| 全开视点 | Main平均/P95 ms | GPU平均/P95 ms | Draw平均 |
| --- | --- | --- | --- |
| 中庭 | 6.258 / 7.087 | 5.842 / 6.350 | 3910 |
| 行政 | 6.769 / 9.304 | 5.598 / 6.993 | 3887 |
| 仓区 | 6.686 / 9.473 | 5.428 / 7.328 | 3971 |

四组合完整统计在原JSON；整体仍有Main19.576ms尖峰，不能宣称稳定60FPS。Editor进程约5.2–5.3GB、每帧约13–14KB采集到的GC含Editor/Runner/GUI，不是产品独立内存或零GC证明。相较A01旧全开GPU平均5.715/5.063/5.500ms，本批5.842/5.598/5.428ms；顺序短批差值含Editor干扰，不当作新增装饰单项因果耗时。Draw升到约3.9千级，保留为后续正式玩法/优化关注点，不声称已经完整合批；本批GPU平均未出现大幅开销增加，未为数字关闭已承诺的视觉效果或删掉可通路线。

硬件验收基线继续为Windows10/11x64、1080p：最低i5-8400/GTX1060 6GB/16GB中画质稳定60FPS；推荐i5-12400/RTX3060 6GB/16GB高画质稳定60FPS。**本机Editor不是目标硬件/发行验收**。

本模块独立工程门禁已通过并先归档，再执行下方原生预览与组合回归。本人视觉接受尚未执行。

## 实际组合回归与原生预览

Final_A实际21/21、0跳过、24.869秒，全部output为空，新鲜SceneSafety/RenderSafety通过。组合复测布局9、源模型/遮挡5、装饰3、真实输入/Profiler2、灯面2，覆盖装饰与光照整合后的通行/输入/灯面契约；D02已独立通过的9项光照/雾/六视图不重复生成大批图像，原独立证据保留。三个独立模块合计30个唯一用例全通过（其中六视图在D01/D02各执行一次），不是声称本次Final跑了30项，见ModuleGateSummary_A。

最终EditorProfiler_543b7c79实际720帧/712有效GPU；默认全开中庭/行政/仓区GPU平均5.936/5.375/5.435ms，P95 6.687/5.868/6.009ms；Main P95 7.831/6.787/6.616ms。四组合全样本仍有Main20.042ms、GPU18.679ms尖峰，保留原值，不证明全时稳定60FPS。目标硬件基线和本机Editor限制同上。

NativePreview_A实际打开自有场景、进入Play、获得GameView焦点并截图2560×1440（GameView_A.png），不是1080p性能样本；DR_ForwardHDR、内部HDR/SDR、1个监听器、无AI读回。代理检查实际HUD/入口门岗/灯条/中庭格栅投光，无临时补光。finally退出自有Play并恢复原runInBackground=false和干净空宿主，NativePreview_A_Closed证实。

NativeRestore_Final_A、NativeVerify_BeforeLeave_A、NativeVerify_AfterLeave_A已逐项确认Graphics/Quality/EditorSettings的native JSON、磁盘字节和dirty精确一致，PC1/AA4、默认/PC原VC恢复；Refinement-WorkspaceRestored还原原A01场景/选择/播放选项。没有保存或丢弃未知场景。旧SHA、打开新场景与最后Console/预算收尾见下方最终补记。

## 最终保护与交付

Protection_Final_A实际14,777个旧Assets/Packages/ProjectSettings/Builds/Tools文件SHA核对，changed=[]、passed=true；原A01/模型/原包/构建/证据保留。ImplementationManifest_A另记录275个本轮实现/工具文件SHA，只是静态清单，不替代Unity测试。

完成原布局恢复后，Handoff_Open_A有意打开新ES_Infiltration_Dressed，干净Edit、未播放。NativeVerify_Handoff_A再次确认原native/disk/dirty全部精确。Handoff_Inventory_A实际0缺失脚本、505个带源追溯的模型标记、78新装饰、50灯面、4唯一岗位、SSAO/局部雾启用、2张Lightmap、净面积1007.70074m²。Console_Handoff_Final_A实际0错误/0警告。

Budget_Final_B19.937625/20GiB，相对A01基线约86.8MiB；最终少量文本收尾另见Budget_Final_C，仍低于本轮100MiB计划与总20GiB。没有删除历史、下载、训练、打包或调整全机设置。曾发生的编译/材质/UV/夹具错误及失败收据均保留，F-352–359记录原因与复测。

[Editor走图指南](E:/Projects/Unity/EchoStation/Docs/InfiltrationPrototype/A01D_EditorPlayGuide.md)。本轮三个模块工程交付完成，当前只等待本人视觉反馈，不把非黑图/代理看图或本机短采样当本人接受/目标机稳定60FPS。守卫仍仅Idle展示，潜入玩法、真实守卫行为、Echo伙伴LLM均未进入；本轮到此停止。
