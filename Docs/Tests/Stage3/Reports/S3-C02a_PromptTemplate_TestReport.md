# S3-C02a 原样正文与1536输入预算

2026-10-26，复测16/16通过，2.262秒，job `6af5f96b8bf04e7a84561dc1e3206662`，C02a_B；首轮A为15/16、交付文本CRLF失败F124保留。正文内容未改，改显式UTF8/LF输出后通过。a通过后才接后台b，C02整体尚未完成。

环境Unity6000.6.7f1/Win x64/D3D11/1080p普通Forward内部HDR-SDR、i7-12650H/RTX4060Laptop/16GB；最低i5-8400/GTX1060 6GB/16GB中档1080p60及推荐i5-12400/RTX3060 6GB/16GB高档1080p60不在本数据用例验收。干净场景/Console/实际RenderSafety通过。

入口module_tests.py，assembly EchoStation.AI.Editor.Tests，groups PromptPreparationTests、ContextBudgetTests。新增7项加9项C01预算回归。

正文与DevelopmentPlan固定块、交付PromptTemplate.txt逐字匹配，UTF8/LF SHA256 `78a7640483b9619502dcb9b7bb9db7a84bab2abf6115d5e51a5057792792bd5d`。10组来自固定本地上游tokenizers/官方apply_chat_template的golden IDs全一致；普通输入173–371 tokens。role文本不能增添控制token（实际Start3/End2/EOS0）；占位符字串不递归替换；只读结果列表不能修改。

真实最大预算用例：200字符输入、8个长ID/罕见Unicode名称目标与3条已检索数字摘要；移除完整记忆3→0、目标8→4，完整输入1333，预留生成256，合计1589≤2048。有效目标保持排序前缀且ID完整。普通请求保持全部上下文并同时满足记忆512/1200与完整1536。

原始数据Evidence/S3-C02/PromptGoldens.json、Budget_*.json及A/B测试回执。generate_prompt_goldens.py仅本地读取固定权重目录，不下载/上云。输出JSON质量F120尚待J01/T01，本叶未运行真实生成。
