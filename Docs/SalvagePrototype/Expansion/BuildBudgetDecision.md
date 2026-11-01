# 新Windows构建峰值预算事项

2026-10-31，独立只读核验，尚未启动新构建或修改模型。当前已授权20GiB仍有效。

最新确认：用户明确允许“仅打包临时22GiB，交付恢复20GiB”。此限定授权已生效；除打包过程外仍按20GiB，旧模型/构建/历史证据保护不变。

执行结果：四轮实测峰值最高20.882846GiB，未超过22；源权重SHA保持，发行模型副本每轮精确剥离。最终19.187671GiB/20GiB，干净包229.40MiB/501文件，见V01/BuildPeak_D、Budget_Final_A、DeliveryManifest_A及最终报告。本事项已完成，下列估算保留为决策历史。

- 当前按既定合并资源根＋新Expansion目录实测18.793920511GiB；[原始账目](../../Tests/SalvagePrototype/Evidence/Expansion/Run01/Budget_BeforeBuildPlan_A.json)。
- Assets/StreamingAssets/EchoLLM实际1,902,696,028字节，约1.772GiB。上一版真实构建收据显示曾自动复制1,902,692,739字节后才从发行副本移除；原项目模型保留。
- 即使忽略新包其他文件，18.794＋1.772已超过20GiB；加入旧版干净包规模估算约20.81GiB，其他编译/缓存增长另计，不能当成保证峰值。
- Unity6000.6.7f1实际反射的BuildOptions和BuildPlayerContext没有公开的StreamingAssets排除选项，只有添加路径等API；[本机API记录](../../Tests/SalvagePrototype/Evidence/Expansion/Run01/BuildApi_A.json)。官方[StreamingAssets说明](https://docs.unity3d.com/cn/2023.1/Manual/StreamingAssets.html)说明这些文件会随包复制，[BuildPlayerContext接口](https://docs.unity3d.com/ja/6000.0/ScriptReference/Build.BuildPlayerContext.html)为补充添加。

建议仅将打包过程临时峰值上限提高为22GiB；新包完成后移除新发行副本内的模型，回到20GiB以内。旧模型/旧构建/历史证据不移动、不删除。所有清理目标必须先验证绝对路径属于本轮新Builds/SalvageExpansion。构建前重新估算、期间记录实际峰值；预测无法守住新上限仍先停下。

若不批准，20GiB不变，继续其他模块与Unity可玩切片；Windows构建保持待解决，不能标记V01完整交付。本事项不授权下载新权重、训练、切后端或联网推理。
