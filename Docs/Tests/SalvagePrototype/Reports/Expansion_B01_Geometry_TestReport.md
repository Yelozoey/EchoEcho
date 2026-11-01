# Expansion B01 背包格子与形状实际测试报告

日期：2026-10-31。结论：B01独立通过，22/22，0失败、0跳过。仅交付形状/占用快照/放置判定；游戏内背包界面、拾取、存档和新任务尚未接入。

## 依据与实现

用户已确认6×4背包、矩形＋少量L形、90°旋转、共享容量、不堆叠、整理暂停，以及环境危险＋工具操作方向。B01边界见[模块计划](../../../SalvagePrototype/Expansion/B01_ModulePlan.md)。

新增SalvageExpansion/Domain独立程序集，InventoryCell、InventoryFootprint、InventoryBoard为自行实现的纯C#代码。输入防御复制、形状左上归一、四向旋转、6×4实际占用格重叠判断和不可变快照；未复用第三方仓库源码。实际已加载DLL仅引用netstandard，不引用UnityEngine，未接入GameObject或AI。

## 实际执行

- Unity6000.6.7f1，Windows Editor，Direct3D11；干净空宿主S1_P09_RegressionHost。
- module_tests.py启动，筛选EchoStation.SalvageExpansion.Editor.Tests；实际Job：d32b6e0979d341b1927553736993f674。
- 结果22/22，0失败，0跳过，Unity报告耗时0.6891889秒。这不是整套玩法或性能验收。
- 原始[Job](../Evidence/Expansion/B01/Geometry_A-Job.json)、[实际结果](../Evidence/Expansion/B01/Geometry_A.json)、[SceneSafety](../Evidence/Expansion/B01/Geometry_A-SceneSafety.json)、[RenderSafety](../Evidence/Expansion/B01/Geometry_A-RenderSafety.json)。

| 实测范围 | 结果 |
| --- | --- |
| 空/重复/负格和溢出边界 | 拒绝非法输入；极大坐标不会回绕为合法位置 |
| 原输入与只读集合 | 外部数组/列表变更不影响已构造对象；只读集合拒绝写入 |
| 非对称L形旋转 | 90°具体格坐标正确；宽高交换、面积保持、四次还原、负次数及int极值取模正确 |
| 贴边与完整容量 | 6×4整块与右下角合法，负锚点/越界拒绝；1×6长条旋转后可放入 |
| 凹角与碰撞 | 实占格重叠拒绝；L形凹角中的其他物品不会被包围盒误判 |
| 查询不提交 | 成功、重叠、越界查询之后占用快照不变 |
| 组合参考检查 | 12占用布局×4形状×4方向×48锚点＝9216组，与逐格参考判定一致；这些是1项测试内的组合检查，不冒充9216个独立测试用例 |

## 还原与旧工程保护

测试临时绑定既有ES_Gameplay_ForwardHDR普通Forward/HDR管线，未保存临时绑定。测试后GraphicsSettings、QualitySettings、EditorSettings的完整native JSON、磁盘字节和dirty均精确还原，见[NativeRestored](../Evidence/Expansion/B01/NativeRestored_A.json)。

原ES_S3_AI_Demo单场景布局、选择与播放选项由regression_host恢复；最终场景干净、非播放，PC1、默认/PC均VC_ForwardHDR、后台false、播放选项enabled=true/flags1。原生最终再次核对完整native/disk/dirty，Console错误0；[最终工作区](../Evidence/Expansion/B01/FinalWorkspace_A.json)、[Console](../Evidence/Expansion/B01/FinalConsole_A.json)。

最终RenderSafety.allowed=false，因为恢复的原VC管线不在该测试门禁路径白名单；实际普通Forward、内部HDR/SDR、D3D11保持，未在该恢复状态继续跑测试，未修改白名单。[最终RenderSafety](../Evidence/Expansion/B01/FinalRenderSafety_A.json)。

3767项既有代码/YAML/meta及ProjectSettings/Packages SHA全部一致，0差异；[保护结果](../Evidence/Expansion/B01/Protection_after_A.json)。此清单不覆盖二进制美术或模型权重，不将其声称为所有旧二进制文件逐字节复验。旧Windows发行包未重打或覆盖。

## 故障与限制

F300：错误查询了不存在的RenderSafetyGuard.cs，改rg发现实际入口；Unity后台无响应按computer-use窗口流程恢复，未重启或保存/丢弃用户场景。刷新后短暂no_unity_session属于域重载期间状态，等待新程序集与真实空闲恢复后才启动Job；无Job时未误轮询。详见Docs/Troubleshooting.md。

未执行游戏内拖拽、实际物品转移、暂停、存档、危险/工具/完整路线、Windows打包或目标硬件性能测试；这些属于后续模块。B02只在本报告归档后进入。
