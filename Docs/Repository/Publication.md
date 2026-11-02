# GitHub发布与交接基线

仓库：https://github.com/Yelozoey/EchoEcho ，分支 `main`。

主体工程初始提交：`11f0ea97869fbae92e429fbc209ab53beb7685a4`。已正常推送，并通过HTTP/1.1的ls-remote与本地HEAD逐字比对一致，远端HANDOFF页面也已成功读取。当前文件及故障补记为后续普通文档提交，不用初始SHA冒充所有未来main的HEAD；接手以main最新提交及根HANDOFF.md为准。

```powershell
git clone https://github.com/Yelozoey/EchoEcho.git
```

本轮独立21/21→11/11→3/3，最终25/25。主场景为`Assets/EchoStation/InfiltrationCoated/Scenes/ES_Infiltration_Coated.unity`。Assets/Meta、309条主场景依赖与15份模型小文件SHA已审计；约336MiB工作文件，普通Git传输，无LFS。

初次网络连接重置后，使用仅本次生效的HTTP/1.1完成推送；默认协议回读也曾连接失败，HTTP/1.1回读随后成功。没有修改全局代理/TLS或强推。原模型权重、Builds、原始Profiler/私有快照仍在原机，按约定未发布。恢复、许可、测试和已知性能/玩法限制见同目录RebuildAndModels.md、ThirdPartyNotices.md、根HANDOFF.md及F03报告。目标机60FPS、新机Unity完整导入尚未验收。
