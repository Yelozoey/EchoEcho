# B03 背包拖拽与暂停实际报告

2026-10-31，Unity6000.6.7f1/D3D11，最终Ui_B实际9/9通过、0失败/跳过、12.9693638秒；Jobd92c111b1d954298ae493d736a0eb63e。

新增InventoryDrag、BackpackPanel/CellPointer、ExpansionPause。拖拽只预览，松开后检查库存版本并提交；旋转保持抓取格，过时拖拽不能覆盖其他有效修改。6×4真实UGUI显示形状/空角、选择详情、装备与安全放下入口；I/R/Esc与场景输入互斥。暂停使用嵌套租约，保存原时间倍率、移动enabled、NavMeshAgent停止状态和指针，最后租约释放才还原。

## 实际验证

- 4项Domain拖拽测试：取消/非法落点无修改、旋转抓取偏移、过时版本、空角/世界物品拒绝。
- 5项Unity测试：真实NavMeshData与Agent停止/恢复，嵌套租约与原0.75时间倍率、原禁用组件状态；实际UGUI指针事件链拖放；InputSystem合成键盘I/R/Esc；禁用/失焦释放；1080p/720p真实产品UGUI离屏渲染和文字边界。
- 输入夹具复用既有SyntheticInputScope，并在每项UnityTearDown实际断言原InputSettings/HideFlags已还原；未将合成键盘或UGUI事件链描述为Windows人工键鼠验收。
- 实际UI截图已查看，中文可读、24格和L形空角清楚。截图是产品Canvas经URP渲染的1920×1080与1280×720，当前没有背后的整局场景；非最终Windows体验。

原始[Ui_B结果](../Evidence/Expansion/B03/Ui_B.json)、[Ui_A失败](../Evidence/Expansion/B03/Ui_A.json)及B03目录的Backpack/Bounds文件保留。Ui_A为8/9，F301定位为测试漏算旋转抓取偏移，增加固定GrabX=2断言并修正最终X=1；产品行为没有为通过测试而改动。随后同范围9/9复测通过。

独立模块通过后进入B04实际拾取/放下/装备视图/交付；UI内放下按钮目前需绑定B04回调，工具具体使用与危险暂停还需G02/G03整合验证。当前Run01临时渲染scope仍有效，最终场景/设置/保护清单还原留连续任务结束时核验，不提前宣称完成。
