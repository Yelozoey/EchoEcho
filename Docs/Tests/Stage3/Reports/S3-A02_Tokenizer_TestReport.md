# S3-A02 真实tokenizer模块测试报告

2026-10-26。A02a先8/8独立归档，再开发A02b；**最终A02b与受影响A02a共16/16、400条完整token IDs与解码逐项一致，允许进入A03。**

环境Unity6000.6.7f1、Windows11 x64、D3D11/1080p、普通Forward内部HDR/SDR，i7-12650H/RTX4060Laptop7956MB/16GB。目标基准保持i5-8400/GTX1060 6GB/16GB中档1080p60及i5-12400/RTX3060 6GB/16GB高档1080p60；本模块不测目标帧率。

## 实现

正式tokenizer直接读取A01已校验的原始7MB词表/151387合并规则。NFC、真实Unicode预切分、完整GPT字节映射、最小堆按rank/最左位置执行BPE；相邻节点版本防止使用过期合并项，避免长片段二次方扫描。解码区分基础字节、14特殊token和8普通added token，提供与上游一致的UTF8替换解码及严格UTF8错误路径。

EncodePlain与官方tokenizers0.22.2的`encode_special_tokens=True`对照：14个特殊标记不自动提升，8个普通added保持官方ID，没有工具API权限。可信角色/EOS通过强类型TrustedTokenSequence显式加入；相邻文字先合并再NFC，未知分隔枚举拒绝。游戏没有Python、云端或运行时下载依赖。

## 独立用例/实际结果

`module_tests.py`最新job `f4aaeb86f4084affb17ca628a25c1683`，16/16、0失败/跳过、5.893秒。原始结果/新鲜场景与RenderSafety/数据结果保留于Evidence/S3-A02；A02a先行报告另存，不重复加总其重跑次数。

| 范围 | 结果 |
| --- | --- |
| 400条NFC/预切分 | 与同版本原始HF引擎逐片一致，包含补充平面和全部22标记 |
| 400条完整BPE | IDs逐项一致、解码逐字符串一致、严格UTF8往返等于上游NFC文本 |
| 256个单字节 | GPT字母表往返及上游逐ID不完整/非法UTF8替换结果一致 |
| added tokens | 全部22的原样/skipSpecial解码与上游一致，8普通token未被误跳过 |
| 玩家角色注入 | 字面im_start/im_end/endoftext不能生成151643–151645角色/EOS ID，文本可无损解码 |
| 可信包装 | 明确系统/user/assistant包装的完整IDs与G01原始官方模板189tokens完全一致 |
| 规范化边界 | 相邻“e”+组合重音等于“é”；孤立UTF16、超长、未知分隔符/ID拒绝 |
| UTF8边界 | 单个不完整中文字节在strict模式抛DecoderFallbackException，未静默当完整台词 |
| token计数/长片段 | 1536个数字确为1536tokens；8192字符单词完整往返且耗时<2秒 |
| 配置门禁 | 不支持的normalizer/regex/dropout/角色标记等拒绝；全部实际资产库存验证 |

400项数据用例与16个测试方法范围有重叠，不相加冒充独立总数。参考生成器`Tools/Stage3/generate_tokenizer_goldens.py`、原始tokenizer SHA和Unicode源SHA可查，实际词表不改变A01发布版本或模型SHA。

S1/S2保护767项全部一致。当前纯数据/文本算法没有场景或GameObject访问；完整推理层仍在后续模块，不能把tokenizer通过当LLM质量或最终JSON通过。本模块没有产品失败；前置诊断问题见F119。下一步A03验证ONNX dtype/shape/attention/position/KV与多长度参考，再由A04正式生成，C02正式组装原样Prompt。
