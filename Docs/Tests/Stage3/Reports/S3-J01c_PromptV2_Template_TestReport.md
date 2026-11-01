# 已批准Prompt v2：模板与预算复测

2026-10-26。用户明确批准后应用，运行时常量和交付文本均为UTF8/LF正文SHA256 `5b47baccb0048d5ed970c74dfd5f69a62443fd155112788e3eb2b5ca89074bb3`。原版保存在Docs/Stage3/Archive/PromptV1.txt，历史C02 goldens与J01失败不覆盖。

守卫入口Tools/MCP/module_tests.py，Evidence/S3-J01/PromptV2，标签V2_Template_A，job `c9007ca3a745492da7bce6653b0309af`。PromptPreparationTests与ContextBudgetTests共17/17通过，3.014秒；干净场景/Prefab、空Console错误、实际普通Forward/内部HDR→SDR/D3D11绑定检查通过。此前完成生成进程后核对新Context和测试DLL时间戳，未运行旧程序集。

10组固定本地上游tokenizer和官方apply_chat_template对照全部IDs匹配，普通输入525–723 tokens；正文逐字匹配已批准草案，旧版仍逐字匹配总文档原始规范与原SHA。占位符不递归替换，输入伪角色标记不生成可信特殊token。真实压力输入200字符、8个长ID与罕见Unicode名称：完整记忆3→0、目标8→3、输入1449tokens；预留256后1705≤2048，未扩大1536输入/512记忆/1200字节上限。完整裁剪目标ID，无半条摘要或半个ID。

独立Python严格解析61/61通过，PythonParser_A.json；S1/S2保护767/767一致。上述均为模板/预算/解析验证，**尚未据此声称真实模型语义改善或100条验收通过**。通过后才运行真实六类输出及采样回归，另行记录。

环境Unity6000.6.7f1/Windows x64/D3D11/1080p、i7-12650H/RTX4060Laptop/16GB。最低i5-8400/GTX1060 6GB/16GB中档1080p60与推荐i5-12400/RTX3060 6GB/16GB高档1080p60不在本数据测试验收范围。关联F124/F125，原失败证据保留。
