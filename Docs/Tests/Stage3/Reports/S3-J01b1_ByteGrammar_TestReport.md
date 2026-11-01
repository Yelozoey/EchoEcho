# S3-J01b1 增量字节语法

2026-10-26，通过81/81，1.812秒，job `18621a6a9c084b19b0d9a1ee9bef667f`，J01b1_A。新增17项语法测试加64项严格解析回归；a通过归档后开发本叶，下一叶再接采样器与Provider。

Unity6000.6.7f1/Win x64/D3D11/1080p普通Forward内部HDR-SDR、i7-12650H/RTX4060Laptop/16GB；最低i5-8400/GTX1060 6GB/16GB中档1080p60和推荐i5-12400/RTX3060 6GB/16GB高档1080p60不在本纯语法验收。干净场景/Console/实际RenderSafety通过。

入口module_tests.py，assembly EchoStation.AI.Editor.Tests，groups JsonConstraintTests、IntentJsonParserTests。证据Evidence/S3-J01/J01b1_A*。

六类规范JSON逐字节接受且严格解析通过；无目标时不生成investigate/warn，调查/危险权限分开且未知ID拒绝；Allows和失败Accept不修改真实状态，空token/围栏/数组拒绝；UTF8中/火箭与Unicode转义代理对可任意拆分，非法续字节/过长/代理区前缀拒绝；279单元时不接受需2单元的前缀，280后仅可收尾；空/全白台词不能结束或填满280形成死路；第240token后强制合法完成（不补字符串），含部分高代理项时先完整低代理项再结束；字段生成顺序规范化但解析器仍接受合法其他顺序；重复/9目标拒绝。全部通过。

状态是小值类型，Allows复制状态后试算，不分配候选JSON字符串；模板与ID选项预建ASCII字节。语法只限制生成语言，不控制GameObject、不验证事实、不等同真实模型语义正确。真实logits模式在后续b2/c分别测试。
