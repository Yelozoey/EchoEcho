# A01d 回收物视觉适配参数（实施前记录）

2026-10-30，N03／N08普通适配委托。kg／积分／ID／8kg限制／主动结算规则不变；只在新版或临时运行场景换视觉，不修改R2资产。

| ID | 视觉来源 | 最大边初值 |
|---|---|---:|
| cargo_a | 已验证Poly Haven金属工具箱 | 0.44m |
| cargo_b | Quaternius Prop_Crate3备件箱 | 0.44m |
| cargo_c | Prop_Crate4封闭样本箱，浅青色 | 0.44m |
| cargo_d | Prop_Computer便携仪器屏幕 | 0.44m |
| cargo_e | Prop_AccessPoint接口电源模块 | 0.44m |
| cargo_f | Prop_Chest静态记录箱 | 0.44m |
| cargo_heavy | Prop_Barrel_Large超限设备，仍12kg不可搬 | 0.8m |

按原比例统一缩放、底部Y0／XZ中心0，保留PBR贴图，以自有材质副本调整色调。Collider按实际视觉Bounds＋XZ各2cm／Y1cm余量适配，不改变传统搬运逻辑；同步本对象NavMeshObstacle的center／size且保持carving=false。手持／交付随原Root父级／active变化，标签手持时隐藏、世界时恢复，避免挡住视线。实际六件拾取→放下→再拿起→回收点确认交付及重物拒绝必须通过后才整合。
