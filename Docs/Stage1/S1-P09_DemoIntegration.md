# S1-P09 小屋探索Demo整合

前置30模块均已独立通过。A：专用ES_Cabin_Demo场景+现有Player/Echo接线、脚本按钮/快捷键、日志互斥、存取入口、分辨率/窗口全屏；实际PlayMode独立验证后B：该新GUID场景独立Lightmap/PVS烘焙及实际图像/原灰盒保护；B通过后C：Windows最终构建，原生输入探索与四模式、真实跨进程存档恢复、实际普通帧Profiler指标/截图、受影响及全量回归与文档。

沿用原灰盒静态几何、8合并体、PBR/导航资产，复制出Demo及专用烘焙目录保护原场景。玩家0,.05,-6；Echo1.5,0,-2；初始follow、UseLLM恒false，无自动读档/退出覆写，F5保存/F9读取。Tab脚本控制面板（非S2自然语言对话），1跟随/2待命/3调查当前选择/4面板警告；指令均走既有ScriptCommands/IntentDispatcher，不直接改设备。保存调查/warn仅恢复wait。

项目最终默认/PC绑定现有专用ES_Gameplay_ForwardHDR以支持扫描波纹，保留原基线资产SHA；R01绑定检查扩展为已验收两种项目管线同一绑定，新增P09明确要求Gameplay版本。普通Forward/D3D11/内部HDR→SDR保持。窗口支持1280×720、1600×900、1920×1080及无边全屏；完整三档画质/UseLLM设置仍S4。

当前机诊断采用1080p、vSync关、60FPS节流、10秒预热后至少900普通帧，报告FrameTiming有效CPU/GPU、Main/Render Thread、GC、Draw Calls/内存及帧时间分位；不将此视为最低/推荐硬件60FPS验收。S1仅小屋探索/隐藏扫描原型，不宣称5区、25–40分钟或3结局完成，不进入S2。

F071修订：NavMeshSurface禁止不同Surface共享同一数据，Demo拥有NavMesh.asset副本并显式绑定；复用其已验收几何烘焙结果，不再直接共享资产引用。编辑器卸载Demo时还原此前帧率/vSync，Player正常60FPS节流。
