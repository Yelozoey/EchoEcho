# S1-E04 意图、执行权限、脚本入口与独立测试

2026-10-25，前置E03最终14/14已归档。实现传统执行层入口，不接模型/Prompt/JSON解析器/记忆或对话输入框；S3再将已验证JSON映射为此类型。

意图固定follow/investigate/wait/warn/answer/refuse。前四为行为，后两只发台词、不切行为。follow/wait无参数；investigate带targetName+content；warn带dangerTarget+content；answer/refuse仅content。目标字段互斥，ID严格沿用W03；台词非空白、<=280字符。缺字段、额外目标字段、未知枚举、未知目标、无显式能力均拒绝，失败不得调用执行器或发台词。

EchoTargetPermissions只授予信息调查和危险提示能力，不提供门/机关/解谜API。小屋5目标允许调查，仅power_panel_01为危险提示目标。任意新增WorldTarget默认没有Echo权限，显示名和字符串前缀都不赋权。只把已解析且获得能力许可的目标传给传统IIntentSink；不可把未经验证请求直接交给移动逻辑。

全局GameRuntimeOptions.UseLLM为bool，S1默认false/新运行重置false。脚本入口用固定命令按钮/后续P09整合，不解析自然语言；开关在上层决定推理来源，传统执行器独立可用。S1没有任何LLM提供器或推理实例，不能把开关true称为已有LLM功能。

独立用例：六类合法输入分流、answer/refuse不改变行为；非法枚举/字段/空或过长台词/未知目标拒绝且sink无副作用；无权限门目标不能调查/警告且状态不变；调查与危险权限分别验证；真实EnterPlayMode重置UseLLM=false后脚本可分发；场景5份权限正确。通过报告后才写E05行为树生命周期。
