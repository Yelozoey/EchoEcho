# S2-W05 通风通道测试

Unity6000.6.7f1/Windows11、D3D11/普通Forward/内部HDR→SDR，开发机i7-12650H/RTX4060Laptop/16GB，UseLLM=false。目标最低/推荐硬件1080p60FPS尚未实测。

A-Vent实际2/2通过、0失败/跳过、跨模式框架5.9062148秒。证据../Evidence/S2-W05/A-Vent.json。T01真实E读维护提示、W/D实际移动验证核心关闭门与通道侧墙碰撞；T02真实Echo调查锁定终端，仅设备摘要入库，解锁回执未注册/未知，核心门保持关闭。没有自动操作终端或伪造扫描凭证。

测试后恢复工具拒绝：当前布局已变为干净的S1 ES_Cabin_Demo，并非空宿主或先前Greybox。PostTestWorkspaceInspection.json实测未播放/无脏场景/后台false，475份S1 SHA不变。保留这一新干净布局，后续测试另建恢复快照；没有强行切回旧Greybox，也没有保存或丢弃改动。布局变化来源未确认，见F091。实际雾与供电仍由P03独立实现，密码操作由P02实现，不能用本报告代替其验收。
