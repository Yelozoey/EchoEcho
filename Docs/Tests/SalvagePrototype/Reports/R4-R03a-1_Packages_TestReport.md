# R4-R03a-1 官方依赖与安装保护

2026-10-30，官网registry10条依赖审查通过；Package_B Unity独立2/2、0.193秒，Console/Render/Scene门禁通过。Addressables2.11.2与必需SBP3.0.3均最低Unity6000.0，适配固定6000.6.7f1；安装作业cfc285…实际Succeeded。

新增二包，原73锁条目逐一保持版本、来源、Hash、依赖字段（仅允许depth图变化）。原Manifest只新增com.unity.addressables=2.11.2，其余字段相等；旧TestFramework1.8、Profiling1.0.3、URP17.6保持。运行SDK/ResourceManager程序集实际注册与新包Registry来源确认；原URP本机Source标BuiltIn，F256修正仅新增包须Registry的断言，不改变旧来源。F255测试括号错误已修正，首编译/首失败保留。

安装约140.7秒，期间原生空回执按既有F112未重放安装；后续注册源、真实作业与磁盘确认完成。UI检查无阻塞模态，没有登录/配置服务。默认旧位置upm.log/Editor.log是过往日志，不能据此当本次安装证据。

此项仅依赖安装，不代表Bundle构建或区块流送已实现。原manifest/lock备份、官方选中版本完整metadata、InstalledVersionAudit_A和原生测试回执位于R4-R03a-1证据目录。
