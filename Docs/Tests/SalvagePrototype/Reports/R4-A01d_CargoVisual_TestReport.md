# R4-A01d 回收物视觉与传统搬运测试

2026-10-30；Unity6000.6.7f1／D3D11／普通Forward／内部HDR→SDR。测试入口module_tests.py，独立CargoVisualTests。

Carry_C实际2/2通过，1.473秒。七个视觉Prefab保持原ID，六件最大边0.44m、超限设备0.8m；URP贴图／材质副本／归一化Bounds／无额外Collider与货物逻辑检查通过。真实运行逐件射线拾取→安全地面放下→再拾取→泊位申请并确认交付；手持标签隐藏、落地恢复、交付隐藏、重复确认不重复计分。六件合计28分，12kg设备拒绝，重置恢复七件。原世界位置、kg、分数与8kg规则未改；旧R2仅临时加载，未保存。

Carry_A首命令空输出时实际作业已晚到启动；没有将空输出作为失败证据。Carry_B因external changes门禁拒绝，刷新后Carry_C完成。两作业原回执与实际Carry_GUID.json保留。后续遇到无输出，先检查进程／作业文件，避免重复启动。

本模块不代表新关卡已整合、存档或完整Windows验收。硬件基准沿用Windows10/11x64，1920×1080：i5-8400／GTX1060 6GB／16GB中画质60FPS，i5-12400／RTX3060 6GB／16GB高画质60FPS。本次Editor功能测试不证明目标机帧率。
