# R4-A01c-2 新Rover与传统控制器

2026-10-30，Binding_A实际3/3通过、0失败、0跳过、5.549秒。Unity6000.6.7f1／Windows开发机，真实PlayMode旧R2场景中临时挂载；不保存旧场景，不写旧槽，不执行LLM。

RoverAppearance保留EchoRig原4个隐藏驱动轴，实际6轮按各自测得半径和真实NavMesh速度旋转；新身体／探头／扫描原点／三色灯绑定到原传统表现接口，实时扫描灯随新原点移动。旧核心源码未改。新场景可保存已经放置的视觉，运行时不重复实例化；本报告验证临时挂载／解除，正式新场景仍待A01e。

实测：蓝平静／黄思考闪烁／红警戒材质、探头旋转、扫描灯亮灭与父级恢复；六轮直接驱动和实际跟随位移>0.4m全部转动，Wait后全部停止；保留实体BoxCollider并无墙体穿透。禁用后还原旧Rig／Renderer／扫描灯，重新启用只有一个可见Rover。

真实调查cargo_a使用新探头原点，首次只增加可信发现、扣8电范围正确，物品仍World、交付0，未代搬运／开门／改设备。UseLLM=false。旧四轴缓存与传感器四轴校验保留，不用视觉六轮强改旧契约。

原始Evidence/R4-A01c-2/Binding_A.json及Job／SceneSafety／RenderSafety，States_GUID／Movement_GUID／Scan_GUID.json。各用例卸载临时场景并恢复背景运行标志；测试启动Console0、普通Forward／内部HDR→SDR／D3D11。完整新关卡／Windows流程、目标机性能仍未验收。
