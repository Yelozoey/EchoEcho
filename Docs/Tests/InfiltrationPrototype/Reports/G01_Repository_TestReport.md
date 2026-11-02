# G01 可重建仓库交付审计

用户明确授权目标Yelozoey/EchoEcho、21GiB含Git元数据、可重建工程范围和大件本机保留。只读ls-remote与官网均确认初始为空；本机Git/GCM可用，未安装gh或开LFS/收费服务。创建本地main/origin，不强推、不修改远端权限/可见性。

第一轮审计6344文件：Assets/Meta、309主场景AssetDatabase依赖、15模型小文件索引SHA均通过；凭据URL规则命中一处上游XML安全说明。已红显脱敏核对，按路径/行/整行SHA精确允许该虚构示例，源码不改。第二轮Audit_Index_B实际通过：6345文件/352729689字节（约336MiB）、6032文本文件、2407 Asset GUID、309场景依赖、15模型元数据SHA；无缺Meta、重复GUID、缺当前场景依赖、超过50MiB文件、意外权重/机器配置路径或未审疑似凭据。最大文件onnxruntime.dll17253176字节。该扫描不能保证覆盖一切秘密格式，也不是新机Unity完整导入证明。

审计器真实自检3/3；可选模型校验工具在本机对两份既有最终权重实际SHA验证通过，无下载/转换/训练。模型权重与.meta被忽略，tokenizer/manifest/许可保留。当前场景不依赖模型；恢复与旧脚本限制见Docs/Repository/RebuildAndModels.md。保留Source原字节，关闭的仅是本仓库自动换行；15个模型小文件从暂存blob验证，不仅看工作树。

原始Profiler、完整私有MCP快照、旧Builds、Library/Temp/UserSettings/.codex/.vscode与源ZIP均留本机。仅精选当前三实图、单平面效果和安全的材质/烘焙数字收据进入Git。公开ValidationSummary/PerformanceSummary是原始真实结果的去会话信息摘要；不上传NativeBefore的整份PlayerSettings/base64磁盘快照。发布前最终同范围审计和普通Git推送/远端SHA回读另留原机G01 Publication记录。

许可：已有CC0/OFL/ORT MIT/模型Apache元数据通知保留，嵌入MCP缺的完整MIT通知按官方raw文本核对补在ThirdPartyNotices；未给自有源码或第三方资产统一套新许可。urllib许可证请求TLS EOF使用web同官方raw文本核对，不关闭TLS或改网络；OGA片本轮web受限，沿用此前项目已验证JamesWhite/CC0来源，不冒称本轮完整重验。

HANDOFF.md已提供主场景、架构、约束、实际测试、已知高帧时、模型No-Go/恢复、Git排除范围与下一A02需要确认的规则。未开始守卫、潜入闭环、模型实验或Windows打包。正常克隆能获得本轮所需的309个依赖路径（Unity内建/声明UPM包例外经包管理器还原），未额外创建新工作树或对新克隆启动Unity；不把静态仓库审计当新机运行验收。

最终同范围Audit_Final_A实际6347文件通过；Git fsck退出0。主体提交11f0ea97869fbae92e429fbc209ab53beb7685a4已正常推送，HTTP/1.1 ls-remote逐字核对远端main一致，远端HANDOFF网页已读取；网络初始重置与恢复见F-381。后续仅普通文档补充提交，不改已验证游戏代码。NativeVerified_PostGit_A再次确认5设置/磁盘/dirty/运行时一致，场景clean/Edit；Budget_Final_A20.246182/21GiB（含.git），旧大件均保留。
