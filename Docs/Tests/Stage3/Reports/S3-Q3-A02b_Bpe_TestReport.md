# Q3-A02b 动态词表编码/解码通过

2026-10-27，A02a资产8/8通过后，只把QwenByteBpeTokenizer固定151665字节/特殊标记缓存改成按已验证连续ID数量分配；新profile151669、旧profile151665，真实rank BPE/Unicode表/算法不变。

本地tokenizers0.22.2从已验官方Qwen3 tokenizer生成400条参考（NFC/中英/多语种/空白/控制字符/组合emoji/补充平面/26标记/固定种子随机）。encode_special_tokens=True使数据中14特殊标记保持字面，12普通标记保留官方识别；生成器不加载模型、不联网、不重写历史Unicode表。参考496594B，SHA37ed938e5eb6d7bdc016da2974ad421aa74d41cc1caeabb96f6d3f2cf75343c1。

Unity6000.6.7f1，作业ba17eb2589fb4bc2bae06b70a9cc2a0d实际16/16通过，1.813655秒。新8项：400条逐ID/解码/严格NFC往返、256逐字节解码、26标记skip标志、数据不提升任一特殊标记、四个新普通ID、非法ID/UTF8/UTF16、实际1536数字token/长片段/字符预算、相邻可信Text规范化。旧BpeTokenizerTests8项（含其400条参考）同时通过，仅读取旧词表，不读取/推理旧权重。

测试前唯一Console错误为已知Unity IAP服务联网错误，保存PreflightConsole_A后核对确为该前缀再清理；未忽略错误、未修改网络，后续常规门禁有效，见F138。证据Evidence/S3-Q3-A02b/TokenizerGoldens.json、Bpe_A-Job/Bpe_A.json、安全记录及Ids_639286624877794632.json。

本叶不代表正式消息包装或推理复测已通过。下一叶可信Qwen3非思考包装逐ID对照，然后才接入实际上下文与新后端；正式生成不能继续屏蔽四个新增普通标记冒充分词完整实现。
