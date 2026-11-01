# S3-T01w2 v8显式消息分离与诊断加载

2026-10-27，Profile_C实际24/24通过，4.7295334秒，job `bd62ab0f42134f3eabd246fa2e20277c`。证据Evidence/S3-T01w2；A为23/24、Audit_B为0/1，F156定位Null/String类型差异，保留首失败，未改冻结用例或标签。

v8正文SHA `67399cdccace313c184964eae17235fc1ffb8c98077b3a7d3b89974b3e16b237`，2983字节，规则保持v7，唯一文字调整是用户数据说明。profile显式RulesSystem，用可信静态偏移切割system规则与user摘要/目标/请求，拒绝被改写的规则头；输入中的相同标题、占位、角色标记不能改变拆分位置。默认仍v5/SingleUser。

新5项覆盖精确文档/SHA/布局、C+D共200条真实预算与全部请求目标保留、注入/偏移、非法布局/改头、60条均衡诊断严格对应原C/D且每意图10条；旧包装/Profile/Preparation19项回归通过。诊断60 SHA `affe1d25defeffca4c4dbe3ebf2c0b4bd703e90965e84fc51e0d7ce071e65db3`，全部已见，不能作为独立验收。此叶不加载权重。
