# S1-P07 本地存档验收

A3/3→B真实磁盘7/7按序通过；Evidence/S1-P07下A-SaveCodec/B-AtomicDisk.json含每项断言结果。纯数据字段/校验与版本边界、首次写入、二次File.Replace上版备份、主损坏回退及.rejected留存、双损坏不覆盖、未来版本拒绝、真实Windows锁文件提交失败旧档保持、孤立tmp忽略并回收、空/超限/非法字节全部通过。所有文件仅Library/EchoStation/SaveTests/唯一GUID临时目录，退出核对路径后清理，未触及真实用户数据。

v1/UTF8/64KiB/SHA256检测损坏，非加密或反作弊；未实现跨进程多写入者。当前单游戏实例单槽同步小文件提交，游戏恢复另属P08。Unity6000.6.7f1、Win11开发机、普通Forward HDR→SDR/D3D11；无渲染/性能达标推断。
