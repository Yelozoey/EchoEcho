# S1-P07-A 编码校验

实际Unity EditMode3/3通过（A-SaveCodec.json）：字段独立往返、无原文/UseLLM写入、空/损坏/超64KiB/校验错误/未来版本、非法位置/角度/场景/瞬态Echo模式/ID重复和容量。3项包含多个边界输入，不计成多项测试。环境6000.6.7f1/普通Forward HDR→SDR/D3D11，数据层尚不代表磁盘提交或游戏恢复通过。
