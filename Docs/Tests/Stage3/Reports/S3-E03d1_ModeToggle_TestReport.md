# S3-E03d1 Tab最小模式开关

2026-10-27，Unity6000.6.7f1 / Windows / D3D11普通Forward内部HDR/SDR统一门禁通过；开发机i7-12650H/RTX4060 Laptop/16GB。最低i5-8400/GTX1060 6GB/16GB中1080p60、推荐i5-12400/RTX3060 6GB/16GB高1080p60非本UI测试结论。

StationControls只新增只读Canvas访问器；S3专用LlmModeToggle在Tab面板右上角显示本地AI开/关（脚本），默认关闭且未就绪不能切；仅面板打开可切。状态文字缓存更新，原标题/目标/保存/分辨率行为不改，不实现S4正式设置。

Toggle_A作业be0f7eb664734a3b8ca0fc9fb0c49f7d：8/8、0失败/跳过、5.4551877秒（新UI2项+生命周期6项）。实际Canvas按钮事件验证开启/关闭、隐藏不可切、故障三次后隐藏标题更新、禁用驱动不可开启、重复Configure无重复按钮、真实字体行高与标题实际字宽不重叠。控制Factory明确注入，无新推理计数。

入口`Tools/MCP/module_tests.py start/poll --evidence Docs/Tests/Stage3/Evidence/S3-E03d1 --label Toggle_A --assembly EchoStation.AI.Execution.Editor.Tests --group '^EchoStation.Tests.AIExecution.(LlmModeToggleTests|S3DialogueDriverTests)'`；regression_host Toggle_A原布局/Play选项/管线已还原。d1通过；d2再自动挂接并回归，真实指针/Windows完整显示仍由后续V01验证。
