# R4-R02c 扫描与受限粒子

2026-10-30，Effects_D3/3、6.469秒，module_tests门禁/Console0。已看ScanOn_bd7b…真实蓝色扫描波纹；不是粒子扫描。

真实新Rover调查cargo_a经Aiming事件驱动ScanWave，图像差约0.00799654，FrameDebugger一次DrawProcedural/Echo Scan Wave。扫描新建粒子系统0、首次扣电约8.065、货物仍World、交付0。1000次状态Begin/Tick/Cancel观察0B分配，仅适用于状态操作，不冒充全帧或整扫描零GC。

两危险点共用池容量2、每套8粒子；实测峰值16。第三并发拒绝、过期租约不能停止新代次，24次发射复用相同2个系统，真实自然生命周期自动归还；菜单未捕获/远距离拒绝，禁用归还全局预算，退出场景预算回基线。自动只在12m内、视锥可见、可操作且未暂停时3秒一次；无伤害、机关、积分权限。

F251编译依赖门禁阻止旧DLL运行后补自有Editor引用；F252用真实生命周期代替暂停式Simulate控制；F253菜单释放指针须显式捕获；F254直接移动子相机要还原localPosition，均保留首失败A/B/C并复测，不改产品边界。D启动前既有IAP编辑器空响应按F138保存后清Console再走门禁，没有忽略产品错误/改服务网络。

旧资源保护审计BeforeAddressables_A：3610文件中仅宿主Graphics/Quality临时绑定变化，16.1687GiB/20GiB。最终还原尚未执行。本模块不是Windows性能或目标硬件验收。
