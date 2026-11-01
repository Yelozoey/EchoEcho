# 试玩反馈后的资源、开源参考与下一步

核验日期：2026-10-30。本轮来源是作者网站、GitHub仓库README/实际源码与API树。网页许可已读不等于Unity6000.6.7f1/URP兼容性实测。以下“建议”不是已导入功能；旧AI No-Go保持。

## 推荐的推进顺序

1. 完成本轮亮度修正，再把三个区域做出可辨识的视觉主题：泊位较明亮、整齐、以回收台为焦点；储藏区用不同高度的货架/箱组形成视线层次；检修间以管线、设备和少量橙色警示形成识别点。复用现有包，新增资源只补缺口。新增摆放须逐个检查碰撞/导航、PVS、烘焙、流送包与帧开销；不靠装饰堵住Echo门口。
2. 统一HUD：常驻只展示本轮回收进度、手持物重量/价值、Echo电量/状态；交互信息靠近准星，完整信息放R/Tab面板。先用同一套面板、图标、按键提示做一屏样式，再确认后替换全部界面。现有规则不变；新信息布局是下一轮建议。
3. 随后进入R5首次外部试玩：让未看攻略的玩家完成一次，记录辨路、交付确认、Echo等待/堵路、UI误读及首次时长，再决定扩展。先让5分钟切片清楚、好操作，再考虑10–15分钟版本。
4. 求职展示的AI方向，建议单独做“基于已知信息给出有依据的回收建议/解释取舍”的可验收提案，和脚本建议作对照。当前三偏好已有脚本支持，不能直接称新AI能力；模型路线/训练/任务Schema若变化须重新确认。此轮不启动实验。

## 场景素材候选

