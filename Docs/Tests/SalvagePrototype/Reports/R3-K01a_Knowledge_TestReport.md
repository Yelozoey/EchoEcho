# R3-K01a 只读已知快照与建议排序

2026-10-29，独立模块通过16/16，0失败，0跳过，0.951秒。Unity6000.6.7f1／Windows开发机；EditMode纯数据契约，不是实际导航、模型语义或目标硬件验收。

实现SalvageNext/Domain/SalvageKnowledge.cs：只接收传统层观察，已知摘要只取八项固定目录；未知位置禁止进入快照。就近／高分／轻量依N05排序，不更改显式偏好；手持／交付／未知／无路／危险不进入货物建议，超限设备不在扫描目标目录。

实际用例覆盖三个完整排序、未知摘要与位置隔离、排除集合、空集合、重复／null／无穷／NaN／负距离、类型与可达状态不匹配、非法会话／进度／偏好、不可变集合及源数组无别名。初次编译F223已修复后才运行。

入口Tools/MCP/module_tests.py，assembly EchoStation.SalvageNext.Editor.Tests，group RecoveryKnowledgeTests；原始结果Evidence/R3-K01a/Knowledge_A.json及对应Job／SceneSafety／RenderSafety。启动时空保存宿主、Console0，默认／PC／有效项目管线为S2普通Forward，内部HDR／SDR／D3D11。

保护基线3610文件已建；Start_A审计全部一致，15.948GiB/20GiB。临时测试管线将在本轮最终还原，不把此处临时绑定当最终工作区还原。下一步K01b验证传统发现／真实tokenizer预算；本模块不证明LLM质量。
