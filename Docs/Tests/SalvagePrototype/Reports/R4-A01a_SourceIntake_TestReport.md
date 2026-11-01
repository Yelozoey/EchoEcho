# R4-A01a 来源接收门禁

2026-10-30。实际文件级4/4通过，0.249秒；不是Unity兼容、外观动画或目标硬件验收。

Quaternius Standard官网已重取48,559,316B／93分段，与旧包SHA完全一致、ZIP CRC通过，旧包仍完整，无Unity场景／Prefab，使用其模块而非未购买Source工程。Mars Rover从作者指定OpenGameArt页与公开文件取得，210,646B，SHA256 `474e8e6b29a7dd80304687300f6a8bf16dba54a7963912808011c9f9303d12c4`，5条目／806,295B展开、CRC及安全路径通过，作者JamesWhite、页面CC0证据与哈希保留。

Rover实际包含rover_mesh.obj、rover_mesh.blend、rover_diffuse.png及Mac元数据。OBJ为单对象（1268源顶点、704源多边形），Y向上范围0.065–2.221；没有单独命名车轮／材质文件。连通与边界分析发现六处低位轮组，需要后续在新派生副本拆分，不能把原OBJ说成已带可动骨骼。优先使用OBJ和PNG，跳过.blend／__MACOSX，无需Blender安装或执行来源脚本。

测试Tools/SalvageNext/test_rover_intake.py，原文Evidence/R4-A01a/Source_A.log，ObjMetadata.json／ObjComponents.json；完整接收Docs/Assets/Sources/SalvageNext/RoverReceipt.json与RoverVerification.json。原Standard哈希与CRC再次通过，旧清单不加第8组，新来源单独记录。未导入Assets、未执行下载代码、未改系统网络或付费。
