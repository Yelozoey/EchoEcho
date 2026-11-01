# Q3-P01 正式v5上下文准备通过

2026-10-27，正式后端数值/生命周期通过后，新增Qwen3专用v5正文/单次填充/准备层，保留旧默认模板。源正文canonical UTF8/LF SHA f3524a85ad232bd4077da4237280e778d5b6cc3bc0fcc95d8c069480973bcbe1；使用真实ContextAssembler和已通过Qwen3ChatTemplate，完全C#分词与包装。

Unity6000.6.7f1，作业8f658cdc85d04ebba4f4b2aa29df26f9实际16/16通过，1.927870秒：新8项覆盖正文/SHA、18条已选官方输入全ID一致、占位符不二次替换/角色不提升、完整记忆再完整目标裁剪、实际8/1200UTF8/512tokens/1536输入/256输出/2048预算、密码脱敏、只读返回、空依赖拒绝；旧PromptPreparationTests8项保持通过。仅读取词表和纯数据，不加载旧权重。

证据Evidence/S3-Q3-P01/Prompt_A-Job/Prompt_A.json、安全和Budget_639286646411309458.json。输入/ID不截断，低优先级目标按完整尾项删除，原快照保持；未知事实/密码权限守卫仍在J02，分词安全不等于语义免疫。

下一叶组合新catalog/加载器/真实准备/后端/TokenGenerator，要求显式JSON约束和快照，不允许raw-ID旁路。必须在真正C#准备路径上重跑18条与独立100条，不能直接把R04固定HF输入结果记为正式运行时已合格。
