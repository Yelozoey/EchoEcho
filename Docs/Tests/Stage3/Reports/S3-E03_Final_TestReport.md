# S3-E03 UseLLM路由、故障退化与游戏接入验收

2026-10-27，**E03工程通过，S3为16/24**。仅Qwen3-0.6B；S1/S2资产和历史构建767保护SHA一致，未处理Qwen2.5，不进入S4。

逐叶均在通过/实际报告后整合：a1路由5/5；a2新输入与S2执行回归8/8；b1可信引用/快照/守卫22/22；b2实际相机/遮挡/跨区/移除/线程4/4；c1会话桥6/6；c2生命周期及回归12/12；记录目标兼容先4失败复现再97/97；c3真实Qwen3六意图1/1（6条全部结构/原始意图/目标/执行正确）；d1模式UI及回归8/8；d2自动挂接与回归9/9。

最后Install_A作业76dc520d3f8440b09bb013e01c6a292b，9/9、0失败/跳过、5.9347883秒；入口`Tools/MCP/module_tests.py start/poll --evidence Docs/Tests/Stage3/Evidence/S3-E03d2 --label Install_A --assembly EchoStation.AI.Execution.Editor.Tests --group '^EchoStation.Tests.AIExecution.(LlmModeToggleTests|S3DialogueDriverTests)'`。自动安装唯一Tab模式按钮，重新启用无重复UI/默认无模型创建，现有单消费者/故障/生命周期回归保持通过；regression_host Install_A原工作区/Play选项/管线已还原。

真实链证据`Evidence/S3-E03c3/LiveSix_639286732063362736.json`：主线程1，Owner/Native915，累计Native108次均后台；冷14.389秒，6条准备+生成3.064–6.211秒，模型资源最终释放。六种真实意图与扫描/危险记忆/门状态不变已核验；使用控制定位，非三结局完整路线。控制Factory只在明确测试夹具使用，Player没有测试依赖选择入口；不把其结果当模型质量。

已具备：默认脚本/惰性加载、唯一消费者/无排队、300ms与单请求20秒/冷30秒、黄闪/取消、手动/记忆/区域/进度/读档/结局/卸载取消旧结果、三故障关闭并镜像全局false、失败指令不重放、可信事实/密码/行动及传统执行。下阶段S01需独立场景/导航/存档；最终100条新质量、100对抗、30分钟60请求、三结局on/off、Windows离线/跨进程/Profiler/交付仍未完成，不能称整个S3已交付。

Unity6000.6.7f1、Windows、D3D11普通Forward内部HDR/SDR；开发机i7-12650H/RTX4060 Laptop/16GB。最低i5-8400/GTX1060 6GB/16GB中1080p60、推荐i5-12400/RTX3060 6GB/16GB高1080p60必须对应实机；当前未作此声明。各叶完整原始/首次失败/复测与F145–F149故障在既有目录，当前资源11.568/20GiB。
