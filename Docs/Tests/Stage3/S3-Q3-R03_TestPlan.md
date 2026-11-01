# Q3-R03 Prompt 逐版完整生成

沿用Q3-R01环境/硬件基线与Qwen3TuningPlan所有约束。每次prepare新标签，把完整Prompt、官方非思考输入IDs、正文/案例SHA留档；不改18条预期，不加载Qwen2.5权重，不以脚本重写意图。

1. 先确认上次原始报告completed及ownerCleanupComplete，Console无错误、Scene/Prefab干净、Forward/内部HDR/SDR/D3D11门禁通过。变化只涉及实验输入时compile:none刷新，变更夹具时须新DLL与域重载。
2. module_tests.py显式选择Qwen3TuningTests；单后台Owner、CPU2/inter1、greedy，30秒冷准备、20秒请求、300ms清理后间隔保持。等待完整18条原始生成，保存NUnit结果、每条完整raw/token/模式/时间/目标及预期。
3. 结构必须18/18、意图及目标≥17/18；不得用规则改写后结果计算原始正确率。台词事实、代操作承诺、乱码/多余符号单列，结构正确不等于内容合格。主线程native0、Owner释放、无跨请求KV均验证。
4. 任何外部错误或NUnit中止仍需等Owner排空后才启动下一轮。失败不覆盖，用新标签；summarize_qwen3_tuning.py仅在完整清理后绑定并归档原始SHA，不能覆盖测试结论。
5. 最近邻上取整P50/P95分别记录，目标≤5/≤10秒，不用Editor整个进程内存变化声称AI≤3GiB或目标硬件60FPS。质量通过后固定该版本，进入独立100条新集；否则在已授权范围内迭代Prompt，保留各轮失败。

本轮三个变体为v3-user、v3-system、v4-user。v3-user受IAP错误影响但完整原始18条已排空；v3-system为正常完成的门槛失败。具体结果见R03实际报告，不因输入准备或测试运行本身记作模块通过。
