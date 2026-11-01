# R1-S01 独立原子存档

2026-10-29，通过。Save_A真实EditMode6/6、0.986秒，module_tests完整门禁通过，证据../Evidence/R1-S01/Save_A.json。

独立路径Application.persistentDataPath/Salvage/slot1.json，F5保存/F9读档，手动单槽，不自动读取或覆盖旧S1/S2/S3槽。64KiB、严格JSON字段/类型/重复字段、格式/版本、SHA256、7ID与互斥Held/超限/电量/完成条件验证。成功写入Flush(true)+File.Replace，保留有效备份；损坏主档可读有效备份并保留rejected原件；未来版本/无有效副本不覆盖。

实测全字段与World/Held/Delivered往返；重复/未知/多Held/NaN/伪完成/坏校验/尾对象/重复字段拒绝；两次真实磁盘写入、无残留tmp、损坏主档回退、rejected保留及未来/不可恢复原字节保护均通过。场景整体恢复Held锚点/碰撞/3.3速度、Delivered数量、落地点、电量、手电、时间；墙内玩家快照拒绝且前后编码完全相同；保存→重置→读档恢复通过。

场景先全量验证几何/导航/状态再应用，异常尝试回滚；确认中的事务不保存。PlayMode实际Agent读档与跨Windows进程仍在V01验收，本报告不把EditMode恢复当完整产品/断电故障/最低硬件通过。