| 优先级 | 来源与许可依据 | 本项目用途 | 适配边界 |
| --- | --- | --- | --- |
| 首选，先盘点已拥有内容 | [Quaternius Modular Sci-Fi MegaKit](https://quaternius.itch.io/modular-sci-fi-megakit)；当前Standard包已有校验/CC0许可记录 | 继续用同一套墙体、门框、终端、管线、箱体做三区差异，减少混搭 | 本地已有完整免费Standard；Pro/Source不能当成已拥有；模块包不等于成品Unity关卡 |
| 同作者补充 | [Ultimate Space Kit](https://quaternius.com/packs/ultimatespacekit.html)，作者页标92模型、FBX/OBJ/Blend/glTF、CC0 | 选择少量空间设备或大型外观地标，增强窗外/泊位识别 | 具体适用物件须收到包后确认；不能承诺含某个未核验模型；不要整包堆入场景 |
| 备选室内模块 | [Kenney Space Station Kit](https://kenney.nl/assets/space-station-kit)，作者页90文件、CC0 | 观察其室内模块组合与配色；可筛选辅助设施装饰 | 偏简洁低模，需统一材质/尺度后再决定是否与现PBR设施混用 |
| 备选小规模模块 | [Kenney Modular Space Kit](https://kenney.nl/assets/modular-space-kit)，作者页40文件、动画/变体标记、CC0 | 用于小型模块或布局参考 | 与现Quaternius墙体的比例/轮廓未实测，不建议一次替换整个场景 |

现有单一感需要从房间功能、构图、光照和标识一起修正；单纯新增资源数量无法保证改善。优先“同一主材质家族＋每区一个地标＋不同装饰密度”，这些是拟采用的美术方向，不是已完成场景改造。

## UI/HUD素材候选

| 候选 | 已核实内容/许可 | 具体映射 |
| --- | --- | --- |
| [Kenney UI Pack – Sci-Fi](https://kenney.nl/assets/ui-pack-sci-fi) | 作者页130文件，button/panel/slider等标签，CC0 | 任务板、Echo面板、设置窗口、进度条统一边框与按钮；主选 |
| [Kenney Input Prompts](https://kenney.nl/assets/input-prompts) | 作者页1500文件，含键鼠；[作者说明](https://kenney.nl/knowledge-base/game-assets-2d/using-input-prompts)注明PNG/SVG与64/128尺寸，CC0 | E/G/R/T/F5/F9/F10提示。当前PC优先取键鼠，保留项目中文字体，不引入与本项目无关的手柄功能 |
| [Kenney Crosshair Pack](https://kenney.nl/assets/crosshair-pack) | 作者页CC0；[作者OpenGameArt发布](https://opengameart.org/node/84345)提供200种准星资源 | 选择细点/圆环表示可交互目标与扫描状态；采用克制样式，保持无战斗定位 |

建议面板采用半透明深蓝灰底、明亮文字、少量青色高亮，橙色只提示风险/无法执行；保留中文Noto字体与字号可读性。图标只是视觉资源，焦点、键盘导航、输入互斥、分辨率适配、扫描状态等仍需用项目UI逻辑实现与测试。

本轮尝试获取UI Sci-Fi与Space Station原始ZIP时响应未通过ZIP签名门禁，文件没有进入Assets，未把网页许可当作下载/导入验收。候选链接可直接打开；本轮不购买付费合集或借助第三方不明镜像。研究元数据位于Research/20261030。

## GitHub：按可借鉴功能筛选

| 项目 | 实际核验与许可 | 对EchoStation的借鉴建议 | 采用边界 |
| --- | --- | --- | --- |
| [Unity Open Project 1 — Chop Chop](https://github.com/UnityTechnologies/open-project-1) | 官方完整动作冒险Demo，Apache-2.0；README明确2021年12月停止开发、基于2020.3LTS。已读QuestManagerSO与InteractionUIEventChannelSO，API树含Inventory/UI/Quest模块 | 最值得先读：交互提示事件、物品数据与UI、任务步骤/奖励/保存衔接。将未来“回收委托”设计为清楚的数据与完成条件 | 原游戏有战斗，本项目不搬战斗；旧Unity版本，不能整仓升级/导入当前工程；美术各自许可仍须逐项查 |
| [Unity Behaviour Trees / Blackboard](https://github.com/adammyhre/Unity-Behaviour-Trees) | LICENSE为Unlicense；已读Blackboard.cs，实际类型化读写/键登记/调试入口；API树含Arbiter/Expert | 借鉴Echo状态快照、行为树调试展示，让开发时看得到当前目标/原因/取消结果 | 本项目已有行为树与任务代次，不为使用库重写稳定模块；外部示例的键哈希/全局可变状态需独立审查 |
| [crashkonijn/GOAP](https://github.com/crashkonijn/GOAP) | Apache-2.0；README含Jobs规划、节点可视化、演示；API树实际含Miner/WoodCutter/Cleaner、最近物品/搬运目标传感器 | 可参考“目标→前置条件→动作成本→计划预览”的表达，帮助玩家理解Echo建议与失败原因 | GOAP不是LLM；现六意图/单步确认不变，不新增自动任务队列或Echo搬运。未来若采用规划器需独立方案/性能实测 |
| [LLMUnity](https://github.com/undreamai/LLMUnity) | Apache-2.0；README明确本地CPU/GPU、RAG、grammar/JSON Schema、FunctionCalling示例、CancelRequests、history/warmup；说明Unity6测试过 | 参考请求取消、流式反馈、已知资料检索与模型管理的交互/工程边界，作为AI集成对照资料 | 使用llama.cpp/LlamaLib与GGUF，和当前ORT量化ONNX路线不同；这不意味着现模型准确率会改善，本轮不安装/换模型/换后端。API树请求超时，未声称深入源码验证 |
| 补充：[Unity Inventory System](https://github.com/adammyhre/Unity-Inventory-System) | README：UI Toolkit、数据绑定、保存、多场景/Addressables；API树有Model/View/Controller等。当前根/浅层树未见LICENSE，GitHub许可字段未识别 | 可看物品卡片、信息展开、拖拽的交互演示；对客户端UI学习有参考价值 | 本轮只参考设计，不复制代码；当前单件手持不改成网格背包。需复用源码先查明授权 |

这些是完整Demo与专项功能参考的组合，并非已找到一个可直接搬进项目的“LLM回收游戏”。建议先看Chop Chop的交互/任务/UI，再看LLMUnity的功能边界。公开可查看不自动等于可以复制所有源码和素材。

## 下轮建议模块与验收

| 模块 | 具体工作 | 独立验收 |
| --- | --- | --- |
| V02 三区视觉识别 | 先做泊位样板，再储藏/检修；一处地标、清楚门洞/路径、复用材质 | 同机位截图、导航/携带通过、无流送漏图/漏光、绘制和内存回归 |
| U01 HUD样式样板 | 一套面板/按键/准星，先呈现现有状态 | 1080p/720p可读性、不同背景对比、键鼠焦点、不遮挡交互 |
| U02 全界面接入 | 复用样式到R/Tab/F10/交付确认 | 输入互斥、保存/取消/超时、中文排版、完整操作路线 |
| R5 外部试玩 | 真实首次玩家，无攻略完成 | 首次时长、卡点、目标理解与Echo帮助的实际反馈 |

上述V02/U01/U02/R5是下一轮提议，用户本次直接授权的亮度修正与研究先独立交付，不据此扩展新机制或恢复模型实验。
