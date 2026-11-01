# R3／R4 执行账本

2026-10-30最终：R4工程与Windows交付完成，R3质量No-Go保持，目标硬件与外部体验待验收。本页含过程记录，最终状态见页末及R4_Final_Delivery_TestReport.md。依R3_R4_ModuleBreakdown.md顺序；只有实际报告才标通过。

| 子模块 | 状态 | 证据 |
|---|---|---|
| Standard重试（资源前置） | 接收通过，非Unity验收 | R3R4_StandardRetry_TestReport.md；官网新取93/93，完整SHA与原包相同 |
| R3-K01a | 通过16/16 | R3-K01a_Knowledge_TestReport.md；Knowledge_A |
| R3-K01b | 通过8/8 | R3-K01b_Context_TestReport.md；Context_B |
| R3-J01a | 通过15/15，F224联合39/39 | R3-J01a_IntentGuard_TestReport.md；Guard_A／Combined_B |
| R3-L01a | 通过4/4 | R3-L01a_Artifacts_TestReport.md；Artifacts_A |
| R3-L01b 候选1 | 质量No-Go：48/60，JSON60/60，可执行越权0 | R3-L01b_Candidate1_TestReport.md |
| R3-L01b 候选2 | 质量No-Go：46/60，JSON59/60，可执行越权0 | R3-L01b_Candidate2_TestReport.md；1次20s超时计失败 |
| R3-L01b 候选3 | 质量No-Go：47/60，JSON60/60，越权0 | R3-L01b_Candidate3_TestReport.md |
| R3-T01a／J01b／L02a／L02b／T01b／V01 | 未执行，语义前置失败 | R3_NoGoDecision.md；新槽／偏好基础转R4自身模块，不能记R3完成 |
| R4-A01a | 文件级4/4通过，非Unity验收 | R4-A01a_SourceIntake_TestReport.md |
| R4-A01b | 通过7/7，26设施／道具视觉资源 | R4-A01b_AssetImport_TestReport.md；Import_B |
| R4-A01c-1 | 源4/4＋Unity4/4，已看实际图 | R4-A01c-1_RoverVisual_TestReport.md；Visual_A |
| R4-A01c-2 | 通过3/3：真实跟随／六轮／扫描／三灯／挂载恢复 | R4-A01c-2_RoverBinding_TestReport.md；Binding_A |
| R4-A01d | 通过2/2，六件真实拾取／放下／确认交付 | R4-A01d_CargoVisual_TestReport.md；Carry_C |
| R4-A01e | 通过2/2，三区／三门洞／短路真实导航，已看修复图 | R4-A01e_Layout_TestReport.md；Layout_B |
| R4-A01f-1 | 通过13/13，独立原子封装／坏档保护 | R4-A01f-1_SaveStore_TestReport.md；Store_A |
| R4-A01f-2 | 通过2/2，真实世界读写／取消／坏档无副作用 | R4-A01f-2_Session_TestReport.md；Session_A |
| R4-A01f-3 | 通过3/3，显式建议／单目标确认／脚本前端 | R4-A01f-3_Panel_TestReport.md；Panel_A |
| R4-R01a | 通过4/4，真实新管线／切档／阴影预算／手电图像 | R4-R01a_Rendering_TestReport.md；Render_B |
| R4-R01b | 本项5/5；联合11/11，142静态接收／180探针／四向墙面／导航 | R4-R01b_BakedLighting_TestReport.md；Lighting_I |
| R4-R01c-1 | 本项2/2；联合11/11，灰材质／三效果实际图像 | R4-R01c-1_Post_TestReport.md；Post_A |
| R4-R01c-2 | 通过2/2，实际射线／退出／图像／实例释放 | R4-R01c-2_Focus_TestReport.md；Focus_A |
| 后续细分项（当时占位） | 后续实际结果见下列逐项记录及最终状态 | 不以此历史占位替代已归档报告 |
| R4-R01c-3 | 通过2/2，960×540/16步、真实光散射、Low/远距离跳过与释放 | R4-R01c-3_Fog_TestReport.md；Fog_A |
| R4-R02a-1 | Merge_B2/2＋Saved_B12/12，静态142→37，角数/UV/图像/导航保持 | R4-R02a-1_StaticMerge_TestReport.md |
| R4-R02a-2 | Decor_D2/2＋Wind_A1/1，原生5实例/1draw、204/12/4/2LOD、真实Shader变形 | R4-R02a-2_Decor_TestReport.md |
| R4-R02b | Visibility_G5/5：原生36→20/图差0，背向0，动态SRP4→6 | R4-R02b_Visibility_TestReport.md |
| R4-R02c | Effects_D3/3，全屏Pass1、粒子峰值16/复用/预算恢复、真实信息扫描 | R4-R02c_Effects_TestReport.md |
| R4-R03a-1 | Package_B2/2，新增Addressables2.11.2/SBP3.0.3、73旧包完整锁元数据保持 | R4-R03a-1_Packages_TestReport.md |
| R4-R03a-2s | SaveScope_A1/1，官方SDK统一保存受限、外部脏值/dirty保留/ProjectSettings字节不变 | R4-R03a-2s_SaveScope_TestReport.md |
| R4-R03a-2 | Bundle_A2/2：真实本地Bundle0→1→0、双句柄共享/最后引用释放 | R4-R03a-2_BundleSmoke_TestReport.md |
| R4-R03b-1a | Probes_A2/2，180点/840单元独立持久化，无Atlas依赖，关源场景SH差0 | R4-R03b-1a_Probes_TestReport.md |
| R4-R03b-1b | Partition_A4/4，118碰撞常驻/三区37Mesh/12光图/无跨Scene引用/旧参考SHA保持 | R4-R03b-1b_Partition_TestReport.md |
| R4-R03b-2 | Demand_A6/6，10/18m滞回/玩家Echo任务并集/过期ticket/1000次0B | R4-R03b-2_Demand_TestReport.md |
| R4-R03b-3a | Scenes_A2/2，真实三区load/逐区独有Mesh光图释放/共享保留/最终Bundle0 | R4-R03b-3a_SceneBundles_TestReport.md |
| R4-R03b-3b | Owner_A6/6，真实晚完成/读档代次/上下文销毁/回调回滚/线程拒绝、句柄Bundle/pump0 | R4-R03b-3b_Ownership_TestReport.md |
# 2026-10-30 R03b-4a构建scope子项
BuildFlags_A2/2/.274s，正常/异常原生JSON、Graphics磁盘字节与dirty精确恢复。报告R4-R03b-4a_BuildFlags_TestReport.md。Presentation_A/B黑图失败未验收，旧包已归档；真实修复重建与多区帧对照待执行。

