# F01-V01 场景亮度修正

2026-10-30，用户实机反馈“单一且暗，亮度提高一点”。执行计划见Docs/SalvagePrototype/Feedback01_VisibilityPlan.md。

改动只涉及新回收版共享探索Volume及其生成参数：postExposure +0.25→+1.0 EV；contrast 5→2。保持ACES、饱和度-30、颗粒/色差、原烘焙和灯光数；未增加实时灯或渲染pass。旧Profile/生成器字节已备份，旧Windows484文件逐项SHA验证压缩为R4-Feedback01/OriginalWindows_R4.zip，保留旧清单。

Visibility_C实际7/7、零失败/跳过、17.225秒：新三区×三画质A/B用例1项，既有Post2项、Rendering4项。均经module_tests.py新鲜安全门禁；Console0。不是新一轮188全量回归。

九组1080pA/B均通过平均线性亮度至少增加10%、暗像素比例不增加、白色截断比例增量≤1个百分点门槛。为隔离参数，临时停用随机颗粒/色差/SSAO/动态灯/风装饰/局部雾/对焦；共享资源/临时实例与场景退出后恢复。此测试使用保留的单场景参考，与正式流送场景共用同一Volume；不能替代Windows正式场景验证。

| 中画质区域 | 线性亮度均值前→后 | 线性亮度<0.01像素比例前→后 | 白色截断比例前→后 |
| --- | --- | --- | --- |
| 泊位 | 0.007932→0.018559 | 80.65%→61.45% | 0→0 |
| 储藏 | 0.006596→0.015386 | 82.76%→64.66% | 0→0 |
| 检修 | 0.010528→0.023510 | 77.32%→53.07% | 0→0 |

已实际查看泊位前/后、检修后画面，地面/墙体轮廓更可辨，仍保留暗处与冷蓝灰基调。这里的阈值/均值来自隔离观察，不是全画面艺术品质或显示器校准验收。九组记录与18张图见Evidence/R4-Feedback01/Brightness_e91be1123e844410b716398b0859a624.json。

失败保留：首次编译缺少Rendering命名空间（F283），A外部变更待刷新无测试启动，B7项6过1因夹具错误组件层级失败（F284），修正后C全通过，未降低门槛。

Windows更新构建32dea536306642369283d5cbf1d3106b实际Succeeded、0错误、34.119秒；Player/Graphics/Quality/UnityConnect原生JSON/磁盘/dirty及光图/SDK保存scope全部精确恢复。2条已知警告为Door_Frame_Square未来预烘焙碰撞提示、Unity Services未关联，没有启用云服务或改变模型路线。

Feedback01_D实际可见Windows启动与完整A+B+D路线exit0，分别6.058/60.046秒；包括扫描、实际步行、携带/交付、3件12分主动结算和重开。D3D11、1920×1080、专用Medium普通Forward、内部HDR/SDR、UseLLM=false、输入还原均通过，运行日志无异常标记。已查看真正Windows Startup.png，门洞/墙面/地面更可辨。运行原始结果/图片保留于Evidence/R4-P01c/Windows/Feedback01_D，实际运行独立槽归档在本模块RuntimeValidation。

对原646项产品源码/资源/游戏DLL/Bundle逐项SHA比较，仅共享Volume资产变化，游戏DLL与本地Bundle相同，见ProductDelta_D.json。没有重新测试显示模式/完整188项或目标硬件性能；本轮不据此新增性能达标声明。

最后恢复原S3干净场景/选择/播放选项、VC默认及PC管线、AA4；Graphics/Quality/EditorSettings完整JSON/dirty与磁盘字节一致，NativeRestored_F.json留存。恢复过程C#片段缺少扩展方法命名空间、URP自动修改AA已记录F286，前置拒绝没有覆盖未知字段；最终精确核验通过。

更新版发行清单为本模块DeliveryManifest_F.json，旧R4清单继续对应OriginalWindows_R4.zip。资源预算/3610旧文件最终审计见Evidence/R3R4/Audit_Feedback01_Final.json。场景装饰/HUD素材和玩法参考研究见Docs/SalvagePrototype/Feedback01_ResearchAndNextSteps.md，本模块没有把候选素材当作已导入，也没有进入R5外部试玩或新增玩法。
