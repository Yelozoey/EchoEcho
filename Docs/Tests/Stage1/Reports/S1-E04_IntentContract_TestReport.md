# S1-E04 意图入口与权限验收

2026-10-25，v0.1。Contract.json 6/6、0失败/跳过，1.701秒；本模块功能通过，允许E05。

六意图分流验证：follow/investigate/wait/warn仅进入四行为，answer/refuse仅发台词不切换行为；调查/警告解析为W03精确ID对象，执行器拒绝时不提前发成功台词。11类空/非法/缺字段/额外参数/过长台词/未知目标夹具均拒绝且sink行为/台词调用数0。

默认无能力的door_01目标不能调查/警告，位置和sink无变化；可调查与危险权限分别检查，不由ID/显示名赋予能力。小屋5目标允许信息调查，只有power_panel_01允许危险提示；接口没有开门、改机关、解谜方法。尚未接实际行为树/移动，sink为隔离夹具，不冒充机器人已具备六类完整行为。

真实顶层EnterPlayMode把测试临时true的全局bool GameRuntimeOptions.UseLLM重置false，此时脚本Follow命令仍可分发。运行时Echo程序集没有Barracuda/Sentis引用，S1不构造任何LLM提供器；这不代表UseLLM=true已可推理。Prompt/JSON解析/Schema仍在S3，文字对话输入框仍在S2。

Unity6000.6.7f1/Windows Editor D3D11、当前开发机；无新增故障，执行前复用F-037/F-048门禁与顶层模式切换。未构建此模块独立Windows，P09做最终产品验收；最低/推荐两档目标硬件1080p60FPS均未实测。