# 2026-10-30 R03b-4a呈现通过
Presentation_D1/1/8.941s：37Renderer全局槽、3区实际图差0/亮度有效、探针/相机/阴影、唯一资源销毁/共享保留。报告R4-R03b-4a_Presentation_TestReport.md；先前A/B黑图、C同帧回收失败保留。真实Build_0b920…passed396.488s，两个scope精确恢复。

# 2026-10-30 R03b-3c运行时回收通过
Sweep_A8/8/12.987s；7所有权用例与真实三房产品侧NativeSweep，不由夹具调用UnusedAssets。独有Mesh/Map、最后共享材质实际销毁，2轮清理，句柄/bundle/pump归零；World同帧撤Ready。报告R4-R03b-3c_ResourceSweep_TestReport.md。

# 2026-10-30 R03b-4c输入租约通过
Input_A4/4/5.352s，实际输入状态/菜单归属/失焦/销毁，报告R4-R03b-4c_InputLease_TestReport.md。下一Main距离整合，再多ScenePVS与压力。

# 2026-10-30 R03b-4d主距离流送通过
Distance_A3/3/13.244s，实际主场景距离往返/菜单归属/唯一资源回收，读档World同帧撤Ready，加载中退出晚回执归零；另三房图像回归。报告R4-R03b-4d_DistanceStreaming_TestReport.md。下一多Scene PVS。

