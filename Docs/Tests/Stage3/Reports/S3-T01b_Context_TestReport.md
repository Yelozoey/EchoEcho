# S3-T01b 受控游戏上下文与标签隔离

2026-10-27，Context_A作业4b1567cbc0b84d938c38d4490133b2bc：6/6、0失败/跳过、1.5713569秒。当前游戏ID/区域/中文名称/摘要元数据构造valley_empty/cabin_known/lab_known三种受控可见情景，已知线索为0/1/3；不声称玩家实际完成收集路线。上下文工厂只接input/setup，不接评分期望。

100条实际正式Qwen3准备均≤1536输入、+256≤2048、记忆≤8/512tokens/1200UTF8，期望调查/警告目标实际在裁剪后白名单。反事实替换标签/期望ID，Prompt/完整inputIDs/允许目标逐项一致；只送既有摘要、无全文、已知维护码不入Prompt。未知setup拒绝。

入口`Tools/MCP/module_tests.py start/poll --evidence Docs/Tests/Stage3/Evidence/S3-T01b --label Context_A --assembly EchoStation.AI.Editor.Tests --group '^EchoStation.Tests.AI.T01ContextTests'`。Unity6000.6.7f1 / Windows / D3D11普通Forward内部HDR/SDR门禁通过；开发机i7-12650H/RTX4060 Laptop/16GB。纯准备测试不等同真实生成/目标硬件60FPS。b通过，下一叶仅正式Qwen3真实100请求，标签评分和守卫/脚本分开。
