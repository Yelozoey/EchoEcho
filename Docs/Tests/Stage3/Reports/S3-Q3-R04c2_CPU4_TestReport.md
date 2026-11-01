# Q3-R04c2 CPU4真实对照通过

2026-10-27，独立线程预算4/4通过后整合，真实SessionOptions.IntraOpNumThreads读取已验证配置4；inter1、CPU provider、无自旋、BelowNormal单Owner、固定v5/官方非思考/greedy/JSON语法和20秒/冷30秒/300ms不变。

作业a21fce90e88743d094f816f1a7aae364，NUnit1/1通过，84.157秒。18条结构18/18、意图/目标17/18；原有不存在目标错误仍计错。与CPU2 v5逐条比对输入及完整输出token（含EOS），18/18完全相同，Cases字节SHA均6c36f57626dfa364b0c78b29b70a89aa1efe2da8204ad8a7b3b67c603811c70b，证明此组输出未被线程变化改变。

短回复nearest-rank P50/P95：CPU2为6.288/7.935秒，CPU4为3.885/4.928秒，当前开发机该组达到≤5/≤10秒。冷准备10.779秒；0超时/不完整、主线程native0、Owner清理完成。此次没有并行大文件校验。时间对照为两次实际运行，仍可能有环境波动，不宣称最低硬件或游戏FPS通过。

整个Editor OS峰值Private4,623,323,136B、WorkingSet3,198,615,552B；采样启动晚于模型加载，增量0没有AI增量意义。独立Windows3GiB与并发游戏性能仍待正式模块。

证据V5_CPU4_A/的CPU4_A-Job/CPU4_A.json、Scene/RenderSafety、Summary、Memory、ThreadComparison.json，原始UnityTuning_20261027T0106133742701Z_de68de9abe764ee8a0043c4124e6aceb.json，SHAebb2cf0ddd54f46cae7e97e9e0d3f3aaa7d97ba41ad309346bfb2cb87f1e1b06。ThreadComparison绑定两份原始SHA与18条完整token。

下一叶用同一独立100条B和相同Prompt正式复核CPU4；不根据B预测调Prompt。确认全部输出/语义与延迟后才决定生产候选参数，随后还须完整Qwen3资产/分词/后端适配和权限守卫，不能把该叶当S3完成。
