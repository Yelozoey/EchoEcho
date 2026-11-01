# Q3-R04c1 诊断线程预算通过

2026-10-27，依赖独立100条意图/格式已通过。Unity6000.6.7f1 / EditMode，新DLL229376B和域重载完成，安全/Console门禁通过。作业3762e1f030074e58bc4fe7774936600e，4/4通过、0.071649秒。

历史无cpuThreads配置仍为2；整数2/4接受；0/1/3/8、浮点、字符串、bool、null和缺失配置对象拒绝。仅新增诊断设置，不扩大授权预算、不启动推理。证据R04c_Settings_A/Settings_A-Job/Settings_A.json及安全记录。

通过后才把同一校验值传入真实ORT SessionOptions.IntraOpNumThreads和报告。下一叶固定v5/Cases/greedy/无自旋/inter1，实测CPU4，并逐条比对CPU2旧v5的完整输出token；不能只改报告字段冒充线程变化，不能把本叶通过当性能达标。
