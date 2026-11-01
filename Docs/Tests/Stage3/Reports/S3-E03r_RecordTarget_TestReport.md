# S3-E03r 名称含“记录”的目标兼容修复

2026-10-27，F149先复现：Record_A作业0775b8a5b35643f28f12a4c51cf0b113，5/9通过，4种明确扫描/调查均被误转GroundedReply。修正只根据明确资料问句或无调查动词的资料词判断，保留唯一目标/两次权限/代操作拒绝/可信回复，未改Prompt或原始模型评分。

Record_B作业3bc649c855ee4748b373224f05957fee：97/97、0失败/跳过、0.1839553秒，含新9项、原88项综合（100边界和10真实模型错误回放仍零违规）。证据`Evidence/S3-E03r/Record_A.json`、`Record_B.json`；入口`Tools/MCP/module_tests.py start/poll --evidence Docs/Tests/Stage3/Evidence/S3-E03r --label Record_B --assembly EchoStation.AI.Editor.Tests --group '^EchoStation.Tests.AI.(FactQuestionRoutingTests|GuardBoundaryTests|SessionFenceTests|TargetNameMatcherTests|IntentActionGuardTests|ActionLanguageTests|TrustedReplyPolicyTests)'`。

Unity6000.6.7f1 / Windows / D3D11普通Forward内部HDR/SDR门禁通过，开发机i7-12650H/RTX4060 Laptop/16GB。纯守卫回归非最低i5-8400/GTX1060 6GB/16GB中1080p60或推荐i5-12400/RTX3060 6GB/16GB高1080p60证明。修复叶通过，可继续真实连测；工程总数仍15/24。
