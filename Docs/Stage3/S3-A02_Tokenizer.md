# S3-A02 真实tokenizer细分实施与测试

2026-10-26。前置A01已20/20和S2 JSON12/12通过并归档。原始固定tokenizer实际为NFC规范化、Qwen正则Split、ByteLevel、151643基础词表/151387 merges、22个added tokens；不得假定为无normalizer或以字符数代替token。

按依赖分两个叶模块，分别运行独立测试并归档后整合：

1. **A02a：资产/UTF8字节字母表/真实预切分。** 校验BPE及normalizer/预处理配置；从同版本官方HF tokenizers0.22.2正则引擎提取Unicode L/N/空白范围到编译数据，避免Unity .NET Unicode版本差异。NFC后建立Unicode标量到原UTF16偏移映射，正则在等类别ASCII影子串上匹配，返回原文片段并做标准GPT byte映射。保留CR/LF、ASCII空格与收缩词含long-s的分类语义。输入拒绝孤立代理项；正则有100ms上限，文本最大32768 UTF16字符。独立：256字节往返、400条固定多语种/标点/空白/组合字符/emoji/补充平面预切分golden、非法输入/配置。
2. **A02b：真实rank BPE、解码和可信特殊token。** 对预切分逐片按原merge rank合并；严格词表ID/缺失合并拒绝。普通玩家/记忆文本把特殊标记作为字面字节编码，不提升为聊天角色token；可信模板包装通过明确API插入官方特殊ID。独立：同一400条上游token IDs逐项一致、往返/NFC、特殊token安全、无效ID、部分UTF8、EOS和上下文边界；A03/A04再做张量/生成，C02再做原样Prompt。

生成器只在隔离Python使用原始HF tokenizer作参考/生成数据，游戏不得依赖Python进程或服务。保持模型、tokenizer资产SHA与A01相同；新的Unicode表是可审查的编译源数据，记录工具版本与哈希。无自动在线下载/远程代码。

实际22个added tokens中14个special=true、8个special=false。普通编码与官方`encode_special_tokens=True`逐ID对照：禁用14个特殊标记的自动提升，8个普通added tokens仍保持官方识别行为；它们不能获得工具执行或聊天角色权限。C02只显式插入可信im_start/im_end，不让玩家文本产生角色分隔ID。预切分组件golden在added-token识别之外独立测试，整条编码golden再验证8个普通added tokens的分段。
