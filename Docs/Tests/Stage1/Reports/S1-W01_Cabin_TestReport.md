# S1-W01 观测小屋灰盒验收

2026-10-25，v0.2。前置R01–R05已归档；W01按A几何碰撞、B1合并/UV、B2烘焙、B3遮挡、C Windows顺序独立开发测试。本模块功能通过，允许进入W02。

- 最终Editor回归73/73（R01–R05的63项+World10项），0失败/跳过；EditMode-Final.json。测试前只读脏场景门禁，未自动保存用户工作。
- Windows Development构建成功/0错误，独立D3D11 1920×1080程序退出0/未超时，PlayerBuild.json、Player/Exit.json。23次实际胶囊扫掠全通过，跨两门往返畅通，墙/地/顶/设备/门侧均阻挡；使用默认PhysicsScene同步变换，不对自动模拟场景重复Simulate。
- 30个灰盒源Mesh/Collider保留，8个同室静态合并体，合并后UV2重新展开并验证无内部重叠；两张方向性Lightmap、141探针、8份有效静态绑定。
- Player三处动态采样SH幅度1.2463/3.9216/3.7763；三室最终画面线性平均亮度.032316/.066106/.072031。禁用Baked灯画面差0，移除Lightmap后亮度0/平均差.083304。内部HDR=true，显示器HDR=false。6张1080p图像保存Player目录，主房截图视觉复查可见冷灰蓝房间、设备、桌子，无粉色；仍为灰盒代理。
- B3独立正常帧实测遮挡关闭/开启Draw Calls13→9、入储物室10，画面差0。细节见B3报告，不能用SRPBatch数量代替Draw Calls。
- audit_w01.py九项审计通过（FinalAudit.json），13份受保护资产原始SHA保持、Editor播放选项恢复true/1。源码入口World/Editor与Tests/WorldEditor，独立Player驱动Tests/Fixtures/CabinPlayerProbe.cs。

执行环境Unity6000.6.7f1、URP17.6普通Forward、Win11、i7-12650H/RTX4060 Laptop7956MB/16122MB。目标Win10/11 x64、1080p：最低i5-8400/GTX1060 6GB/16GB中档60FPS，推荐i5-12400/RTX3060 6GB/16GB高档60FPS，均未获得对应硬件实测；本模块没有作性能达标声明。

关联故障F-036合并UV/探针筛选、F-037测试自动保存风险、F-038可读性、F-039批次度量、F-040Editor-only API。失败和首版不足证据保留。已知限制：没有正式PBR资源、玩家、交互、日志、Echo；本报告不代表S1整体完成。
