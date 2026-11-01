# S2-V03-AB1 区域常态分配修复

2026-10-26。前两次原生Windows各1500帧持续24B；Performance02原始Profiler最后60帧GC.Alloc父节点均为StationSession.Update，AllocationParents.json保留且导入前后Profiler关闭/空帧恢复=true。

改动只移除RefreshZone中捕获zone的First查询，使用新区域分支索引查名。未变区域边界、记忆ID/摘要、事件编号或触发逻辑。ZoneMemoryRegression实际2/2，6.522秒：W01_B03区域记忆/文本控制不制造线索或设备改变，F01_B01启动一致性与快捷键约定记录。ZoneHost原场景布局和播放选项已安全还原。

这两项证明功能回归通过；0B须由相同Windows普通帧后测证明，不能从源码推断。F109记录原因，历史24B不删除。构建设置F108的内存/磁盘false已分别核验，后测/最终构建仍要继续检查。
