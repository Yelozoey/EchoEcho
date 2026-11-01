# R2-N01b 动态障碍

2026-10-29，通过。Obstacles_A真实PlayMode5/5、14.309秒（动态2项与受影响导航3项），完整门禁。证据`../Evidence/R2-N01b/Obstacles_A.json`。

玩家Capsule obstacle半径0.33m/高1.75m、货物Box尺寸与实际碰撞相同；均carving、stationary0.15秒、移动阈值0.1m。真实Agent从z9绕过z12货物与z15玩家至z17.7，横向让行>0.6m，每帧位移<0.8m、无玩家穿透、实体碰撞保留；拿起禁用货物Obstacle、放下重新启用且World状态正确。危险碰撞烘焙到独立ES_Salvage_R2_Nav，保留R1 Nav资产。

N01顶层完成，R2为3/6。性能目标机、R4流送/正式美术未验收；本步尚无扫描发现。下一步P01a。

后续B01b修订：F217证明Carving影响有效历史位姿加载，最终8个动态障碍改为carving=false局部避让；静态Nav烘焙与真实碰撞不变。Save_B28/28包含同一2项障碍+3项导航再验收，原绕行/位移/不穿透阈值未降低。上文carving=true为早期版本实测记录，最终参数以R2_NavigationParameters.md修订为准。
