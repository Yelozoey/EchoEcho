# S3-A02a 资产/字节字母表/预切分叶模块测试报告

2026-10-26。前置A01已归档。**守卫Unity测试8/8、其中400条上游NFC及预切分golden全部逐项一致；可进入A02b，A02整体尚未完成。**

环境Unity6000.6.7f1、Windows11 x64、D3D11/1080p、普通Forward内部HDR/SDR；开发机i7-12650H/RTX4060Laptop7956MB/16GB。目标仍为i5-8400/GTX1060 6GB/16GB中档1080p60，推荐i5-12400/RTX3060 6GB/16GB高档1080p60；本叶模块不测或声称这些帧率。

实现：TokenizerAssets校验实际151643基础词表/151387合并规则、22个added tokens（14特殊/8普通）、NFC/正则/ByteLevel配置，不接受静默算法回退。ByteAlphabet是完整GPT字节映射。QwenPreTokenizer按实际HF Unicode分类建立标量/原UTF16偏移表，在等类别影子串上匹配固定正则；保留ASCII空格、CR/LF和收缩词语义，返回原NFC文本的字节片段，避免Unity Unicode数据库版本差异。孤立代理项与>32768字符拒绝，正则100ms上限。

同版本官方tokenizers0.22.2 Regex提取677组Letter、144组Number、10组Whitespace范围，来源tokenizer SHA及生成表SHA见Evidence/S3-A02/UnicodeTableSource.json。只生成编译源数据与测试golden；游戏无Python服务、远程代码或下载器。

独立入口`EchoStation.Tests.AI.PreTokenizerTests`，用module_tests.py运行，job `da7a5fface2643d292e375abacb431ed`：8/8、0失败/跳过、5.927秒。包含全部256字节往返/映射关键值、实际资产完整库存、400条中文/多语种/数字/标点/空白/组合字符/emoji/补充平面/全部added标记的上游NFC与byte预切分一致、4类非法配置和非法UTF16/长度。400个数据子项不与8个测试方法重复累计。

原始结果A02a_A.json、SceneSafety/RenderSafety及PreTokens_*.json保留。没有修改S1/S2场景/渲染资产。当前尚无BPE编码/完整解码，不能把预切分通过当作token IDs通过；下一叶模块A02b才验证400条完整ID序列、普通added token识别、特殊角色隔离、可信分隔符/解码与边界。近期诊断命令失败见F119，未据此修改正确tokenizer。
