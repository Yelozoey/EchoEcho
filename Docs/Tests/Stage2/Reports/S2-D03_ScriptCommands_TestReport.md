# S2 D03 脚本命令识别测试报告

2026-10-25通过。Unity6000.6.7f1、Windows11开发机，普通Forward/HDR到SDR/D3D11及干净场景/Console门禁通过。缓存stale时通过已有主线程空闲/实际TestRunner检查后继续，仍执行新鲜安全门禁。目标硬件性能沿用总文档，纯规则测试不作FPS验收。

独立运行Dialogue.Commands.Editor.Tests/ScriptCommandParserTests，A-ScriptParsing：**12/12通过，0失败/跳过，框架0.4340609秒**。

实际用例：跟随称呼/请/标点和等待同义词；否定不误执行；调查唯一ID/全名/别名及必需字段；模糊供电面板只澄清并支持后续明确选择；缺目标或未知/更长ID不猜测；warn只接受危险目标、空危险集只给信息；Hint/Summary/Password/Fact分类不编答案；替操作拒绝而密码/方法询问走信息；多动作不执行、外部JSON/代码不执行；输入/目录边界；外部数组隔离；输出仅六意图、无直接UnityEngine依赖、澄清短句≤280字符。

[原始结果](../Evidence/S2-D03/A-ScriptParsing.json)、回执/新鲜门禁保留，无新故障。本模块只识别预设表达，不保证任意自然语言；没有访问GameObject、检索未开发知识或实际执行命令。D04再绑定已有权限执行器，当前10/30。
