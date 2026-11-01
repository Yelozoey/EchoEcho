# S3-S01 独立场景与全状态存档逐叶

2026-10-27，E03通过，S3工程16/24；复用已验完整状态codec/原子文件存储，不迁移S1/S2。

1. a路径与显示绑定：复用StationPersistence.ConfigureDirectory，在S3启动最早Awake配置Application.persistentDataPath/Saves/S3（slot1.json），S2默认目录保持。阶段显示标签改为可配置且默认S2；S3启动记录使用S3标识。独立目录/名称/旧入口回归。
2. b专用场景/导航：干净门禁后复制既有五区灰盒为Assets/EchoStation/AI/Scenes/ES_S3_AI_Demo.unity，并复制NavMeshData且更换唯一引用；渲染/美术资源只读复用。专用Bootstrap早于其他Awake绑定目录，安装S3DialogueDriver，保留S1/S2资产。新局本就原地全状态恢复，不需修改场景加载路线。
3. c全状态恢复及取消：独立单槽保存真实日志/摘要记忆/对话/扫描/门/回路/玩家与Echo；开关不写入、启动默认false，无KV/请求状态字段。读档/新局/失败回滚时旧响应零执行、清理不主线程等待。用独立临时目录，验证S1/S2哨兵/哈希不变、主备原子一致。
4. d专用场景启动/导航/脚本基线与真实LLM保存再恢复：场景配置/完整状态/实际线程/重开关闭开关独立验证，产出入口说明。T04再覆盖全部结局，V01再做Windows跨进程，不能提前宣称完整S3完成。

每叶独立通过/报告后下一叶；不更改全局BuildSettings/渲染模板，不碰用户未保存场景，不进入S4。
