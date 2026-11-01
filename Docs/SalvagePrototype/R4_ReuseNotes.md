# R4 复用接口与保护要点

2026-10-30，代码审查记录；不是R4完成报告。

- EchoRig.Configure固定四轴（旧核心不改），可用独立新视觉层兼容实际轮组；Body／ProbePitch／ScanOrigin／Status可读，Configure可重新绑定。先实查Mars Rover ZIP格式与分离部件，不能先承诺有可动轮子。
- CargoRegistry／Definition／State在SalvageCatalog.cs，八扫描目标与可信摘要在SalvageTargetContract.cs，重物不在扫描目录。新视觉不能改六件ID／kg／积分。
- SalvageCommandHub.Submit非virtual且直接脚本解析，旧Panel直接订阅GameTextInput。若新版需要显式偏好与可选AI文字路由，使用自有Panel并在新场景禁用旧Panel；不要反射拆私有事件或修改旧R2源码。复用GameTextInput中文输入和SalvageWidgets／Ui布局；保留Esc捕获到LateUpdate的F211后续规则。
- 按钮始终传统层。建议按钮只显示C#已知可达排序结果，确认单目标才Hub.Select。禁用旧UI时还要验证Mission模态／暂停与输入拦截，避免文字框同时拾取或跳跃。
- SalvagePersistence可Capture／TryRestore世界v2、内部已有校验／几何／回滚；旧Update处理F5/F9。新槽适配可在新场景禁用其Update，由新原子外层封装世界＋偏好＋画质，仍调用经过测试的内存恢复；不要写R2目录。有效恢复成功后同步递增新会话并取消旧模型，主线程同步调用间不应用回包；无效档不得先取消当前任务。
- 旧历史上限100显示／50存档，玩家200／回复280保持。不要通过截断当前100条历史来伪造失败恢复；复用旧恢复器对原History引用的回滚。
- 旧StaticRoomMeshCombiner只禁用source Renderer，仍保留MeshFilter引用。新流送场景若离线合并，应在已归档可重建原版与A/B证据后删除自有临时实例，避免以合批名义常驻双份几何；不能删除第三方源文件。合并按房间、材质与实际Lightmap索引分组，烘焙后保留UV2／scaleOffset。
- RenderSafety硬编码允许旧Baseline／ScanWave／S2管线，新场景测试可由干净S2空宿主入场，然后显式断言实际新专用Pipeline／Renderer／HDR／D3D11与非黑图；所有作用域finally还原。不能把入口S2证据写成新场景已绑定验收。若必须改旧安全工具，应记录兼容例外并重测，不静默改旧受保护文件。
- 来源清单原SalvagePrototype严格恰好7组；Mars Rover用新来源清单，不破坏原校验。新Docs/Assets/Sources/SalvageNext、Addressables新本地构建缓存／新构建路径都必须加入20GiB核算，不能借目录漏算。
- 原包含FBX／FBX Unity／OBJ／glTF与贴图，免费Standard无Unity场景。仅选择需要模块，不整包导入敌人／武器／脚本。只保存新自有资产，禁用AssetDatabase.SaveAssets批量保存；沿F220最终精确还原旧设置SHA。
- R4新Windows目录复用本轮自有Builds/SalvageNext；最终可裁剪该新构建副本中不使用的模型，旧项目权重／R1R2构建不动。No-Go版本不显示AI已可靠，仍保留原AI实验源码与报告。
