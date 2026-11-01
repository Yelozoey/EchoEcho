# S3-J02b1 只读知识问题API与S2回归

2026-10-27，Unity6000.6.7f1、Windows开发机i7-12650H/RTX4060 Laptop/16GB、D3D11普通Forward内部HDR/SDR；渲染/干净场景门禁通过。最低i5-8400/GTX1060 6GB/16GB中1080p60、推荐i5-12400/RTX3060 6GB/16GB高1080p60仍须对应硬件实测，此纯数据模块不作性能证明。

ScriptKnowledgeResponder增加AnswerKnown：仅四种问题类别、有界200UTF16且严格Unicode；同一内核处理已知摘要、密码/掩码、三级提示和版本重置，不返回执行动作、不写记忆。原Answer入口保持并调用同一逻辑。

实际：Knowledge_A为17/18，非法代理项经NUnit属性被替代导致夹具预期错误，F146留档；B被已知IAP空响应Console门禁阻止（未启动，F138）。修夹具并保存/核对已知外部错误后，Knowledge_C作业0e75315b5fe04bec9f5516475be17b31：18/18、0失败/跳过，0.0797498秒。

执行统一入口`Tools/MCP/module_tests.py start/poll --evidence Docs/Tests/Stage3/Evidence/S3-J02b1 --label Knowledge_C --assembly EchoStation.Dialogue.Knowledge.Editor.Tests --group '^EchoStation.Tests.DialogueKnowledge'`。含原S2知识11项全部回归、新API7项（非法/null/长度/类别、运行时非法代理项、两入口密码一致/摘要遮蔽、全部类别无动作无写入）。证据A/B/C原始文件均保留。b1验收通过，下一叶b2才绑定授权与可信答复。