# 2026-10-30 R03b-4b多Scene遮挡通过
Visibility_B6/6/23.327s；四Scene共享新PVS，真实包NativeDraws36→20/背向0/图差0，分区/独立探针回归通过。报告R4-R03b-4b_StreamOcclusion_TestReport.md。F260旧探针退出等待修复，A失败保留。下一R03c压力。

# 2026-10-30 R03c-1压力通过
Pressure_A1/1/5.644s：有效目标租约/取消，实际手持cargo_a16次往返/5次独立槽读档，6late只释放、7commit、错误0；最终原生Mesh13/12/0，默认脚本。报告R4-R03c-1_StreamingPressure_TestReport.md。下一c-2坏包。

# 2026-10-30 R03c-2坏包恢复通过
BadBundle_B1/1/5.516s，实际本地4KiB坏Bundle不提交、失败句柄释放、原路径重试成功，原包SHA/委托精确恢复、句柄/bundle0。预期原生错已归档后clear。报告R4-R03c-2_CorruptBundle_TestReport.md。R03已收尾，下一P01细分。

# 2026-10-30 P01a-1世界字通过
最终Labels_C3/3/12.242s，视觉收紧目标/区域.08/.14，墙后5.25e-8、旧GUI.00018143、前景.00163178、字高.20272；atlas实际1次256→1024。报告已更新最终C，下行B为历史。
Labels_B3/3/12.022s，真正深度墙后5.25e-8/旧GUI穿墙.00030009/前景.0026961；源字体SHA/Atlas原生256→1024事件、持有/发现/恢复/材质释放。F263字太小已实图校准不降阈值。报告R4-P01a-1_WorldLabels_TestReport.md。下一a-2危险模型/可见灯面。

# 2026-10-30 P01a-2a危险模型通过
最终Hazards_B5/5/32.079s，Computer/Fan面向Approach、基座/指示条，Native49→20/背向0/图差0，已看实图；A为中间版。报告已更新。
Hazards_A5/5/35.494s：118碰撞/8目标/NavSHA、尺寸/权限/实际模型图，两种渲染对照回归；新动态设备后Native45→20/背向0/图差0。报告R4-P01a-2a_HazardVisuals_TestReport.md。下一灯面。

# 2026-10-30 P01a-2b1灯几何通过
LampAssets_A2/2/1.478s，960角点/UV/朝下/2.2×.16×.4及发光/实例材质/源SHA/保存范围验证；报告R4-P01a-2b1_LampGeometry_TestReport.md。下一b2真实GPU。

# 2026-10-30 P01a-2b2灯GPU通过
LampRender_A2/2/9.010s，实际HDR/开关图像/Native实例事件/视锥距离剔除，1000计算0B。报告R4-P01a-2b2_LampRendering_TestReport.md。下一b3三房实际包绑定。

# 2026-10-30 P01a-2b3灯流送通过
LampStream_A8/8/45.166s，实际包灯GPU/相机/共享最后释放，加光照/PVS/距离/压力回归。九包47cc构建43.766s/恢复scope通过。报告R4-P01a-2b3_StreamedLamps_TestReport.md。下一a-3设置。

# 2026-10-30 P01a-3a显示存储通过
DisplayStore_A10/10/1.316s，严格格式/原子备份/坏档未来版保留/slot1不变。报告R4-P01a-3a_DisplayStore_TestReport.md。非原生窗口验收。

DisplayTransaction_A13/13/1.753s，假后端确认/超时/取消/失焦/漂移/异常/回滚失败显式状态；报告R4-P01a-3b_DisplayTransaction_TestReport.md。下一真实Unity后端与面板。

DisplayUI_A2/2/5.883s，真实Unity面板按钮/模态拥有权/失焦、假显示后端确认与还原；Editor显示未改。报告R4-P01a-3c_DisplayUI_TestReport.md。Windows原生显示仍待。

PlayerScope_A2/2/2.491s，原生Player/Graphics/Quality＋磁盘/dirty正常与异常恢复；报告R4-P01b-1_BuildScope_TestReport.md。下一正式新Windows构建。

