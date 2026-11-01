# R4-R03 区块流送实施参数

2026-10-30，前置R02c Effects_D3/3已通过。用户已授权2.11.2与必需依赖；本文件是实施选择，不是运行验收。

R03a再拆a-1官方依赖/安装与版本保护、a-2本地Bundle构建/加载卸载冒烟。官网registry已读2.11.2最低Unity6000.0，依赖新增SBP3.0.3；既有Profiling1.0.3与TestFramework1.8.0满足其1.0.2/1.4.5要求，保留既有版本。先备份manifest/lock与完整版本图，再安装；发生旧包版本被迫改变则停下，不默许升级Unity/URP/其他包。

设置与内容优先进入SalvageNext自有Addressables目录，仅本地Build/LoadPath和Catalog，无远端地址。必要EditorBuildSettings的Addressables配置映射属于依赖接入，原构建场景列表保持并另列精确例外。三区静态视觉独立本地Addressable Scene，碰撞/Nav/逻辑货物常驻，默认UseLLM=false。

R03b：距离到区域Bounds预载10m、卸载18m，玩家/Echo/有效调查目标取并集；相机/阴影依赖运行时绑定，不能序列化跨Scene对象引用。主场景保留独立探针；房间绑定自己的Mesh/反射/Lightmap引用，全局Lightmap槽注册/释放后再Unload handle。原两Atlas跨区共享，后续需要房间自有派生Atlas或明确最后引用释放，不能声称共享Atlas已逐区卸载。移除主场景LightingData对全部贴图的强引用须实际验证；原数据留存。

R03c：实际快速往返、加载中取消/退出、过期代次、持有物/存档读回、Mesh/贴图/Lightmap资源清理。10m在3.5m/s约2.86秒，拿实际最慢加载验证缓冲。拆场景后的PVS重新构建/验证，不能沿用拆分前36→20结论。所有Unity API在主线程；异步加载不阻塞主线程，资源句柄只有一个所有者，晚完成也要释放。

SDK源码核查：Settings.Create/Group.AddSchema/DefaultObject setter与SBP.ContentPipeline内部调用AssetDatabase.SaveAssets。不能在用户工作区不加范围保护就调用。a-2前先细分a-2s保存范围门禁：AssetModificationProcessor仅允许本轮SalvageNext资产与明确列出的必要EditorBuildSettings映射，其他资产保存请求留Native脏状态并阻止写盘；全部ProjectSettings字节前后比较。用新建外部占位脏资产实测拦截，只有通过后才调用官方完整构建API，不修改SDK。


b-2细化：滞回retained指已有/进行中句柄，避免加载中抖动；任务位置必须携带当前World/Generation租约，否则不参与需求。房间LoadTicket只在需求退役/重入或World读档代次变化时失效；同区仍需要时的新普通指令不强制重新下载视觉资源，任务执行权限仍由原行为树代次负责。计算无Unity对象、无加载或游戏执行权限。

