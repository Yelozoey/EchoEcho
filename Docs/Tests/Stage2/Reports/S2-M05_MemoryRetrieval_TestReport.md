# S2 M05 相关片段检索测试报告

2026-10-25通过。Unity6000.6.7f1、Windows11开发机，普通Forward/内部HDR到SDR/D3D11、新鲜干净场景与Console门禁通过；目标i5-8400/GTX1060 6GB及i5-12400/RTX3060 6GB的1080p60FPS基准沿用总文档，此测试不作渲染性能验收。

执行 `python Tools/MCP/module_tests.py start --evidence Docs/Tests/Stage2/Evidence/S2-M05 --label A-SearchAndMemory --assembly EchoStation.Memory.Editor.Tests --group ^EchoStation.Tests.Memory`，随后poll同标签。**48/48通过，0失败/跳过，框架0.7443336秒**；M05新增10项，数据/JSON/文件回归38项。

实际用例：中文同义词要求问题和已知记录同属主题；目标ID大小写归一且不误匹配更长ASCII标识；索引别名不暴露未发现记录；同组同义词只计一次；平分排序稳定；200字符输入/非法索引明确处理；外部数组与后续写入不改变固定快照；三类及已知危险目标均可检索；无关关键事件不强塞历史；显式已知密码/标记可查且无原文接口。

已知密码或标记精确命中30分；这只是记忆相关性，实际对话是否复述密码仍由已确认的D05显式询问/提示规则控制。没有分词或向量模型、网络、GameObject操作或Prompt组装。

证据：[原始结果](../Evidence/S2-M05/A-SearchAndMemory.json)与启动回执/新鲜门禁。本模块无新故障，当前5/30，进入M06时间降权与上下文预算。
