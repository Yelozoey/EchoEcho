# S3-G01 兼容性Spike实际测试报告

2026-10-26，08:44–09:44，Unity6000.6.7f1 / Windows11 x64 / D3D11 / URP17.6.0普通Forward、内部HDR→SDR。开发机i7-12650H / RTX4060Laptop7956MB / RAM16GB。**12项兼容工作流通过、守卫Unity测试5/5；选择B路线的ORT CPU，允许进入A01。不是S3整体或自然语言玩法验收。**

## 决策与模型

Barracuda3.0.2实际安装并编译后，完整Q4模型Convert抛出OnnxImportException：Unknown type Trilu。完整堆栈保留[BarracudaActualImport.json](../Evidence/S3-G01/BarracudaActualImport.json)；根据已确认B路线切ORT1.31.0 CPU，未升级Unity/URP/既有依赖，未修改第三方算子。结论仅针对所测路径，非穷尽所有Barracuda模型。

候选1社区INT4未达原始FP32数值容差；候选2动态INT8输出偏离参考；候选3用官方ORT工具从固定FP32 ONNX转换为block32对称weight-only INT8，168个主要矩阵节点。accuracy_level=4选择CPU内部INT8激活计算，embedding/共享词表头/norm等保留浮点；不是FP16或全模型FP32冒充量化。只在lm_head之前Gather最后隐藏状态，注意力/KV和权重不改；短/1536长输入三步logits与未裁剪图最大绝对差均0。

模型965074339字节（约920.37MiB）；SHA256 `6d04df6470b91cc7a977be0d8eae87e27f3400ef654940ed85bd80e315411055`。原始Qwen revision `7ae557604adf67be50417f59c2c2f167def9a775`，HF onnx-community转换revision `22942cb7d7ba4cc81bb4673549ca4d4614469b5e`；原始Apache2许可证已留存。社区ONNX不称为Qwen官方ONNX。工具版本/源URL/全部下载SHA见Evidence中的DownloadedAssets、SecondCandidateDownloads、OrtInstalledFiles及转换记录。

## 独立可执行测试及实际结果

| 用例 | 实际步骤与结果 | 状态 |
| --- | --- | --- |
| G01-01 | UPM3.0.2编译、官方NuGet1.31.0、本机Win64构建成功，既有包版本未变；插件限定Windows Editor x64/Win64 | 通过 |
| G01-02 | 实际图168个bits8节点、UINT8打包存储；原始权重抽样一致、转换SHA/体积/许可校验 | 通过 |
| G01-03 | 后台Owner真实执行Add图，输入1/2加2/3得3/5 | 通过，仅证明小图 |
| G01-04 | 真实模型prefill189tokens；FP32参考cosine0.99778–0.99894、RMS0.13864–0.20294、top10均10/10；保留原门槛0.98/1.0/5。Windows相同图logits最大差0 | 通过 |
| G01-05 | 5轮各三步真实自回归/KV，73594→2236→198，与Python和原始模型一致；这是Markdown前缀，未当作合法JSON | 通过，质量另测 |
| G01-06 | 创建/执行/释放均Owner；Windows所有15次Run线程ID均非主线程1，探针类无Unity引擎引用 | 通过 |
| G01-07 | 1536tokens/三步，KV首步[1,2,1536,64]、logits[1,1,151936]；外部Windows私有内存增量峰值2205552640字节≈2.05GiB；prefill7.552秒 | 通过 |
| G01-08 | 官方与转换tokenizer逐ID一致；中文实际序列105043/74994/3837解码“你是Echo，”，与参考一致；EOS151645 | 通过，仅探针 |
| G01-09 | Windows模型5次创建/运行/释放；缺失模型B01–B05各独立后台失败并释放，无主线程Join/Thread.Abort | 通过 |
| G01-10 | 可见1920×1080 Win64，5轮实际原生DLL/真实模型，进程21284正常exit0，25.04秒；模型从项目本地探针缓存读取 | 通过，非最终随包加载 |
| G01-11 | 五区灰盒副本实际渲染与推理并行：1200帧全部CPU/GPU有效且时间戳唯一，Profiler原始99096952字节；截图已查看无黑图/错误控制台 | 通过，当前山谷视点 |
| G01-12 | 自动核验12项；S1/S2保护767项全部一致，资源约6.7GiB量级低于20GiB；原失败全部留存 | 通过 |

最后通过`Tools/MCP/module_tests.py`真实门禁执行`EchoStation.AI.Editor.Tests`：5/5、0失败、0跳过、6.131秒，job `fd34aeb9dabc483ea8f52af6d731a27f`。包括实际最终模型后台/数值与释放、缺权重、Windows插件隔离、独立导航和量化来源。不要把5个Unity测试与12项跨环境工作流相加冒充唯一用例数。

可复跑：`Tools/Stage3/run_unity_probe.py`（新tag）、`sample_process.py`、`test_failure_release.py`（新标签），专用G01菜单构建后用`run_g01_window.ps1 -Evidence <新目录>`；最终`validate_g01.py`验证实际证据。后续生产测试继续使用module_tests的场景/Prefab、Console、RenderSafety门禁。

## 性能/保护与故障

Windows_B01原始1200帧P95：主线程2.638ms、渲染线程0.595ms、GPU0.916ms、含60FPS节拍的CPU帧16.714ms。此处只有三token的短推理，不是完整回复P50/P95，也不是全流程60FPS验收。进程全部内存峰值3025731584字节，不能把它和Editor中的AI增量混为同一指标。

Windows_A01虽然exit0/数值正确，但Profiler开关顺序错误，没有.raw，因此旧轮不算性能通过；Windows_B01修复后保留有效raw。原长图内存超3GiB约76.6MiB，裁剪词表头后复测通过。所有初失败/连接回执、下载超时、异常枚举误断言、插件平台残留等见F112–F118。既有Services未关联、MCP UAC/System.Windows.Forms及未启用Panini剥离警告沿用历史记录，不连接云服务或改S2渲染来掩盖。

末次原生检查Editor空闲、dirty资产空、FrameTiming getter/disk均false/0、Profiler false且无已载捕获；原始S2场景保持干净，专用场景/导航是独立副本。S1/S2保护767项再次一致。

## 限制和下一模块

本机并非最低i5-8400/GTX1060 6GB/16GB中档1080p60或推荐i5-12400/RTX3060 6GB/16GB高档1080p60，不能据此声称目标实机达标。全部模型调用读取本地文件；没有禁用全机网络，没有把静态无云调用当成真实断网测试。

G01只是兼容Spike，尚无生产tokenizer、完整生成/JSON约束、调度/取消、游戏权限/事实守卫、LLM对话路由和独立S3存档。100条质量、100条对抗、30分钟/60请求、LLM on/off三路线及最终独立包均待后续模块。下一步A01正式固化模型包、SHA/许可和安全加载，继续逐模块测试归档，不进入S4。
