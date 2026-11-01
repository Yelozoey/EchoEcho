# S1-P07 单槽本地存档

授权内具体选择：v1封装JSON（版本、UTC、payload JSON及SHA256），最大64KiB；payload仅小屋ID、玩家位置/yaw/pitch/手电、Echo位置/yaw/稳定follow或wait、获取顺序日志ID与扫描ID。最多128条、不含原文/LLM内容；身份数据在P08按当前场景核验，P07只做结构校验。

A：纯数据/编码校验及非法/重复/越界/校验/版本测试；独立通过后B：临时目录真实磁盘往返、第二次原子File.Replace与上版备份、损坏主档回退、双损坏拒绝覆盖、未来版本拒绝、锁文件提交失败保留旧档、残留tmp忽略与清理。只测试Library下唯一GUID目录，禁止触及玩家真实存档。

生产路径Application.persistentDataPath/Saves/slot1.json、.bak与.tmp；先写独立tmp并Flush(true)，再原子替换。未来版本直接拒绝，不回滚旧版；损坏主档且备份可用时读取备份并提示，后续保存先留.rejected副本且不把损坏主档覆盖到好备份。双损坏保留原件并拒绝覆盖。校验用于损坏检测，不宣称防作弊/加密。尚不实现P08状态适配或S2记忆。
