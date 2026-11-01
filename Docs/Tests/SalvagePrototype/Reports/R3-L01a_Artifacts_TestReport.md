# R3-L01a 冻结输入／评分器／探针准备

2026-10-29，Artifacts_A独立4/4通过、0失败、0跳过、1.412秒。Unity6000.6.7f1／Windows开发机，实际Qwen3 tokenizer；此报告没有执行推理，不能当作模型或产品验收。

新D60／H100数量、意图分布、160唯一编号、集合无相同输入、≤200字符、所有夹具可构造合法可信状态、冻结逐文件SHA通过。评分器明确拒绝错误follow、空泛建议及重复字段，正确建议才计原始任务正确；不调用脚本纠错评分。未知集合不生摘要、手持／交付E不参与高分建议。

候选1英文释义／官方非思考System＋8可信示例，D60全部实际tokenizer包装检查通过：最大1082输入tokens，完整256输出预留，总≤2048；检索≤8条1200字节，高分建议真实首选cargo_e仍保留。问候等无关检索可为0，非丢失所有记忆错误。旧默认v5未改。

原始Evidence/R3-L01a/Artifacts_A.json及Job／SceneSafety／RenderSafety；候选预算R3-L01/Static/Candidate1_Budget.json；冻结Inputs/Freeze.json。D60 SHA2a3c7e55ad52…，H100 SHA98fb9c1f5fec…，Candidate1文件afa921c273e3…；正文31b102d25d03…，示例7fc0069ce226…。

探针一次性ActiveRun入场后消费，独立Run目录不可覆盖／重复，最多3诊断和1独立集；无配置时普通回归忽略模型用例。真实探针复用既有后台Owner／4线程／20秒／300ms冷却，输出原文与安全层分列，执行结果在L01b另归档；代码编译不证明真实后台已运行。
