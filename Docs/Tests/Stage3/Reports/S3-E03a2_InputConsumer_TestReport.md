# S3-E03a2 输入单消费者与S2执行回归

2026-10-27，Unity6000.6.7f1 / Windows / D3D11普通Forward内部HDR/SDR，统一门禁通过；开发机i7-12650H/RTX4060 Laptop/16GB。最低i5-8400/GTX1060 6GB/16GB中1080p60及推荐i5-12400/RTX3060 6GB/16GB高1080p60仍待对应实机。

既有ScriptDialogueController增加默认null拦截器，返回true即消费/清旧澄清；false才走原脚本。拦截器异常记录明确日志并拒绝回复，不自动重放输入。新增PlayerIntentRequested在外部手动意图执行前通知，用于后续作废在途结果；Configure/销毁清旧拦截器，原Speech/记忆事件保持。

Inputs_A作业6db33baae93445469708a79d7618e26f：8/8、0失败/跳过、11.2617251秒。3项新增真实面板输入覆盖消费一次/false与null旧路径、故意抛错不转脚本/不写动作、手动事件先于执行/禁用解除订阅；故意fixture异常由LogAssert明确预期，非隐藏产品报错。原S2 DialogueExecutionTests5项完整回归通过（实际移动/扫描/警告/澄清/知识边界）。

入口`Tools/MCP/module_tests.py start/poll --evidence Docs/Tests/Stage3/Evidence/S3-E03a2 --label Inputs_A --assembly EchoStation.AI.Execution.Editor.Tests --assembly EchoStation.Dialogue.Execution.Editor.Tests --group '^EchoStation.Tests.(AIExecution.DialogueInputInterceptionTests|DialogueExecution.DialogueExecutionTests)'`。regression_host Inputs_A原工作区/Play选项/管线绑定已还原。a2通过；实际Host及开关仍待后续叶，当前不是全链LLM验收。
