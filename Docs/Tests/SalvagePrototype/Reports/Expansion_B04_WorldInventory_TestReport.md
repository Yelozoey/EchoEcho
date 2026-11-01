# B04 场景背包转移与批量交付实际报告

2026-10-31，Unity6000.6.7f1/D3D11，World_A实际12/12、0失败/跳过、16.1163762秒；其中本模块7项（1目录/6实际Unity场景）和受影响B03面板5项回归。Job516080d15b214a99a66d5eb7efd4b8d5。

新增7类物品目录、ExpansionItemActor与ExpansionWorld。实际射线/距离和对象身份检查后收纳；背包物隐藏碰撞体，手持绑定相机并按既有重量规则减速；安全地面放下先验证落点再提交，失败不丢物。泊位批量交付预览、确认、取消、库存版本/位置/焦点检查，工具与电池保留。未接设备前核心默认锁定。

实际测试：InputSystem合成E＋真实Physics射线收纳、重复拾取/伪造同ID对象拒绝、核心权限/空间不足保持源物体、真实UGUI装备和暂停内放下/取回、落点受阻原状态不变、批量交付取消/原子确认/过时版本和重放拒绝、离泊位/失焦取消。初始三件装备8格，核心同样占6格，7物品唯一ID；无总重减速。

[原始结果](../Evidence/Expansion/B04/World_A.json)、[Job](../Evidence/Expansion/B04/World_A-Job.json)、[场景门禁](../Evidence/Expansion/B04/World_A-SceneSafety.json)、[渲染门禁](../Evidence/Expansion/B04/World_A-RenderSafety.json)。新脚本仅在独立运行时夹具内执行，未写旧场景或发布新Windows包。实际新任务地图留B06，设备/工具解锁留G01/G03。

B04发现并修正UI物品已放下后详情仍称“背包内”的呈现；同轮5项B03真实UGUI/暂停/输入/截图回归通过。Run01全局临时scope仍在，结束时统一精确还原原设置与S3布局。
