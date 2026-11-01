# R3-K01b 可信发现到摘要上下文

2026-10-29，最终Context_B实际8/8通过、0失败、0跳过、2.020秒；此前Context_A8/8后补充三种排序的可信首选摘要，再完整复测。Unity6000.6.7f1／Windows开发机，真实PlayMode场景＋实际Qwen3 tokenizer；不是模型质量／目标机FPS验收。

新增主线程RecoveryKnowledgeCapture，只遍历已发现的八目录目标，真实NavMesh完整路径长度，排除不在场／已持有／已交付；后台调用在任何Unity访问前拒绝。RecoveryContext只由可信快照重建摘要、显式偏好、交付状态与三种已知可达首选，不把玩家输入写成记忆。未知指向只提供身份和调查权限，不提供隐藏摘要或位置。

实际：新场景发现集为空；通过有效快照恢复cargo_a/b后取到两项真实非零NavMesh路线、高分首选b；移动未知f到远处不影响已知集合；Task后台捕获明确拒绝。纯数据覆盖猜测不成事实、未知指向隔离、中文短名称检索、无关问候不灌历史、快照互不污染与三偏好首选。

真实tokenizer检查检索≤8完整条目、≤1200 UTF-8字节、Prompt≤1536tokens及256输出预留／总≤2048，逐行确保无半条裁剪。默认旧Prompt只用于本模块预算验证，未修改原v5，未执行模型推理。

入口module_tests.py，EchoStation.SalvageNext.Editor.Tests，Recovery(Context|CaptureScene)Tests。原始结果Evidence/R3-K01b/Context_B.json、Job、SceneSafety、RenderSafety；空宿主／Console0／普通Forward／内部HDR→SDR／D3D11门禁均通过。复用的R2场景、源码与槽未写入；测试只做内存恢复，结束卸载并还原背景标志。

F224复审修复：同状态重复采样不再递增revision，仅会话／显式偏好／已知成员状态／可达性／交付变化递增。新增真实场景断言后Combined_B39/39联合通过（Evidence/R3-J01a），本模块8项包含其中。
