# J01 Qwen1.5B官方输入隔离叶

2026-10-26，项目专用Python3.13.12 / transformers4.57.6，本机Windows。只生成输入，不执行模型、不更新生产C#或游戏。依赖下载工具报告中已验证的官方六小文件；所有读取local_files_only=True、trust_remote_code=False。

实际运行Tools/Stage3/prepare_qwen15_candidate.py退出0，CandidateQwen15/CandidateCases.json含固定18条。Prompt SHA为5b47baccb0048d5ed970c74dfd5f69a62443fd155112788e3eb2b5ca89074bb3，与已批准v2相同。逐项核对原预先锁定输入/期望/目标，复用先前候选同一任务正文，六条基础正文已有Unity实际准备的逐字对照；未增加例子或改写问题。

官方ChatTemplate使用Qwen2.5 system/user/assistant角色包装，输入tokens依次581、583、611、609、581、584、584、584、585、616、583、583、583、585、611、610、610、582，全部≤1536且加生成预留256≤2048。最大616，未裁剪玩家输入或目标ID。

实际新旧tokenizer.json的model（含全部vocab/merges）、normalizer、pre_tokenizer、added_tokens、decoder、post_processor六字段深度全同，文件SHA均c0382117ea329cdf097041132f6d735924b697924d6f6fc3945713e96ce87539。输入由新官方HF分词器实际产生，不把“相同”布尔当未来文件免校验凭据；参考/Unity诊断仍需核验当前实际文件。

本叶输入来源、逐字正文、分词规则和长度检查满足。不是新模型的400组C#分词验收、张量/数值、真实输出、语义或预算验收；S3保持11/24，J01未通过。证据CandidateQwen15/CandidateCases.json及Downloads_Small_A.json；转换/实际模型测试按J01_Qwen15_CandidatePlan.md继续。
