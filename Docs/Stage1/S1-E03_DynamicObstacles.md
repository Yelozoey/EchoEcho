# S1-E03 动态障碍与重规划、独立测试计划

2026-10-25，前置E02已独立通过。复用已验证NavMeshAgent.autoRepath/HighQualityObstacleAvoidance，新增可复用移动柜障碍模板；不引入门机关/推动玩法或行为树。普通实现与数值由连续授权委托代理决定。

障碍模板1.1×1.3×.8m，BoxCollider与NavMeshObstacle同范围、根scale1，动态URP Lit/BlendProbes、无Static/MPB。Carving=true、OnlyStationary=true、移动阈值.05m、静止.2秒；移动时参与局部避让，静止后更新导航孔。产品场景暂不增加额外谜题机关，模板与实际测试场景证明能力。

规则：有完整路径时自动绕行；通路完全封闭时安全停止并报告不可达，不穿过障碍。障碍移除后等待剔孔更新，再发出新的有效移动指令即可恢复；不在后台自动复活已经被wait/取消的旧指令。之后follow等行为可按自己的已授权节流策略重发请求。

独立测试：配置与Collider包络；正在行走时前方主房新增障碍，静止剔孔后路径/实际轨迹绕过、逐帧无穿透并抵达；门斗门洞完全堵塞时拒绝/安全等待，移除后同目标新指令恢复；记录实际NavMesh.SamplePosition/路径前后变化、位移与超时，不以carving开关为唯一证据。通过归档后才进入E04意图权限。
