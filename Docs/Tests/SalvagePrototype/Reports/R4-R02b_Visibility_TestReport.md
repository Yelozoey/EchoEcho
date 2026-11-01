# R4-R02b 视锥、墙体遮挡与动态SRP Batcher

2026-10-30，Visibility_G联合5/5（本项3＋Decor2）；原始runner汇总16.829秒跨域重载不代表各测试耗时总和。门禁通过、Console0。

新版PVS实际烘焙0.891秒，资产52,916字节、PVS25,344字节，布局已还原。仅新版保存，未SaveAssets全局写入。区域35.5×2.7×23.5m，最小遮挡1m、孔0.25m。

实际FrameDebugger选中各SRP事件获取内部draw数：受控静态墙体Off36→On20，背向场景0；Off-Off和Off-On画面差均0，非黑均值0.009119。首次Warm仍有过往实例提交，Warm-Off差0.00001324，说明不可把首请求当稳定基线。已看原始首失败Off/On并排图，旧穿墙标签会改变图像；本因果对照明确隔离标签/货物/Echo/实例装饰、实时灯/时间性后处理，完整产品标签可读性仍须P01修复。

真实可移动工具箱SRP事件4→6，移动0.4m画面差0.00013433，SRP Batcher开启；此数为批次事件，不与上一段底层draw数混用。Decor原生InstancedMesh也随工具修改重测通过。

F249四轮首失败保留，阶段日志定位到跨EnterPlayMode域重载的捕获局部变量DisplayClass，改静态回调后进入真正图像采集；此前camera/this假设无效，不宣称产品故障。F250稳定基线/非静态控制修复后未放宽1e-6阈值。退出恢复播放选项true/DisableDomainReload、背景false及SSAO等Feature启用。

此项针对拆分前新单场景；Addressables重分场景后必须重新验证PVS绑定与实际收益。非Windows性能、目标机60FPS或UI验收。
