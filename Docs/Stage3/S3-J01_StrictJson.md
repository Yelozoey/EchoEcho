# S3-J01 严格JSON与结构约束

2026-10-26。C02通过后开始，raw格式问题F120仍未通过；拆a严格解析/Schema/离线脚本、b增量JSON解码约束、c真实模型及回归。每叶通过归档后继续，不能靠剥围栏、补字段或脚本猜意图。

a：纯数据六意图，不引用Echo/Unity执行程序集。最多4KiB、单对象、四种JSON首尾空白可接受；拒绝评论/围栏/重复解码键/未知字段/错大小写/null/错类型/坏UTF16及转义代理项。follow/wait仅intent；investigate为intent/targetName/content；warn为intent/dangerTarget/content；answer/refuse为intent/content。ID沿用稳定ASCII格式，台词非空白且≤280 C# UTF16单元。日期字符串不能自动变日期。JSON Schema交付同时注明UTF16限制需运行校验器附加执行，脚本离线不下载schema。

b：在生成选择token之前按增量状态机限制结构和本请求有效目标，保留真实logits greedy；不改固定正文，不修复生成后的坏JSON。未知目标/无目标不强制猜测，允许answer/refuse。固定字段顺序仅属于生成器，解析器允许合法字段任意顺序。EOS只能在完整对象后；UTF8跨token、转义、280长度等有独立用例。

b实现细节：规范结构不生成无意义空白，UTF8/转义前缀必须可继续合法完成；content按280个UTF16单元且至少一个非空白符。第240个已接受token后只允许完成当前字符、必要的非空白字符和JSON关闭/EOS，预留16个token；这是选token时的结构预算，绝不在生成后补字符串。最长未完成代理项转义加关闭/EOS≤15字节，基本byte词表可在≤15token完成，仍服从真实logits选择。目标ID最长64使固定前缀最多约116字节，240token前已进入content或结束。

c：真实六类输入、原始与约束模式分别保留。J01只验结构，语义正确率由T01独立100条核验；任何原始/约束/守卫/脚本结果不混算。LLM无GameObject或机关写接口，权限和事实由J02/E01随后处理。
