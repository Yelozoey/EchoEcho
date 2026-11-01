# A01f 新槽与显式偏好基础

2026-10-30，按N02 No-Go后转R4基础项，N05／N06参数不变。A01e后先细分三步，不将它们记作R3通过。

f-1：SalvageNext/slot1.json，format echostation.salvage-next／version1；payload为原v2世界完整封装、Preference枚举0–2、Quality枚举0–2。默认Nearby／Medium。UTF-8严格JSON、重复／额外字段拒绝、SHA256、96KiB预算，临时文件Flush(true)＋原子replace，保留有效备份；未来版不回退或覆盖，双坏档拒绝写，坏主档只有有效备份时才保留rejected副本后恢复写。仅保存显式设置；UseLLM不入档且始终false。

f-2：复用原SalvagePersistence.Capture／TryRestore的世界合法性和几何验证，不调用它的旧槽Save／Load；新控制器F5／F9写新槽。世界恢复成功后才提交设置并推进会话代次、通知界面；失败不改设置、不取消任务。恢复基础follow／wait，取消in-flight调查／警告。原子档将设置与进度统一提交，读坏档不能半应用。

f-3：旧面板源码保留，在新场景移除旧面板组件并提供新版R／T前端，仍复用原脚本Hub。新增明确“就近／高分／轻量”偏好按钮，以及建议与单目标确认；建议只基于已扫描且可达、World且≤8kg货物。文字建议可指定本次排序但不改长期设置；建议不自动执行或排队，确认前重查资格。所有按钮为脚本，AI No-Go状态直说未认证。保留T文字输入／歧义目标选择／历史与取消规则。
