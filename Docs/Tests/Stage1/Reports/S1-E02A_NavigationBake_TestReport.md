# S1-E02-A 导航烘焙与路径连通

2026-10-25，v0.2。A-Envelope.json 3/3，0失败/跳过；小屋持久化NavMesh实际147顶点/195索引，专用Echo agentTypeID=-1372625422。负数ID合法，只有-1代表未找到。半径.54/高.9/步高.15/坡度35、Voxel.08/Tile128，PhysicsColliders/Children收集，无自动Links。

原Humanoid半径.5/高2/步高.75/坡45以及原Area段保持，修改前完整NavMeshAreas在Evidence留档；实际NavMeshQueryFilter验证门斗→主房→储物室两段Complete路径。单房合并Renderer没有代替实体Collider，数据保存Echo/Navigation/ES_Cabin_NavMesh.asset并由场景Surface引用。

v0.1 .38半径初测3/3保留A-Bake.json；转弯包络审查按.75m方形根Collider外接半径.5303修订为.54，增加包络断言，重新烘焙/3项复验后才进入B。重建仅替换成功生成的本模块固定DataPath派生资产，不删除其他文件。

F-045公开API误用已修复并经safe_menu门禁；Unity6000.6.7f1/AI Navigation2.0.16/Windows Editor，当前机功能测试，无目标硬件性能声明。A通过，允许B实际Agent控制与不可达测试；E02整体尚未完成。