PlayerScope_C最终3/3/2.674s含真实原生回收，按路径重载管线、UnityConnect快照；首次构建e4fa恢复失败保留F267/F268。Build_9064最终Succeeded/0errors/29.911s，四设置/Lightmap/保存scope恢复true，新输出约248MiB、仅新模型副本移除；报告R4-P01b-2_WindowsBuild_TestReport.md。Windows运行未验收。

Windows_A startup/write通过，display中间结果不能当独占稳定成功（F269），read在D瞄准失败（F270），未继续route/alternate/performance。稳定显示修订后StableDisplay_A16/16/10.925s通过，含14事务＋2面板；新构建与Windows重测待。

DRoute_E2/2/22.780s，初始与读档后的真实D导航/扫描通过，各成功收费1次/货物World；报告R4-P01c-1_DRoute_TestReport.md。Windows让路必须真实Walk，不用Editor位置设置冒充完整路线。

Gameplay_D write/read两个独立Windows进程通过，44.028/46.029s，实际F5/F9混合状态、坏槽拒绝无World变化、继续完成3件12分主动结算；报告R4-P01c-2_WindowsSave_TestReport.md。主/另一返回路线与性能尚待最终结果。

Gameplay_D route/alternate通过60.042/69.038s，两条真实步行/扫描/交付/主动结算/重开，报告R4-P01c-3_WindowsRoutes_TestReport.md；performance无有效GPU而失败，不计达标。显示点击回执/墙钟预热修订仅涉及验证DLL，646项产品签名已捕获，后续最终构建须实际对比。



## 2026-10-30 最终交付状态

| 项目 | 最终实际结果 | 报告/证据 |
| --- | --- | --- |
| P01b-2最终构建 | 9e6d Succeeded/0错误/32.576秒，设置/光图/保存scope精确恢复 | R4-P01b-2_WindowsBuild_TestReport.md |
| P01c Windows流程 | startup、write/read、主/替代路线exit0；坏槽拒绝无World变化，实际结算与重开 | R4-P01c-2_WindowsSave_TestReport.md；R4-P01c-3_WindowsRoutes_TestReport.md |
| P01a-3原生显示 | Display_F实际点击/稳定/确认/取消/15秒超时通过，本机独占可用；失焦同产品入口注入 | R4-P01a-3d_WindowsDisplay_TestReport.md |
| P01b-3性能采样 | Perf_F 1620/1620有效唯一CPU/GPU；三画质三区，非目标机证明 | R4-P01b-3_Performance_TestReport.md |
| 产品一致性 | 646项源码/资源/游戏DLL/本地包SHA零差异，发行前再次逐项验证 | Product_Final_F.json；DeliveryManifest_G.json |
| 完整回归 | FullNext_C188/188、零失败/零跳过，36.685秒 | R4-Final_Regression_TestReport.md |
| 保护与预算 | 3610旧文件仅3项授权AA必要例外，约17.74GiB/20GiB | Evidence/R3R4/Audit_Final_H.json |
| 工作区还原 | 用户最新S3干净布局、原VC/PC1/AA4、Graphics/Quality整体原SHA、播放选项/dirty恢复 | Evidence/R4-Final/FinalWorkspace_G.json及恢复收据 |
| 交付与停止 | 新Windows目录/README/许可/清单已完成；R3仍No-Go，目标硬件/外部体验待测，不进R5/R6 | R4_Final_Delivery_TestReport.md；R4_Delivery.md |

此处为当前结论，上方“下一/未跑”等行是历史过程，失败轮次保留，不覆盖为成功。


## 2026-10-30 试玩反馈01

共享Volume曝光.25→1/contrast5→2，Visibility_C7/7、九组三区/三画质A/B通过；Windows32dea构建/启动/完整回收路线通过。646项产品SHA仅Volume变化；原R4包484文件完整校验归档；原S3/VC/PC1AA4与原生/磁盘/dirty精确恢复。报告R4_Feedback01_Visibility_TestReport.md。研究候选见Feedback01_ResearchAndNextSteps.md，未批量导入或改变玩法/AI路线。
