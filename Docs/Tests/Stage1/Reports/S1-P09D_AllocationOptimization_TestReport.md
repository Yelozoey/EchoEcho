# S1-P09-D 实测分配定位与修复

MetricsBefore真实Windows900帧全部提供唯一有效CPU/GPU时间，原始Profiler文件143958489字节。三视点GC基线主要为40B/帧，约每0.2s出现168B（40+128），900帧合计45696B。原生RawFrameDataView读取最后59帧：59次20B来自EchoPresentation.ApplyFrame枚举装箱，另59次20B来自Enum.IsDefined→GetValue，5次128B来自EchoBrain.Update的方法组委托。调用栈与源码行号记录在ProfilerAllocationCallstacks.json。

修复仅用无装箱合法枚举范围比较及Initialize缓存/Shutdown释放Predicate；不改变三色、传感器筛选或自动warn/手动冷却语义。独立受影响回归11/11通过（D-AllocationFixRegression.json，39.790s），包括实际动画/颜色图像与自动警告/用户覆盖/冷却。不能以此宣称GC已为0，Windows控制对比尚待完成。

Profiler导入前确认为未录制/无帧，分析后只清除自有导入，恢复原空状态（ProfilerStateRestored.json）。普通帧采样预热10秒、三视点各300帧，截图与写JSON不在采样区间；CPU总帧含等待，线程时间用FrameTiming原生字段，ProfilerRecorder记录真实GC/内存/Draw Calls。开发机数据不是最低/推荐硬件验收。

Windows控制对比MetricsAfter已通过：同样900帧/900有效GPU/同样启用分配调用栈，GC由45696B→96B（898帧0，两次48B来自夹具首次取样，原始栈已核实），40B每帧及周期128B业务分配均消除。最终普通轮预热实际API，禁止删除结果中的异常帧；最终结果另见P09总报告。
