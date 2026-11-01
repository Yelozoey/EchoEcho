# EchoStation 故障与经验记录

## F-380：凭据格式扫描命中上游脱敏说明示例

Audit_Index_A资产/依赖/大小及15个模型元数据SHA均通过，但凭据URL规则命中BlenderBridgeTool.cs856。未上传前先红显脱敏上下文，确认是上游XML注释对user/pass虚构URL的安全说明，并非本机凭据。用路径/行号/类型/整行SHA精确白名单加理由留档，源码不改；修改这一行将重新触发审查。扫描报告不输出疑似值，只输出位置和哈希，未将真实凭据自动忽略。

## F-379：Git自动换行可能破坏已校验元数据与旧Prompt

首次git add因全局autocrlf及初版attributes出现CRLF/LF转换提示；未改工作树文件，也未提交/上传。模型manifest包含NOTICE/PROVENANCE/tokenizer等逐字节SHA，旧C# verbatim Prompt也可能受源换行影响，不能默认统一换行。将新仓库attributes改为* -text（保留正常diff），仅本仓库autocrlf=false，再add --renormalize重建索引；发布审计新增从git show暂存blob读取模型小文件实际字节对照manifest。没有改全局Git或模型资产，不能只核验磁盘而忽略索引转换。

## F-378：发布准备的许可证下载TLS中断

原机urllib读取官方raw.githubusercontent.com许可证遇到SSL UNEXPECTED_EOF，未关闭证书校验或改代理/全机网络。web工具仍能读取同一官方raw MIT全文，按已核对全文保留完整版权与许可到ThirdPartyNotices，记录来源和实际文件SHA，不声称本地原请求下载成功。Mars Rover官网本轮web受限，复用项目先前已核验的JamesWhite/CC0来源记录，不据抓取失败判断授权改变，也不下载其他镜像。

## F-377：浅色墙使无背板岗位白字失去对比

F02真实三视角检查发现，原暗金属墙上的白色PostLabel在涂层墙上不易读；11/11渲染工程测试不能代表这个视觉问题不存在。F03只在新场景将4个岗位TextMesh顶点色改为深蓝灰，保持内容/位置和黑底路线牌白字；已读WorldLabel Shader确认会使用顶点色。它是无烘焙Unlit文字，不改变GI；新增针对实际4文字的对比配置检查，并随F03重看入口图，不修改旧场景/共用字材质。

## F-376：Windows默认文本编码影响批量适配

本轮复制自有涂装模块脚本时，少数read_text未显式encoding，Windows默认GBK读取含中文UTF-8的MetalBake失败，尚未请求编译/生成场景。改所有读取/写入显式UTF-8，检查已生成文件后只补齐自有模块。类名前缀适配使用Metal后接大写的定向替换，避免把Shader属性_Metallic误改成_Coatlic。旧模块文件未写入，后续仍需新DLL/Console及实际模块门禁。

F-369本轮复发：F01_Compile_A保留一条完全相同的旧IAP Editor部署同步empty response错误，没有本轮CS编译错误。保存完整回执、确认只有这一条已知错误后仅清理临时Console并检查新鲜B结果；未升级/卸载该包或改网络配置。若复发不能将其当Console0继续测试。

## F-375：仓库初检没有Git元数据/gh，读取保存作用域时误猜文件名

本轮上传预检确认项目无.git，gh命令不在PATH；使用已安装Git2.53/Git LFS和GCM（已有目标账号）即可，不下载CLI、不读取凭据明文。指定远端ls-remote成功且无refs，网页确认空仓库。两次误读MetalBakeSaveScope.cs/MetalSaveScope.cs不存在，rg发现保存作用域实际上与MetalBake同文件；以后先rg定位类，不猜拆文件。所有操作为只读检查，未把探测错误当工程错误或改Unity/全机配置。

## F-374：涂层修订域重载后连接未恢复，两次激活超时

2026-11-01 W03新DLL已写出，Compile_A/B与Ready_A仍no_unity_session，未把它们当Console0或派发涂层生成。computer-use按新鲜唯一窗口选择激活，两次activate_window超时，没有点击/保存/重启或停止用户Play。按技能恢复上限请求本人手动前台，用户确认“Unity 已在前台，继续”后再核对原生clean/Edit/Console并继续；不跳过统一测试门禁。旧请求回执保留，实际涂层生成/测试须另有成功证据。

补记：恢复后原生clean/Edit/错误0，CoatedCreated_A真实成功。W03_A在新纹理生成后被external_changes_dirty门禁挡住，没有测试Job；按既有规则refresh compile:none后换B标签，禁止误poll无Job或把启动拒绝记成测试失败。

## F-373：墙模型有三个材质槽，不能按单槽配置预览

2026-11-01 Created_B在复制自有新场景后报Unexpected wall geometry；finally回干净宿主，未创建派生材质/灯，也未改旧场景。只读检查South_4正反面各一个Renderer、三槽依次Trim02/Trim01/Decal。改仅替换槽0，槽1结构与槽2标识保留，切换/清理同样逐槽处理。新场景只有在字节仍与原场景精确一致且派生材质不存在时允许继续，否则停止保护；保留B错误，复测W01，不把复制文件存在记为模块通过。

## F-372：墙面预览准备期后台失联与重载门禁

2026-11-01初次MCP只读状态返回空结果，Unity后台Responding=false；按computer-use只激活唯一Unity窗口，恢复后原生确认金属场景clean/Edit，无保存/停止用户Play。新夹具编译后Created_A又被reload incomplete挡在派发前，stderr已归档；原生确认新场景不存在、干净空宿主、ready_for_tools=true后才用新标签B创建。不得把preflight失败当创建失败或盲目重复未知写操作；沿用F-363，Console0与新DLL仍需原生就绪状态配合。

## F-371：反馈归档补丁只匹配了长行的前半段

2026-11-01归档A01-M反馈时，apply_patch将AGENTS中长段的前半段当成完整上下文行，校验失败；回读确认整组未写入、验收JSON未创建，不能宣称已归档。改按实际独立标题行插入，保留历史段落，再核对JSON及各文档新记录。本次没有Unity或产品资产操作，不重跑无关产品测试。

## F-370：材质生成时MCP连接中断，不能重放未知副作用

MaterialsCreated_A返回success=false/data=null，后续Console只有WebSocket close handshake警告，无C#错误。未重跑Create覆盖已有文件；原生检查确认干净空宿主、Edit/无编译，自己的新场景已保存、窗口网格存在、MaterialAssignments含1124槽/56材质，生成时间新鲜。仍需真实模块测试验证内容与绑定，不能只凭文件存在记成功。保护旧场景，错误回执与网络警告保留；刷新仅compile:none后过统一安全门禁。

## F-369：金属大场景准备遇到旧IAP编辑器联网错误与2K源遮罩

M01_Compile_A捕获既有com.unity.purchasing Editor部署同步RequestFailedException/empty response，非本轮源码编译错误，尚未创建新场景或启动测试。不修改该无关包/联网设置；保存原错误后仅清理临时Console，再检查新鲜错误并过统一门禁，若复发不能当通过。只读PNG尺寸同时确认Trim01/02/03原遮罩为2048、Toolbox1024；创建前将派生遮罩改为整数面积平均至1024，保留作者原贴图/法线/AO，仅R/粗糙度A按表面改变，不复制大体积2K遮罩耗尽预算。

## F-368：EditMode失败夹具写回后台开关与TimeManager格式升级

Protection_Final_A实际检出两旧文件差异：ProjectSettings.asset与TimeManager.asset，不能宣称旧文件零差异。前者唯一runInBackground 0→1，候选反转后SHA精确匹配本轮原始基线；TimeManager不只是fixed timestep数值，Unity将原无serializedVersion的`Fixed Timestep: 0.02`旧格式写成v2 rational m_Count=2822399。仅把Count加1不能匹配原SHA，继续校验旧格式候选后唯一精确命中。保存原字节与变更字节，逐文件核对当前仍是诊断时SHA、候选等于保护基线后恢复；原生后台false/时间倍率1/步长约.02，清除本轮设置dirty，重跑完整SHA保护与交付设置验证。R01_A在EditMode修改临时后台/时间设置为已知诱因；不将程序测试完成当全局配置无副作用。

本项目无.git，收尾误用git diff两个路径触发no-index对比两种不同设置文件，非有效前后差异；后续不再输出整份PlayerSettings，使用SHA/字段摘要和有界记录定位。旧失败/中间候选保留，不能覆写原基线或删除不一致项来过门禁。

## F-367：Editor采集中单帧Draw Counter为0

R03_A真实2项1通过，按键全部通过；240帧有237有效GPU，性能断言因第140帧draws=0失败，内存240帧均有效。保留该帧/所有时序，不能把0Draw当有效渲染或删行。夹具改分别记录GPU与GPU+正CPU/Draw计数，每个开关模式至少100个有效组合样本才通过；原每帧draw>0不能代表每次Editor更新都发生渲染。原始均值/Editor尖峰仍保留，不根据开比关更快的短批数据宣称反射负开销。重新实测，不修改反射质量或预算。

## F-366：反射样板数值通过，但实图墙面朝外

R01_B实际3/3，环境/高光各自差.003007/.002337，实看Material_On_ae9afe8a却背景异常黑。单面Wall四周摆法方向反了；仅新场景四面墙转180°，保留探针A并新烘焙B，再复测图像。R01_C被external_changes_dirty挡在启动前，没有Job；按已有规则只refresh compile:none后改新标签D，不把C记运行失败或跳过。后续Builder重建方向同步改为a+180，旧源不动。

## F-365：R01第二次重载后窗口自动激活超时

R01_RetestCompile_A/B均no_unity_session，不能记Console清空。computer-use的activate_window两次超时，后续get_window_state画面与Unity目标元数据不一致；没有据这些画面点击/输入，更没有操作其他应用。按技能恢复要求停止自动输入，向本人请求只把Unity置前、不保存/重启/播放。等待连接恢复后检查当前DLL/原生状态，再重跑R01；不绕过module_tests门禁。此时独立探针已真实生成512/1,936,831B，R01_A仍2/3失败未验收。

## F-364：R01夹具重犯嵌套EnterPlayMode错误

R01_A实际3项2通过1超时121.56秒；output明确Enter play mode before calling LoadSceneInPlayMode。查F-048/F-199后确认EnterPlayMode位于Load嵌套迭代器，未进入Play。原生最终Edit/干净空宿主，未调用取消API或保存/丢弃场景。改UnityTest顶层yield EnterPlayMode，Load先断言播放并加8秒加载超时；不增加总超时，不改材质/像素阈值。旧失败保留，重跑受影响模块。一次诊断返回重复TestRunner对象过大，后续只查询唯一类型/限量摘要；只读rg目录通配符错误沿用F-360改正。

## F-363：R01域重载期间无会话，不等于编译失败

新DLL尚未写出时一次Get-Item不存在；随后实际DLL出现、控制台0，但资源状态/SceneCreated_A又返回no_unity_session或reload incomplete，未发场景创建。复用F-351，用computer-use仅激活唯一Unity窗口，无模态/保存操作；Compile_B实际0错误后SceneCreated_B真实创建成功。Native/旧文件快照与临时管线已有实际收据。不要凭旧Console0在新DLL尚未出现时宣称全部编译就绪。

## F-362：反射小样启动时用户仍在Play

NativeCapture_A和首次宿主快照被SceneSafetyGuard拒绝，无快照/保存/切场景/测试发生；只读原生状态确认playing=true、精修场景干净。没有重试绕过守卫或替用户停止；请求本人停止后收到“已退出Play，继续”，再以新标签核对。先前MCP缓存idle不足以代替原生播放状态。

## F-361：反射小样准备读取了不存在的Shader文件名

误猜VC_Planar.shader不存在，rg实际定位旧实现使用VectorCityPBR.shader；未改Unity/资源，后续仅依据实际路径复用。样板会独立派生、保护旧跑酷文件，不据路径失败判断反射不可用。

## F-360：反射小样研究再次把通配符目录直接传给rg

只读核对时Library/PackageCache/com.unity.render-pipelines.universal*在PowerShell外部命令参数中未扩展，rg返回os error 123。已有项目PlanarReflection实际搜索仍命中；改从Library/PackageCache用-g文件过滤，确认SSR的条件编译守卫。未修改Unity/资产，不把路径错误当不存在SSR源码或反射实现。

## F-359：D03复制交付夹具漏掉原工具命名空间

D03_Compile_A实际CS0103，RefinementDeliveryTests仍复用LevelLighting.Ao方法但仅导入新Editor命名空间。补回旧Editor using，仅使用已有只读特效查找函数，不修改旧源码；编译成功与Console清空后才能启动测试。没有把旧DLL当新夹具编译通过。 收尾只读误猜ThirdParty/NOTICE.md不存在；rg定位实际SOURCE_NOTICE.txt后读取，未修改资源。

## F-358：只读状态片段调用不存在的安全检查方法

D03准备期CodeDom只读片段误调用SceneSafetyGuard.Inspect，实际CS0117，未运行片段或修改场景。改用项目已经验证的RequireClean，再返回原生编辑器状态；不把代码片段编译失败当项目程序集错误。后续优先复用实际工具和已读API。 另一次离线统计误将MCP structuredContent的result包装当直接data，KeyError；改解析实际text JSON后确认D02所有输出为空，旧证据不改。

## F-357：桌面支撑夹具错误依赖对象名

D01_C真实17/18，第二UV已通过，桌面支撑断言却要求碰撞对象名含Desk。SupportRay_A实际12个杯子/终端全命中Role=Desk的碰撞体，射线距离.012412m；行政工位名是Admin_Workstation。改按已存在的ArtSampleModel.Role判定，不改变射线长度或桌面高度。实图另将顶部线槽进一步贴靠房间侧墙，修复独立悬挂感；只改本轮位置，三路/回环仍须复测。

## F-356：复用道具没有第二套Lightmap UV

D01_B真实18项17通过，作者图集已通过，但Barrel复用网格uv2为空，严格断言阻止把可显示当可烘焙。旧源不修改；对本轮预制体缺uv2的Mesh制作自有副本，Unity Unwrapping生成第二UV，再保存自有Prefab、刷新本轮场景实例/导航并重测。旧失败及原纹理/模型保留。线槽已代替悬挂线圈，未放宽UV要求。

## F-355：已转URP Lit不代表已绑定作者图集

D01_A实际18项17通过，杯子作者图集断言失败；MugMaterials_A/ImportedMaterials_A定位5个新FBX自动转Lit但BaseMap为空，名称为MI_Trim_01/02/03、MI_Props_Crates，不能按先前Batch1假设映射任意图。实际ZIP比对证实三套Trim色图/法线与已导入MegaKit对应文件SHA相同，故新派生材料复用已正确打包PBR的A00材料；Crates专用色图/法线/ORM从同一已下载包提取，ORM按现有工程R=金属/G=AO/A=1-粗糙度转换，仅改本轮资源。A失败保留。实图另发现线缆模型是悬挂线圈，改复用实际TopCables直线槽模型，保持数量与净空；不以17/18写装饰完成。

## F-354：FBX作者根旋转影响道具归一化

PreparedProps_A真实收据显示杯子Y高.1816m、Z深.14m，而目标Y高.14m；原实现按visual自身局部轴计算包围盒，却保留了FBX作者根旋转。尚未摆放或运行测试，不将资产已生成写成比例通过。改在新父根坐标计算实际包围盒、乘保留原始缩放并按父坐标对齐底部；A收据保留，重新生成本轮自有B版预制体收据，旧A01和原包不改。

## F-353：D01导航枚举缺少命名空间

D01_Compile_A实际1项CS0103，RefinementScene遗漏UnityEngine.AI，NavMeshCollectGeometry无法解析。未执行新场景创建或测试；补入与旧LevelBuilder相同命名空间，旧源码不改，重新编译后检查Console再继续。原配置快照与临时宿主/管线绑定均已成功，14,777旧文件保护基线捕获完成。

## F-352：精修准备期复用了错误的作用域路径

本轮只读时先猜Tools/MCP/module_scope.py、又误用Tools/InfiltrationPrototype/module_scope.py而返回不存在；实际rg发现Tools/SalvageExpansion/module_scope.py。无Unity/配置修改。后续直接使用已发现路径，原生快照与恢复片段复制到本轮独立目录；旧工具不改。


## F-351：Editor性能采集包含夹具自身分配

预分配后Delivery_C实际2/2，720帧/713有效GPU；全开GPU均值5.41–5.81ms、Main均值6.06–6.16ms，本轮未出现110ms，仍有仅AO单帧Main18.92/GPU18.08ms。不能据不同采样批次把原尖峰单独归因JSON，或宣称产品全时60FPS/零GC。工程门禁通过、目标机验收缺席，实际报告分账。

Delivery_B工程2/2，三视点四组合720帧且713个有效GPU时序；GPU均值约4.87–5.81ms，但仓区Main Thread出现110ms尖峰，不能报稳定60FPS。首夹具每帧创建JObject，数据包含采集器和Editor开销，无法据此归因产品或单项特效。改预分配720个struct、采集结束后才构建JSON，每组合按墙钟预热.75秒；复测保留原数据，仍区分Editor统计与目标机/发行验收。未修改玩法或把0GPU时间视为通过。编译后Console查询no_unity_session未当成功；按技能刷新观察后仅激活唯一Unity窗口恢复，发现截图误为Codex时没有据图点击/输入；DeliveryCompile_E再实际0错误。读取技能附属文件一次错误使用skills/computer-use/docs，按SKILL明确的../../docs纠正。


## F-350：交付夹具将Animator状态名误当源Clip名

Delivery_A实际2项1通过1失败；1080p真实Profiler采集通过，跳跃/Tab已通过，但第47行硬写clip.name==Idle失败。源控制器状态名Idle，FBX真实片段为RobotArmature|Robot_Idle且meta loopTime=1，旧产品无需修。新夹具改核对唯一真实Idle源片段、实际isLooping及超过首周期的骨骼旋转，并记录真实名称；不修改旧模型或放宽运动门槛。增加AO/雾各自开关的四组合Profiler采样，重测本项后才做最终全量。


## F-349：新局部雾适配的命名替换顺序

新LevelFogFeature适配时先替换类型NextFog→LevelFog，使随后针对_NextFog的Shader变量替换未命中；旧四局部光数组残留，产生3项CS1061。FogCompile_A保留实际编译错误，未执行场景配置/测试。修正新Feature全部Shader变量为_ILFog、移除旧局部光数组并接入独立主光/阴影选项；旧SalvageNext文件不改。只读rg再次对路径传通配符导致123，后续传实际目录+glob，不能当相关API不存在。


## F-348：A01-C1第二轮复测门禁与路径读取

恢复上下文时误把新测试目录写成Editor/Tests，实际在InfiltrationLevel/Tests；只读失败，没有因此改旧资产，随后rg发现实际路径。Lighting_B被external_changes_dirty拒绝、无Job，未轮询；执行refresh compile:none后以Lighting_C实际6/6通过。BakeJob_B39.624秒成功，旧A光照草稿与立方体仍保留。模块实际报告区分总烘焙贡献与未隔离间接光，不把开关像素阈值当用户审美或目标机验收。

## F-347：A01光照首轮工程通过仍需视觉修正

BakeJob_A实际301.623秒、2张Lightmap/216探针/8额外离线反射、0错误，保存Scope和宿主恢复通过。一次computer-use截图内容错误显示Codex，未据该图点击/输入；重新列唯一Unity窗口并激活后核对，不操作Codex聊天UI。

Lighting_A6/6，AO/反射/烘焙贡献实际像素均值差分别.001630/.002211/.033966；该烘焙对照验证总烘焙贡献，未隔离纯间接分量，不能扩写为逐区颜色溢色定量通过。四张无临时补光图4589b3d0显示灯具发光面朝上、观察窗反光影响后侧透视；故未进入C2。新样板顶灯转向下，玻璃关闭环境反射/高光作为低反射透射近似，曝光.4→.55；先保存LightingDraft_A原场景/贴图，再重新烘焙。反射输出加版本后缀，保留A记录与旧立方体；不把图像观感问题当Unity管线不支持。

## F-346：A01-B自动模型检查通过后的实景缺陷

Art_A6/6，实际四视点bcd43113仍有反面文字、玻璃过强灰反光、6m中庭与4.5m相邻屋顶间的上墙缺口。没有据自动通过进入C。仅派生A01文字Shader改背面剔除并修P02方向；玻璃实际序列化SrcBlend=One、PreserveSpecular=1，改关闭保留高光并明确AlphaBlend/较低透射着色；补上中庭高墙环。旧A00 Shader/材质不改，旧照片/结果留存。

屋顶远处条纹尚未隔离是阴影还是几何，不先宣称z-fighting根因：WallFaces_A实际单面18顶点、法向全部-X，材质Cull=Back，不能凭印象删反向墙或改成双面。后续C隔离阴影验证。一次LayoutAfterArt_A被external_changes_dirty拒绝，无Job；需新鲜refresh:none后以新标签启动，未据该次宣称通行回归通过。

实际ShadowDiagnostic_A同视角阴影On/Off中条纹都存在，排除仅由阴影导致。FloorTriangles_A实际总面积16m²且无重复面，不按重复地板猜测删网格。进一步MaterialCulling_A显示WallCap/Floor共用Trim03为CullOff，与成对正反WallCap发生共面双面绘制；墙的Trim01/02虽Back，Decal又为Off，之前只查一种材质不足。仅A01派生PBR副本改CullBack（玻璃保留双面），保留所有真实源网格/旧材质，待图像复测确认条纹消失。

Art_C6/6，实际58caf77f入口/观察窗图确认顶板条纹消失；没有删地面或停用最终阴影来掩盖问题。后续单面薄墙/屋顶仍需明确双面投影与烘焙GI，以免材质剔除修复引入漏光；在C验证。

## F-345：A01输入用例以固定帧数错误推断持续时间

Layout_A实际9项8通过1失败；净面积、三路径、两回环、真实CharacterController遍历已通过，输入测试18帧W只移动0.426m，未达夹具0.5m。编辑器NoThrottle条件下Application.targetFrameRate并不构成固定deltaTime保证。改以实际被移动模块使用的min(deltaTime,.1)累积持键0.5游戏秒，并设8秒墙钟超时；提高位移要求至>1.2m，不下调门槛或修改旧移动代码。旧失败留存，待同组独立复测，不将第一轮写成9/9。

## F-344：A01准备期未知返回与快照核验

NativeBefore捕获请求外层success=false/data=null，但NativeBefore_A文件实际写完；后续原生闲置/干净场景预检及bind对原native/disk/dirty逐项比对通过，宿主/绑定收据均成功，故确认快照有效，没有重放捕获或覆盖原记录。此类外层未知返回必须先查副作用与新鲜状态，再决定是否重试；后续依赖请求保持逐次核对。新保护清单对三项设置使用已确认快照的原磁盘字节，避免长时间SHA过程中临时管线绑定污染基线。

一次读取Navigation包时误用未完成的@前缀目录，返回不存在；已列出实际com.unity.ai.navigation@d0cf77b009bb，后续不再猜路径。LevelSolid/Floor/Post/RoutePreview各用同名独立源文件，以保证Unity组件序列化可恢复，未将多个MonoBehaviour塞在不匹配文件中交付。

B编译后ArtCompile_A/B均为no_unity_session，未记为零错误。按既有经验用computer-use只激活唯一Unity窗口，未点击保存/丢弃或版本控制登录；ArtCompile_C恢复实际0错误。新夹具的FindObjectsByType排序参数收到Unity6000.6弃用警告，按实际编译提示仅改新文件用无参数重载；旧代码不做全项目API更新。

## F-343：反馈02渲染研究的路径与版本信息边界

只读检索再次误将com.unity.render-pipelines.universal@*/Documentation~作为rg路径，Windows返回123；应复用F297：先Get-ChildItem取得实际包目录，再把完整路径交给rg。猜测Station/Runtime/LocalVolumetricFogFeature.cs不存在，已用rg定位实际Rendering/Runtime/LocalFogFeature与SalvageNext/Runtime/NextFogFeature，不据空结果判定工程没有体积雾。Bee响应文件检索无匹配exit1是未匹配，不是编译失败。

官方6.6功能表仍将URP SSR标No，但本地17.6包含受URP_SCREEN_SPACE_REFLECTION控制的代码；Unity官方预览说明亦不等于本项目已编译启用。实际只读unity_reflect packages查SSR0类型、SSAO1类型，留存Feedback02_RenderReadback.json；没有开启宏/修改包/运行渲染验证。后续先查本版本实际加载与门禁，不能仅凭旧印象说URP永远无SSR，也不能因搜到源码就承诺可用。

## F-342：A00叠加测试监听器与MCP播放状态缓存

Final_D真实16/16通过，但四个PlayMode测试输出重复双AudioListener警告。样板YAML只有1个监听器，叠加测试对进入播放后已存在的监听器未作隔离；新增仅测试用作用域，记录进入前监听器名称/场景，临时关闭，卸载样板后还原。后续实际记录定位来源，不把猜测当已证实。

原生预览A的资源状态一直停在playmode_transition旧时间戳，70秒超时；finally确认退出并回到干净测试宿主，未截图，不记原生通过。沿用已有stale_status经验，预览B改直接读取原生EditorApplication播放/请求/编译状态和实际frame，再采Game图。不在未知状态重放Play。两次只读路径曾漏掉Runtime目录，已用rg定位实际文件；没有修改旧版。

Final_E实际16/16、0跳过、15.588秒，四个RunnerListeners收据均定位为未命名临时场景Main Camera，非样板额外监听器；作用域还原断言通过，全部测试output为空。编译域重载中的一次Console查询返回no_unity_session，不记编译失败或Console已通过，之后测试启动使用新鲜原生安全/编译门禁。

## F-341：A00高帧率下CharacterController小位移阈值影响着地

Final_B实际16项15通过，新增前置断言定位为实际地板上Grounded=false，尚未提交跳跃键，不是高度期望太严。新场景YAML确认CharacterController默认m_MinMoveDistance=0.001；NoThrottle高帧率时微小竖直Move可能被阈值忽略，着地状态不连续。按Unity官方Character Controller手册对多数场景的建议，仅将新样板控制器minMoveDistance设0，保留既有移动算法与旧版全部资产。

夹具增加显式Physics.SyncTransforms与真实着地等待，仍要求实际Space触发>0.35m跳跃、落回地面；不手动给角色向上位移或强行修改Grounded。旧Final_A/B和Walk_A都保留，修改后的因果与稳定性以新实际测试为准。一次只读查找PlayerJumpTests.cs名称不存在，未据此改旧测试，复用已有PlayerVerticalMotion实际代码。

Final_C仍15/16、捕获前置Grounded=false，说明设0不足以证明已解决，不能报告因果已确认。继续记录dt/位置/碰撞标志/实际地板射线，并让输入测试实际显示GameView、以60FPS执行后还原原targetFrameRate，排除隐藏GameView下NoThrottle极速循环的非代表状态；不删除跳跃要求，不改旧移动算法，待新数据。

Walk_D3/3、Final_D16/16实际通过。JumpPrecondition记录实际Below、Grounded=true、dt0.035、minMoveDistance0，真实Space跳跃与落地断言均通过。测试显示GameView/限60FPS后恢复原targetFrameRate，旧移动源码未变；这支持此前极速测试条件相关的判断，但没有逐项A/B隔离，因此不宣称minMoveDistance单项是已证实根因。

## F-340：门卫Idle引用正确但默认导入只播放一轮

Walk_A实际3项2通过1失败：真实WASD/鼠标/跳跃/墙体碰撞与Esc/点击/失焦门禁通过，门卫初段运动与1.8m尺度也通过，但超过3.333秒后局部旋转差为0。FBX默认take未启用循环，引用到动画不等于持续播放。

仅修改本轮复制的Robot.fbx导入设置：由默认14段take建立显式列表，只对Idle设置loopTime，并重新确认真实源Clip.isLooping、控制器引用；原FBX字节和作者源包不改。原meta另存RobotBeforeLoop_A.meta，重跑受影响资源与真实PlayMode动画/输入，不用脚本位移冒充源动画。

Final_A实际16项15通过，动画循环已真实通过，但跳跃最大高度仅0.0318m。原Walk_A跳跃通过，新的失败保留，不能直接判为产品跳跃坏或放宽高度门槛。输入夹具原先用yield null后立即释放按键，现改沿用已验证Frames辅助等待真正Time.frameCount推进，并在跳跃前独立断言捕获/着地；保留>0.35m要求，复测后再判断。

## F-339：A00自动渲染检查通过仍有明显视觉缺陷

Scene_A实际5/5，但实看四张View_0–3_3bc3abeb：TextMesh标牌尺度过大且镜像，原GUI字体材质可越过深度显示；地面/顶面偏白、深蓝墙体反差过强。不能把非黑图/未数值裁切当美术通过，因此未推进A00-C或交付。

修正仅限本轮自有资产：标牌改正确朝向/物理尺寸和带ZTest LEqual的URP文字Shader；复用现有中性色图集，按已验证的涂装方案派生金属通道R×0.12/GBA保持的遮罩，降低灯能量/曝光并补局部前向烘焙填光；隔墙加相反面、标牌补实体门楣。先归档初版场景/Lightmap为ArtDraft_A，历史截图/收据保留，后续重烘焙与独立复测，不能据修正代码直接记视觉已解决。

## F-338：A00烘焙受NUnit内部未命名场景阻塞

Bake_A进入UnityTest后框架创建了内部Untitled/MainCamera/DirectionalLight，样板以Additive加载。Lightmapping.BakeAsync检查所有加载场景，弹出“untitled scene needs saving”；MCP轮询TimeoutError/data=null，旧module_tests.py轮询随之TypeError，并非实际用例已完成。computer-use只点Cancel，不保存或丢弃内部场景；后续Bake_A_AfterCancel实际1项失败，BakeAsync被拒绝，原宿主已恢复。

改用项目已有烘焙模式：非TestRunner期间先检查所有场景干净且具名，只打开自有样板Single，使用限定自有目录的SaveScope与完成回调保存新Lightmap并恢复原宿主布局。之后统一module_tests.py独立验证实际保存的Lightmap、源场景与布局还原收据；不能单凭JSON或BakeAsync返回true记通过。旧失败及空轮询回执保留，不重复启动未知状态的烘焙。

## F-337：A00场景编译被脚本API自动更新确认窗阻塞

A00-B刷新看似长期无响应，computer-use真实截图确认Unity停在Script Updating Consent，指向新自有ArtSampleScene.cs。没有批准全项目自动改写；点击No保留源码，编辑器恢复并显示CS0618：RenderSettings.customReflection已由customReflectionTexture替代。依据本机实际编译信息只改新文件为新属性；旧源码、项目依赖与全局设置不升级。新程序集需核对新鲜度和Console后再生成场景。单纯等待或重复refresh不能解除模态确认窗。

随后新鲜截图又显示同文件CS0619：LightType.Area已弃用为错误，要求LightType.Rectangle；此前只读到一条warning并不足以证明全部根因。再次拒绝自动改写，仅修正新文件的灯类型。没有在旧Editor DLL上生成场景或把无响应Console记成零错误。

## F-336：A00资源测试发现真实低面数地板与缺失Animator

A00 Assets_A真实7项5通过2失败。Floor源FBX确有12顶点、有合法Assets网格路径；夹具额外要求总顶点>24错误排除了简单但真实的建筑模块。移除该任意数量要求，保留至少三顶点与源网格资产路径门禁，仍拒绝内置Primitive占位。

另一失败是Guard预制体没有Animator：源模型使用独立刚性部件，Generic导入并未自动附加Animator，之前仅遍历已有Animator设置控制器因此漏配。改为仅在新门卫实例缺失时显式添加，再绑定已存在的源Idle Controller；不改源FBX内容或假装已播放动画。保留Assets_A与InspectAdapted_A，再独立重跑资源；真实播放动画另在后续PlayMode验收。

首次refresh请求在60秒等待域重载时超时；实际新程序集已完成编译，Console0。按F329使用compile:none刷新清除external_changes_dirty，不重复物化资产或将超时写成编译失败。准备期误读通用Editor.log只读到另一个Unity实例启动段，后续仅通过本项目MCP Console取诊断，不再输出原生启动参数。

## F-335：A00准备期的路径、后台响应与Vector3元数据序列化

2026-11-01，A00已由用户“好的，开始吧”授权。两次只读rg查询带入不存在的Tools/SalvagePrototype、Assets/EchoStation/SalvageNext/Tests目录，返回os error 2；后续按已有文件列表查找，不创建猜测路径或将其当无测试。

首次Unity execute_code返回success=false/null，复用F300/F288，computer-use唯一选中实际Unity窗口；首次activate_window超时，重新列举后再激活恢复。新鲜NativeSnapshot_A确认环境场景干净、VC/PC/AA4与D3D11；未保存/丢弃用户内容。

ExistingModels_A只读查询将Vector3直接传JToken.FromObject，默认Json.NET递归normalized导致Self referencing loop。未改模型或场景；元数据输出改显式[x,y,z]，保留失败回执，使用新标签重查。不要通过忽略引用循环来掩盖缺失字段。

## F-334：建筑资源可用不等于关键交互物美术已完成

2026-11-01用户反馈上一版仍靠方块占位，担心新3D项目重演。只读复核ExpansionSceneBuilder.cs第51–52行：七件物品和七台FacilityDevice均调用Cube；历史MergeAfter_1.png也可见资源墙体与占位配电箱并存。此前工程与灰盒测试不构成最终美术验收，资源网页/许可证/模型数量也不能代替逐交互物映射、材质和第一人称近景检查。

当前仅定位与记录，未替换旧版。新提案建议先做实机美术样板、让用户决定视觉是否接受后再扩玩法；该样板尚未获实施范围授权。区分真实箱体造型与临时占位，以及可见模型和不可见碰撞体，不以禁止所有简单几何作为修复。

同轮检索的Spaceship Interior正文受restricted URL限制、Unity商店预览图超时；未下载重试绕过，未将其作已见实机效果。Sci-Fi Facility Builder表面适配题材但实际只有俯视PNG，已排除；Kenney Starter Kit FPS为Godot工程，不称Unity即用场景。只读大批枚举/搜索输出曾截断，后续改精确目标和小段读取，不把未显示的内容当已审阅。

## F-333：新方向规划时Git状态检查遇到非仓库目录

2026-11-01，伪装潜入规划的只读git status返回“fatal: not a git repository (or any of the parent directories): .git”。未初始化仓库、修改全局Git配置或据此删除/迁移旧项目。后续版本保护继续遵守项目原有文件证据与SHA核验规则；需要Git操作前先确认实际仓库根，不把Unity项目根默认当作Git仓库。本轮仅新增提案及文档索引/本经验记录，未启动Unity、测试或模型。

2026-11-01资源复核补记（复用F299抓取限制经验）：Kenney Furniture Kit主站返回restricted URL，作者OpenGameArt家具页直读返回403；没有重试绕过、下载错误正文或把第三方许可摘要当完整导入证据。改用本地已有Sci-Fi Essentials桌椅清单作为当前道具依据；另以Kenney官方itch的Space Station Kit和Quaternius作者页核实备选。Q01仍为疑问未确认，未因此启动资产导入或实现。

## F-332：静态门禁遇到既有IAP后台错误与重复物化保护

最终CandidateStatic_B13/13、Integration_C6/6、Compatibility_Final26/26实际通过，原生完整配置还原、Console0。两条已知IAP日志及无用例初始化超时保留，未全局忽略错误或修改网络/依赖，模型诊断仅20次。

Integration_B被服务端初始化20秒超时结束：completed0/resultnull、editor_unfocused，未开始用例，不计32项失败或通过。复用F325，先读取IntegrationB_NativeIdle：Edit/无编译/无Runner/宿主干净，IntegrationB_ConsoleAfter零错误，再激活实际Unity主窗口，以较小分组新标签复测工程；不刷新模型、不重做20条诊断。

Q01 C接入后Integration_A再次在启动前被同包同文件:69唯一empty response日志拒绝，无Job。保留回执并严格核验后归档清理到ExistingIAP_Clear_B.json，再用Integration_B；此时真实诊断已完成/Owner已释放，不重跑模型，不改联网或依赖。

Q01 CandidateStatic_A新鲜程序集已恢复，但唯一错误为既有InAppPurchasing RequestFailedException，来自已安装purchasing包SyncItemsWithRemoteCommand.cs:69初始化环境客户端的catch；未产生测试Job或模型调用。复用F138/F202：保留原Console回执，并核对当前全部错误恰为该已知一条后归档清理，以CandidateStatic_B重走门禁，不改包、网络或服务，也不忽略新产品错误。

候选已成功冻结后，编排误重复调用prepare_candidate；目标文件存在断言拒绝，未覆盖源码/冻结记录、未生成第二候选。后续先检查已有Candidate_Freeze与源SHA，不重放成功的物化步骤。

## F-331：参数化测试的孤立代理字符被测试传输替换

修正夹具后Retrieval_B46/46实际通过，非法UTF-16在方法体构造时确实被严格编码拒绝。

Q01 Retrieval_A实际46项45通过，InvalidRetrievalKeyRejected的孤立高代理字符参数在回执名字中已变为“��”，校验未抛异常。NUnit/Unity参数传输不能作为无效UTF-16的可靠载体；将非法字符改为测试方法内new string((char)0xd800,1)构造再断言，保留UTF8Encoding(false,true)产品校验。旧API/检索/预算等其余45项已过，不据此放宽规则或开始模型实验；新标签重测独立门禁，原失败保留。

## F-330：用玩家问题作记忆标题混淆数据角色

经用户批准后实现独立RetrievalQuery与真实目标名标题，旧构造/默认行为保留；Retrieval_B46/46通过，v2真实UsedMemory已显示“蒸汽阀门：扳手…”而非玩家问题。联合诊断从8/20到15/20仍No-Go，仍见错误工具/天气；标题分离不是模型忠实性的保证，未对单因素作因果认证。当前只读v2待本人测试，不继续追加候选。

L02-Q01只读复核确认：QuestionCapture为触发关键词检索，将input前80字符用作ClueMemory.Title。原始UsedMemory把“门后的密码是什么？”放在steam_valve线索标题，而摘要仍是阀门操作。问题没有泄露隐藏事实，但不应充当已知记忆标题；需独立受控检索键与原始输入，标题恢复真实目标名，保留默认旧API行为并测试。第9/10/17/18/20条原始模型错误已保留；这不证明改标题即可解决，更不能把单一阀门示例的复制偏置假设当成已证实因果。当前仅提案，未修改运行时代码或追加推理。

## F-329：重载中的刷新失败不能当刷新完成；文本导出须保留LF

收尾：Panel_B实际6/6、Compatibility_Final21/21通过，FinalRestored完整配置还原、Console0。未将Panel_A无Job超时计为产品失败/通过，也没有因重试门禁再次运行真实模型。

L02 Panel_A编译门禁240秒超时，无测试Job。第二次refresh在域重载中返回no_unity_session，但client.py进程exit0；不能只凭shell退出码继续假定已导入后续改动。检查发现DLL早于最后源码且external_changes_dirty=true。等待实际服务恢复后用payload.success显式核验刷新，再以新标签跑，不在旧程序集上验收。

L02 Prompt归档首次SHA拒绝：Windows Path.write_text默认将LF转CRLF，未改运行时Prompt或生成第二候选。只在确认现有字节恰为同文本CRLF变体后用UTF-8原字节写入，SHA与真实推理回执一致；此为归档格式修复，不重跑/替换模型结果。

## F-328：Prompt构造的可选参数也要求直接程序集引用

修复后Service_B5/5、RealDiagnostic_A真实20次运行完成；后者语义8/20另列No-Go，不能把编译修复或运行夹具通过当模型质量通过。

L02 Service首次编译CS0012：Qwen3PromptProfile构造签名的Qwen3ChatExample属于AI.Tokenization，即使不传该可选参数也需要直接asmdef引用。尚无Service_A测试Job；补显式引用后重新编译，使用新标签跑实际用例。不得用旧DLL继续测试。此前只读误按类名读取PreparedPrompt.cs/InferenceThinking.cs及尚未生成的Service_A收据，未产生产品写入，已按实际声明PromptPreparation.cs/LlmFeedbackView.cs定位；继续复用F303，不以预期文件存在代替检查。

## F-327：文本提交回调同步关闭面板后键盘已释放

修复后Facts_B内4项IME生命周期及Compatibility_Final原输入/面板21项实际通过，Console_Final零错误；不改全局输入系统或重打旧包。

L02预检发现用户试玩后的Console三条GameTextInput.cs:132 NRE，regression_host门禁拒绝，未启动测试。源码SubmitRequested可同步Close→OnDisable→DetachKeyboard，将keyboard置空，下一行仍读escapeKey。保留PriorPlay_ConsoleErrors原日志；先捕获本帧Enter/Esc，再只触发一个回调，避免回调后访问已释放设备。新增真实EventSystem回调关闭字段的生命周期回归，重跑输入和L01受影响用例；仅归档并严格确认已知三条来源后才清理历史错误，不忽略新错误。

## F-326：Windows Player的IME修复不自动覆盖Editor Game视图

L01用户编辑器本人测试确认“仍只能输入英文，无法出现中文候选”。读取实际场景确认Play运行、中文布局00000804、真实键盘native=true；用户转回聊天后面板已正常失焦关闭，此时Auto/未聚焦快照不能单独证明活动时的IME故障。源码明确使用UNITY_STANDALONE_WIN && !UNITY_EDITOR，故IME01原生初始化刻意排除了编辑器。按最新Editor策略将路径扩为Windows Player或Windows Editor，仍只在Application.isPlaying且真实键盘时捕获/启用引擎IME，虚拟测试键盘不触及原生模式；关闭/移除键盘恢复原模式。新增实际Editor真实键盘启用/关闭还原测试，并重新验证原有IME及L01场景；候选字最终仍由本人再次复测，不用模式字段冒充原生选字成功。

结果：EditorIME_A1/1，随后EditorIME_Regression_B24/24（新面板11/完整场景2/旧输入4/旧对话4/生命周期3）实际通过；Console0，EditorIMEFix完整native/disk/dirty还原。2026-10-31用户重新Play明确确认“中文候选、发送和重开均正常”，EditorIME_UserAcceptance.json保留人工原话，L01完成。未打包，不扩写为其他输入法/另机验收。

## F-325：测试Job初始化超时不能计为实际用例失败或通过

L01 EditorFinal_A服务端返回failed，但summary=null、completed=0，error为“Test job failed to initialize (tests did not start within timeout)”，Console0，无任何实际失败用例。读取新鲜editor/state确认当前已退出Play、宿主干净/空闲、runner不在执行后再发新任务；不把无结果的一轮算41项测试通过，也不凭Unknown结果重叠启动。保留原回执，先激活真实Unity窗口，再独立跑完整场景/面板组，后续实际fullName核验覆盖。

## F-324：L01报告用例的多断言单行难以区分拒绝来源

Report_B独立重测定位为真实调查分发拒绝，blocked=false/focus=true；读取实际IntentContract/BehaviorLifecycle后确认夹具Boot的echo.enabled=false触发ExpansionEcho.OnDisable并Dispose行为树。移除此禁用，保留真实组件运行，且加强待命/跟随/调查的成功回执断言，不能只看baseMode/CurrentTarget字段便算执行接受。重跑全部面板及原Echo回归；未修改产品执行门禁。本轮两处按类名猜测文件路径失败，改用rg -l获取实际IntentContract.cs/BehaviorLifecycle.cs，复用F303经验。

L01 Panel_A真实10项9通过1失败，ReportsRequireActualScanAndKeepStaleWarningWithoutDeviceWrites第62行True断言失败；原行合并了两次打开及真实调查命令，暂不能确认是UI、暂停状态还是扫描权限。保留Panel_A完整堆栈；拆成独立断言并附带world.Message/暂停/焦点与实际扫描提交检查后重跑失败用例。尚无产品修复结论，不放宽命令权限/自动代扫描门禁。

## F-323：Windows安全权限弹窗阻挡原生验收，超时不能算产品失败或通过

IME01 Native_B实际遇到Windows防火墙权限请求，computer-use规则禁止代理代为处理，已异步请求用户手动取消。等待期间完成原编辑器恢复，未操作安全权限。600秒超时后仅外层测试宿主终止本轮自有游戏PID75684，Process退出1/Timeout.json保留；没有组合输入，原生修复后验收受阻，不能计通过或把启动两帧context=0当普通T使用结论。后续必须同一Game_B包新进程、新证据标签重测，不覆盖Native_B。

## F-322：模型文件过滤应按扩展名而不是Windows大小写宽泛glob

IME01封包检查使用*.onnx*，Windows大小写不敏感匹配到了Microsoft.ML.OnnxRuntime.dll，断言拒绝且尚未写BuildInfo/最终清单。实际权重已由构建器精确剥离；修为真实扩展名.onnx/.onnx_data检查，不删除ORT运行库或放宽权重检查，再生成发行SHA清单。

## F-321：全局输入命令回调必须容忍测试作用域尚未建立或已拆除

IME01 Fix_B真实11项8通过3失败，三项新生命周期用例断言后均在TearDown出现onDeviceCommand回调NRE，Console定位自有ImeLifecycleTests.cs:23。命令回调直接访问inputs.Keyboard，在测试域/设备拆除期间inputs已空；不是产品中文提交或原生桥错误。增加inputs/command有效性检查，清理时先注销全局钩子再关闭字段/设备。原始失败与限定Console归档，只有已确认两类且直接NRE归属该测试文件才清理历史Console；不忽略运行期日志，之后新标签重跑全部11项。

## F-320：IME命令钩子的unsafe赋值不能放在测试协程内

IME01 Fix_A编译门禁拒绝，CS1629：IEnumerator Boot中的unsafe委托赋值块仍属于迭代器，未启动测试或构建。把指针委托初始化移到独立普通unsafe方法；测试语义与产品门禁保持，编译恢复后新标签重新运行。原Console回执保留。就绪脚本同时核验本轮测试和Player.Runtime DLL新鲜度，不能只看两个工具程序集。

## F-319：IME请求On不代表Windows已关联输入上下文

IME01隔离Probe_A真实Windows/D3D11：输入框focused=true、Application=true、keyboardLayout=00000804、ImeMode=On，而imeSelected=false、ImmGetContext=0、组合事件0；微软拼音布局已选择仍只收到英文字符。证据Native_A/NativeEvents.json。此为本机实际失效状态，不把历史macOS问题UUM-61922当Windows根因。原生输入过程中检测到用户操作，停止抢占键鼠，交用户点击“引擎IME On 对照”测试旧引擎IME入口；结果未到前不据猜测修改全局Active Input Handling或安装其他输入法。

Probe_A构建0错误/4警告、84.337秒；新输出模型副本1,902,692,739B已精确剥离，原模型与旧包保护仍待最终SHA复核；临时峰值21.184GiB<22。第一次新程序集domain reload有Running managed callbacks延迟，activate_window超时后按F300/F288刷新窗口，待实际程序集新鲜和Console0恢复才创建/打包，没有重复调度构建。一次只读预算脚本猜测路径不存在，随后rg确认实际resource_budget.py；之后按实际路径读取。

## F-318：模拟中文事件不能替代微软拼音原生切换验收

2026-10-31用户确认旧SalvageNext T面板使用微软拼音始终输入英文。已有GameTextInput的SetIMEEnabled/组合事件桥及S2-D02A/C受控测试不足以确定原生根因，不能把旧通过标记扩展为原生候选测试通过。computer-use首输入被用户输入保护拒绝；一次后续截图出现非目标Codex，未输入，重新列举唯一游戏窗口后才继续。原生T/焦点操作没有形成有效IME复现，本次按未完成记录，已正常关闭本次游戏进程，不猜测并修改Windows设置/生产源码。下一步隔离诊断需记录实际焦点/设备/布局/IME请求结果与组合事件，见LLMShortTaskPlan；旧报告和包保留。

本轮只读检查再次发生rg参数直接带S2*/Expansion*的Windows语法错误，复用F297/F054改为目录加-g；误读Expansion/ExecutionState路径后由rg确认实际SalvagePrototype/ExecutionState。宽泛ime检索误匹配time产生截断，后续仅查精确IME符号和目标代码段，不把截断输出当完整阅读。Unity1.20键盘文档网页工具不可读，改以安装1.20.1源码核查及1.17官方接口说明作背景，不捏造新版本文档或运行验收。

## F-317：全局AA也必须与长线作用域原始快照比较

定位补充：本机URP UniversalRenderPipeline.cs:389在绑定时把QualitySettings.antiAliasing同步到资产MSAA；临时GamePlay.msaa=1对应引擎AA0，原VC.msaa=4。原生与磁盘都只差PC AA0和已知临时GUID。先在精确逐字段门禁下切回原VC并恢复AA4，再按原始字节构造唯一允许的临时磁盘变体验证、还原Quality原字节；未放宽原module_scope。两次拒绝回执另存，最终原三设置native/disk/dirty全部true，AaRepair/AaDiskRepair与NativeRestored已归档。后续仅恢复原场景并做最终核对，不在VC白名单外继续测试。

2026-10-31，最终Windows验证结束后module_scope restore拒绝QualitySettings的未知原生差异，尚未写回或恢复场景。只读逐字段比较发现仅PC antiAliasing由原4变0，以及已知临时GamePlay管线GUID；其他质量字段一致。四次构建各自作用域虽原生/磁盘/dirty全还原，只能证明还原到各构建开始，不能替代本轮Run01初始快照。保留拒绝/Current/Diff证据，按受限修复仅还原已捕获AA字段，再通过原完整门禁恢复；不删除门禁或接受任意配置差异。

## F-316：显示验证必须保持实际游戏窗口前台

2026-10-31，最终D套件启动、双路线、跨进程写读、失败恢复均通过；Display_D在原生应用等待时失败，现场focused=false/captured=false/blocked=false，说明失焦安全回退已关闭面板并释放租约，不能计成显示通过。该模式故意不采用路线模拟焦点，使用computer-use选中唯一实际游戏窗口并激活后，以新Display_D2重测，保留D失败。GPU模式仍检查真实Application.isFocused与非零GPU时间，不将虚拟焦点算前台证据。

## F-315：后台虚拟输入的焦点与退出顺序要单独控制

2026-10-31，Valve_C于21.767秒写出失败Result，首段走向(9,6)超时；随后进程未退出，180秒外层仅终止本轮PID并保留日志。此前同路线A、Editor多轮通过，不能在缺焦点/位置现场时断言玩法障碍根因。审查验证器：虚拟设备虽IgnoreFocus，PlayerInputReader仍接收真实失焦并禁止移动；且结果写出前把runInBackground恢复false，后台时可能在处理Quit之前停止循环。仅显式fan/valve/write/read诊断模式维持模拟焦点；普通启动、显示失焦测试和GPU真实Application.isFocused不覆盖。增加超时位置/焦点/捕获/租约现场，背景设置延至OnApplicationQuit恢复并落Quit收据，外层超时也复制已生成Result。原C失败不计通过，须新构建/新标签复测。

## F-314：新拾取物不能只继承旧版的动态障碍结论

2026-10-31，V01复核发现新ExpansionItemActor未使用旧SalvageCargoObstacle（该类绑定旧Registry），已有导航/放下测试不等于新物品动态carving通过。只在新World为七实体各建Box NavMeshObstacle；仅World位置/Collider启用时carving，背包/手持/安装/交付禁用，静止0.2秒后更新。新增实际NavMesh.SamplePosition由可走→放下成孔→拾回恢复的测试，不以组件存在替代行为证据；随后全回归和新包测试。

## F-313：HUD对象活跃不代表准星字形实际可见

2026-10-31，Startup_B HUD可见，但实际1920×1080中心20×20像素最大通道119、青白像素0；原32px Text框中的25px Noto“+”受到行高裁切。改为两条12×2/2×12非射线Image组成准星，保留既有UI显隐；新增Windows启动图中心像素门禁，不能仅查GameObject活跃。随后需完整回归和新Windows重建/复测。

同轮Performance_B真实810/810非零GPU样本通过，开发机Main/GPU P95=4.530/2.379ms，但GC中位1816B/帧、P95 5330B。按已测数据将每帧库存Get/Held与暂停租约查询中的LINQ枚举/闭包改为固定小表循环；不改变事务或快照语义，随后全89回归及新包再次采样，不用编辑器/源代码推断优化完成。

## F-312：访问跨程序集返回类型仍需直接声明引用

2026-10-31，最终包装新增读取NextAddressables.Settings.ConfigFolder，触发CS0012；虽然已引用Next.Editor，AddressableAssetSettings的定义程序集不会自动传递。只在新Expansion.Editor.asmdef增加Unity.Addressables.Editor，不改包或旧程序集；Import_C新鲜度等待期间没有调度构建或运行旧DLL，错误留档后重查编译门禁。

## F-311：失焦前未捕获指针，关闭面板应保持该状态

2026-10-31，Windows Display_A原生720/900/1080、窗口/无边框、15秒超时与失焦回退均执行，最后“Settings closes and input restores”失败。夹具失焦后仅SetApplicationFocus(true)，未重新捕获鼠标便重新打开设置；租约正确恢复原PointerCaptured=false，不能把它等同UiBlocked未释放。保留失败图与Result，改验面板关闭/UiBlocked=false后以真实左键重新捕获，再断言CanPlay，不修改产品租约的原状态恢复约定。新轮须重跑完整显示流程。

## F-310：Addressables构建会复制临时link.xml并留下meta

2026-10-31，首Windows构建d7ca601成功86.220秒/0error/5warnings，SDK作用域曾保存旧Next两管线但SHA均与原始3767清单相同；新增link.xml.meta不在原始清单。官方AddressablesPlayerBuildProcessor.PrepareForBuild明确从Library AddressablesLink复制至Settings.ConfigFolder/link.xml，并在InitializeOnLoad清理，属于SDK预留临时路径。新包装在确认原始无该meta、实际ConfigFolder路径完全匹配后，归档本轮生成XML/meta并仅DeleteAsset该文件，不调用会尝试清理目录的内部方法、不删整个配置目录。最终仍须旧清单复核。

同轮Windows Panini警告复用F019/F271：只影响未启用Panini pass；不为消提示关闭剥离，另追加真实已用后处理开关图差验证。其余五构建警告与原始回执保留（Door_Frame_Square未来预烘提示、未绑定Dashboard、FindFirstObjectByType过时、DEVELOPMENT_BUILD过时、System.Windows.Forms元数据）；不改第三方或全局联网配置，实际启动/路线另测。

## F-309：验证器方法名Key会遮蔽表达式中的枚举名

2026-10-31，V01 Prebuild_A被Console门禁拦截，未创建Job。ExpansionWindowsProbe.Key(Key)方法使Walk中的Key.W解析成方法而报CS0119；参数类型上下文虽可编译，成员表达式需完整UnityEngine.InputSystem.Key.W。修正该处并把Validation也加入程序集新鲜度检查；没有运行旧DLL或启动Windows构建。保留编译错误与门禁回执，重新编译后新label全回归。

## F-308：真实路线需避开未拾取货物，跟随机器人需让路

Integration_D17/17实际通过，两条来回路线、远程抬高设备扫描、G04/G03回归均已完成。新增侧向让路未取消物理碰撞；后续全回归与Windows仍需独立执行。

2026-10-31，Integration_C14/16；上路在样本箱(16,.32,18)前被挡，测试路径应绕物或拾取，不应穿透实体；改明确绕行点，不移除物品碰撞。下路已完成隔离/三夹具/核心拾取，返程在(27.83,12.90)受阻，旧FollowNode没有回头让路逻辑；新版采用带MovementLease的侧向让路节点，保留物理实体，后续以完整返程实际验证。另审查发现调查目标中心y=.9超出Nav采样.6范围；将单层调查目标投至机器人地面高度，并新增从出生点实际走到抬高设备的扫描测试，不能仅靠近距离夹具宣称远程调查可用。原失败结果与首场景帧保留。

## F-307：设备摆在主通路中心会被真实移动碰撞揭示

2026-10-31，Integration_B实际38/40：所有六整合用例已加载；上路在(9.67,17.82)、下路在(15.47,6.01)被阻，分别对应FanSocket与Valve实体，UI未阻断。原相机帧/存读/失败/互斥/三画质通过，不能把它们当两路线完成。把新场景通风口移至(10.2,.9,20)、阀门移至(16,.9,4.5)，均离开通路中心但仍可在原交互站位3米内操作；独立重烘Nav_Clearance，保留初始Nav与失败证据，不改旧场景或移除碰撞门禁。之后原路线不改重测。

## F-306：Runner成功可能只执行了已加载的筛选子集

2026-10-31，B06 Integration_A34/34但仅2.767秒，与两条实际移动路线不相符；核对实际结果发现只有B01/B02纯测试，新IntegrationTests未进入已加载DLL，不能称整合通过。旧模块通过记录由实际fullName核验，新增此轮独立保留作无整合覆盖。run_module改为每轮显式Refresh/compile、等四个自有程序集DLL时间不早于对应源码/asmdef，并在结束断言每个请求group至少实际运行一项。真实整合需要新label重跑。

## F-305：复用画质枚举需按实际Domain命名空间

2026-10-31，B06编译门禁CS0234，ExpansionFogField误写EchoStation.Salvage.RecoveryQuality；实际类型在SalvageNext Domain。按rg声明修正完整命名空间，不改旧枚举或包。尚未调用场景创建、未运行新整合测试，修正后重新查Console与实际构建入口。

## F-304：Echo扫描首轮4例拒绝，先留分支诊断

最终Echo_D11/11通过（新增真实墙遮挡）；B06后续扩大远程扫描与位置快照回归，不能用仅静态发现计数代替路径完成。

Echo_B仍6/10，确认目标已注册但discovered=0；Echo_C补射线现场证明distance2.214/angle0，命中Device_3与ExpansionPlayer。单Raycast可能先撞玩家自身CharacterController，错误地当环境遮挡。改NonAlloc最近有效命中，明确忽略自己玩家/机器人层级，保留墙体遮挡；新增真实墙挡住目标后移除才可发现用例。不是取消发现门禁，所有原失败轮保留；复测待执行。

2026-10-31，G04 Echo_A6/10，四例在ScanValve发调查命令时返回false；取消/替换例与危险回归5例通过。尚不能仅凭布尔断言认定寻路或电量根因。保留原Job/结果，给拒绝分支与断言增加输入门禁/发现数/注册/执行树错误文本，重跑同套后定位，未放宽语义/权限门禁或进入B06。

## F-303：查到类名后仍须按实际检索路径读取

L02审查补记（2026-10-31）：只读搜索误带不存在的Tools/SalvagePrototype目录与ExpansionFacility.cs，随后由rg --files定位实际Tools子目录与FacilityController.cs。宽泛检索把历史Evidence大量路径输出造成截断；后续限定源码目录/扩展名和目标代码段，不把截断内容当已完整审查。本次未修改产品或执行测试。

2026-10-31，G03测试等待期间，参考代码读取误把EchoBattery归入Domain、IntentBehaviorTree按同名文件猜测，Get-Content报不存在；未修改资产或影响正在运行的测试。统一改用rg类声明及rg --files的实际输出定位，不能先检索却又以猜测路径读取。属于F300同类检索错误，无产品测试失败或回滚。

交付核对补记：压缩上下文误记滚动状态为Expansion/ExecutionState.md，实际为Docs/SalvagePrototype/ExecutionState.md。只读失败后使用rg --files确认正确入口再读取；没有重放构建、恢复或任何场景写入，最终交付结论不受影响。

## F-245：Light依赖组件要按依赖顺序移除

2026-10-30，F244第二次安装仍计6，Console两条Can't remove Light because UniversalAdditionalLightData depends on it。DestroyImmediate没有抛异常但实际拒绝移除，不能只据方法返回继续。源码RequireComponent证实依赖，先移除新版副本该灯的UniversalAdditionalLightData，再移除Light；仍未保存Scene／改Renderer／启动Fog作业。新增实际4源数断言，编译清理历史Console后重做配置和测试。旧R2源码／场景不改。

## F-244：新版故障灯与R2旧警告灯重复，预算必须实读

2026-10-30，NextFogSetup首配置抛Session and four local lights required；尚未改Renderer或保存Scene，finally还原宿主，未启动Fog_A作业。LightInspection_A确认session1、实际Realtime局部6：手电＋扫描＋R2原ReadOnlyWarningLight两盏＋新版Fault两盏。此前文档“共4”是计划，旧批量ShadowBudget实际包含6，不应当已核验4。

查R2TargetSetup／SalvageTargetView，旧警告Light仅装饰、无玩法引用。仅新版Scene移除两个重复Light组件，保留其GameObject／危险ID／Collider／文本及R2源场景；使用新版故障灯提供该位置实时闪烁，重新将4光注册ShadowBudget与Fog。无静态照明改变（这些原本Realtime，反射离线捕获已排除），不重烘焙；后续实际验证源数4、权限／扫描回归。

## F-243：补充资源先比对既有文件，不重复制

2026-10-30，R01c-1中性灰来源从已验证ZIP读出CRC／SHA7b13c6a7…；复制保护发现Raw内已存在，FileExistsError明确停止、无覆盖。随后比对既有文件1,125,543B与官方条目SHA完全相同，记录NeutralReused_A并直接复用，不把它记新增下载或新增原文件。历史Extraction不变，Review临时副本计入本轮预算。

## F-242：Cull Off不替代面朝房内的烘焙几何

2026-10-30，Lighting_H联合8/8：材质R×0.15后静态移除差0.013649、灯关闭差0；动态0／0.006973／0.020292，所有像素有限／UV2／三反射／运行预算通过。但人工看图西墙和背侧仍黑，不能将自动功能通过写成最终场景美术完成。F234的Cull Off解决可见性，不会给错误朝外的单面模块提供正确室内烘焙法线。

按原模块正面方向修正：西外围转180°、北外围转270°；x12／24隔墙在原0.3m碰撞厚度内拆成两份0.15m、相背朝各自房内，各归所属RoomVisual。既有低成本碰撞、门洞和NavMesh不变，更新房间caster注册；新增四个方向实际墙面中心亮度检查，重烘焙与联合复测后再看图。原所有失败／功能通过回执留存。

## F-241：Unity6000.6的Scene.handle不再隐式int

2026-10-30，F240诊断新增handle记录编译CS0619／CS1503，SceneHandle需GetRawData，不应塞入JObject或隐式int。只需识别场景，改记录准确path，避免依赖底层句柄。Lighting_E启动被编译错误门禁拦截，未运行新测试。F240原始DynamicAB保留：0／1／2倍中心值0／0／1.621e-5，微弱响应存在但不达标，不能表述为绝无Shader响应；继续排查相机场景和实际照明强度。

## F-240：Lightmap非黑与图像接收是两层门禁

2026-10-30，Lighting_C1/3，静态移除差仅3.64286e-8，动态SH0／1／2像素增量0，实际帧近黑，不能把Atlas／UV2／探针结构通过当受光完成。NativeLighting_A证明两Atlas平均0.01635／0.00857、峰9.61／10.46，三个位置插值L0约0.0036–0.0111，数据并非全黑；尚未定位实际Play接收问题，禁止直接换灯或放宽阈值猜根因。

新增实际Play地图数量／接收索引／探针与管线快照，图像对照数值先落盘再断言（首失败已留PNG与测试结果），继续区分加载生命周期和Shader接收。此轮不修改产品光照，等待证据。F239根筛选修复已生效，当前是不同问题。

## F-239：运行时UI会新增场景Root，夹具不能假定Single根

2026-10-30，Lighting_B1/3：重开静态贴图全像素有限／136UV2映射／180探针／三不同反射通过，两个Play图像夹具在Boot抛Sequence contains more than one element。新版面板运行时会生成独立SalvageEventSystem Root；Editor只有World根不代表Play仍只有一个。只将夹具根查找改为唯一ES_SalvageNext_World，保留失败回执，重跑全部三项，未改产品照明或验收阈值。

F238补充：派生静态UV2由551到603顶点（UV接缝拆点），1557三角索引数与Bounds保持。第二轮同UnityComputeGPU 38.787秒、bakeErrors=[]、Console0，2Atlas／180探针／136映射并恢复布局；非有限问题不再出现，仍保留首失败输出，不据此宣称所有GPU烘焙都无问题。

F240诊断：Lighting_D实际Play仍2Atlas／180Probe／136mapped，插值L0与Editor一致，排除简单“没加载图”。复查既有R03LightingProbe构造器明确camera.scene=scene；新版夹具尚未设置此独立Camera场景属性。下一轮只补相同隔离设置并记录前后handle，检验相机渲染请求的场景范围；未改产品光照或因未证实的面积灯猜测而重烘焙。

F240后续：Lighting_F仍1/3，CameraScene准确从null到新版path却无任何改善，排除该假设。原生实读Rectangle强度3、单位Candela、方向向下；低照明可能使PBR／ACES落在黑区，且2倍SH已有微弱响应。保留LowEnergyBake_B，接下来仅将18矩形灯调60cd并重烘焙，门槛不变，尚未宣称修复。同步将新增故障灯注册动态ShadowBudget，原先R01a捕获发生于故障灯创建前。

F240调优：60cd后Lighting_G联合6/7；动态SH0／1／2平均0／0.005689／0.016490，证明低照明部分已改善；静态移除差1.619e-5仍失败。原生MaskMetalInspection_A的Trim01／02分别99.778%／99.743%像素metal≈1，Trim03为0。封闭室内纯金属缺少漫反射、反射捕获也受黑表面影响，非简单Shader未加载。按N08静态涂装材质适配，派生Mask只把R乘0.15、其余GBA保持；不改源及动态材质，留MetalSurfaceBake_C，重烘焙验证，尚不提前记通过。

## F-238：Bake回执含Atlas不等于有效；静态装饰必须独立UV2

2026-10-30，首UnityComputeGPU烘焙95.408秒，2Atlas／180探针／136映射，但随后Console有8条Non finite value（两张direct／indirect irradiance与directionality）。Lighting_A被门禁拒绝，未启动测试；首完整LightingData／Scene备份FirstBake_A约1.90MB，源错误回执保留。原Finish.passed只表示结构计数，不能作为完整成功。

Uv2Inspection_A查出三处静态Prop_Computer共用551顶点、uv2=0的道具源；道具导入原本刻意不生成UV2以保护可搬物。新版将它当静态装饰却未补独立UV2，是实际缺陷，但尚不声称已证明非有限值唯一根因。仅派生自有静态Mesh，GenerateSecondaryUVSet并断言三角数与Bounds保持，源FBX／货物版不动；新增全部静态接收器UV2断言。先保持同GPU后重烘焙、查看新Console再跑图像对照，仍失败再诊断。辅助CodeDom查询误用UnityEditor.LightingSettings仅编译失败未执行，实际类型UnityEngine.LightingSettings已纠正。

## F-237：Unity API升级模态框会让MCP有连接却不处理请求

2026-10-30，添加NextLighting后read_console ping不答、execute_code空回执、refresh超时；进程Responding=true，instances仍连接。computer-use激活实际Unity窗口后看到Script Updating Consent，仅列新NextLighting.cs；选择No保留源码，Console准确报CS0619 LightType.Area已移除，要求Rectangle。没有重启、保存／丢弃用户内容或允许自动修改其他文件。先前Prepare／StartBake均无Settings或Started附件，不能当烘焙已启动。

手动改Rectangle，同时按6000.6警告选择UnityComputeGPU并使用customReflectionTexture；更新原CPU实施参数。以后域重载连通但不处理先查模态，不连续重放变更。首张被遮挡窗口截图实际是前台Codex，未据其输入；激活唯一返回Unity窗口后重拍才操作。编译恢复后必须先Console0，再准备／启动并检查实际回执。

## F-236：灰盒手电强度不能直接算新PBR场景已验收

2026-10-30，R01a Render_A3/4；资产、真实切档／阴影tier／远处casting／还原通过，实际手电画面对照均值增量0.00055998，未达0.001。新场景金属贴图与高反射环境下贡献偏弱，未降低阈值或将正数当通过。只在新版把手电强度5调20，保留旧R2与失败图，完整四项重测；正式烘焙后还需复核。

## F-235：新版测试不能依赖旧程序集的internal写知识接口

2026-10-30，f-2测试CS1061：RestoreValidatedIds在Salvage内部，仅旧friend测试可用。保留生产知识写权限，不加friend／公开传感器注入接口；新版测试通过完整合法存档TryRestore种入明确夹具发现，再测生命周期。编译门禁期间未运行旧DLL。

## F-234：源墙面应检查房间两侧可见性

2026-10-30，A01e Layout_A2/2功能与导航通过，但人工看图西侧墙面呈黑背景，未因此宣称美术布局验收通过。源模块墙材质单面，房间从反侧观察不能依靠实体Box证明可见。派生新版专用墙材质设置Cull Off与doubleSidedGI，其他源／旧场景材质保持；新断言逐墙绑定，重新渲染与导航复测，保留初图。此为表面适配，不改变物理开口／规则。

## F-233：ReceiveGI的命名空间与旋转模块缩放

2026-10-30，A01e编译CS0234，ReceiveGI实际位于UnityEngine，不是UnityEngine.Rendering。未启动布局创建，改正后重新编译检查。实施审查同时将90度旋转模块的世界尺寸比转换回局部轴，避免把墙长加到墙厚；后续Bounds断言验证实际结果。文件查找继续遵循F221，先rg实际文件再读，不能按类名猜目录。

## F-232：启动命令无输出也可能已有晚到测试作业

2026-10-30，A01d Carry_A shell最初无输出，仅两份安全文件；随后Job与Carry实际附件写出，不能据首次空回执认定没有作业。Carry_B因外部变化门禁未启动；刷新后的Carry_C2/2通过。以后先检查session／Job文件／editor.tests，再决定重试；本次两个实际作业保留，不把重复执行当额外覆盖率。

## F-231：非黑帧不等于有效资产画面，BakeMesh应保留本模型缩放

2026-10-30，人工查看Import_A图发现整幅蓝色平面，虽然数值方差／亮度断言通过，不能当视觉兼容通过。原生包围盒定位到Chest／Fan静态姿态转换后约150／167m，其余模块0.5–5m正常。SkinScaleProbe_A对照实际transform scale100：BakeMesh(false)产出米级顶点再乘100，BakeMesh(true)产出正确局部厘米坐标，与100变换后原始世界尺寸匹配。

改用BakeMesh(mesh,true)，不任意缩放所有原始资产；新增所有模块最大边≤10m、画廊七模型必须入镜且相机不在任一模型包围盒内。保留首张无效图／Import_A／Probe，修复后要重跑7项并人工看新图；“非黑”只是一项门槛，不是完整视觉验收。

## F-230：Texture2D.whiteTexture不是可持久化素材绑定

2026-10-30，R4-A01b Import_A实际6/7，场景渲染／四组ORM／源SHA通过，几何材质用例在BaseMap not-null失败。原生逐项查询确认ES_Black／Light／LightFade的albedo引用为null，平法线仍正确；序列化Material中运行时whiteTexture未留下持久引用。不是粉材质或来源贴图缺失，仍不能将整个模块记通过。

保留三个原始.mat、MissingBaseMaps_A、Import_A与首张图。生成自有2×2白色数据贴图作为三种无贴图原材质的持久基底，不放宽断言；重新保存11材质后全模块复测。未来几何／图像输出用GUID新名避免覆盖失败附件；原Render_A.json与PNG保留。

## F-229：不能只查MI_前缀；静态道具有SkinnedMeshRenderer

2026-10-30，R4-A01b第二次在M_LightFade_Red停下，7模型完成；原始ModelProgress／MaterialMapping_Stop_B保留。此前二进制扫描仅MI_漏掉M_Black、M_Decal_Screen、M_Light等，改以实际Unity Renderer槽导出SourceMaterialSlots.txt核对。另发现Chest／Fan是SkinnedMeshRenderer，不能按无MeshRenderer判定源无几何。

新增明确URP映射：黑色面板／灯面／屏幕；源LightFade仅作源映射，在派生副本移除其装饰光锥三角，实际体积光仍由项目局部雾实现。静态Skinned道具在自有临时场景按当前姿态BakeMesh，移除Animator／Skin后存新自有Mesh，源FBX不改；随后全26几何／法线／UV／材质／可视化要重测。没有把静态姿态转换写成动画已实现。常规适配属用户N08委托，不改玩法。

## F-228：场景模块还有PaddedWall／Decal材质，不能只映射三套Trim

2026-10-30，R4-A01b首次PrepareModels在Column_MetalSupport的MI_PaddedWall明确中止，完成0模型；未默认粉材质或套错Trim继续，Console0、原空宿主仍干净。扫描26实际FBX的MI_名称，另发现MI_Decal_Red及蓝色Cable变体。补取同一已验证CC0包中PaddedWall四贴图与T_Decals，保存Extraction旧副本／追加SHA，不重新下载或覆盖旧包。

新增Padded真实ORM打包和法线、Decal透明裁剪与平法线材质，保持URP Lit；Cable蓝色只作同Atlas常规配色。重新编译后先重跑材质／26模型准备再独立测试，不把素材接收／代码编译称兼容通过。原止步回执MaterialMapping_Stop_A.json留存。

## F-227：三候选语义均不达标，按确认的No-Go分支继续美术

2026-10-30，C3格式60/60但任务47/60，三候选最好C1仅48/60，均未达54/60诊断门槛。存在留守／问题／推荐与动作混淆，以及混合越权漏拒绝。全部模型原文与守卫结果保留，越权可执行0不替代原始正确率。无合格候选进入H100，H100明确未执行；R3标未完成，停止AI实验，按用户已确认N02继续R4脚本版，不追加训练／第四候选／换模型。

## F-226：候选2仍未提升语义且一次推理触发逻辑deadline

2026-10-30，中文System＋8示例D60实际任务46/60、格式59/60，D60_039为Inference logical deadline exceeded，20.212秒完成通知；失败按原始60分母计入，不重跑或放宽20秒。Cold27.325秒、P50/P95 14.618/16.587秒只属当前开发机Editor观测，不把语言改动与延迟差异直接做因果结论。Owner真实后台恢复并完成余下请求、最终清理。

按已批准上限只剩候选3：更精简规则与官方SingleUser包装、内联八例，保持同一模型／非思考greedy／4线程／全部预算／质量门槛；先独立实际tokenizer门禁。原C1／C2／冻结D60／H100保留；不以脚本安全输出替代原始质量。

## F-225：回收候选1只满足JSON格式，语义与建议仍失败

2026-10-29，R3新D60真实格式60/60而原始任务48/60；存在仪器错目标、未知目标替换首项、问摘要却调查、建议空泛，以及混合越权只做安全半句。C#实际可执行越权0，不能把拦截算原始质量。全部Results.json与12项原文保留。候选2仅据此诊断加强中文优先级、先拒绝越权、知识／建议不派发、未知不替代；仍须先预算门禁，再完整60，H100未推理／未用于改版。web工具读取Rover ZIP报unsupported application/zip只表示网页解析器不读ZIP，已得到官方文件URL，不能写成资源下载失败。

## F-224：快照重复采样不能无条件增加状态版本

2026-10-29，J01a守卫独立15/15后复审发现K01b每次Capture都增加revision；若回包时再次捕获，即使世界没变化也会让所有回复被stale_context拒绝。尚未执行模型／整合，非实机已观察卡顿。改为只在会话、显式偏好、交付进度、已知ID／状态／可达成员变化时递增；路线距离仍实时刷新但单纯采样不变。新增真实场景同状态版本稳定／改偏好递增断言，K01b与J01a须一起复测，保留先前回执。

## F-223：Unity当前NUnit不提供Assert.Multiple

2026-10-29，R3-K01a三个测试方法编译CS0117，regression_host Console门禁正确拒绝，未切场景／启动测试。当前TestFramework所带NUnit无该API；改普通方法顺序断言，不删任何验证条件。原文见R3-K01a/CompilationErrors.txt。编译／门禁恢复后新标签实测，不能运行旧DLL。

## F-222：重连期工具空回执与诊断参数

2026-10-29，新轮regression_host的只读快照返回success=false/null，尚未生成WorkspaceBefore，未切场景。get_history同样空；另一次调用误用max_history被schema拒绝，应使用limit。实例资源仍唯一，随后最小只读execute_code成功，确认原VC干净；先以新标签重新安全入口，不重放未知副作用。读取系统Editor.log误遇启动参数，后续仅用项目Logs/Editor.log过滤必要诊断，不输出启动参数或复制凭据；遵守F190。新增报错日志不包含凭据。

## F-221：新一轮只读审查仍须先定位文件

2026-10-29，读取CargoRegistry.cs及SalvagePrototype/ModuleBreakdown.md时文件不存在；前者实际定义在SalvageCatalog.cs，后者已有R2_ModuleBreakdown而新R3/R4尚未创建。无文件被覆盖，未运行测试。按F192先rg --files确认，再读实际接口；本轮新文档明确命名R3_R4_ModuleBreakdown.md。不把路径读取失败计作产品失败。

## F-220：不能用SaveToSerializedFileAndForget直接保存已持久化ProjectSettings对象

2026-10-29，R2最终还原时为避免保存无关Package Shader脏导入对象，尝试仅将两设置对象序列化到自有预览目录。API记录两条“You may not pass in objects that are already persistent”并返回，未创建文件；外部CodeDom成功不代表文件产出，FileExists/读取已拒绝，未将该方法用于目标文件。Console原文留Evidence/R2-V01b/RestorationPreview_A/ConsoleErrors.json，原因现已确认是persistent对象限制。

按F108：先确认原VC场景/默认与PC管线、原生AA4及无用户脏资产，再仅对磁盘两文件的S2→VC GUID、PC AA0→4生成候选，候选完整SHA必须逐一等于最初2403基线才允许原子替换；保留before/candidate/Receipt。不保存其他脏资产、不重写未核对字段。FinalWorkspace_A原生完整通过，FinalAudit_A2403文件全一致。清理前只确认并归档上述两条自有恢复诊断错误；它们不是五进程产品运行错误。

## F-219：Unity6000.6只读诊断中的GetInstanceID已过时

2026-10-29，列出设置对象时附加GetInstanceID，被CodeDom因obsolete诊断拒绝，代码未执行。删除无关ID读取，仅保留资产路径/类型，后续原生核对通过。不改引擎版本、不把此诊断失败当作产品或还原失败。

## F-218：回归证据路径使用Guid要显式命名空间

2026-10-29，Regression_A被Console门禁拒绝，未生成Job，未poll。原因是ArtBench原文件仅using System.IO，新GUID路径用了未限定Guid导致CS0103。改为System.Guid，保存ConsoleErrors证据；编译恢复后新标签重新完整门禁，不把此次门禁拒绝记成测试用例失败。

## V01复审补充：交互顺序与回归证据隔离

2026-10-29，静态复审发现Echo面板Esc关闭若只当帧捕获，随后PlayerInputReader仍可能执行Escape释放；在自有Panel LateUpdate完成捕获，UI用例新增PointerCaptured断言，Routes_A真实9/9通过。扫描失败原因由执行器传到会话（不足电量/遮挡等中文反馈），避免仅泛化成不可达。固定历史输出的回归测试改为GUID新目录：R1路线/R0美术/P01反馈，不改变断言；之后全量回归不覆盖旧报告附件。跨场景夹具背景状态只捕获一次，沿用F089原则。

## F-217：当前动态Carving洞会错误拒绝历史有效Echo位姿

2026-10-29，Save_A18/18通过后附加SaveGeometry_A实际0/1：把当前玩家移到存档Echo旧位置，等0.5秒后旧位置被Carving挖洞，ValidateGeometry报Saved Echo not on navigation surface；该存档本身有效。不能删除位姿校验、强制Warp或传送到其他点。8个动态物改用NavMeshObstacle实体局部避让（carving=false），静态墙/两危险仍烘焙，保留真实绕行/无穿透/不瞬移测试，避免临时世界洞影响同步读档。另明确校验存档Echo碰撞体与存档玩家/World货物/静态几何不重叠，再校验NavMesh；关闭Carving不等于允许非法位姿。修复须重跑N01b、导航、全部存档及生命周期，原失败保留。

## F-216：Newtonsoft是预编译DLL，不能用不存在的asmdef名称引用

2026-10-29，R2存档测试新增JObject后CS0246，未启动测试。检查CompilationPipeline实际Test引用中没有Newtonsoft，package提供Runtime/Newtonsoft.Json.dll而非Unity.Newtonsoft.Json asmdef；原Runtime依靠默认预编译引用才可编译。按既有Memory/Persistence测试写法，仅修改Salvage自己的Runtime/Test asmdef为overrideReferences+precompiledReferences（Runtime Newtonsoft、Test Newtonsoft+nunit），移除无效asmdef名，不改包或旧程序集。先编译清错再完整门禁，不能运行旧DLL。

## F-215：Editor异步截图请求不等于PNG已产出

2026-10-29，Feedback_B全部10项通过，但随后检查KnownTargets_A.png不存在，不能把CaptureScreenshot调用算图像证据。测试Runner中的异步屏幕捕获未产出原因未确认，保留真实UI文本断言并改写实际摘要文本回执；完整窗口图留V01 Windows实际文件校验。相机显式渲染的WaveOff/Repeat/On三图均实际存在且已目视检查，像素差与噪声证据有效；没有用缺失UI图替代它们。

## F-214：冷却期间不能把旧Completed回执用于新目标

2026-10-29，Feedback_A实际8/9，连续八目标测试在旧扫描完成→新调查等待1秒冷却时被“扫描结果验证失败”停止；其余扫描/取消/交互通过。旧engine.Phase仍Completed，新active已换，包装器过早检验旧receipt。安全校验已阻止错误发现，不能直接跳过目标ID核对。增加begun与传感器Generation，只处理本任务真正TryBegin成功后的Failed/Completed；冷却仍逐帧推进，取消清代次。复测完整八目标、单目标事务与UI回归，不取消冷却或复用旧回执。另Windows rg路径通配符应传-g过滤（F192），不能把未展开*当实际路径。

## F-213：手动推进夹具必须先停自动Update再初始化Echo

2026-10-29，Sensor_B真实3/4，8.01电应在到达稳定期间降到不足8却仍发现。检查发现夹具先Initialize再echo.enabled=false，触发OnDisable令Initialized=false，AdvanceResources因此没有执行待机耗电；不是正式运行中电量规则失效。按R1既有模式先停自动组件、再Initialize，并断言Initialized=true；同样修正N01a夹具顺序，重测传感器和导航，不削弱8电阈值或添加测试特权。Sensor_B取消/遮挡/拿走/距离角度及重复收费其余实测保留。

## F-212：扫描夹具的玩家视线也必须避开实体Echo

2026-10-29，Sensor_A真实1/4，3项在显式指向/命令接收阶段失败，尚未开始扫描；玩家与A之间已摆放Echo，不可跳过机器人碰撞去选后面的目标。将测试玩家初始位改到A西侧(7,.1,19)、yaw90/pitch35，机器人仍在南侧approach(9,0,17.5)，保留真实最近遮挡与传感器距离/角度判定，再重测。未把接收失败说成扫描成功/扣电故障。该几何原因以复测为证，不修改产品射线过滤。

## F-211：任务板文字Canvas新增按钮后也需要GraphicRaycaster

2026-10-29，R2-U01b UI_A真实2/3：R面板键鼠与中文/IME预算通过，任务板Echo入口点击后未打开。原任务板是只读Text，Canvas没有GraphicRaycaster；新增Button仅有EventSystem不足以收到鼠标事件。仅在任务板接入按钮时补GraphicRaycaster，保留真实MouseState点击测试与输入阻断，不用onClick.Invoke代替实际输入；修复后重跑整组。另读取文件需先rg定位（F192），此次误猜SalvageInputReader/Dialogue路径未改文件；CodeDom只读诊断漏return被编译器拒绝，未执行，不据该调用推断Editor状态。

## F-210：诊断访问返回的跨程序集类型也需要显式asmdef引用

2026-10-29，验证驱动新增echo.Navigation.State/Goal诊断后触发CS0012，EchoNavigation属于EchoStation.Echo.Runtime，Validation asmdef此前仅引用Salvage/Player/URP。Regression_C被Console门禁拒绝、没有Job，未poll。补充Validation→Echo.Runtime显式引用，不改旧程序集；成功编译后用新标签复测。另HUD测试显式补UnityEngine.UI引用，不能依赖间接程序集传递引用。

## F-208：Windows低帧步长/门洞接触时，仅看实际位移无法触发让行

2026-10-29，Windows R1_B前三进程write/read/route均exit0；alternate在2件8分、手持B时停在主门附近(12.48,0.03,19.64)，该轮3/4，未标R1完成。已有接近让行测试通过不代表门洞接触情形已覆盖。改为SalvageMotor记录本帧期望水平速度，让行依据玩家运动意图而非被挡后的零位移；采样落点需有实际>0.65m完整移动且远离玩家，尝试左右侧与前方安全退让，不缩小/关闭实体碰撞。补充真实PlayMode“已接触机器人→穿过门洞”用例；失败诊断补充机器人位置/导航状态，之后重测受影响模块与Windows。

## F-209：中途还原还要显式还原AA，且编排必须检查前一步结果

2026-10-29，regression_host正常恢复原VC场景/绑定后，原生AA仍0而最初快照为4，保存守卫正确拒绝。后续编排却仍运行audit与重新enter，audit如实记录GraphicsSettings/QualitySettings两项不同并exit1，但PowerShell换行没有自动终止，R1_B宿主继续进入；不能报中途完整还原成功。保留IntermediateRestoration_A失败，最终必须在原绑定路径/干净场景核实后显式还原原AA4、持久化并重算2403 SHA。后续依赖动作分调用核对exit_code/success，禁止把多行命令当自动失败停止。

## F-207：跨进程写档夹具放下C后走回自己的落物

2026-10-29，Windows R1_A/write真实exit1，1920×1080/D3D11/HDR→SDR有效；已交付A、拿放C后，夹具从(19.92,0.03,3.71)直线走向z6，被刚放在约z4.15的C碰撞阻挡，尚未保存/进入read。Processes.json/Player.log/失败Result均保留。更正夹具先侧走到(18.5,3)，再走返回短路，不改物品/玩家碰撞或状态；新增真实PlayMode写档→卸载拥有场景→全新实例读档继续完成测试，之后再重建/Windows新标签复测。Panini未启用剥离信息按既有F019为单独未启用Pass日志，不能据措辞推断全部后处理禁用，也不为消提示改旧URP资产。

## F-206：Windows首构建IL后处理DLL被其他进程占用

2026-10-29，真实构建54.491秒失败，2 errors/4 warnings；Unity.Jobs.CodeGen.JobsILPostProcessor读取Library/Bee/artifacts/1900b0aPDev.dag/post-processed/Unity.RenderPipelines.ShaderGraph.ShaderGraphLibrary.dll触发IOException“being used by another process”，随后Player scripts compiler errors。保留WindowsBuild_A_ILPPFailure.json，不能把构建菜单Attempted或纯C#编辑器编译通过当Windows通过。没有证据确认占用者，不归因于用户/防病毒/其他Unity实例。先确认目标Editor空闲且干净、只读检查该DLL当前是否可读，清理已归档构建错误后仅有限重试，不杀其他工程进程、不递归清Library、不关闭安全或联网服务。

## F-205：构建菜单已接收不等于delayCall开始构建

2026-10-29，Salvage构建菜单返回Attempted，数分钟无回执/输出文件；原生确认Editor空闲、BuildPipeline.isBuildingPlayer=false，而自有pending=true。只能说明delayCall尚未执行，不能报构建已开始/成功，也不推断网络或模型故障。核对干净场景/未构建后移除该私有Build对应的旧delayCall并复位自有pending，保留BuildQueueDiagnosis_A.json。调度改为已有StationWindowsBuild使用的EditorApplication.update两tick队列，清Inspector后再进入Build；之后只读观察实际回执，不再次重叠排队。

## F-204：验证会话名检查不能使用新版.NET专属Char API

2026-10-29，给Windows验证驱动增加隔离会话目录时使用char.IsAsciiLetterOrDigit，Unity实际编译CS0117（SalvageWalkthrough.cs:24）。该API不在当前Unity运行时表面，已改显式ASCII a-z/A-Z/0-9/_/-判断，保留同样1–40字符和路径隔离，不改项目API兼容等级。待成功编译后继续回归/构建，不能在编译错误状态启动测试。

## F-203：放入正式灰盒物品后，早期空场景测试的目标点不再可达

2026-10-29，R1回归Regression_A真实53/54，G01主路夹具直线走向(18,0,18)时在(15.47,0.03,18)被已批准B备件盒挡住；早期目标点(18,12)也已放入超限设备。产品两条完整路线Walk_C已真实2/2，并未证明玩家穿物能力。将G01夹具路径改为穿主门后在14.5m处绕到z15再去检修门；短路仍走z6。保留实际CharacterController/原碰撞/墙阻挡/到达断言，不移走物品、缩碰撞或直接传送。复测仍按完整安全门禁，原失败保留。

## F-202：R1收尾再次遇到IAP后台日志，未启动作业不能poll

2026-10-29，Battery_C新鲜SceneSafety/RenderSafety均通过，但被唯一既有InAppPurchasing empty response环境错误拒绝，未产生Job。随后编排仍poll该标签导致FileNotFoundError；此为编排错误，不能记为用例失败/通过。参照F138保留ConsoleErrors回执、核对仅已知IAP错误后清Console，以新标签重新走完整门禁，不关闭服务/改网络。后续必须检查start实际退出码与有效Job后才poll，不能因exec工具调用返回就假定启动成功。

## F-201：跟随停止半径内的Echo会挡住返回路线

2026-10-29，修正PlayMode入口后的Walk_B真实0/2，两条实际控制器路线分别停在(16.59,0.03,6.06)/(6.87,0.03,14.56)。原R1跟随逻辑在玩家靠近1.9m内只Cancel，实体机器人停在返回路径上。不能忽略双方碰撞或传送玩家来使路线通过。补充检测“玩家朝Echo接近且<1.6m”，通过现有完整路径导航向侧面让行1.6m，最多保持1.4秒后恢复跟随；仍保留实体BoxCollider和原1.5/2.2速度。新增PlayMode实际迎面通行/侧移>0.25m用例，先重测E01，再重跑完整路线。这个修复限于基础跟随不挡路，R2动态障碍/调查仍须独立验收。

## F-200：失效测试后的宿主出现来源不明未保存相机，必须保留后再继续

2026-10-29，R1收尾恢复时原生Runner已空闲/未播放未编译，但SceneSafetyGuard拒绝，实际空宿主isDirty=true且新增Main Camera/Camera/AudioListener/URP AdditionalCameraData。未假定为Runner产物、未直接删除/保存覆盖宿主。向用户说明AGENTS/安全门禁后，用户明确选择“另存恢复副本后继续”。SaveScene(saveAsCopy=true)到Assets/EchoStation/Salvage/Recovery/Host_MainCamera_20261029_A.unity，实际OpenPreviewScene核实3种组件存在后，才重新打开磁盘空宿主。HostRecovery_A.json记录空宿主rootCount0/dirtyfalse与副本路径；原宿主文件未覆盖。保留新副本供用户恢复，后续不把这次局部同意扩大为任意未保存内容处理授权。

## F-199：R1路线夹具重复了F048的嵌套EnterPlayMode问题

2026-10-29，恢复后读取Walk_A最终回执为0/2，两项各180秒超时，原始output明确“Enter play mode before calling any LoadSceneInPlayMode functions”。上次仅按MCP editor_unfocused提示描述为焦点阻塞不完整，真实证据与F048相同：EnterPlayMode位于嵌套Run迭代器，测试没有进入播放。先保留最终回执，确认原生Runner空闲/干净场景，再将EnterPlayMode移至两个UnityTest顶层；Run先断言Application.isPlaying，加载加8秒独立墙钟超时。后台运行只在PlayMode中临时开启并还原，不在EditMode改PlayerSettings，不提高180秒总超时掩盖问题。物理Esc停止Computer Use的事件保留，不能作为Unity测试根因。

## F-198：NavMeshAgent真实绑定与移动要进入PlayMode

2026-10-29，R1-E01 Battery_A数值3项通过、场景1项失败。诊断有198索引导航三角、SamplePosition成功、AgentType一致，失败为NotBound；参考既有EchoNavigationTests确认原导航模块在EnterPlayMode后验证Agent，不能在EditMode激活真实运行Agent。将场景用例改为UnityTest真实PlayMode加载并验证0电量1.5m/s、实际移动>0.3m、泊位充电/暂停；纯数值仍EditMode。不是放宽绑定断言，不能把可采样网格当真实移动通过。

## F-197：重新打开隐藏UI不能依赖Graphic.canvas缓存

2026-10-28，R1-I03 Delivery_A真实6/7。取消交付会隐藏Canvas，第二次Request访问Text.canvas得到null（堆栈SalvageUi.CanvasOf），导致确认面板无法重开。改为GetComponentInParent<Canvas>(true)查实际父级，包括inactive对象；保留“请求→取消→再请求→确认→重复确认”的真实测试，修复后重跑整模块。

## F-196：交互射线会先命中玩家自己的CharacterController

2026-10-28，R1-I02 Carry_A真实2/6，正常拾取失败。实际RaycastAll诊断相机(9,1.7,17)、35°向下的射线在0.0496m先命中自身控制器，而工具箱在2.3587m可见。改用固定32槽RaycastNonAlloc，忽略玩家自身层级后选最近遮挡，满槽直接拒绝；不会跳过墙体找任意物品。保留实际碰撞射线测试与遮挡用例，修复后重测，不以脚本直接选ID代替玩家视线。

## F-195：还原内存绑定后仍须检查磁盘Settings哈希

2026-10-28，R0-A02按空宿主测试并恢复原跑酷场景后，regression_host确认原布局/选中/播放选项与VC绑定；native进一步确认VC/AA4。2403保护SHA却发现GraphicsSettings/QualitySettings仍是测试时S2/AA0（创建新资产期间Unity保存了全局设置）。按既有F173/F175，先核对native值与原快照完全一致、场景干净，再仅持久化当前已恢复设置，不直接覆写备份、不保存用户场景；之后重算2403 SHA。不能把native恢复当作磁盘也恢复。

## F-194：工具箱自动Lightmap UV导入会丢失小部件几何

2026-10-28，R0-A02-I Import_B真实8/9，工具箱handle和两latch导入为0顶点/0索引，不能跳过空Mesh假装通过。保持源FBX不变、仅关闭generateSecondaryUV的真实对照后，三件恢复2390/212/212顶点，其余壳/盖/托盘也正常。工具箱是后续可搬运动态物，改为不生成Lightmap UV、后续使用探针；静态地板仍生成UV2。测试保留“所有Mesh必须非空”断言，只按动态/静态区分UV2要求；原失败及对照JSON归档。若以后要静态烘焙该道具，须另做独立UV展开兼容性测试，不复用此次动态适配结论。

## F-193：生成导入文件后先排空外部变更门禁

2026-10-28，R0-A02-I生成Mask/材质并重导入后，Import_A被module_tests的external_changes_dirty拒绝，未启动测试、未生成RenderSafety；随后读取不存在的后置文件也报路径错误。按门禁先scope=all、compile=none刷新，再以Import_B重新执行完整门禁；不得把Import_A说成测试失败或通过，不反复请求编译。

## F-192：适配脚本的文件定位与原子补丁失败

2026-10-28，读取旧测试asmdef时使用了未经rg确认的路径，文件不存在；之后补丁携带了错误的故障文档标题上下文，整批校验失败，确认新Editor目录尚未创建。先读实际标题，再重交独立补丁；没有把补丁失败当作代码已落地。后续先用rg定位、按实际上下文编辑。

## F-191：初次CodeDom外部空失败仍要查实际历史

2026-10-28，R0-A02首个只读guard/快照调用外部success=false/null；随后get_history明确success=true、elapsedMs=85728.2，实际守卫与读取已完成。没有重复执行场景操作。下一次只读快照先写自有证据JSON，约7.8秒成功返回；SceneSafety导出确认旧跑酷场景干净。延续F112原则：外部超时不等于未执行，使用历史/实际文件判断，不重放有副作用动作。

## F-190：Unity Hub 进程参数不能整行输出

2026-10-28，R0-A02诊断Unity所属工程时输出了完整进程命令行，其中可能带Hub临时启动凭据。后续只在进程内匹配项目路径，输出PID/可执行路径/项目路径等必要字段；不再显示完整CommandLine，不把凭据复制进文档或证据。不因工具连接故障读取账号信息或修改登录状态。

## F-189：Unity仍运行不代表本地MCP服务已启动

2026-10-28，R0-A02首次访问localhost:8080/mcp返回WinError10061；目标Unity进程仍运行且Responding。未保存/切换场景或启动测试，先检查现有嵌入式MCP包与本地启动工具。已有project-setup.done标记只代表历史准备，不作为当前服务/编辑器会话连接证据；恢复后重新做握手和原生安全门禁。

## F-188：独立补收成功后必须合并真实回执再整体校验

2026-10-28，原接收任务在 Essentials/Toolbox 首次失败后继续下载其他组，最终因为manifest仍有failed而退出1；独立补收两个资产已成功，但不会自动改写另一个运行中任务的内存清单。门禁拒绝是正确行为，不直接把failed改为received。保存合并前清单，从Supplemental_Essentials_A / Supplemental_Toolbox_A真实回执逐文件检查SHA/大小、CC0与ID后合并，再运行完整--verify。运行中修改.py也会使回溯打印当前磁盘行号，定位应看实际异常类型与清单，不能只看已经移动的行文本。最终通过与资源统计以独立完整校验回执为准。

## F-187：尾部分段超时与元数据短读不能混入完整接收

2026-10-28，R0-A01 Essentials 已验证304/319分段后仍有原生curl exit28超时；Meta Toolbox元数据读取另抛 IncompleteRead。保留完整分段，延长单段有界传输等待到120秒，仍精确核对范围/长度/CRC，元数据短读加入有限重试，不把接近完成算通过。实际超时阶段未完整定位，不归因于许可或模型。续传峰值预算同时扣除已存在有效分段，避免把既有缓存和全部待下载再次计为两份；旧缓存、待下载缺口和最终拼接文件仍各实际计入。补收使用独立回执，最终统一清单只在实际校验后合并。

## F-186：Python HTTPS 的断连不能用关闭证书检查来修

2026-10-28，资源接收还遇 RemoteDisconnected、TLS EOF 和短正文。相同 Metal Toolbox FBX 使用系统已安装 curl 默认HTTPS验证实测HTTP200、536,476B、6.200秒，而Python首次仅取得524,288B；这是本机传输对照，不推断所有Python或资源站都有缺陷。二进制接收改用已安装Windows curl、参数数组调用、证书验证保持开启、严格长度/MD5/CRC，分段最多4连接；匿名表单仍按官网流程处理并做有限网络重试。不安装新依赖、不改全机代理/证书/防火墙。探针原始文件留在R0-A01证据，完整资源结果另记。

## F-185：网络提前结束也可能返回不完整正文，必须验证实际长度

2026-10-28，R0-A01 首轮 Essentials 分段及 Metal Toolbox 普通下载分别触发 Range length mismatch / HTTP size mismatch，没有发布为完整文件或记接收通过。逐块 read(amt) 的 EOF 不必然抛出异常，不能只看 HTTP 成功状态。对范围响应严格核对206、Content-Range与实际字节数，短正文最多有限重试；普通文件同时核对Content-Length和官网MD5/大小，旧 .part 原样改名保留再取。仍失败则报告失败，不接受短文件或修改期望值；修正后的完整接收结果另记。

## F-184：公开 CDN 签名链接有效期短于资源传输时间

2026-10-28，R0-A01 分段接收 MegaKit 得到46个有效分段后遇HTTP错误。重新通过同一个作者免费入口只读诊断签名参数，实际 X-Amz-Expires=60；记录仅有效期与签发时间，不记录签名或匿名令牌。因此不能把一次取得的下载 URL 固定用于数分钟传输。

修正：仍使用同一免费 Standard upload ID，在25秒后按官网正常 file endpoint 续取公开签名链接；串行续签、最多4个分段连接、校验刷新前后的HTTPS主机与文件路径一致，变更即停止。既有完整分段按精确范围/长度复用，最终ZIP长度/CRC/SHA校验仍强制，不降级证书、不改URL签名、不读取付费文件。初次6MiB部分文件保留到最终前缀比对通过后才清理。完成与否以实际接收报告为准。

## F-183：公开资源站长连接低速，须区分可访问与接收完成

2026-10-28，R0-A01 的官方免费 Standard 链接已取得，普通连接多分钟仅接收约6MiB/48,559,316B；文件仍为 .part，未登记通过。站点公开的官方备用模式返回文件 URL 后实际 GET 为404，未继续改写地址或降低校验。相同免费标准 CDN URL 的 256KiB 探测实际206、精确 Content-Range、3.639秒，证实支持HTTP Range；改为最多4连接、512KiB精确分段，保留初次部分文件。拼接后必须长度/CRC/SHA检查，失败不发布；仅完整验证后删除本工具产生且与完整包前缀一致的旧部分文件和分段缓存，不动其他文件、不改全机网络。完整接收结果另记实际报告，下载方式本身不算成功。

## F-182：公开免费下载表单的元数据位置不能按惯例猜测

2026-10-28，R0-A01 首次只读探测把 itch.io 的 csrf_token 限定为非空 input，断言失败；改 HTMLParser 后又被后出现的空 input 覆盖了 meta 的有效值。实际字段长度诊断确认 meta 为81字符、input为0，匿名 Cookie 存在；这些诊断只打印长度与 Cookie 名。修正为只接收非空 meta/input 值，空字段不得覆盖已取得值。此前失败均在 POST 前退出，没有提交支付或账号操作。按官网“跳过捐赠”按钮所用的免费 download_url 流程获取链接，只接受 min_price=0 和明确 Standard 文件，不读取 Pro/Source 下载；令牌仅在内存使用，不写入报告或日志。另一次读取静态脚本遇 TLS EOF，不降低证书校验或改系统网络；所需按钮流程此前已从官方脚本只读确认。实际下载与校验结果另记接收报告。

## F-181：文档与资源准备也应先判断可选配置是否存在

2026-10-28，回收原型规划时直接读取项目根 `.gitignore`，PowerShell 报路径不存在；此读取没有修改配置或资源。目录结构不能据 Unity 项目惯例推断，后续对可选配置先 Test-Path/rg 定位后再读。原始资源固定接收到 Docs/Assets/Sources/SalvagePrototype，不根据不存在的忽略规则假定文件已排除版本管理；本轮不创建或改动全局 Git 配置。

## F-180：出生点上方安全间隙需等待真实落地再测试跳跃

2026-10-27，PK06 Player_Final 1/2；按实际地面+.10出生后，夹具仍只等两帧便按Space，此时未grounded，空中起跳按设计被忽略，后续等待“已起跳再落地”8秒超时。保留该回执，改为4秒墙钟上限等待CharacterController实际grounded再发Space，不允许空中二跳来迁就测试。最终Player_Final_B实际2/2通过，起跳峰值1.598865m并落地；独立主场景播放的LiveReady再次读回grounded=true。

## F-178：Player全场回归发现当前楼体顶面与旧路线标记高度不同

2026-10-27，PK06 SceneRegression为12/13，Player真实输入通过，旧PK01 Waypoint_00期望12±.15、实际命中12.22。RouteRayDiagnosis.json和进一步原生读回确认：Roof_Cap顶面12，Building_Mass中心Y=.22/高24，实际顶面12.22；不是Player胶囊命中（player=false）。此位移来源尚未证实，不归因于用户或新控制器，不改断言掩盖失败，也不擅自重置现有楼体。Player出生改为从上向下查最高实际屋顶+.10，以适应当前场景；保留原几何和旧路线失败，归档为现有场景不一致项。该失败不冒充13/13或完整跑酷路线验收。

## F-179：Newtonsoft默认序列化Vector3会沿normalized属性循环

2026-10-27，只读诊断匿名对象含Vector3时JsonSerializationException，尚未写出结果或执行随后场景关闭。改为明确坐标字符串，finally关闭拥有场景并保存已还原基线；实际RouteRayDiagnosis.json成功留档。未修改场景几何、没有重复执行配置方法来恢复这一只读诊断。

F177图像补充：对Player_B截图目视复查时发现亮度/引导带与独立场景视图不一致，Additive环境不能作为干净渲染证明。按F062扩展夹具隔离：暂禁外部已启用Light/Camera/Listener，拥有几何临时30层、相机与自有灯仅30层，Volume仍0层；退出恢复外部enabled，临时场景卸载。未修改产品层、光照或材质；最终另做实际独立场景播放截图。Player_B的运动结果仍为当时事实，首图不冒充最终外观。

## F-177：Player播放测试的Runner监听器必须隔离

2026-10-27，PK06首轮日志出现两个AudioListener警告：加载拥有Player之前，Unity测试宿主已有启用监听器。产品独立场景只有一个Player监听器；测试按既有StationPlayFixture处理，暂禁外部已启用Listener，卸载拥有场景后恢复原enabled。未删除用户组件或关闭产品监听器。Player_B真实输入2/2通过后补此隔离及Shift实际位移验证，最终回归另行归档。

## F-176：LoadSceneInPlayMode返回时场景可能尚未完成加载

2026-10-27，PK06 Player_A 1/2，实际输入测试在SetActiveScene前失败：返回Scene尚未isLoaded，未进入玩家运动验证。改为带8秒墙钟超时等待isLoaded，再激活/查组件；清理也只卸载已加载场景。保留首轮回执，不算移动/跳跃通过。复测继续使用真实PlayMode、局部Physics和合成输入，不跳过场景加载或直接伪造结果。

## F-175：实际PlayMode还需还原URP写入的QualitySettings抗锯齿及跨生命周期管线

2026-10-27，11项EditMode回归通过后额外真实PlayMode图像非黑，但退出后Quality绑定仍为VC，保护SHA失败。手动按原S2路径还原后SHA仍不同；只读候选哈希明确PC antiAliasing从原0变4即可精确解释剩余差异。本机URP Render常规循环ApplyRealBackbufferMSAARequest会写入该字段，StandardRequest没有这一步，所以不能以显式图像测试代替真实播放后的全配置还原。

修复：Scope记录/序列化后备AA并在退出拥有管线时还原；在生命周期已清掉NonSerialized绑定状态或画质索引改变时使用已保存非空原资产，不再因bound/index早退而留下VC。先精确还原原S2/AA0，再重新真实播放、关闭场景、SaveAssets和1571 SHA核验。首后置失败与保护结果保存为PlaySmokeAfter_FirstFailure/ProtectedAfterPlay_FirstFailure；最终结果待后续实际回执，不提前计通过。

F161补充：最终QA刷新曾等待60秒超时；没有再次请求编译，后续实际state显示域重载完成/idle、Console0错误，继续配置。日志未显示C#编译失败；不将工具就绪超时归因于某个未证实的产品模块。

F175实测补充：第二轮退出后、关闭场景前实际private qualityIndex=0而当前PC index=1，明确证实生命周期索引变化触发原还原条件早退。新Scope关闭后原S2默认/PC、AA0、播放选项true/1、临时相机/RT0均有效，`PlaySmokeAfter_v2.json`留档；1571 SHA全部一致。实际播放图平均亮度.565102339，两反射各渲染3次。未改变用户画质等级或播放选项。

## F-174：重复配置场景不能把临时管线记录为恢复后备值

2026-10-27，最终参数微调前审查发现Initialize在已绑定美术管线时会覆写fallback为本身；此时尚未重新调用，属于静态发现的潜在恢复问题。改为仅接受非空且不同于美术资产的原引用。新增实际模拟“启动时磁盘已是美术管线”的场景打开/关闭测试，要求仍回原S2引用；不以普通干净启动覆盖这个边界。

## F-173：临时Pipeline在内存还原后仍须保存并核对磁盘ProjectSettings

2026-10-27，保护检查发现1571份中GraphicsSettings/QualitySettings两个SHA不同，其余完全一致。内存已回S2，但此前在美术场景调用SaveAssets曾把临时绑定落盘。回拥有空场景后SaveAssets，将原S2引用写回；两份原始SHA均精确恢复。不通过只看内存名称声称磁盘已还原。最终交付前重新检查全部1571份；打开美术场景时的临时内存绑定不保存为全局默认。

## F-172：URP17.6后处理CameraStack期间嵌套请求使共享CameraData失效

2026-10-27，PK05 Finishing_A为0/2。开启Volume后主图全黑，Console UniversalCameraData has not been created yet，堆栈SetupCullingParameters←RenderCameraStack；无后处理时走不同相机路径，故PK04图像通过不能证明组合可用。核查本机URP源码，CameraStack在beginCameraRendering前已创建FrameData；此回调内嵌套SingleCameraRequest会重置共享数据。改为beginContextRendering预先渲染，beginCameraRendering仅绑定缓存，保留真正平面反射与颜色后处理两者。截图工具补齐异常时targetTexture/type/active的finally还原；临时反射相机改为独立HideAndDontSave根节点，避免父节点停用时DestroyImmediate子物体错误，关闭先解除targetTexture再释放。修复后须重跑PK04+PK05和完整回归。

另一个自查失败为场景实际使用6种材质但有第7个未使用的Graphite资产；移除本工具未使用资产和生成调用，最终六种均分配。历史PK02七资产结果仍保留为当时事实，最终以全套回归为准。

## F-171：Transform.Find将物体名中的斜杠解释成层级路径

2026-10-27，PK04 Reflections_B平面反射通过，区域新烘焙五个SHA已各异，但共10个探针失败。重复配置时Transform.Find("09 — Reflections / Baked regions")未命中含斜杠的单个节点，创建重复组。改为根节点直接子项按完整name查找、统一不带斜杠的名字，只移除本工具刚生成的重复组；仍保留严格五个探针断言并重新烘焙/重开验证，不将数量断言改为≥5。D3D11平面反射UV需UNITY_UV_STARTS_AT_TOP翻转，已从实际地面/墙面镜像检查确认，并增加两面分别开启的像素对照。

## F-170：单独探针烘焙只捕获静态对象，native bakedTexture不作为场景独立持久化引用

2026-10-27，PK04 Reflections_A为1/2：平面反射图像/清理通过，但重载探针bakedTexture=null；五张首EXR SHA相同，说明只捕获天空，不能算区域完成。根因核查：几何均未设ReflectionProbeStatic，且场景YAML没有可依赖的bakedTexture持久化引用。补齐184个静态捕获标志；继续使用Lightmapping.BakeReflectionProbe真正离线烘焙，结果以Custom模式customBakedTexture序列化引用独立EXR（不是运行时伪烘焙）。新增重开后Cube引用和五个SHA各异断言；首轮同内容EXR保留一份FirstSkyOnly。UV方向也进行独立视觉核对，原开关结果不冒充反射几何对应正确。

## F-169：Unity6000.6.7f1禁止新代码使用Object.GetInstanceID

2026-10-27，PK04反射相机名称使用GetInstanceID时CS0619（Use GetEntityId instead）。名称只是诊断用途，不承担对象身份；改为场景物体/源相机的名称，实际状态仍以Camera引用字典区分。没有压制obsolete错误。临时相机挂在拥有根节点并设置HideAndDontSave，关闭组件释放，场景结构测试只统计持久化相机。

## F-168：Light.shadowResolution不配置URP阴影图分辨率

2026-10-27，PK03配置产生“Light.shadowResolution is compatible only with the Built-In Render Pipeline”警告。删除该无效调用；真实URP分辨率已由专用Pipeline.mainLightShadowmapResolution=4096设置并经测试读回。不改全局Quality，不把这个旧接口当有效URP配置。最终回归继续验证实际Pipeline参数。

## F-167：既有RenderSafety只允许S1/S2资产，不包含独立美术管线

2026-10-27，Lighting_B预检字段全部符合D3D11/普通Forward/HDR/SDR，但allowed=false：共用RenderBindingValidation的路径白名单仅含三个既有Pipeline。未修改/放宽共用守卫；测试改从拥有的VC_TestHost空场景和原S2绑定启动，继续使用module_tests.py所有门禁。夹具明确加载ParkourArt场景并逐项断言专用实际配置/图像，finally关闭后检查原资产路径还原。拒绝的首预检保留。

## F-166：URP资产部分公开属性只读，应核验setter与实际序列化字段

2026-10-27，PK03初次编译三项CS0200：supportsSoftShadows/reflectionProbeBlending/reflectionProbeBoxProjection为只读，类型成员列表不能证明可写。改用本机资产已核验的m_SoftShadowsSupported/m_ReflectionProbeBlending/m_ReflectionProbeBoxProjection序列化字段。Lighting_A被Console门禁拦截，未运行，不称配置完成；修复后重新编译/配置/验证。

## F-165：refresh scope=scripts只请求编译，不能保证新文件已导入

2026-10-27，新增ParkourFrameCapture.cs后仅scope=scripts，工具回refresh_triggered=false，新类型尚未导入导致CS0234。Materials_B被Console门禁拦截未运行。改为scope=all、force刷新导入后编译，类型正确加载；Materials_C实际2/2通过，Console0错误。

## F-164：既有SceneFrameCapture限定PlayMode，不能直接在EditMode释放临时资源

2026-10-27，PK02 Materials_A实际渲染触发“Destroy may not be called from edit mode”。调用点是既有通用夹具的Destroy，不是PBR Shader编译失败。未改既有夹具；ParkourArt改用独立ParkourFrameCapture，依播放状态Destroy/DestroyImmediate，RenderTexture.active始终finally还原。保留首轮结果，修复后重新验证全部材质测试与Console。

## F-163：EditMode Runner使用临时空场景，场景测试需显式加载独立资产

2026-10-27，PK01 Geometry_B为0/2；测试错误依赖“当前场景”，Runner执行时实际是临时未命名空场景，未读取产品几何。改为每项SetUp明确加载拥有的ParkourArt场景，TearDown回空场景，由Runner还原启动前布局；不保存任何用户场景。原始失败保留，不计作几何通过。

## F-162：ParkourArt测试使用了当前NUnit未提供的Is.AnyOf

2026-10-27，PK01首次编译CS0117（VectorCityGeometryTests.cs）；未生成场景、未运行测试。改为Does.Contain检查允许的基础Mesh名称，保留同一断言范围；编译/实际测试结果随后归档到Docs/Tests/ParkourArt。

## F-161：ParkourArt启动前MCP空失败与短暂未响应

2026-10-27，最初execute_code两次返回success=false/message=null，read_console出现ping not answered；Windows进程Responding=false。先查F064/F112/F113，未据缓存空闲状态切换/保存场景，未重启/强杀。稍后get_active恢复，明确codedom的只读代码成功，实际SceneSafetyGuard通过，S3场景干净、D3D11、S2默认/PC绑定非空、HDR显示关闭。根因未证实，不称其为编译器缺陷。WorkspaceBefore.json保留实际原布局与绑定。

## F-160：早期资源账本须扩展到后续所有S3构建与AI测试目录

2026-10-27，审查resource_budget发现沿用I03固定清单，仅列S3-G01构建和AIEditor测试，未覆盖后续AIExecutionEditor及新S3构建目录。此前没有因此证明超20GiB，但旧统计不能当后续完整峰值证明。

改为纳入Library/EchoStation/S3*、Builds/Tests/S3*、Builds/S3和Tests/AI*，按解析绝对路径去重/排除子目录重复，并计入直接文件；只stat，不读模型大文件、不删除资产。默认模型回归结束后另存新账本并核对各新增根和20GiB总量，再用于后续训练提案或构建预算。

实际Resource_Final_A为12,440,618,223字节（11.586GiB），Final_Round_A独立检查无重复/祖孙重计、总量等于分项且覆盖AIExecutionEditor和所有当前S3测试构建，通过；没有借漏记达到预算。默认2/2与767保护也通过。

## F-159：少量通用示例修好已见边界，仍可能无法泛化拒绝策略

2026-10-27，v11已见60为55/60，但新E首次仅88/100；11/12错误是代操作请求未选refuse，禁止操作类仅4/15，其余85条84正确。结构/时延/后台清理均通过，不是模型完全不理解中文或框架不可用；当前主要薄弱点是拒绝策略的措辞泛化。不能因为接近90就四舍五入通过、重打标签或把守卫回填正确率。

E原始结果与冻结SHA保留，不选v11默认，不跳T02–V03。下一路线建议本地Qwen3-0.6B LoRA可行性门禁，训练明确超出现有普通调优委托，须用户确认LocalLoraProposal后执行；默认与S1/S2先回归保护。未来训练验收另冻F100，不复用已见E。

## F-158：先做约束消融，不能把语义失败直接归因解码器

2026-10-27，g1相同六输入各有/无JSON约束实际12生成，语义都2/6；格式约束6/6、无约束5/6。取消约束未修好待命、面板目标、警告和代操作，还放出follow附额外字段的非法Schema。故未改生产语法，也不实施未经证实的空白/字段序放宽。

对照成功只说明证据收集、输入一致、线程/清理通过，不算模型质量通过。后续试英文分类定义、明确follow不能表示“遵从任意请求”；仍是Prompt试验，不改原始评分或C#纠错口径。

## F-157：官方消息分离正确不代表语义准确率提高

2026-10-27，v8包装19/19、profile24/24均通过，但真实已见C/D60仅43/60，结构60/60、清理正常，未达到54。将规则放system仍无法阻止代操作→answer等误判。不要把token一致/预算正确当模型语义改善，也不因“官方模板”就直接替换默认。

保留v8 No-Go；下轮短分类指令与真实user/assistant多轮示例，仍非思考greedy、原始JSON评分，先包装叶测试、再profile/预算、最后真实诊断。失败样本不从固定集删去；新版本最终必须新E100。

## F-156：Json.NET的null字符串JValue不等同于JSON Null类型

2026-10-27，v8 profile叶Profile_A为23/24，唯独诊断60源行严格相等失败。Audit_B独立复现首行T01_C_001，target实际类型Null/预期String。JObject初始化时直接放入静态类型string的null，生成JTokenType.String(null)，与从JSON读出的Null在DeepEquals不相等，尽管两者序列化都显示null。

只将测试加载器的预期target改为null时JValue.CreateNull、非空时new JValue；不修改C/D/诊断60字节、标签、模型或Prompt。保留A/B失败，Profile_C重跑全24项后才可进入模型诊断。此错误不证明v8模型语义失败，因为尚未跑权重。

复测Profile_C实际24/24，4.7295334秒，严格DeepEquals保留且60原行全部相同；随后才启动v8真实诊断。

## F-155：已见集达标不能代替独立泛化

2026-10-27，v7已见C100为91/100，但新冻结D100首次仅84/100（结构100、P50/P95=4.810/6.602秒，线程/释放均正常）。拒绝9、目标/调查3、警告2、待命2错误。新用例揭示lab摘要里的回路信息干扰设备代操作分类和目标选择；这是观察，不认定唯一根因。

v7不选默认，D原始结果/冻结前SHA保留，D转诊断。下一轮先试官方system规则与user摘要/请求分离，纯包装token与注入边界独立测试后再真实诊断；新版本最终须另冻结E100，不改旧D标签、不降低门槛、不把C#守卫算模型正确。

后续v9多轮短分类仍只46/60（固定已见难例），结构60/60、P50/P95=4.113/6.083秒、Owner释放正常。Prompt改版收益不稳定，不能一直堆例子后挑高分；先对完全相同输入做约束/无约束隔离定位，保持原始数据与正式生产约束。

## F-154：增加优先规则与示例可能加重小模型意图偏置

2026-10-27，v6完整C诊断100结构/88语义，低于v5的89；新增4条Follow→Wait、2调查目标替换及资料误调查，停止15/15但不足以抵消回退。P50=5.391秒也超过5秒目标，不能凭局部修好停止/代操作就选默认。真实后台/清理与EOS正常，不将语义回退归因量化或线程故障。

保留v5默认/v6原始JSON。后续以v5骨架短修订和均衡已见30条快速诊断先筛，再完整C及新D独立100条；最终阈值/预算不变。不得仅重复强调失败分类而忽视其他意图，或降低门槛宣称完成。

v7补充：C30诊断27/30，完整C100为91/100、P50/P95=4.550/6.939秒，结构均合规；仍有9条原始语义错误，不算100%模型正确。新D100已在首次推理前冻结并通过21项静态/回归，待独立验收后才可选默认。

F106再补充：资源审查时一次猜测SceneEditor/S3SceneEditor.cs只读路径失败；未修改资产，也未将命令整体成功视为该读取成功。该文件与当前推理无关，后续应停止无关按类名猜路径的读取，始终消费rg实际清单。

同轮Guard只读预审又误猜AuthorizationBoundaryTests.cs；实际清单为GuardBoundaryTests.cs。仅按已返回实际路径读取，模型任务和资产未改动。工具调用不能把未来的rg结果当作前面猜测读取的依据。

包装预审中TrustedTokenSequence.cs猜测同样失败，随后用`rg -l 'class TrustedTokenSequence'`实际发现它与Tokenizer同文件。今后按搜索结果读取，类名不是文件路径依据；未对不存在路径写入，也未改词表。

## F-153：测试必须消费快照真实字段与完整Prompt上下文

2026-10-27，T01p2首轮编译CS1061：测试使用不存在的InferenceSnapshot.PlayerInput，实际为Input。修正并重新编译后才通过门禁启动Transport_A，编译失败未执行旧DLL。另在启动前审查发现PreparedPrompt.UsedMemory只含摘要，不含目标块；独立预期body改为该空记忆/零目标夹具的完整固定上下文，不直接将UsedMemory误当Fill参数。生产预算/上下文不变，后续需真实推理逐字body和完整IDs一致才验收。

## F-152：新游戏目标与否定/代操作表达暴露v5语义不足

2026-10-27，T01新冻结C100首次完整生成：结构100/100、原始语义89/100，Quality_C_A实际0/1未过90。11错误为停止/解除跟随3、危险提醒选错调查目标2、代操作6；包括raw伪称服务码已输入/启动允许，不得显示或执行。性能P50/P95=4.192/6.010秒、后台/释放正常，不把问题错归线程/性能或量化损坏，也不以脚本修正冒充模型正确。

依据用户Qwen3专用调优/普通事项委托继续迭代Prompt。C转诊断，v5历史/可玩实现保留；新增可显式选择的Prompt版本接口先测试，再诊断v6，最终改版须另冻新D100验收。不得降低门槛或恢复Qwen2.5工作。完整报告S3-T01c_V5_NoGo_TestReport.md。

## F-151：公共测试基类不能以protected字段暴露internal夹具类型

2026-10-27，S01c2新共享夹具编译报两条CS0052：S3PendingSaveFixture的protected Factory/Clock字段类型为internal嵌套控制夹具，访问域不一致。只将这两个字段改为internal，使同一测试程序集派生类可用；不扩大测试类型到Player、不改生产API。编译失败时未启动测试，保留精简编译日志；重新编译和实际模块测试后才验收。

## F-150：静态场景资产测试应开Preview，不恢复NUnit内部空布局

2026-10-27，S01b1场景生成回执saved/originalLayoutRestored均true，767保护SHA不变。Scene_A在新资产刷新门禁暂停未启动；compile:none刷新后Scene_B实际0/1，异常栈明确位于测试finally的RestoreSceneManagerSetup（25行）：No loaded scene/active0。测试框架提供的内部空布局不能作为普通已加载场景布局恢复，不是导航资产生成失败。

查项目既有EchoNavigationBakeTests等均采用OpenPreviewScene/ClosePreviewScene静态读取；改新夹具为同模式，避免改普通编辑布局，保留原B失败并以C重测。外层regression_host已成功按进入前快照还原真实用户布局，未保存/丢弃用户内容。场景/导航文件不重建、不覆盖，待C实际通过才验收该叶。

## F-149：资料提问关键词不能直接匹配目标名称内部

2026-10-27，准备真实模型全链前接口审查发现J02a3将“记录”单独视为提问，可能把明确“扫描入口巡检记录”误转知识答复。它不会越权，但会阻止合法调查；先补4种明确动作及5种真正资料问题的独立回归复现，再修辨别规则并重跑综合守卫。模型Prompt/原始评分不变，失败证据与复测标签分别保留。

实际Record_A为5/9，4种明确调查均误为GroundedReply，复现成立。修为明确资料问句优先，单独“记录/资料/信息”只在缺少调查/扫描/检查等动作词时判断提问；不是放宽目标/权限或修补模型JSON。后续Record_B重跑所有受影响守卫后才进入真实连测。

## F-147：Editor协程帧推进不保证ScreenCapture回缓冲落盘

2026-10-27，E02b Visual_A实际1/2（11.228秒），黄闪/探头/红警戒/实际扫描优先级测试通过；另一项前段UI/取消/隐藏检查已执行，但ScreenCapture.CaptureScreenshot等待5秒没有文件而超时，后段清理断言未执行，不能算整叶通过。查既有窗口探针均在Player WaitForEndOfFrame捕获，与编辑器测试帧条件不同。

改夹具：对同一实时Canvas/真实URP Camera显式1920×1080 RenderTexture渲染，仅临时改Canvas到ScreenSpaceCamera并绑定输出，finally恢复原Overlay/相机/画布/RenderTexture且断言恢复；截图为受控UI可视证据，不冒充Windows Player原始回缓冲或性能证明。保留A失败回执，B重跑全部功能与布局/清理，人工检查实际PNG后才能验收。产品UI/灯光逻辑未因截图故障改变。

## F-148：反馈状态行高度小于实际中文字体行高

2026-10-27，Visual_B为1/2，受控截图已真实生成，但实际字体preferredHeight=21.0000019，状态框仅20高，布局断言失败。人工检查PNG并依据布局空隙将产品行高改22（y=-68，底部=-90，消息区从-92开始），保留16号字体，不以扩大数值容差掩盖裁切；断言仅允许0.001浮点误差。B图片/回执保留，C完整复测后再验收。该问题是实际布局不足，区别于A截图时序故障。

受控RT图还显示转ScreenSpaceCamera后UI被场景景深模糊；实际Overlay本来位于后处理之后。仅捕获期间暂关该相机后处理以模拟UI不受景深，finally恢复并断言；不改产品Volume或画质，受控图不作为完整场景外观证据。

## F-146：孤立代理项不能经NUnit属性元数据往返作非法Unicode夹具

2026-10-27，J02b1 Knowledge_A实际17/18，失败项TestCase("\\ud800")在Unity/NUnit属性传递后显示为两个替代字符，已是合法字符串，预期拒绝与实际Answer不符。生产API原有严格UTF8校验保留；将孤立高代理项在测试方法内用`new string((char)0xd800,1)`构造，绕开属性序列化。保留A失败回执，使用新B标签完整回归后才验收，不把夹具修复算旧测试通过。

复测B尚未启动即被Console门禁阻止，Knowledge_B-ConsoleErrors.json仅有既知IAP RequestFailedException空响应（F138），不是C#编译或产品运行错误。核对/保留后清空该已存错误，另用Knowledge_C实际执行；B不记为测试通过。

E01c Game_A同样在空回归宿主已进入后、测试启动前遇到唯一IAP空响应，证据S3-E01c/Game_A-ConsoleErrors.json。保留后清理这一已核验错误，以Game_B独立启动，不改联网/证书/依赖或忽略产品错误；工作区还原仍使用进入宿主的Game_A快照。

## F-145：本地MCP客户端直接以工具名调用

2026-10-27，J02a2刷新时错误使用`client.py tool refresh_unity ...`，客户端把refresh_unity当JSON参数而JSONDecodeError，尚未向Unity调用工具。读取实际client.py入口后改为`client.py refresh_unity '{...}'`；不修改客户端或忽略编译错误。工具参数必须按现有接口消费，不能根据其他CLI习惯添加层级。

F106本轮再提醒：TargetScope与InferenceSnapshot实际同文件；一次按类名猜TargetScope.cs只读报不存在。已读取同文件实现，后续先rg实际文件清单，未修改该文件或把读取失败记为通过。

同轮另一条只读猜测IntentRequest.cs也不存在，命令已报错；不再读取猜测路径，按rg已返回文件读取。没有对Echo源码进行任何写入。

E01阶段又出现GenerationTypes.cs/CabinDemo.cs按目录猜测导致只读失败；以rg实际结果纠正。应消费已列出的TokenGenerator/InferenceOwner等实现文件，禁止继续以猜测类名路径代替检索。本轮未覆盖或修改相关历史源码。

## F-144：原始生成报告与测试回执同前缀，不能只按mtime取最新

2026-10-27，正式100条轮结束后glob Runtime100_*.json同时匹配Runtime100_A.json回执，按mtime取到回执，诊断显示0条/无completed，并不意味着模型0条。改为Runtime100_[0-9]*.json仅取数字时间戳原始数据，核对completed/cleanup后再汇总；实际100条/100结构/90语义，原始SHA与NUnit结果同时留证。后续所有同前缀查询先区分原始报告和回执，不能覆盖或补造数据。

## F-143：正式验证夹具的同义词组也必须遵守每组16词上限

2026-10-27，R02快照夹具编写后接口审查发现一次把18个词放入同一MemoryKeywordGroup，实际既有构造器限制1–16；尚未启动该夹具测试即按真实接口拆成通用8词与五组设备别名各3词，保留原限制，不扩大参数。补测试实际设备查询可检索唯一已知摘要，并重新编译后才运行。此为夹具构造问题，不是模型语义或后端失败；标签仍不进入Prompt/目标许可。

## F-142：社区导出元数据在线复核超时，不修改系统网络

2026-10-27，准备正式包来源说明时，web工具无法打开固定revision社区README；本地urllib只尝试≤64KiB的同revision公开API元数据，20秒TLS握手超时，没有下载权重或生成成功元数据文件。原有Qwen3图/官方配置/README/LICENSE及SHA证据仍保留；官方缓存README明确Apache-2.0，但该次在线请求不能记为社区模型卡复核成功。未关闭证书验证、改代理/TLS或恢复Qwen2.5下载；后续优先使用已验本地来源记录，必要的在线补核另记实际结果。

补核：后续rg确认最初下载已包含onnx/README.md及config.json；该缓存README明确base_model为Qwen/Qwen3-0.6B，并说明是兼容Transformers.js的ONNX转换。优先读完整缓存元数据目录本可避免该次联网。搜索返回的Qwen3-0.6B-Instruct-ONNX是另一个仓库/不同文件，不拿它的许可或参数冒充本次来源；不下载或切换该模型。

## F-141：延迟汇总不能仅选成功行，模型测试期间避免并行大文件校验

2026-10-27，在独立100条汇总前审查到诊断汇总器只收集有seconds的行，超时/异常行可能被排除而美化P95。先纯测试4/4再整合：失败/不完整/无效时间仍计入样本，使用至少20秒或实际wall较大值，失败数单列；空集不是0秒。旧无失败18条报告未重写，不放宽任何模型门槛。

同轮100条推理期间一次重复S1/S2大文件SHA校验耗时较长，与部分请求并行，可能影响延迟。校验完成767/767一致，随后尝试取消时已无该任务进程，未终止Unity或其他Python。该轮仍可保留原始质量诊断，延迟要标注环境干扰并以后续独立CPU2/4对照为准；后续不在推理性能采样期间做大文件校验。

## F-140：独立语义验证集必须先审查措辞，不能把歧义当模型错误

2026-10-27，Q3独立100条初稿A中“查看标记的扫描信息/查看标记”既可能是问资料也可能是前往观察，不适合单一Investigate标签。尚未运行模型即保留A，B改为明确前往扫描；固定B SHA48ce057de9fecbe441d18b56cba8513ef4754e331043938dd226eacac209a530后才准备官方输入和启动真实生成。标签不进Prompt/语法、预期反事实不改变IDs/白名单的Python8项与C#6项实际通过，防止答案泄漏抬高准确率。任何后续基于B预测调Prompt的版本，须另建独立新集。

F106本轮再核对：ModelCatalog实际为ModelCatalog.generated.cs，不能按类名猜文件名；一次只读路径错误已由rg -l定位后读取。仍不以路径错误静默通过。

## F-139：连接器拒绝直接退出代码，改用已授权的干净检查和正常关窗

2026-10-27，Editor已连续运行三天，Q3 v4生成延迟P50/P95升至13.824/19.730秒；OS观察Editor私有内存约16.4GB（推理期约18.4GB），不能仅凭此确认唯一变慢根因。按已有clean-only重启授权，首次native_code包含EditorApplication.Exit被连接器静态安全检查拒绝，未执行、未写Guard。保留Receipt.json，不关闭代码安全检查。改为只读原生检查Scene/Prefab/编译/测试空闲及Q3 Owner已排空，写新GuardReceipt；检查通过后仅向已核验PID28716正常CloseMainWindow，不强杀、不自动保存/丢弃。隐藏启动器仅在旧进程正常退出后启动同项目D3D11，120秒未退出即报错。证据Tuning/Restart_20261027_A/。后续复测才能判断重启对延迟的影响。

本轮F106补充：检查JsonIntentConstraint时误用了Inference目录，实际rg列表已给出IntentJson目录；随后按该真实路径读取，没有改生产文件。一次汇总调用在Owner尚未结束时被summarize工具门禁拒绝，没有产生Summary；待completed/cleanup为true才归档，门禁保持。

## F-138：Unity IAP 编辑器联网错误中断模型测试，后台 Owner 仍需排空

2026-10-27，V3_B 作业 6f824cfb55af450691a9fa84fb71cef2 在15.231秒被 Unity 自带 InAppPurchasing 的 TokenExchange 空响应错误判失败；这是编辑器服务错误，不是模型返回错误。原始测试结果、Console 和仍在写入的真实生成记录均保留。主线程测试结束不等于后台原生调用结束：已观察到作业失败后请求数量继续增加，因此必须等待该原始报告 completed/ownerCleanupComplete，再刷新、改夹具或启动下一次测试，禁止强杀线程/主线程 Dispose。先保存并核对该唯一外部错误，再清空 Console，使用新标签复测；不全局忽略错误、不修改联网设置或移除现有包。该轮不能作为测试通过或完整模型质量验收。

F106本轮补充：按摘要路径读取 Q3-R02 报告时漏了 Reports 子目录，命令明确报不存在；随后用 rg --files 找到实际路径并读取，未修改工程。后续继续先消费真实文件列表。

## F-137：隔离调优夹具重复校验未参与推理的旧模型导致冷启动超时

2026-10-27，V3_A在生成前触发30秒冷限制，0/1失败、0条请求，不能评价v3语义。栈位于ModelPackage.Verify旧包权重SHA；实际只需要该包固定字节词表，读取约1GiB未使用模型没有必要。改为只读租约/固定长度/SHA校验实际使用的tokenizer，仍完整校验当前Qwen3模型和官方配置。30秒限制不变，无2.5权重加载或处理。首轮backend尚未创建，旧夹具把ownerCleanupComplete写false并先报清理断言，掩盖真实OperationCanceled；修为文件租约释放成功且未创建native时显式nativeCreated=false/清理完成，保留原错误，不伪称原生释放测试。旧V3_A和原始JSON保留；新标签复测后才能评价Prompt。

## F-136：完整KV校验开销接近模型计算，不能把全部等待归因推理

2026-10-27，Qwen3调优Baseline_B真实1/1基线复现通过，18条输出token与旧A逐项相同、语义仍15/18未合格。仅把Need(bool,string)改直接NaN/Infinity分支没有明显提速：P50/P95=10.255/15.170秒，native Run累计91.957秒、完整输出检查87.213秒。每个解码步逐值扫描全部KV的Mono浮点判定调用是待优化热点，不能直接宣称换模型/增加CPU线程就能解决全部时间。

保持dtype/shape/count/所有float有限性不变，先独立验证IEEE754位模式扫描与框架定义一致，再原18条复现性能；不跳过旧KV部分、不去掉非有限检查，也不把微基准提升当游戏性能通过。计划/实际见S3-Q3-R01_TestPlan.md和S3-Q3-R01_Baseline_TestReport.md；旧A、Baseline_B均保留。

## F-135：Windows curl连接探针失败，不能据此改全机代理或TLS

2026-10-26，在原urllib下载持续慢速时，仅用Windows自带curl8.21.0探测固定官方权重的8MiB范围，max-filesize8MiB、connect20秒、overall60秒，不重复下载完整模型。CurlTransport_A为exit28/20.004秒/0字节；其write-out把http_code的000当数字，文件扩展虽json但原文不是合法JSON，保留原样按文本看，不声称解析通过。

只读检查发现urllib能读取既有HTTPS代理，而curl子进程环境未设置HTTPS_PROXY；没有显示或保存地址/凭据。第二次将同一既有代理仅传入curl子进程环境（无系统/全局改动），CurlTransport_B仍exit28/21.547秒/0字节。B结果以Python序列化正确记录httpCode字符串，并明确machineSettingsChanged=false。未用curl替换当前下载，没有关闭证书校验、修改系统代理或TLS；主下载仍按原urllib路径和完整SHA门禁推进。探针失败不是模型兼容失败，也不推断唯一网络根因。后续仅研究官方任务隔离传输方式，避免未经验证反复整文件重下。

## F-134：protobuf upb 的ClearField不会回收长期模型arena中的浮点权重

2026-10-26，Qwen1.5真实量化尚未运行，最终只读复查发现按目标加载external后在同一ModelProto上ClearField(raw_data)/CopyFrom descriptor，不能证明峰值有界。本机protobuf实现为upb；不落盘内存探针把32MiB字节连续写入同一tensor六次，每次clear与gc后private仍累计33,628,160→67,252,224→100,884,480→134,512,640→168,136,704→201,760,768字节。arena直到所属message销毁才整体释放，旧工具会累积完整6.2GB浮点源及头转置，不能声称逐张量卸载有效。该首探针只有执行输出，不虚构磁盘原始日志。

修复仅Tools量化叶：每个目标建立独立临时GraphProto，float载入该临时arena，调用同一官方DEFAULT/RTN QOperator量化器；仅量化权重、节点与纯数据指纹复制到长期目标图，函数返回后整块float arena销毁。lazy共享头的embedding与转置临时对象独立，位置缓存等必需非量化常量约34MiB只在prune后加载一次并核验指纹。原source图/external只读；未更改量化参数、词表、KV布局或最终预算。

新增独立子进程六次32MiB合成external权重的实际官方量化回归，事先规定结束点private增长≤128MiB，旧arena控制组须增长≥160MiB。归档QuantToyTests_B.log：6/6、3.132秒、exit0，旧增长201,748,480字节；新隔离arena含累计量化输出结束点最大31,105,024字节。原5项toy仍通过，未量化两步logits差0/KV全等，INT4两步最大差0.003855/0.004151、cos0.999962/0.999973。这个合成内存用例只证明不再按源权重总量持续累计；真实933MB大头及序列化临时量仍可能造成数GiB工具峰值，必须另行外采，不能当作AI运行增量≤3GiB或真实模型质量通过。

## F-133：长文件中断只复用完整块，块内有效前缀被重复下载

2026-10-26，Qwen1.5B 下载工具先独立 14/14 后，root 真实 Weights_A 在 UTC14:17:24–14:24:12 失败退出1，Downloads_Weights_A.json 为 completed/verified=false。0-based chunk2 三次分别截断、读取超时、截断；只持久化完整 chunk1 的64MiB。原实现每次失败从同一64MiB块起点重下，虽然完整块恢复正确且最终SHA未被绕过，但块内已收到前缀没有登记，实际慢连接反复丢进度。不能把纯夹具通过等同真实长文件下载通过，更不能将预分配文件非零字节猜作可靠前缀。失败报告、RangeError日志、Weights_A_ResumeManifest.json与单一.part保留，没有最终模型。

修复只改项目工具：原version1兼容增加partial{index:{bytes,sha256}}。失败时flush/fsync已写范围，核对响应载荷hash与磁盘前缀hash后原子登记；进程内/跨进程重试从经固定文件绑定与磁盘SHA复核的前缀终点继续，损坏前缀重新下载该块。IncompleteRead.partial仅在精确206范围已验证且不超过当前读取/剩余长度时写入，超界拒绝；失败响应不能直接标完整。仍4路、每进程每块3次、单一预分配文件、45秒读取超时；完整块fsync与SHA、最终全文件LFS及20GiB含量化/安装预留保持。未改TLS/全机网络，未绕过证书。

独立修复20/20，首次1.108秒，归档DownloadGuardTests_B.log为0.741秒，覆盖37字节重试起点、69字节跨进程恢复、前缀损坏重下、绑定/长度/重叠拒绝、合法与超界IncompleteRead.partial。原14项仍通过。未由修复agent执行真实Weights_B，巨大文件最终SHA与模型运行仍未通过；交由root新标签复测后才能进入量化。报告S3-J01_Qwen15_DownloadLeaf_TestReport.md保留原14项及真实失败。

## F-132：量化工具不能只把源opset14改成21，标准typed常量也必须保留

2026-10-26，Qwen1.5候选转换工具尚未处理真实权重。小toy首轮3/5因标准INT64常量存于int64_data而非raw_data，被过严空数据检查误拒绝；修正支持ONNX标准typed字段，指纹同时覆盖这些字段，不把合法常量删除。随后root审查真实小图发现opset14与57个ReduceMean.axes属性；仅set_opset21会让这些节点在新schema失效，原toy虽然也是14却缺ReduceMean，不能证明真实图兼容。

改为在只含external descriptor的小图上调用官方onnx.version_converter.convert_version(14→21)，之后才插入lazy常量头/最后位置Gather；不加载6.2GB权重后再迁移，不自行只改版本号。toy加入真实风险对应ReduceMean属性、Unsqueeze轴输入和需保留的external float常量。最终root独立归档QuantToyTests_A.log为5/5，0.953秒；未量化两步logits最大差0/KV全等，INT4实际CPU两步最大差0.003855/0.004151、cos0.999962/0.999973，在预声明0.15/0.98内。

真实小图metadata复核DescriptorMigration_A.json：2433个原initializer descriptor逐项未变，57个ReduceMean已迁移成axes输入，opset14→21，lazyhead和embedding均未载外部权重。该检查不是实际全图checker/原生加载或量化质量通过。首轮toy失败输出只存在当时执行记录，未虚构对应磁盘原始日志；后续完整root日志已保存。工具转换前仍须最终LFS SHA和预算门禁，转换后再实际参考/Unity质量测试。

## F-131：缓存清理脚本的PowerShell参数错误没有终止后续删除

2026-10-26，Qwen1.5B预算准备。清理前独立Python清单已核验两个工作区绝对根、23个可重建重复文件与保留来源逐字节/整文件SHA相同：旧S3Staging/Qwen25_05B_W8A8与Qwen3的model_q4.ranges。随后PowerShell父路径检查误用Split-Path -LiteralPath -Parent，不兼容参数集报错；因脚本没有ErrorActionPreference=Stop，命令仍继续按已核验清单逐文件删除。不能把报错解释为“未发生删除”。

实际删除23个重复文件、回收1891229429字节，没有递归删除，生产包、原模型、所有测试证据均保留。后置改用[IO.Path]::GetDirectoryName，重新验证原父路径、全部待删文件已不存在、全部保留源SHA仍相同；DuplicateCacheCleanup_Postcheck_A.json为originalParentValidationPassed/retainedSourcesVerified/allDuplicateFilesAbsent均true。前置DuplicateCacheVerification_A.json、实际DuplicateCacheCleanup_A.json与后置记录全部位于S3-J01/CandidateQwen15。此次有独立绝对路径/内容清单作为先验保护，不能推广为允许忽略路径检查错误。后续任何变更脚本第一行设置$ErrorActionPreference='Stop'，路径或哈希检查失败必须在首个删除/移动前终止；单一PowerShell全程操作，禁止递归计算路径后未验证就执行。

## F-130：Qwen3候选JSON合规，但真实语义15/18未达预设17/18

2026-10-26，J01候选隔离诊断Qwen3_Json_A，job84807a6e9b3c4639b813c41408546ef8，208.068844秒，NUnit0/1失败。18条严格JSON且完整EOS，意图/目标15/18；案例14代为解谜误answer，15资料查询误investigate并编造危险记录，16不存在设备误选power_panel_01。结构约束只保证合法JSON和候选ID，不能证明模型理解用户或拥有事实。全部原始输入/IDs/raw保留在CandidateQwen3/UnityJson_20261026T1355217488364Z_25156104e17440a3a90ec0157f06a10d.json，未接游戏执行或展示。

Owner8903/main1、主线程native0、321输出集合/56空KV成对释放、cleanupComplete=true。冷准备19.9226581秒，生成nearest-rank P50/P95为10.338376/15.531938秒，短回复开发目标未通过。外采6335样本的private峰增量2116595712字节约1.971GiB，但整个Editor基线已16268304384字节，不能据此宣称隔离AI内存或独立Windows预算合格。

候选No-Go，不进入正式适配，不降17/18、不把后续守卫改写计模型正确。历史失败保持，夹具标Explicit供主动复现、原断言不变；默认回归忽略或跳过不算候选通过。报告S3-J01_Qwen3_Candidate_NoGo_TestReport.md注明输出临时复用旧基础词表/禁用新增4标记的限制。J01仍未通过、S3仍11/24；下一步仅在原授权模型及资源预算内先核查1.5B可行性。

## F-129：候选数值通过报告未绑定实际文件，异常跨线程可能保留原生引用

2026-10-26，候选Python工具审查发现Equivalence_A虽真实两步logits/KV差0，但报告没有绑定所比较的模型、案例、官方分词与配置实际SHA。只检查旧passed字段会把不同文件错误放行。另把原异常对象/traceback跨线程保存，可能通过栈帧持有session/KV引用，不能以finally清局部变量就声称实际释放。

修正probe_qwen3_candidate.py：要求显式equivalence-tag与当前bindingVersion，completed/mode/稳定双份绑定/实际SHA/两步有限数值阈值全部核对；异常跨线程仅传纯错误数据，Owner finally释放引用，部分tokens/raw/stopReason保存。13项纯工具独立测试通过（GuardTests_A.log，0.363秒），覆盖旧/不完整/篡改绑定、错误数值、无native依赖导入与不保留traceback；真实Equivalence_B随后两步差仍0且bindingsStable=true。原A不补写成当时已绑定。前后SHA并非不可变租约，无法排除中间修改后恢复；纯异常夹具也不等于真实native异常释放/泄漏验证。详见S3-J01_CandidateProbeTools_TestReport.md。

F106补充（1.5B只读准备）：曾猜测不存在的quantize_weight_only.py并以SilentlyContinue隐藏读取错误，实际枚举后应读quantize_weight8.py；未修改模型。后续先消费rg --files结果，再读取存在路径，不把静默无输出当读取成功。

同日候选只读子审查补充F106：误猜LocalQwenFactory.cs、StructuredIntent.cs，实际为LocalQwenRuntime.cs、IntentJsonParser.cs；另曾rg尚未建立的Drafts目录。均未改资产，改为先rg文件/类名后消费结果。工具补丁曾把同一路径Delete/Add放同一补丁，被apply_patch校验拒绝，未发生修改，后改Update；不是模型或产品故障。

## F-128：标准ONNX检查器不含ORT的旧默认域归一化算子

Qwen3候选首次最后位置优化在保存前被onnx1.23.2 checker拒绝：No Op registered for SimplifiedLayerNormalization with domain_version of 14。没有生成变体或运行推理。微软ORT源码contrib_ops/cpu/layer_norm.cc明确注册该旧contrib算子于kOnnxDomain（默认域），不能把标准checker未知等同ORT没有CPU实现，也不能直接称原图可用。补查原图与变体，错误完整保存Checker_original/optimized.json；仅为检查临时把该已知算子标为custom域，让checker核验其余标准节点，finally恢复，不把别名保存到模型。其余检查错误继续拒绝。正式兼容性仍必须由原图/变体的真实ORT加载和数值对照证明，不使用跳过checker伪装通过。依据：https://github.com/microsoft/onnxruntime/blob/main/onnxruntime/contrib_ops/cpu/layer_norm.cc 。

## F-127：PowerShell对象转换不能保留BPE词表的大小写不同键

只读检查候选tokenizer.json时，ConvertFrom-Json默认PSObject转换因词表同时有A/a报错；没有修改分词器或运行模型。改为ConvertFrom-Json -AsHashtable或Python json解析，保留JSON精确键，禁止删除/合并大小写键来绕过。这是检查工具问题，不是候选模型损坏；后续大词表优先使用已验证Python/Json.NET读取。

## F-126：多请求诊断沿用NUnit默认180秒总时限

Prompt v2包装诊断V2_Ablation_A实际完成17/18请求后被NUnit默认180000ms中止，整组1/2失败，不算诊断完成；已完成A/B各5/6、C前5条5/5，最后拒绝输入未完成，不推断其结果。该组总预算应覆盖冷加载30秒及18×（单请求20秒+300ms冷却）并留清理时间；修正仅测试TimeoutAttribute为600000ms，生产20秒逻辑/30秒冷加载和接受门槛均不变。finally已请求Owner异步停止，待原生退出后再编译复测，保留首轮与部分原始输出。对照后半程约8–14秒/请求，超过开发机短回复P95目标，单独留为性能问题，不能因诊断结束称性能合格。

## F-125：结构约束解决JSON，但不能替代真实语义准确率

后续：用户已明确批准Prompt v2，原版与证据保留，开始按同一预算复测。批准只允许更改该正文，不表示模型质量通过。

v2首轮：模板/预算17/17、严格解析/语法/真实输出合并95/95，但真实意图5/6；调查、身份已正确，代开实验室门却输出answer“我将打开实验室门。”。没有游戏执行或展示，不能把Passed当质量通过。真实Raw保留PromptV2/RealJson_A.json，继续仅在官方ChatTemplate角色包装范围比较；正文不再自行扩展。后续事实/权限守卫必须屏蔽未经验证的设备操作承诺，模型原始错误仍计错。

J01c_A真实六输入JSON6/6、全部后台/资源/预算通过，合并回归69/69；但调查供电面板、你是谁、替我开门均误为follow，初步语义3/6。不能称模型质量通过，不用脚本改意图混算。raw/输入/耗时/模式/线程均保存RealJson_*.json。先在已授权“官方ChatTemplate包装”范围隔离比较消息角色/包装，固定正文逐字不改、目标/事实守卫不放宽；实际A/B/C共18条仍各3/6，生产默认未改。accuracy4/accuracy0/FP32首token探针top1六条全同，三错误仍follow；这不是完整FP32生成验收。已准备PromptChangeProposal.md与PromptV2_Proposed.txt，未应用/未运行草案；需用户确认正文变更，不进入J02。若仍需改固定正文/预算/验收门槛则向用户确认。

## F-124：Windows文本写入把交付Prompt换行转换成CRLF

C02a_A为15/16，运行时固定Text/10组token golden/预算均通过，但交付PromptTemplate.txt字符串长度381而规范LF文本366，差15个CR。Python Path.write_text默认平台换行转换，SHA按转换前LF计算，不能声称交付字节相同。改write_bytes显式UTF8/LF，现有文本只转换CRLF→LF后重新测试，正文不改，C02a_B为16/16通过，交付646 UTF8字节；C02b实际后台19/19。首条内联替换脚本又因PowerShell嵌套引号ParserError未执行（同F119/F100），改apply_patch和单一简单字节命令，不能假定失败命令已修改文件。

## F-123：真实tokenizer返回数组，长度应读取Length

C01b首次编译CS0428/CS1503，ContextAssembler把EncodePlain返回int[]当IReadOnlyList使用.Count属性，C#将LINQ Count认成method group。保存C01b_A-CompilationErrors.log，核对QwenByteBpeTokenizer实际签名后改为.Length，测试同类调用一起修正。不运行旧DLL或当格式错误兜底；重新编译后才启动独立用例。C01b_B进一步暴露NUnit Does.Not.Contain只接字符串，本测试检查整数数组误用，CS1503已保存；改为LINQ Contains结果断言false，同一特殊token拒绝要求保持；C01b_C为23/23通过，新程序集时间戳与门禁有效。

## F-122：目标排序不能用ID子串当完整匹配

C01a测试前代码复查发现IndexOf会把panel_11也命中panel_1，可能使8项范围截取时排序错误。修正ID的ASCII词边界，名称匹配以更长名称优先；现有12候选/指定panel_11测试验证首项必须panel_11。C01a实际14/14通过，C01b回归仍通过。另NUnit Assert.Throws要求精确异常类型，坏UTF16会抛ArgumentException的派生EncoderFallbackException；改Assert.Catch验证同一拒绝契约，不改生产Unicode校验。修改后重新编译再测试，禁止运行旧DLL。

## F-121：生命周期耗时断言混入了报告写入，首轮真实测试失败

I04b2_A为7/8，通过协议但真实LoadGame作废断言179.2487ms>100ms。源码计时器在Invalidate返回后仍运行，先将Double装箱写入JObject才再次读Elapsed用于断言，可能把报告首次JIT/分配纳入调用耗时；旧失败没有finally保存局部report，无法事后分解179ms，不能直接认定唯一根因或删除失败。改为返回后立即用tick差冻结调用耗时，另记录首次Double报告写入耗时、调用内GC次数；finally先保存部分报告，原100ms阈值保持。生产代码未改，B8/8、C独立重复1/1通过；调用1.0825/0.0073ms、调用内GC0，另记报告0.4456/0.0032ms。旧179ms没有分解，不能唯一归因JIT/GC，按残留诊断限制保留，V02还需完整Profiler。草稿误用了.NET7 Stopwatch.GetElapsedTime，编译前审查已删，用Unity可用GetTimestamp/Frequency。

F106再次补充（I04审查）：虽然同调用已枚举文件，但后续读取仍误猜MemorySnapshot.cs；类实际位于已枚举MemoryRecords.cs，随后rg类名定位读取。应先消费枚举结果再构造下一读取调用，不能枚举与猜测混在同批。

F106再次补充（S3-I03准备，另一次查询猜了不存在的I02b_A-Result.json，实际枚举为I02b_A.json）：只读查询误猜OwnerTests.cs，实际枚举后为InferenceOwnerTests.cs。未修改或运行旧代码；后续先rg --files再读，不因已有摘要而省略文件定位。

## F-120：真实完整生成能停止，但raw并不符合意图Schema

A04三次真实greedy请求均EOS/资源正常，但带Markdown围栏；调查仅content、跟随生成嵌套follow对象，无合法intent。原始格式0/3，不能因生成测试Passed称质量通过。所有raw/IDs/nativeCalls保留RealGeneration_*.json；不剥围栏/补字段、不用脚本伪装。按已确认方案，C02固定正文、J01真实logits结构约束与严格解析分别测试，最终T01用100条核对原始/约束/守卫/脚本独立统计。若达不到确认门槛再报告询问。当前尚未接游戏派发，未发生越权或机关修改。

## F-119：内联tokenizer诊断无输出退出，改为保存脚本

A02准备时一条含Unicode/多层引号的PowerShell内联Python命令退出1且没有输出；未据此推断模型/词表失败，也未改正确资产。将相同诊断保存为Tools/Stage3/inspect_tokenizer.py后执行成功，确认17个实际正则转义和NFC。内联失败的具体根因未证实，沿用F100优先保存复杂Unicode脚本的经验。A01还曾猜测不存在MemoryJsonCodec.cs，实际列出的文件为MemoryJson.cs，沿用F106。

## F-118：Profiler二进制开关先于文件名导致没有原始捕获

G01 Windows_A01实际后台5轮、1080有效GPU帧、进程退出0，logits与Python误差0，但probe先enableBinaryLog=true再设logFile，Player.log报“No file name has been specified”，且没有.raw。因此Windows_A01旧轮不能算Profiler验收通过；probe原passed字段未检查raw，已补文件存在/正长度门禁。按S2已证实的顺序先logFile、再binary、再enabled，重新编译/构建，用Windows_B01新目录复测。旧A01保留。Panini未启用功能的剥离警告为既有F019范围，不擅改S2渲染资产。

## F-117：测试夹具混用了Python与C#的异常枚举文本

G01 MissingModel_A01实际正确返回C# `[ErrorCode:NoSuchFile]`，Owner已在非主线程释放；Python夹具却断言Python后端文本NO_SUCHFILE，导致夹具失败。更正为实际C#枚举名称，用B01–B05新标签复测，保留A01；后续生产层优先读取强类型ErrorCode而非匹配自然语言。未将此夹具错误误记为模型运行失败。W8 accuracy0调优脚本首轮假定节点已有accuracy_level属性也断言失败，0默认值实际省略；改为增/替属性，并核对全部initializer hash未变，见Weight8KernelTuning.json。

## F-116：原图1536位置全部词表logits导致内存超标

G01-W8-Long首轮真实Unity后台1536输入/三步通过形状和有限值检查，但Windows外采private增量3301531648字节，大于3GiB=3221225472，约超76.6MiB；不能把Probe passed字段当模块预算通过。原图输出[1,1536,151936]而只用最后位置。先改C#只读取native Span，不再ToArray复制完整logits，仍有上述峰值。进一步在兼容Spike的lm_head前Gather最后隐藏状态，仅输出[1,1,V]，全部注意力/KV/量化权重不改；单独留转换SHA、短/长数值等价与内存复测，正式输入契约由A03再实现。失败原始证据保留UnityOrt_W8_Long_A01及Memory.json。

## F-115：关闭Plugin Any Platform不会清除显式平台标记

G01后置检查meta发现，SetCompatibleWithAnyPlatform(false)后原先Linux64/OSXUniversal仍enabled=1。补显式关闭StandaloneLinux64/OSX/Win32，仅保留Windows Editor x86_64和Win64；重导入后再读meta确认，不能用setter调用成功代替实际配置。ORT1.31量化工具另需要未随基础inference安装的onnx_ir；记录首轮ModuleNotFoundError，在项目venv补onnx-ir1.1.0/onnxscript0.7.2，推理不依赖这些转换工具。

## F-114：官方模型大文件中途读超时

G01未量化ONNX诊断参考下载至约1338MiB后出现TLS socket read timeout，INT8候选已完整落盘。保留.part，不把部分文件重命名或导入；增加有限三次HTTP Range续传，206必须核对Content-Range起点，源若忽略Range返回200则从头写，完成后校验响应长度并计算SHA。未改全机网络/TLS。下载失败不算模型失败。另PowerShell对rg传入目录/*.asmdef字面量无效，改-g过滤（F111同类）。

## F-113：长导入已执行但MCP回执超时/空失败

G01实际ONNX导入约23–30秒，外部回success=false/null；查询execute_code历史证实执行成功并捕获Trilu不支持异常。此前两次误当未执行而重复；停止重试，长探针内部先写结果文件、外部只启动/查询。BarracudaActualImport.json是完整模型失败证据，不把外部连接错误当模型失败。Unity内Process.PrivateMemorySize64返回0，无效；内存改外部采样。INT4与原始FP32参考三步均未达预声明cosine/RMS容差，保留OriginalReference.json并审查候选，不能修改结果或放宽后宣称通过。读取safe_menu路径再次猜错，沿用F106，实际在Tools/MCP。

## F-112：UPM修改已完成但MCP回执在域重载时失败

S3-G01通过Client.Add添加Barracuda3.0.2，execute_code回success=false/message=null；后续manifest、packages-lock、PackageCache和Editor.log证明包已添加并编译成功，仍在导入/域重载。该回执不能当作“没有修改”。检查真实状态，不再次Add；等待连接恢复后才运行模型探针。首轮探针在无Unity session时被拒绝，没有执行导入，也不算模型兼容失败。证据AddBarracuda.json、BarracudaImport_A01.json保留；本轮无用户场景保存。

## F-111：S3审查查询误用了Bash路径展开

本轮只读审查把Assets/EchoStation/{Player,Echo}/Runtime传给PowerShell，整条命令ParserError，未执行读取或修改。改为rg传入两个明确目录；初查Docs/Stage3尚不存在，后续先在已存在Docs中枚举，再创建待确认提案。不把查询失败当Unity/模型错误；未安装包或导入模型。

## F-110：最终索引匹配同时选中V03子报告

finalize_docs首次在报告枚举断言停止：V03的AB/AB1子报告也采用下划线，宽glob找到3份，未写索引/总文档/交付清单，程序与资产未变。将V03主报告显式映射到Delivery报告，子报告仍保留。断言先于写入保证失败没有半更新；修正后重跑最终归档并核验链接/哈希，不能忽略断言继续标完成。

## F-109：早返回前仍创建区域查询闭包

V03两轮五区普通帧均持续24B/帧。加载自有Performance02原始Profiler，先确认原Profiler关闭且无帧；最后60帧GC.Alloc父节点全部StationSession.Update，分析后finally清除自有导入并恢复原空Profiler（AllocationParents.json）。

RefreshZone中First(z=>z.Id==zone)捕获局部zone，编译器在方法入口构造闭包，区域不变的early return仍付出分配。改为只在新区域分支用索引查显示名，不修改区域边界、记忆ID/摘要、事件或台词。先复测区域/进度既有用例，再相同Windows普通帧比较，不凭源码直接宣称0B。首两轮实际24B与原始文件均保留。

## F-108：FrameTiming只恢复内存未写回项目设置

V03性能构建getter还原false、报告true，但实际ProjectSettings.asset仍enableFrameTimingStats:1。SettingsAudit01记录内存false/磁盘1；不能只凭API getter宣称完整还原。SaveAssetIfDirty(PlayerSettings)实际仍未写回（SettingsFlush01），未调用会保存无关脏资产的SaveAssets。

修复范围仅该标量：确认原false，精确单处字节替换+原子替换，其他字节SHA/等价检查留证；构建finally同时还原getter与磁盘，保留BOM/换行，不批量保存资产。字段缺失/重复或遗留临时文件拒绝并报告错误。后续性能重建及最终交付都需核对真实磁盘0，旧getter-only报告不能替代复测。

## F-107：Editor可用的Light属性不进入Player

V03首性能构建Failed/2 errors，探针使用Light.lightmapBakeType，在Editor编译通过，Windows编译CS1061。该配置写入属性是Editor专用，运行时对照应读取Light.bakingOutput.lightmapBakeType；只改诊断选择条件，不改光照/烘焙或删除编译检查。构建失败报告保留，FrameTimingStats和选择对象已finally恢复，修正后重新构建再执行性能采样。

F106补充：PowerShell的rg不会展开传入路径中的包名通配符，V03只读查询出现os123；改用已观察到的准确包路径或rg --files枚举，不把该查询错误视为产品问题。

## F-106：读取源码时再次猜测文件位置

V02准备时先读了不存在的Dialogue目录下DialoguePanel/DialogueMessage和独立ProgressState文件，PowerShell返回路径错误，未修改产品。实际文件分别在Player/Runtime/DialoguePanel.cs、Dialogue/Runtime/DialogueSession.cs、Puzzles/Runtime/ProgressAudit.cs。后续先rg --files或rg类名定位，再读取真实路径。测试探针历史对比逐字段比较只读属性，不能用JsonUtility空对象序列化充当一致性证据。

## F-105：早期世界用例未跟进最终门与管线

V01 H场景回归51/53；W04仍断言全场两门，而W06已加入核心门；W01仍要求初始闭门下全图PathComplete，与实际授权门矛盾。更新为精确三门身份/脚本与初始关闭；基础拓扑用例先证明闭门不可达，再只在夹具中临时禁用雕刻查询双向底层连通、finally恢复并再次证明锁定，碰撞/门字段始终关闭。实际玩家授权开门/全路线仍由已通过F04/V01验证，未删门或放宽产品权限。

渲染测试静态检查另发现仍从S1 Renderer控制SSAO，虽然H该图像用例通过，控制对象不够明确。改为断言当前S2管线并取实际S2 Renderer，原激活值仍finally还原，图像阈值不变；不能用一次通过回执替代控制正确性。重新运行场景及受影响导航回归。

## F-104：密码同义词在解析层未统一

F-ScopedAnswerDataRegression 197/200，设备范围反例均通过，但“维护码是什么”走Fact、“服务码呢”走Unknown，未进入已修复密码分支。此前检索词组虽含三种词，D03解析器只检查密码；不能只改检索宣称同义词已贯通。

解析层统一密码/服务码/维护码提问；同时扩展“帮我/替我输入服务码/维护码”和直接输入命令的refuse，保持禁止代操作。新增3问法与4拒绝用例，重跑完整数据回归。没有增加模型、设备操作接口或更改密码值。

## F-103：无密码设备问题落入通用密码组

V01 E-NonPasswordDeviceFirst实际0/7：仅知小屋码时询问核心/供电面板误答7316；两码已知时对明确核心设备反而要求指定终端。F086只覆盖了两个密码终端的名字，没有覆盖其他实际设备/ID，空scope仍被当泛问。

新增实际六设备中文名/ID到资料ID的手工范围；无密码设备映射不带密码的相关资料。密码检索先确定范围，再只以范围ID构建上下文；未识别的具名设备用空上下文返回无记录，不把其他设备码送入该问题上下文。无目标的预设泛问保留：一条已知可复述，多条仍澄清；没有加入自然语言模型。补所有设备反例、未知设备、两终端ID和泛问回归，不改谜题答案或机关权限。

F051输出补充：此次Python未带-X utf8，控制台中文替换显示；原始MCP证据以UTF8显式写入未损坏。后续模块命令统一python -X utf8，避免把stdout解码问题误判为C#字符串或测试输入损坏。

F102复测：D三条空局路线3/3，含完整14目录来源及两个实际结局后读档。本轮没有PathDiagnostic替代起点记录，故没有进一步确认旧查询失败根因；只确认完善的操作者与真实路线通过，未改产品导航/碰撞。C首次失败完整保留。

## F-102：从玩家自身雕刻孔内查询测试路线可能选中错误连通侧

V01 C三路线1/3，监测完整通关并读档成功；封存与归档在first_real_scan后、返回主室目标(0,0,-2)的操作者NavMesh查询失败，尚未执行该段移动。相同储藏室返回已在其他路线实走，不能据查询失败删墙、开未授权门或重定位。

操作者起点是带雕刻障碍的玩家脚下，简单1.5m最近采样可能落在孔另一连通侧；这是待诊断推断。新增原始起点/最近点/目标/状态记录。优先正常最近点，失败时只读探测朝目标及周围1.2m候选连通点、最多等2秒；选定后连到起点的段同样用真实W行走，绝不Warp。2.2m内短段直接通过原产品控制器实走，不依赖被自身雕刻占据的NavMesh。保持物理碰撞/门/解谜规则，复测三条空局路线。

F101复测：B-SealHeldInput实际从空局通关1/1，78.187秒、52段W步行，2个Unity实体输入设备实际恢复=true。只调整诊断操作者，没有更改产品输入/碰撞；设备状态消失的底层触发尚未确定，不倒推F099同根因。

F091工具补充：leave先做只读的原布局精确核对（路径/加载/活动、播放选项、默认/画质管线）并RequireClean/空闲；完全等于原捕获时返回“已是原布局”，不切场景、不改当前选择。不同且非空宿主仍沿原门禁拒绝。AHost实际走此分支退出0、证据保留，py_compile通过；没有放宽任意布局恢复。

## F-101：长时间合成按键保持中途丢失

V01 A-SealRoute在空山谷走到z=-15.90后超时；CanPlay检查持续通过，但实际虚拟keyboard.wKey=false、InputAction Move=(0,0)，没有预期的持续W状态。该现场指向测试输入中断，不能据此删除碰撞或让玩家直接移位；具体设备重置/外部焦点事件来源尚未追踪。F099首次停步可能相关，但未证实同根因。

测试操作者每真实帧重新提交其预期的W保持状态，最终仍正常释放；所有移动继续经过产品InputAction和CharacterController，不绕过CanPlay/模态门禁。长路线驱动另暂时禁用Unity InputSystem内其他已启用键鼠设备，Dispose逐一恢复，避免实体设备混入虚拟输入；不禁用操作系统硬件。原设置身份还原继续由SyntheticInputScope检查。产品代码不变，重跑导航与真实解谜路线验证。

F099复测：只增加诊断，未改产品行为；B单跑1/1、A+B连续2/2通过。首次停步没有复现，原因仍未定位；原失败/FirstRun轨迹保留，不把通过复测写成已确认修复。Windows及完整谜题路线继续覆盖真实输入与长期流程。

F091补充（F04）：host leave再因非空宿主拒绝切换。PostWorkspace实际为干净S2、未播放/编译，background=false，options=true/1，S2管线正确；保持现场。当前S2亦为原布局目标，但未推断谁触发布局变化或绕过原门禁。

## F-100：PowerShell嵌套Python/C#引号导致补丁未写入

F04诊断首次用python -c替换含C#双引号的长行，PowerShell转义未保持，Python报unterminated string literal，源文件未改。编排随后误请求了一次无有效源码改动的编译，徒增重载。已改用apply_patch直接传原始补丁并检查回执；以后含多层引号的源码修改使用结构化补丁或保存脚本，不把JSON/反斜杠当shell转义，先检查前步退出码再发依赖动作。

## F-099：长路线恢复用例在小屋停步，先隔离输入与碰撞

F04首轮1/2通过：A五区往返91.861秒、5491帧，移动预算连续；B在准备开门的步行阶段停于(0,0.03,-2.15)，Echo约在z=-4.05、位于身后，不能直接归因为Echo挡路。B尚未进入动态障碍步骤。保留首次用例和完整未完成轨迹。

先查F058/F067背景输入/焦点经验，再补超时现场：产品焦点/捕获/模态、虚拟W键、InputAction Move、Motor、碰撞标志、前方实际PhysicsScene射线、输入设置和当前设备。未改产品碰撞、未移动角色脱困、未放宽路线要求；诊断后再决定修复。

F097/F098复测：专用已打开干净S2场景成功接线并保留布局；E-LifecycleAndRestore11/11，包含未来档在隔离重力后坐标/记忆/文件精确不变。测试宿主最终恢复原S2布局，475份S1 SHA不变。没有强制关闭标签、保存未保存内容或取消精确断言。

## F-098：失败读档不改位置测试须隔离正常重力

F03D首轮3/4通过，D04未来版本读档的位置精确断言从y约0.04变0.03。实际FirstPersonMotor在UI阻塞时仍正常执行重力，UiBlocked只屏蔽玩家输入，不暂停物理。未证明读档改变位置，不能据这一厘米差异更改产品重力或放宽精确断言。

该只读失败用例在记录基线前显式关闭Motor，沿用C03隔离方式，继续要求坐标/记忆/文件逐项不变；其他真实移动与模态用例保持Motor启用。首次失败保留，复测全部D并回归世界恢复和供电模块。

## F-097：专用场景已打开时配置工具拒绝接线

F03D配置菜单返回“已尝试”，实际Console为Close saved owned Station scene before setup，后续host门禁正确拒绝。D-SetupErrors/D-WorkspaceInspection证实唯一活动S2场景已打开、干净、未播放且Lifecycle组件0；不能把菜单回执当成功，也不推断布局变化来源。

更新自有StationWorldSetup.Edit：仍先RequireClean检查所有场景；若S2专用目标已干净打开，复用它，仅保存本次任务修改的自有目标并保持原布局；若未打开，沿用附加打开/保存/关闭路径。未放宽脏场景检查，不保存无关场景、不强制关闭用户当前标签。随后重新编译和执行配置，再实际测试。

## F-096：域重载后外部变更标志仍脏，测试提交反复断线

F03C多轮测试提交得到no_unity_session且无job_id。已编译DLL新于全部源码，日志数轮无源码更改的重载仍约76–81秒；原生状态最终idle/tests=false，但assets.external_changes_dirty=true。先保留所有失败回执，不猜job或强制重启/清理场景。

明确选择已发现的EchoStation实例，只调用refresh_unity(mode=if_dirty,scope=all,compile=none,wait_for_ready=false)；成功回执compile_requested=false，后置external_changes_dirty=false/idle。随后C-WorldRestoreCleanRefresh在3.37秒取得真实job_id。该对照支持测试前脏标志/刷新路径引发重复编译的判断，未追踪服务端全部调度根因。

后续：一次写完当前细分模块源码和直接引用，再请求一次编译；等实际DLL/空错误/idle。若域重载后仍有外部脏标志，做不追加编译请求的普通刷新并确认清除，再走全部测试安全门禁。不能反复用compile=request追赶同一轮重载，也不能手动伪造清除状态。未修改第三方插件或绕过场景/渲染检查。

module_tests.py另加外部脏标志门禁：未清理时保存状态并拒绝启动，提示先做不追加编译的刷新及实际DLL检查，避免进入同一循环。C实际恢复4/4、随后纯存档/对话25/25，支持工作流修复后测试确已执行。

F095启动门禁补充（F03C）：新代码自动重编译恰逢测试提交，run_tests回执为no_unity_session、没有job_id，不能poll或计入测试。保留C-WorldRestore-Job.json，module_tests.py新增必须success且有真实job_id才返回成功；随后用新标签重试，不猜测作业ID。

## F-095：程序集名称猜测导致零用例回执

F01 C-DialogueRegression误写DialogueExecution.Editor.Tests，工具返回Passed但total=0，不构成回归通过。随后rg发现实际文件/程序集为Dialogue.Execution.Editor.Tests，重新以新标签运行；保留零用例回执，后续必须同时核对程序集文件和正数用例数量。此前猜测同名文件读取亦失败，归入F054，不以工具完成代替测试执行。

F069补充（F01A）：ProgressAudit直接使用MemoryClueKind，首次刷新早于补全Puzzles.Runtime的Memory.Runtime直接引用，CS0012/CS0234；A-CompilationErrors.log保留。补引用后再编译，不运行旧DLL。直接依赖必须在首次刷新前补齐。

F054补充（F01准备）：PlayerJournal实际属于Player/Runtime，DialogueExecution测试目录亦应先rg发现；两次只读路径错误未改文件。后续同类读取必须用发现结果。

F094复测：C诊断1/1、D完整4/4、E故事36/36。实际初始路径PathPartial且止于闭门前，执行5秒内转待命、无发现。证据支持接收请求不等同执行成功，未证实引擎级雕刻时序根因。夹具可达点重试不作为生产传送功能。

F054补充（P04收尾）：只读rg再次传Docs/Troubleshooting*目录字符串导致Windows123，改为已知确切路径；不据失败搜索推断文件内容。

## F-094：扫描阻挡用例不能用初始接收结果替代执行结果

- P04 B-ScanIntegration 4项中3通过；B04期望闭门路径立即拒绝，但TrySubmit返回true。尚未证明是动态雕刻尚未更新、路径绕行、夹具物理场景还是产品问题，首次失败保留。
- 增加初始实际路径角点、门障碍世界中心/尺寸/雕刻启用数据；若请求被接收，继续要求5秒内转入失败待命且不得生成发现，再在可达夹具位置真实重试。没有取消最终阻挡/未发现要求，也没有直接把假设写成根因。

P04乱序内容复核：初稿校准架原文先写“与储藏室序列一致”，在A区先扫时会提前声称比对。改为“标记注明配对位置，取得两处后复核”，增加缺小屋的明确台词；只更新S2 ClueDefinition，摘要/密码/条件不变。该问题由内容审查发现，后续重跑D06来源测试和两种真实顺序，避免把假定顺序藏入台词。

F052补充（P04B）：一次完整补丁把Story路径漏写Assets/EchoStation，工具在验证时拒绝全部内容。已实际检查新控制器/报告都未创建，按正确路径重发；未混用半份更改或误记通过。

F093复测：D-PlayerBuild-01成功0错误/2既有警告，frameStatsRestored实际true；WindowsGPU01通过480/480有效帧、退出0。首次编译错误完整保留，未以旧可执行文件替代构建。

## F-093：Unity6000.6构建计数器为int

- P03-D新构建器将报告errors/warnings写为uint，与实际BuildSummary.totalErrors/totalWarnings的int类型冲突CS0266，尚未开始构建。D-CompilationErrors.log保留。
- 改为与已验证D02构建器相同的int；构建结果移到finally设置还原后再保存，frameStatsRestored用实际比较而非原值冒充。后续先看当前已安装API/既有可编译实例，不凭旧版本记忆选择类型。

F092复测：C-TerminalRetest三项实际通过，成功模态关闭和单独E开门均通过。475份S1不变，父面板Esc没有绕过IME保护。

F054路径补充（P03准备）：Windows下rg不展开作为目录字符串的PackageCache/com.unity.render-pipelines.universal*，返回路径语法错误；改在已确认PackageCache根搜索并以文件过滤，不把shell glob当rg目录。

## F-092：终端成功后只读输入停止处理Esc

- P02 B-TerminalIntegration 3项中1通过，B01/B03在成功后退出与开门步骤失败。GameTextInput.OnUpdateSelected先检查CanEdit，readOnly=true时跳过CancelRequested；若鼠标提交后焦点在按钮上，同样不能依赖输入框处理关闭。
- 父终端面板独立监听Esc，沿用输入框的IME事件保护窗口；新公开只读CanHandleModalEscape供模态容器查询，不改文字输入或删除IME保护。首次报告保留，修复后重跑三项并给每个门/关闭断言标注阶段。

F091再次记录（W06 B2）：测试安全预检确为空宿主，结束时活动布局却为干净Greybox，开始前是Demo，B2-PostWorkspace留证。门禁保持拒绝覆盖；保留新布局，重新采样作为后续操作起点。未修改工具允许任意布局恢复，来源未确认。

F002补充（W06 B2）：实际Lightmap153.68秒/2图集/325探针/13接收器成功并2/2验证，Editor日志再次Curl35/UnityTLS7。仅记录环境提示，不改TLS、不启用云服务，也不把这些提示等同本地烘焙失败。

F054补充（W06准备）：读取ScriptKnowledgeTests、RoomVisibility和RenderSafetyCheck时曾猜测不存在文件；均为只读失败，后续使用rg --files获取实际路径。再次要求先发现文件再读取，不能把名称相似当文件存在。

F052补充（W06 B2归档）：再一次用部分长行作为补丁上下文导致整份拒绝，确认未创建报告后去除无效更新再应用；没有依据失败回执推进下一步。

## F-091：测试后布局变化时保留现场，先检查返回码再继续编排

- W05实际2/2后host leave返回非零：当前不是空宿主。原生PostTestWorkspaceInspection证实S1 ES_Cabin_Demo已是唯一活动、干净、未播放场景；开始前为Greybox。变化由谁/何时触发尚未确认，不推断为用户操作或框架缺陷。
- 门禁正确拒绝切换，未保存/丢弃/关闭新的布局；保留新干净Demo，后续操作为它另建恢复快照。475份S1哈希不变，背景false。不能为恢复工具“成功”而强制打开旧场景。
- 编排错误：同一批次未先检查外部命令exit_code便写了“恢复成功”报告并准备W06源文件。已按实际证据更正报告，暂停后续场景创建直到查明当前状态。以后依赖步骤逐次检查非零返回码；外部命令失败不一定抛工具异常，不能仅看函数调用完成。

## F-090：多MonoBehaviour文件生成内嵌脚本引用后丢失门组件

- W04 A-Lab两项在结尾Single查找Vent_Isolation失败，前面的实际阅读/扫描已完成。原场景YAML有两个StationGate和两个局部MonoScript记录，但原生只读重开仅找到小屋门，MonoScript资源路径为空；GateAndAudioInspection.json留证。StationGate与StationDevice原本同文件，门组件未使用稳定.asset GUID脚本引用。
- 将StationGate独立为同名文件，将S2专用场景两处脚本引用改为导入后的StationGate.cs GUID；保留改前YAML，重开及PlayMode同时验证两个门。未改S1，也未跳过门关闭断言。
- A-Lab另有2监听器警告，专用场景原生检查只有一个。测试加载器将记录并临时静音外部测试场景监听器，退出还原；不删除产品监听器或把警告当完整根因。
- B-GateRetest实际5/5通过，两门GUID与运行期均验证；外部监听器实为无路径测试场景/Main Camera，5项重复监听器警告均0，原监听器退出还原。S1 475 SHA不变。

F022查询补充（S2-W03）：run_tests启动请求尚在重连时提前module_tests poll，Job文件未生成而FileNotFoundError。等待启动请求真实返回，取得保存的job_id后查询3/3成功；不要把启动超时/回执未到当作测试失败，也不要在无回执时猜ID。

## F-087：PlayMode加载回执早于场景isLoaded

- W01-B首轮配置1项通过，2项启动夹具在SetActiveScene报场景尚未加载。EditorSceneManager.LoadSceneInPlayMode返回有效句柄不等于isLoaded=true。B-Foundation.json保留失败。
- 夹具恢复已有D04的加载等待：明确等scene.isLoaded，带6秒超时与真实帧推进，再设活动场景和查对象。生产场景不作规避性改动，修复后重跑全部B。

## F-086：明确询问未知终端时跨目标复述已知密码

- D06-C首轮15项中14通过；C02发现仅有小屋维护记录时询问通道终端密码，检索的通用“终端/密码”相关项使回答复述7316。没有泄露尚未知的0428，但回答了错误设备的信息。C-StateFirst.json保留实际失败。
- MemorySearch增加显式线索别名范围，范围只含ID、不暴露未知正文或数值；密码回答仍只筛已知记录，同时尊重明确范围，未知通道返回“我目前没有相关记录”。新增生产小屋/通道别名与“密码/服务码/维护码”组。泛问多个已知码仍澄清，不自动选择。
- 修复后D05/全部D06/记忆回归实际111/111通过，C-Final.json留证；未知通道不复述小屋码，两个已知目标分别回复对应码。

F052补充（D06归档）：补丁把状态长行的一部分误当完整上下文，apply_patch验证拒绝整份补丁。已检查报告尚未创建，按实际完整行重发后才更新账本；没有把失败回执当归档成功。

F054补充（S2-D06）：读取报告时再次猜用了不存在的S2-D05_Knowledge_TestReport.md，实际为S2-D05_KnownAnswers_TestReport.md。只读错误，没有改动报告；用rg --files确认实际路径后继续。不能依据类似模块名推断文件名。

## F-085：Windows诊断在UI事件系统就绪前读取当前模块

- S2-D02C首窗口在startup阶段NullReference，行位于EventSystem.current.currentInputModule.inputOverride读取；宽高/HDR尚未写入，文字输入未执行。WindowsUI01/Report.json与Player.log保留，inputRestored=true，不能当UI已通过。初次未分别记录EventSystem或currentInputModule哪个为空，不将推断当完整节点诊断。
- 诊断增加启动时两节点布尔记录，明确等二者就绪后再取覆盖，Camera也单独校验。没有改产品输入、取消断言或隐藏失败。并按本Unity构建警告建议将自有FindFirstObjectByType换为FindAnyObjectByType；首PlayerBuild完整保留，既有ServicesCore/Windows.Forms警告不关联云服务或修改依赖来消除。
- 重建0错误/2既有警告，WindowsUI02明确startupEventSystem=true、startupInputModule=false；等待当前模块激活后全部文字/发送/模态/100行/清理通过，真实退出0。支持首帧模块时序的根因，原35保护SHA不变，后置Console0错误。没有放宽实际UI断言。

## F-084：测试助手方法与Key枚举重名

- S2-D02A首次编译，测试助手Key(params Key[])在表达式中遮蔽InputSystem.Key，导致Key.Backspace/Enter等CS0119。生产GameTextInput已编译，但没有用旧测试DLL运行；首错误保存在S2-D02/A-CompilationErrors.log。
- 助手更名PressKeys，明确保留Key枚举含义；以后助手采用动词名称，不与类型/枚举同名。A2实际输入/IME4/4通过，覆盖还原和同一输入设置对象/flags恢复，原工作区已还原；不能以更名或生产DLL生成代替通过。

## F-083：记忆 JSON 测试程序集缺少直接预编译 DLL 引用

- 2026-10-25，S2-M02。Memory Runtime 已生成新 DLL，但 Memory.Editor.Tests 的 Newtonsoft/JObject 出现 CS0246，未启动测试。实际测试 .rsp 不含 Newtonsoft，而 Runtime .rsp 有该 DLL；不能通过 Runtime 的引用推定测试能直接使用 Json.NET。相关经验先查 F069。
- 两个记忆 asmdef 明确 overrideReferences 和 Newtonsoft.Json.dll；测试还明确 nunit.framework.dll，保留已有 TestAssemblies 和 World/Memory 引用。没有安装新包、升级依赖或修改第三方源文件。首编译错误摘录保留在 S2-M02/A-CompilationErrors.log。
- 修复后编译完成，M02/M01 共26/26通过，后置 Console 零错误、Gameplay 绑定与干净场景保持。不能以 DLL 存在判作通过。

## F-082：MCP 刷新等待超时后须重新确认实际域重载状态

- 2026-10-25，S2-M01 首次新增两个程序集。refresh_unity 返回 success=false，等待编辑器就绪 60 秒超时；回执保存在 S2-M01/A-Refresh.json。Editor.log 同期记录两个新程序集 Csc 成功、Tundra build success 和域重载，不能将接口超时直接判断为 C# 编译失败，也不能据此直接运行测试。
- 重新读取 editor/state，实际 is_compiling/is_domain_reload_pending/is_updating 均 false、activity idle、ready_for_tools true，再通过 module_tests 的新鲜 SceneSafety、RenderSafety 和 Console 门禁启动。M01 实际 14/14 通过，后置 Console 零错误，场景与管线不变。
- 超时原因没有完整归因；未重启 Unity、重复强制编译或使用旧 DLL。后续同类等待先查实际状态及编译日志，只有完成证明和安全门禁都满足才继续。

## F-081：基线幂等性测试未还原项目管线绑定

- 最终全量244/244通过后RenderSafety仍allowed，但默认/PC/实际从Gameplay变为ES_PC_ForwardHDR。具体写入源已查实：ForwardHdrBaselineTests.T09调用CreateBaseline验证GUID幂等，该真实安装器会按R01职责绑定基线，旧测试没有finally还原。与F061空绑定不同，此处始终为有效普通Forward资产。交付Windows包是此前已验证的Gameplay配置，未被改写。
- T09记录原默认/Quality资产路径，finally重新加载非空资产并还原/保存；full-regression工作区也记录/比较渲染路径，防止只验证两种资产之一却漏掉变更。显式恢复本轮开始前已确认的Gameplay绑定，保留前后证据，再跑R01与Demo接线相关检查。原模板/基线资产继续SHA保护；不静默掩盖未知来源的漂移。


## F-080：预热必须实际调用取样API，不能只等待

- F079修复后的900帧残余96B位于采样器首两次取数，原始CPU栈没有Echo业务；之前10秒只是等待，并未调用FrameTimingManager.GetLatestTimings/读取计数器。
- 最终采样预热期间实际Capture/GetLatest并读取三项ProfilerRecorder，报告预热校验值；900普通帧仍全部保留，不从结果删掉异常帧来伪造0。后续正常轮关闭分配调用栈，保留二进制Profiler及真实数据。


## F-079：实际Profiler定位状态枚举与感知委托的持续GC

- MetricsBefore真实Windows900/900有效CPU/GPU帧、原始Profiler143958489字节、退出0；稳定40B/帧，约每12帧增128B。导入前原生Profiler未录制且无帧；只读取自有捕获，59帧调用栈明确两次20B来自EchoPresentation.ApplyFrame及Enum.IsDefined→GetValue，5次128B来自EchoBrain.Update方法组委托。ProfilerAllocationCallstacks.json留证。分析后清除自有导入数据，恢复原空Profiler状态。
- mood校验改为无装箱的无符号有界比较（负值仍拒绝）；危险筛选Predicate在Initialize缓存、Shutdown释放，保持同一实例状态与8秒冷却语义。先重跑表现/行为独立用例，再真实Windows对比GC，不能只凭改代码宣称0分配。MetricsAfter同仪表配置900/900有效帧并退出0，898帧0B、仅两次48B；业务持续40/128B消失。剩余两次经原始调用栈定位到采样夹具与FrameTimingManager.GetLatestTimings首次调用（ProfilerAfterColdAllocations.json），分析后恢复空Profiler。


## F-078：Player线程ProfilerRecorder名称不可直接假定可用

- WindowsPrepare7全部玩法/显示/受控剔除通过，性能阶段所请求5个ProfilerRecorder至少一项不可用，未采900帧并未伪造0ms。首失败保存。
- 查本Unity6000.6 SDK XML：FrameTiming提供cpuMainThreadFrameTime/cpuRenderThreadFrameTime/PresentWait，cpuFrameTime包含等待；改为这些真实原生字段，ProfilerRecorder只负责实际GC/总内存/Draw Calls，记录真实可用描述和三项有效性，缺失仍报错。启用独立诊断Player的Profiler与二进制日志，finally恢复；metrics轮额外记录分配调用栈，正式普通帧轮关闭分配调用栈。
- 不把主/渲染线程缺失计数器当0开销，也不把节流等待当全部有效CPU工作，完整原始Profiler.raw随结果保存。


## F-077：Windows遮挡图像对照必须先控制时间变量

- WindowsPrepare6完整玩法/显示均通过，PVS真实Draw Calls66→25，但图差.00074359未达零，首失败保留。仅关闭相机PP不等于关闭Renderer的SSAO，手电/软阴影/动态Echo也仍在运行；首轮未采同状态稳定基线，不能把差异直接认定为PVS错误或随意放宽阈值。
- 复用W01/P09-B已有因果控制方式：临时禁用SSAO、实时手电/故障光、Echo动态表现并冻结玩家，保留真实产品相机/静态场景/自身PVS；新增Off→Off稳定基线和非黑亮度断言，Off→On仍要求<1e-6并实际减少Draw Calls，finally全部还原后再作普通帧性能采样。不把受控对照当完整产品画质截图。WindowsPrepare7受控复测通过：Draw Calls19→12、Off-Off与Off-On图差均0、平均亮度.1807，所有设置finally还原。


## F-076：缩放后按钮路由与实际分辨率需分别核验

- WindowsPrepare5已完成全部原生探索/跟随/待命/warn/调查、真实黄灯波纹/隐藏授权和三条日志/两项扫描保存，供电设备保持；显示检查1280成功后1080恢复超时，未作为通过。WindowsResume1以另一个真实进程F9恢复通过并退出0。
- 给每次鼠标按钮点击加入EndOfFrame/Canvas布局同步与真实onClick见证，并记录退出时实际width/height，区分点击未命中与运行时尺寸调整。未放宽1080/全屏或实际扫描断言。原窗口失败留档；不预先把原因定为布局延迟。WindowsPrepare6加入布局同步与点击见证后1280↔1080、全屏/窗口均通过，终态1920×1080；产品显示API未修改。


## F-075：后台Editor的delayCall未排空，菜单返回不代表构建启动

- F074构建调度改delayCall后，菜单成功但PlayerBuild时间戳一直未变。原生诊断未播放/编译/构建，delayCall中仍有自己的QueueBuild及初始化回调，证明尚未构建，未启动旧包冒充新包。
- 改用既有稳定EditorApplication.update计数两轮后调度，并用ActiveEditorTracker.sharedTracker.ForceRebuild释放选择对应缓存；不手动执行/清空其他插件或Unity的延迟队列。只认实际新PlayerBuild时间/错误数和窗口结果。


## F-074：构建期间Transform Inspector持有已失效SerializedProperty

- 一次构建返回Succeeded但totalErrors=2，不能当0错误通过；明细只有2条既有警告，实际Editor.log两条TransformInspector/UIElements异常（m_LocalPosition disappeared/Next returned false），堆栈处在BuildPipeline重绘内，无产品代码异常。Build-WithInspectorErrors/F074-InspectorConsole/Stack保留。
- 清除当时ES_Echo选中对象并重绘以释放失效Inspector（不保存/丢弃场景）；构建器之后按GlobalObjectId保存选中项、暂清选择并延迟一轮重绘后构建，finally按仍能解析的对象恢复。修复后清理已存档的旧Console异常再重建，仍要求summaryErrors=0及产品实际运行。


## F-073：调查指令测试需区分接受与自动警告中断

- WindowsPrepare4证明UI已接受，但20秒仍无实际扫描，不能据此归因于临时warn。新增最终导航/扫描/台词诊断；真实Demo独立A-PlayerDoorway1/1通过：玩家6.5/1与7.7/1位置静态NavMesh路径均Complete，不能宣称前者不可达；玩家7.7/1时实际调查/隐藏扫描成功，终点(7.928,.04,2.032)，传感器FailureNone。Windows观察点改用该已实际验证位置，并继续完整流程验证，初次动态避让/警告序列原因仍待运行时诊断。

- WindowsPrepare3先前原生输入/收集/跟随/待命/警告均通过，调查按钮后立即要求CurrentMode==Investigate失败，缺少当时Status/导航原因，首轮保留。产品允许自动warn临时中断并恢复调查，不能仅凭某一帧根模式判定按钮未接受。
- 检查实际UI关闭与成功状态（失败会保持面板），记录失败时Mode/Status/NavFailure/位置，并继续要求实际到位扫描/隐藏/日志完成。当前仅修复接受断言与增加诊断，若最终扫描不完成继续查实原因，不预先宣称根因。


## F-072：Windows合成鼠标与物理鼠标并存的输入隔离

- WindowsPrepare2启动/渲染基线已正常，但实际90像素虚拟鼠标增量未得到预期9度，探索尚未开始；首失败保留。不能拿Editor鼠标测试代替Windows接线。
- 诊断Player内暂时Disable已有且enabled的物理键鼠，仅保留自己创建的虚拟设备，退出恢复原设备与背景输入设置；不更改产品绑定或系统设备。增加实际yaw增量字段并重跑，以验证是否来自多个Mouse的PassThrough事件/锁定游标事件竞争；未经复测不声称已确定。WindowsPrepare3隔离后实际yaw增量9.0度，移动/F/E/J及警告均通过，支持多设备事件竞争判断，仍不改产品绑定。


## F-071：复制场景共享NavMesh资产被AI Navigation清空

- 首Windows在CabinDemo.Start失败，Ready超时、退出1，WindowsPrepare/Report与Player.log保留；实际Demo YAML的NavMeshSurface.m_NavMeshData已为0，原灰盒仍为有效原资产。Editor.log有Duplicating NavMeshSurface警告，AI Navigation源码OnValidate/UnshareNavMeshAsset明确检测另一Surface引用相同数据后清空，不能把编辑器残留全局NavMesh的TryBind成功当本场景资产就绪。
- Demo改用自己目录的NavMesh数据副本（几何不变无需重算），显式RemoveData/赋值/AddData；配置测试检查专用GUID，构建前要求单Surface和非空专用路径，启动失败额外记录NavigationFailure。原导航哈希保持。重新配置后重跑接线、Windows及最终回归。
- 首构建另有6条新FindObjectsSortMode弃用提示，诊断代码改用本Unity版本无参数重载；2条既有服务/Windows.Forms提示仍保留。失败启动退出时JobTempAlloc提示留档，后续成功运行再核对是否复现。


## F-070：组件enabled不代表不活动根对象会渲染

- P09-A原生按键/四行为/模态测试通过；预览配置检查把已停用的旧验证相机根对象也计入enabled，共2而非1。GetComponentsInChildren会返回调用根本身，不能把enabled单独当实际活动。
- 检查同时要求gameObject.activeInHierarchy，保留旧验证相机为inactive以保护来源结构，重新跑两项；实际产品仍仅玩家相机渲染。


## F-069：测试程序集引用不传递

- S2-M04B复用合成输入夹具时遗漏其实际定义程序集/命名空间，CS0246。先保存B-CompilationErrors.json，空工作区门禁因Console错误拒绝且布局未改；rg定位SyntheticInputScope实际位于PlayerEditor.Tests、EchoStation.Tests.Player，再补直接引用和using。没有复制夹具或修改产品输入策略；复测结果另见M04报告。

- P09接线测试直接使用ScanWaveView/Emitter/DistanceShadowCaster，但PlayerEditor.Tests未直接引用Rendering.Runtime，CS0246；安全门禁保存A-DemoWiring-ConsoleErrors并拒绝启动。
- 补明确asmdef引用后编译复测，不能假定通过Player.Runtime的依赖自动获得类型。只读检查LightingData.asset也证实为二进制，后续用Unity API或二进制元数据，不再用Get-Content输出。


## F-068：景深UI图像断言混入透明背景

- P06 Windows实际近对比不变、远对比.05229→.00289，4组有效样本69/65/85/79，但底部广区域白像素801→808超过任意5像素阈值；首失败保留First-*。
- 透明提示底板使背景抗锯齿变化进入统计。按已知真实文字位置/颜色取字形内部，514像素最大差1/255、平均.110/255，证明文字保留。修复探针用字形内部平均差<2/255且像素>200，重新构建运行；不放宽景深图像/有效计时要求。


## F-067：FrameDebugger窗口改变夹具应用焦点

- P06首轮实际图像通过（近对比.1555→.1271，远.06565→.00420，5个Bokeh事件），后续UI关闭恢复焦点断言失败。FrameDebugger切换窗口后输入仍失焦，单独SetPointerCaptured不能绕过产品焦点门禁。
- 恢复测试交互前明确SetApplicationFocus(true)再捕获；仍单独测试失焦退出，产品门禁不改，首失败保存。

## F-066：刷新尚未结束时状态响应不完整

- P06-A刷新请求尚在等待时启动预检，editor/state的data缺少advice，原脚本KeyError退出，没有启动测试或保存场景。
- 等待刷新返回并确认编译完成后顺序启动测试；预检对缺失advice/tests明确拒绝并提示重试，不将不完整状态视为空闲。

## F-065：Editor构建助手Object类型歧义

- P05窗口构建助手同时using System/UnityEngine，Object.DestroyImmediate出现CS0104；日志保留，未用该错误版本执行构建。
- 按现有工具惯例明确UnityEngine.Object（或使用别名），重新编译Console0错误后才执行安全菜单。C1面板3项已独立通过，C2继续真实Windows验证。

## F-064：MCP状态缓存过期导致空闲编辑器被持续拦截

- P05-A编译已结束、Console0错误，但资源advice只报stale_status，age从126增长至333秒；没有启动测试。原生execute_code确认未播放/编译、实际场景干净，重绘/QueuePlayerLoopUpdate及telemetry_ping未更新缓存。
- module_tests只对唯一stale_status原因增加真实主线程回读（播放/编译/更新/原生TestRunnerApi.IsRunActive/项目路径），任何忙碌或无法读取都拒绝；之后仍必须新鲜SceneSafety、RenderSafety和Console门禁。不把缓存过期当空闲证明，也不绕过自动保存保护。NativePreflight保存每次实际读值。其他阻塞原因保持拒绝。

## F-063：字体源API限额与旧标签目录不同

- P04中文字体资源核验时，GitHub API共享额度403、git ls-remote连接重置，旧Sans2.004标签/历史提交的Sans/LICENSE路径404。未引入账号/凭据或把错误响应当字体文件。
- 官方当前main的字体文件HEAD200/16437364字节，许可页面明确OFL1.1；改为官方raw当前快照，保存完整许可、字节数/SHA256以及原始字体name元数据，运行期不下载/自动更新。不声称当前快照来自不存在的历史路径；后续复用本地哈希审计，避免为取commit重复耗尽API。
- P04读取FontImporter XML时误用父目录（F054），改为实际编辑器反射确认TrueTypeFontImporter的fontTextureCase/includeFontData/fontSize，不猜接口。

## F-062：普通Additive相机.scene未隔离其他场景灯光

- P03恢复绑定后图像比较通过，但检查FrameDebugger发现4个MainLightShadow事件+1个AdditionalLightsShadow；夹具只有Spot，没有Directional，确认存在外部场景光/几何污染，不能将总5个事件全归于手电。首对照文件保留ForeignLightControl。
- 普通Additive场景的camera.scene不能替代完整渲染隔离。夹具几何/相机/手电用专用30层，暂禁非本夹具的已载入Light并保存原enabled，清空夹具环境SH，退出还原灯和SH。统计只认AdditionalLightsShadow且要求MainLightShadow=0，静态/动态表面各测亮度增量。重新测试再验收；产品灯参数不改。

## F-061：项目默认与PC画质管线绑定意外为空

- P03-B全黑、所有图像差0、FrameDebugger空，Console五条SubmitRenderRequest no SRP提示；首失败与空记录保留。读取实际Graphics/Quality设置确认PC当前/默认/实际管线均null，ProjectSettings也为null；不是仅图像采样阈值或灯强度问题。
- 已查既往F016初始化经验；此处资产绑定本身为空，单纯warmup不足。空绑定具体产生时点尚未证实，不能宣称已锁定单一根因。现有可写绑定路径集中E12诊断Probe/构建和临时图像夹具。
- 增加诊断Awake严格Application.isPlaying/非空Pipeline/专用CLI门禁，临时图像夹具只有确实覆盖绑定后才还原，避免未初始化清理写null。干净场景下恢复用户已确认的ES_PC_ForwardHDR默认及PC绑定，前后值保存；重跑R01绑定/功能及P03图像，随后跟踪E12循环还原，若再次漂移继续定位而不掩盖。

## F-060：InputSystem设置替换会销毁HideAndDontSave旧对象

- P03-A功能断言完成，但TearDown恢复InputSystem.settings抛ArgumentNullException；首失败A-InputAndLifecycle.json保留。确认运行器已自动退出播放并回原R03场景，无用户保存/丢弃。
- 包源码InputManager.settings setter明确DestroyImmediate旧HideAndDontSave设置，之前P01测试if(oldInputSettings)会因对象已毁跳过恢复，不能把该条件当作原样还原证据。原型产品输入未改，但夹具还原证明不足，需追溯复测。
- SyntheticInputScope在替换前只对该临时对象暂改hideFlags，克隆用DontSave防自动销毁；Dispose还原同一原对象及原flags、移除虚拟设备、销毁克隆，幂等。P01与P03统一用此夹具，TearDown明确断言对象身份与flags都还原，重跑依赖的Player测试。
- 后续临时替换第三方ScriptableObject先查setter是否拥有/销毁旧对象，不只记录引用。

## F-059：附加灯阴影级别属于URP附加数据

- P03-A测试直接访问Light.shadowResolutionTier导致CS1061，配置/测试门禁未放行。
- 先复查R04/E11已有实现：本版本用UniversalAdditionalLightData序列化m_AdditionalLightsShadowResolutionTier，改为相同真实字段检查，不更改已验收Light模板。URP特有字段先检索已验收代码和包源码，避免按Light接口猜测；重新编译后执行A。

## F-058：后台编辑器中虚拟键盘实际Update未移动

- P01-B首轮3项2通过：真实四向控制器均3.00000024m、yaw90前进X=3、Prefab配置通过；虚拟W经过InputSystem.Update后未能在5秒内移动.5m。B-ActualController.json保留。
- 不将纯ApplyInput通过冒充键鼠接线通过；补InputStallDiagnostics后复现：CanPlay/captured/keyPressed/deviceEnabled/motorEnabled均true，但Move=(0,0)、appFocused=false、编辑器PointersAndKeyboardsRespectGameViewFocus。源代码说明默认把未聚焦GameView键鼠分流到Editor，确认夹具问题。
- 仅此后台虚拟输入用例创建InputSettings内存克隆，AllDeviceInputAlwaysGoesToGameView/IgnoreFocus让合成键到达真实InputAction；仍通过产品SetApplicationFocus/SetUiBlocked验证门禁。TearDown还原原设置并销毁克隆，不修改产品配置或关闭产品失焦保护。首诊断失败文件保留。

## F-057：跨程序集调用internal目录辅助函数

- P01-B Prefab构建器引用Rendering.Editor内internal EnsureFolder，Player.Editor程序集编译CS0117，安全门禁拒绝菜单和测试。
- 查源码确认访问范围，改为当前固定父目录下的AssetDatabase.IsValidFolder/CreateFolder，不扩大旧辅助函数权限或添加无关依赖；编译通过后再实际构建Prefab/测试。

## F-056：浮点累计位移测试误用精确整数相等

- P01-A首轮6项5通过，30步浮点结果3.00000024与精确3比较失败，误差约.00000024m；输入/焦点等通过。保留A-InputMotion.json。
- 位移/角度数值应使用明确小容差，改为.00001m，仍保留帧步差/斜向速度/90度旋转断言；不修改3m/s实现来补偿浮点舍入。重新运行A全部6项。

## F-055：URP显示器HDR开关属于AdditionalCameraData

- E12-B图像测试把allowHDROutput写在Camera上，CS1061；safe_menu和测试门禁均阻止继续，没有使用旧程序集。
- 当前URP17.6字段在UniversalAdditionalCameraData，参照已有R01/E11实际接线改正；Camera.allowHDR仍控制内部HDR。两项语义分离，修复后重新编译/配置/运行图像测试。

## F-054：按推测类名读取不存在的测试文件

- S2-D02只读查找一度使用推测的S1P09WindowsProbe.cs且返回未读；实际rg文件清单为P09DemoWindowProbe.cs，随后读取实际文件复用既有窗口/输入隔离经验。没有把未读当作已检查，也没有修改不存在文件；后续继续先定位再读。

- P08又误写不存在的EchoRigSetup，实际为EchoModelSetup，CS0103阻止编译；按目录rg定位并更正。P06只读查询也误猜CabinBake/README/许可路径，均未改文件。再次明确所有类/路径先rg定位，不把猜测带入代码。

- P03调查中又误把PowerShell -TotalCount传给rg（不支持），以及向rg传未展开Windows包路径通配符（F004已记录）；查询失败未改变项目。统一先rg --files或实际目录清单定位，rg限行用管道Select-Object -First。

- E11继续任务时两次Get-Content使用推测的EchoWaitRuntimeTests/ForwardHdrConfigurationTests路径报不存在；实际文件为WaitNodeTests/ForwardHdrBaselineTests。未影响Unity或产品测试。
- 先用rg --files确认现有文件名，再读取；不用忽略错误来当作文件已检查。

## F-053：手动警告未更新自动冷却，导致立即重复自动提示

- E10-C基础接线9项通过后补冷却边界；两项1通过，实际自动重复间隔8.1029秒符合规则，但手动警告后2.1秒内automaticWarnings=1/ModeWarn，违反去重预期（C-CooldownBoundary.json、ManualWarningDedup-FirstFailure.json）。
- 把目标冷却写入统一的成功warn入口，手动/自动都更新8秒自动提示冷却；显式手动请求仍可重复，自动计数只统计自动触发。自动分支不再独立重复写冷却。保留首失败，重新测试边界与手动去重，不缩短1.5秒保持或8秒冷却来绕过问题。

## F-052：没有锚点的补丁把using追加到文件尾部

- E10-C增补Predicate依赖时，补丁使用空上下文将using System追加到DangerPerception.cs尾部，编译报CS1529。safe_menu识别错误后拒绝Prefab修改，未启动依赖测试。
- 用明确首行/尾部上下文把using移到文件头；后续补丁必须带可核对的上下文，尤其插入using。重新编译后再配置控制器并测C，不使用旧程序集继续。

## F-051：Windows Python默认GBK不能读取含中文的UTF-8证据JSON

- E08最终审计临时脚本用Path.read_text()默认编码读取ActualScans中文摘要时报UnicodeDecodeError，非Unity/扫描失败；81项测试已全部通过。
- 统一显式encoding='utf-8-sig'读JSON、'utf-8'写出，保存可重复执行Tools/Assets/audit_e08.py；不重跑无关81项或删改中文证据。测试耗时也区分框架汇总durationSeconds=4.104与实际作业起止墙钟108.183秒，整体耗时用后者，不能把跨模式运行的根汇总时长当作完整墙钟耗时。

## F-050：低位探头看不到桌面上方笔记，不能用穿透桌面补救

- E08-C1首次2项1通过，行为取消保护通过；维护笔记扫描失败，实际射线首遮挡Main_DeskTop（C1-ActualScans.json、ActualScans-DeskOcclusion.json）。这是灰盒摆放/真实几何关系，非传感器误报；Echo探头约.78m，桌面顶.925m。
- 将同ID临时维护纸片从桌面改为桌前侧(-2.8,.8,1.885)、X旋转90度，保持原文/摘要/ID/批准地面点；静态桌子和碰撞不改，纸片仍动态触发器，无需重烘焙。禁止忽略桌面Collider或放宽穿墙检测。修复后重新实际扫描5目标/取消/重复提交。

## F-049：导航剔孔更新后旧NavMeshPath不能用作稳定历史快照

- E03修复模式切换后的3项2通过；实际carved=true、新路径净空1.09m、最大穿透0且已抵达，但在更新后读取before.corners为空，oldClearance=Infinity导致对比失败（Obstacles-Retest.json、Detour-PathSnapshotFailure.json）。不能据此声称绕行未发生或放宽路径变化断言。
- 在剔孔前立即复制corners数组和计算旧净空，并断言至少两点/有限值；更新后独立重算路径，保留原净空增量>.25m、抵达与碰撞断言。运行期路径对象是可失效的原生句柄，历史证据用普通数据快照。待最终Echo整合回归。

## F-048：EnterPlayMode指令放在嵌套IEnumerator导致测试未切换模式

- E03首轮配置项通过，动态测试Setup内嵌套yield EnterPlayMode后仍是EditMode，LoadSceneInPlayMode报必须先播放；无超时的加载等待卡住，第一项触及180秒测试超时。Obstacles.json保留，不将MCP“失焦”提示当作根因。
- 通过实际TestRunnerApi唯一运行器CancelRun取消；确认IsRunActive=false、原R03场景已恢复后，清理MCP遗留running标记（CancelStuckRun/ClearOrphanAfterCancel.json）。未重启Unity、未保存用户场景。
- EnterPlayMode改为每个UnityTest顶层直接yield；Setup先断言Application.isPlaying，场景加载加5秒墙钟超时。帧/游戏时间等待仍有独立超时；修复后重跑，不提高180秒总超时掩盖卡顿。
- 取消后原生IsRunActive=false，但MCP clear_stuck只清作业、TestRunStatus仍true，refresh仍拒绝。再次原子核对原生未运行后调用MCP已有MarkFinished清理孤立状态（FinalizeOrphanStatus.json），随后刷新编译恢复。没有在真实运行中的测试上强清标记；后续核对播放选项恢复。

## F-047：Editor高速测试循环中固定帧数不等于有效运动观察时间

- E02实际导航通过，但Editor的数万极短真实帧覆盖数秒移动；原取消测试固定20帧，未显式证明已经移动。不能据此充分声称“移动中取消后持续停止”。
- 改为先实际位移至少.4m，再取消并观察至少.5游戏秒（均有5秒墙钟超时），保持路径为空/位置变化<.01。轨迹位置按.02m取样减小报告，碰撞仍检查每一真实帧并记录总检查帧数；不把Editor循环帧率当作产品FPS。首次通过数据保留。

## F-046：NavMeshPath原生对象不能在MonoBehaviour字段初始化创建

- E02-B首次配置Agent时Console报InitializeNavMeshPath不允许从MonoBehaviour构造/字段初始化调用；测试安全门禁拒绝启动，4条错误原文保存在B-Movement-ConsoleErrors.json。预制体反序列化可在非主线程初始化字段。
- NavMeshPath改为首次TryMove主线程调用时懒创建，不在构造/字段初始化/Awake以外线程调用Unity原生API；IsBound额外保护销毁顺序导致Agent缺失。编译后再配置和独立测试，不跳过错误门禁。

## F-045：AI Navigation2.0.16的m_GenerateLinks没有同名公开属性

- E02-A编译报NavMeshSurface.generateLinks不存在，safe_menu检测Console错误后拒绝菜单，未烘焙/未修改导航设置。
- 本地Runtime/NavMeshSurface.cs和Editor/NavMeshSurfaceEditor.cs显示该配置是私有序列化m_GenerateLinks；Editor builder/测试改用SerializedObject字段，不猜测公开API。普通运行时代码不依赖私有Editor字段。重新编译后再执行A。

## F-044：URP材质验证会按GI标志重新计算Emission关键词

- E01首轮4项3通过，三色材质的_EMISSION断言失败；EmissionColor非黑已保存，但导入后关键词为空、m_LightmapFlags=4。源BaseShaderGUI.cs:948–950依据AnyEmissive决定关键词，单独EnableKeyword不足。
- 对3份专用动态指示灯材质设置BakedEmissive标志后启用_EMISSION，保存；动态Renderer无ContributeGI，不因此开启Realtime GI或向场景烘焙灯。新增无灯/仅指示灯的实际HDR像素断言，原正反面/结构断言保持。首轮Rig.json与图像保留，待复测。
- Rig-Retest.json最终4/4通过；三材质关键词与GI标志保持，无灯StatusEmissionDark.png实际蓝色HDR像素>1。修复得到配置与实际像素双重证据。

## F-043：Sketchfab官方下载指南访问失败

- E01资源核查时web首次连接失败，单次重试官方developers/download-api/guidelines返回403。已发现CC Attribution轮式Rover页面，但未下载/未核实骨骼与源文件，不把搜索摘要当作适配完成。
- 不绕过访问控制、不执行账号操作；使用用户已授权的轮式灰盒兜底，来源审查与尺寸记录见S1-E01文档。这是资源访问限制，不影响无LLM灰盒实现，也不声称外部不存在适配模型。

## F-042：PBR接入后的烘焙灯开关图像对照出现差异

- W02首轮完整78项77通过，B203失败：Lightmap/141探针/三室可读性有效，移除Lightmap为黑，但关闭Baked灯图像差.00238416超过1e-6。首轮结果C-Final.json、图像EditorLighting-FirstFailure保留。
- 排查中：不能直接认定灯混入实时。先增加10个实际相机帧预热，记录两次相同灯状态基准图差，并记录5盏灯bakingOutput.isBaked；原1e-6灯开关断言不降低。该假设尚待独立复测，不声称已修复。
- 独立C-WarmupDiagnostic复测1/1通过：预热后基准图差0、灯关闭差0，5盏isBaked均true、移除Lightmap全黑。支持初始渲染状态未稳定的判断，但未定位具体异步资源/缓存来源；保持真实帧预热和基准稳定性断言，随后重跑完整回归。

## F-041：W02遗漏运行时命名空间，编译错误后菜单不可执行

- CabinPbrSetup中的DistanceShadowCaster缺少EchoStation.Rendering引用，报CS0246。Console检查与菜单调用被错误地无条件连续执行，菜单失败且未导入资产；F-009/F-031经验需落实为判断返回错误后禁止调用依赖菜单。
- 增加实际运行时命名空间。后续刷新完成后先独立检查Console为0错误，再执行导入及测试；不将菜单受理视为导入完成。

## F-040：Light.lightmapBakeType仅在Editor提供，测试共享运行时代码不能引用

- W01-B3 Editor两项通过，但首次Windows构建报CS1061，CabinLightingProbe引用了Editor专用lightmapBakeType。失败构建报告保留VisibilityPlayerBuild-FirstFailure.json。
- 运行时通过夹具明确拥有的Cabin_Lighting组获取5盏Baked灯；Editor配置测试仍逐个验证其Baked类型。此修改不改生产光照、不放宽图像因果断言；重新编译、构建并重跑受影响光照测试。

## F-039：同一SRP批次包含多个绘制，不能用批次数断言小屋遮挡收益

- W01-B3初测PVS资产存在、画面差0，但开启/关闭/入储物室都只有1个SRPBatch，原“批次数必须下降”断言失败（2项1通过）。这不证明无剔除；该度量不等于底层Draw Calls，R05已区分两者。
- 修正验证分工：Editor检查有效PVS、画面一致与实际Opaque路径，不假称批次减少；B3独立Windows程序用正常帧ProfilerRecorder采样实际Draw Calls，仍严格要求Off>On、16个不同帧/状态及非零有效值。没有Windows实际下降证据不得验收B3，也不进入W01-C。

## F-038：图像非黑的数值通过不能代替探索可读性

- W01-B2首烘焙30.706秒、2图集/141探针/8渲染器有效，实际图像控制通过（关Baked灯差0、移除Lightmap差.02276且全黑），但人工查看三室截图过暗、偏饱和蓝，储物室取景过近。
- 调整：保持R02 ACES/曝光0不变，提高本模块Baked灯能量（主房9/6、储物7.5、门斗4.5、危险点2.5），冷光改.78/.86/1，保留少量暖橙；储物室相机改为室内角落观察全屋。画面平均线性亮度门槛由.002提高到.01，原光照因果/UV/配置断言不降。首轮图像/烘焙结果保留，须重新烘焙并视觉复查后才验收B2。

## F-037：MCP测试服务会自动保存已命名的脏场景

- 本地TestRunnerService.SaveDirtyScenesIfNeeded源码确认自动保存行为，可能违背用户“不能保存/丢弃未保存场景”授权边界。此前各阶段保护SHA均未变，未发现实际用户数据被保存的事件；这是工具前置风险审查。
- 实施：SceneSafetyGuard只读导出所有场景/Prefab脏状态、播放/编译状态；Tools/MCP/module_tests.py先验证新鲜预检且全部清洁、Console无error，之后才调用run_tests。拒绝时不调用测试服务、不保存/丢弃任何内容。后续测试统一优先此入口。

## F-036：W01首次烘焙前必须重新展开合并体UV2

- 集成预防性发现：未烘焙源各自UV2可能占同一0–1区域；R05保持属性的合并路径不能直接当作新Lightmap UV布局。W01-B1对合并输出重新GenerateSecondaryUVSet，并用三角形严格内部重叠检测后才允许烘焙；不重展已经过同图集offset变换的成品UV。
- B1实现中短路条件第一项失败时out变量可能尚未赋值；改为条件外先声明/初始化error，避免CS0165。该修正不改UV有效性断言。待编译与模块测试。
- B1复测World5/5通过，八个合并体UV2均不重叠，23个物理扫掠保持。另一个集成注意点：合并Renderer.bounds包含整个空房，不能用它排除“固体内部”的Light Probe；B2使用原BoxCollider实际体积筛选探针。

### R05最终复测与设置恢复

F-034编译修复后Windows构建0 errors/2既有warnings，运行退出0、80个正常帧计数样本有效。F-035最终同时备份/恢复选项flags与enabled，完整63/63、外部9项审计全部通过；EditorOptions-Final.json实际API为true/1。共享Pipeline/Renderer及保护资产SHA全部不变。首62/63及每次无效假设的失败证据保留；原生缓存细节仍按F-035限制处理，未宣称所有缓存根因已定位。

## F-035：R05完整回归的编辑器遮挡采集与独立初测不一致

- 日期2026-10-25；完整63项62通过，B02从独立初测2→1变成2→2而失败；反转仍1、入远室2、图像不丢失。同时Windows正常帧80个有效计数样本稳定：未合并31、合并24、遮挡18、反转7、远室20，Player通过退出0。
- 分析：不把Editor异常推广为运行期遮挡失效。测试在切换后只等待1个真实帧，随后Frame Debugger暂停游戏；怀疑可见性状态尚未稳定，但尚未确认完整根因。
- 修正尝试：每次相机/遮挡状态变化后等待10个真实游戏帧再暂停采集，与Player正常帧预热策略一致；不降低2→1的绘制判定。保留完整失败与采集数据，待复测。

- 十帧预热的相关4项复测仍3/4，不能认定时间预热已修复。发现同次运行C02先加载R05可正常剔除（基线1批），退出后B02再次附加加载才失效；27/27静态PVS对象引用仍匹配Scene。进一步假设是Editor重复PlayMode加载保留了已注销场景的PVS缓存。仅在R05场景确实未加载时，针对该夹具PVS Resources.UnloadAsset后重新加载；不卸载其他场景或用户数据，待因果复测。

- 定向卸载PVS仍3/4，未解决；移除该无效处理。进一步修正测试生命周期：原finally调用UnloadSceneAsync后立即ExitPlayMode，没有等待卸载完成；两个R05运行测试改为在UnityTearDown确认异步卸载结束（临时runInBackground=true）后才退出。此为待验证修正，不宣称已经定位唯一原因。

- 等待场景卸载的相关复测仍3/4，故也不是唯一原因；保留完整清理作为测试卫生。动态SRP Batcher测试不需要PVS，原来共享同一带PVS场景导致验证互相依赖；改为独立动态测试场景（同几何/材质、无PVS引用），遮挡测试仍使用原烘焙场景和原严格断言。Windows程序仍使用原场景，不改产品或运行期剔除策略。待相关/完整回归确认；已查公开Issue Tracker，未找到足以确认本机现象根因的匹配条目，不将旧版问题套作已知引擎Bug。

- 独立动态夹具后仍3/4，单凭共享PVS资源也不足解释。发现模板EditorSettings开启Enter Play Mode Options且DisableDomainReload=1；而测试默认EnterPlayMode期望域重载。为检验残留编辑器静态状态，在遮挡运行测试内临时使用正常域重载，原设置保存在SessionState并在退出测试后恢复；不永久更改用户编辑器选项。待复测。检索测试框架时一个辅助类猜测路径不存在，随后仅用rg实际定位的EnterPlayMode.cs读取API，未修改第三方包。

- 正常域重载后相关4/4通过（D-DomainReload.json），实际遮挡提交再次2→1，EditorSettings两项原值1/1均恢复。此结果支持关闭域重载产生测试状态残留的判断；尚未将具体原生/托管缓存完整归因。准备完整63项回归，不以4项通过替代完整验收。

- 首完整修正版63/63通过且外部8项审计通过，但随后核对发现EditorSettings序列化选项变为1/3，原先只备份enabled不够。MCP TestRunnerService自身也管理重载选项；本次不声称某一组件是全部漂移来源。测试改为SessionState同时备份/恢复enabled与flags；专用恢复菜单恢复本轮操作前多次观察到的序列化1/1，并用实际EditorSettings API读数留档，随后重新回归。此恢复入口仅针对本轮已记录原值，不作为任意项目默认配置。

## F-034：R05 Player探针HDR API命名空间写错

- 日期2026-10-25；编译CS0234：将HDROutputSettings误写为UnityEngine.Rendering中的类型。原R03/R04已正确使用UnityEngine中的该类型。
- 修复：复用已有类型归属，改UnityEngine.HDROutputSettings。Console门禁阻止了构建，未用旧程序集继续验收。
- 复测：待编译和Windows执行；工程无渲染方案变更。

## F-033：遮挡烘焙统计接口0值不能代替实际PVS检查

- 日期2026-10-25，R05-B首烘焙约.312秒，场景已绑定新OcclusionCullingData，文件12881字节且有完整m_PVSData，但StaticOcclusionCulling.umbraDataSize读0导致包装器判失败。
- 不确定项：0值统计刷新/加载时机尚未完全归因，不能宣称“没有烘焙数据”或“烘焙效果已通过”。
- 修正与复测：保存PVS5568字节、数据资产12881字节且场景引用有效；原native统计仍0并保留。R05-B两项通过：遮挡开关提交2→1且图像差0，入远室恢复2并确有物体像素贡献。因此实际遮挡功能已验证，统计0值的刷新来源尚未完全定位；不把接口值解释为内存0。首轮结果/数据保留。

### R04最终复测

F-031修复后B-Final 2/2且最终完整47/47通过；先前0测试的任务不算验收。F-032最终实际捕获14→8总事件、4→0阴影SRPBatch提交；详细draw数未获得，仍保留限制。Windows Player图像通过并退出0；F-011/F-019既有警告照实保留。Editor部分绝对亮度与Player不同，不把两环境亮度当一致性证明；同环境控制与GPU级联均通过。

### F-032采集通路修正

已安装MCP源码有manage_profiler/FrameDebuggerOps，但其逐事件字段名不适用于本版本全部m_字段，因此沿用已反射确认字段的独立测试采集器。ProfilerConnections实测-1:Editor，先前“未连接”推测不成立，也不能把运行期64位session GUID转成性能连接ID。最终改为打开官方FrameDebuggerWindow、调用其EnableFrameDebugger、跨6个Editor更新重绘后采集，真实取得On/Off事件列表。说明UI/Editor更新激活路径是必要因素；未孤立证明其中每个因素的独立作用。

GetFrameEventData立即调用依然false（详细单次draw数不可用）；实际事件列表明确出现SRPBatch + MainLightShadow/Shadowmap。检查改为真实阴影绘制提交批次数On>0、Off=0，单次draw数仍标不可用，不用批次数冒充draw数。增加远物体显隐图像对照，验证其实际可见。待最终复测。

### R04外部文档检索提示

2026-10-25查UnityCsReference GitHub树API遇403 rate limit，未更改认证或系统网络；改查已安装MCP源码和本机反射结果。原始远端源码路径404也不作为Unity API不存在的证据。

## F-032：Frame Debugger直接RenderRequest首次未取得任何事件

- 日期2026-10-25；R04-B实际灯光/阴影图像、四级GPU索引、近远密度和距离对照全部通过，但绘制证据为0事件，整项验收失败。
- 初步分析：RenderRequest提交后可能尚未等渲染线程执行，不能把空事件当成零绘制。6000.6官方绑定明确独立相机调用需要Enter/LeaveCapturingScope，已有该scope；增加读回同步与捕获环境字段以检查线程/目标问题。
- 处理/复测：待执行；保留B-FirstActualRun.json及首轮FrameDebugger空记录；不删除ShadowCaster实际绘制断言。

- 第二轮读回同步仍为0事件，但诊断明确connectedProfiler=-1、FrameDebugger.enabled=false；不能将渲染线程延迟判为根因。第三轮尝试EditorConnectionInternal.GetLocalGuid，实测该GUID并非Int32连接标识，转换OverflowException；保留B-Retest2/3.json。改为读取ProfilerDriver.GetAvailableProfilers及标识字符串选择真正Editor性能连接，不混用Runtime session GUID。此为测试工具故障，实际图像结果仍通过。

## F-031：R04图像夹具遗漏运行期命名空间

- 日期2026-10-25；编译CS0246：R04ShadowProbe未using EchoStation.Rendering；asmdef已正确引用，但命名空间仍须显式导入。
- 修复：补using。首B测试请求在编译错误存在时仍被MCP接受，不将该任务视为有效新程序集测试；后续检查Console结果为空后才能单独发起测试，避免同脚本无条件连续调用。
- 复测：待执行；原B-Job.json保留。

## F-030：EditMode不能当作普通MonoBehaviour生命周期执行环境

- 日期：2026-10-25；R04-A首轮15项12通过，3种原投影模式测试失败，实际仍为On。
- 根因：测试先Configure捕获On，再在EditMode切换enabled期望OnEnable重捕获；普通非ExecuteAlways组件不会因此执行完整运行期生命周期。
- 处理：生命周期测试移到实际PlayMode，仍逐个测试TwoSided/ShadowsOnly/Off、远近、禁用与销毁恢复；不为了夹具在产品组件开启ExecuteAlways。边界纯运算保持EditMode确定性测试。
- 复测：A-Retest.json中13/13通过；三种模式合为一个PlayMode用例但逐种执行全部断言，覆盖不减。保留A-FirstRun.json。

## F-029：R04工具输入中的路径与补丁上下文错误

- 日期：2026-10-25；现象：查询UnityEditor XML时遗漏Managed/UnityEngine子目录；首代码补丁的最后一段使用不存在的文档上下文，整批校验失败且未写入。
- 处理：先rg --files定位真实XML路径；确认新代码文件尚不存在后，移除错误文档段并重新应用同一代码补丁。未运行错误代码，未改引擎/模板。
- 复测：真实XML位于Managed/UnityEngine；完整代码补丁成功。后续不猜XML路径，不将批量补丁失败当作部分成功。

所有后续任务开始前查阅；遇到卡顿、报错、异常或同类问题时优先复用已验证处理。不能把尚未执行的建议写成已验证修复。

每条记录：编号、阶段/模块、现象与证据、根因（或待证实）、处理、复测、适用范围。记录时间采用 Asia/Shanghai。

## F-001：Unity MCP Git 安装 URL 混入说明文本（历史经验）

- 日期：2026-10-24；范围：开发工具配置，早于阶段 1。
- 现象：Package Manager 检出失败；`Logs/upm.log` 中目标路径为 `MCPForUnity OpenUPM: openupm add com.coplaydev.unity-mcp`。
- 原因：Git URL 的 `path` 参数后误带入另一种安装方法说明；Git 将整段当作目录名。
- 处理：清单中的正确地址为 `https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#main`。仅复制 URL，不附带 OpenUPM 命令。
- 复测：清单解析正确，后续 UPM 日志的路径为 `MCPForUnity`、分支为 `main`。

## F-002：UPM Git 长时间无数据、Unity Registry 连接重置（历史经验）

- 日期：2026-10-24；范围：开发工具配置。
- 现象：Git 临时 pack 长时间为零；UPM 日志出现 `packages.unity.com` 的 `ECONNRESET`。
- 根因：当时网络路径异常/停滞；没有证据证明 Unity MCP 包本身损坏。
- 处理：从同一官方仓库下载 main 源码归档，仅将 MCPForUnity 包内容内嵌到 `Packages/com.coplaydev.unity-mcp`，保留原仓库地址。依赖工具 uvx 放在项目 Library；服务端匹配 10.3.0。
- 复测：744 个包文件逐一与官方源码归档字节一致；HTTP 服务健康检查和 MCP 握手通过，返回 30 个工具。后续发现 `Library/MCPForUnity/project-setup.done` 已存在；不能仅用该标记代替当前聊天的实时 MCP 工具连接验证。
- 注意：嵌入包优先于清单中 Git 依赖，后续切版本须处理嵌入包，不能只改 URL。Library 被清理后项目内 uvx 也会丢失，需要按工具安装记录恢复。

## F-003：URP 模板是 Forward+，不满足项目普通 Forward 约束

- 日期：2026-10-24；模块：S1-R01；类型：静态审查发现的配置差异，非运行异常。
- 证据：`Assets/Settings/PC_Renderer.asset` 的 `m_RenderingMode: 2`；本地 URP 17.6 源码 `UniversalRenderer.cs` 定义 `ForwardPlus=2`、`Forward=0`。
- 处理决定：用户确认保留模板，新建专用 Pipeline/Renderer/测试场景，首模块将专用 Renderer 设为普通 Forward。
- 复测状态：S1-R01 最终 T02/T03 及 Player 图像测试通过；专用 Renderer 为 Forward，原模板保持不变。详见模块实际报告。
- 经验：不要仅凭“Universal Renderer”名称判定 Forward；必须检查有效 Renderer 的渲染路径及画质/相机覆盖。

## F-004：PowerShell 下 rg 收到带通配符的字面路径

- 日期：2026-10-24；范围：文档前置审查工具。
- 现象：传入 `Assets/Settings/*.meta` 时 rg 返回 OS error 123，通配符没有按预期展开。
- 处理：将目录与过滤器分开：`rg -n 'guid:' Assets/Settings -g '*.meta'`，或读取已知的确切文件。
- 复测：改读确切 `PC_RPAsset.asset.meta`、`PC_Renderer.asset.meta` 后成功获得 GUID。
- 经验：路径查询优先 rg；不要把 shell 通配符展开当作跨平台保证。
- 2026-10-25 复用记录：R02 源码查询再次将包版本通配符作为字面路径时出现同类错误；已改用 Get-ChildItem -Filter 解析实际包目录，再向 rg 传入确定路径。
- 2026-10-25 R05复用：查询ProfilerRecorder时同样发现包路径通配符错误；查本条后改为向rg传入确定的Library/PackageCache目录并用-g过滤，查询成功。未修改工程配置。

## F-005：Temp 中的 MCP 验证脚本随 Unity 临时目录清理丢失

- 日期：2026-10-24；模块：S1-R01 前置诊断。
- 现象：再次调用历史 `Temp/verify_unity_mcp.py` 时返回文件不存在。
- 原因：Temp 是 Unity 临时目录，不能作为可重复运行工具的持久存储。
- 处理：将新的标准库 MCP 诊断入口放在 `Tools/MCP/client.py`；不依赖历史 Temp 文件。
- 复测：新入口握手成功，读取到 EchoStation 的 Unity 6000.6.7f1 实例及空闲 Editor 状态；脚本缺失不等同于 MCP 服务故障。

## F-006：MCP SSE 响应以保活注释开头，客户端误作普通 JSON

- 日期：2026-10-24；模块：S1-R01 创建资产。
- 现象：菜单调用结束后 Python JSONDecodeError；但 Unity 已打印资产创建完成日志，文件也已存在。
- 原因：较慢请求的 SSE 响应可能先发送 `: ping` 注释，不能仅以响应首字符判定是否为事件流。
- 处理：遍历行查找 `data:` 消息，并按请求 ID 选择真正的响应；不因客户端解码失败就盲目重复变更操作。
- 复测：修正后的客户端成功获取后续菜单回执、较慢的刷新结果和多轮完整测试结果；按 ID 选择 SSE 数据，不再因保活注释误解析。

## F-007：修改 Player Graphics API 后，当前 Editor 仍运行 D3D12

- 日期：2026-10-24；模块：S1-R01。
- 现象：首轮 EditMode 12 项中 11 项通过；T05 的运行 API 断言失败，实际为 Direct3D12，预期 Direct3D11。配置列表已经只有 D3D11。
- 根因：Player 配置和已经创建的 Editor 图形设备不是同一状态，修改列表不会原地重建当前图形设备。
- 处理：用户已明确允许在无未保存场景时以 `-force-d3d11` 重启。重启入口同时检查加载场景和 Prefab Stage 的 dirty 状态；发现未保存即取消，不保存或丢弃用户内容。
- 复测：新 Editor PID 28716，以 -force-d3d11 启动，重启后 12/12 通过；后续最终扩展测试 13/13 通过；保留 `EditMode-FirstRun.json`，不删除失败历史、不放宽 D3D11 断言。
- 重启尝试补充：同一工程调用 `EditorApplication.OpenProject` 后当前进程未退出、运行 API 未变化。改为用户已授权的干净场景退出，再由隐藏的等待助手在该 PID 正常退出后启动指定版本 Unity，并明确传入 `-force-d3d11`。助手 120 秒无退出则失败，不强杀进程。

## F-008：不能猜测 MCP 工具的 C# 源码路径

- 日期：2026-10-24；范围：S1-R01 测试工具发现。
- 现象：按工具名猜测 `Editor/Tools/ManageTools.cs` 时文件不存在。
- 原因：该工具由服务端提供，不一定有同名 Unity C# 文件。
- 处理与复测：使用 MCP `tools/list` 读取真实 schema；通过 `manage_tools` 激活 testing 组后正常启动并取回测试任务。后续先查文件列表或工具 schema，不按名称猜路径。

## F-009：异步编译/编译失败时可能继续调用旧程序集，菜单受理不代表动作完成

- 日期：2026-10-24；模块：S1-R01 重启/验证工具。
- 现象：菜单返回已尝试调用、干净场景检查记录已写，但原 PID 没有退出；后来明确读到新增构建验证脚本的 CS0266 编译错误，新的退出代码无法随程序集重载生效。
- 原因/边界：非阻塞刷新后过早执行菜单，以及编译失败时旧程序集仍可响应。曾怀疑后台 delayCall，但没有证据证明它是根因，不把该猜测当作 Unity 缺陷。
- 处理：脚本刷新后观察编译完成、实际 reload 和错误列表；`ready_for_tools=true` 也不能单独证明编译成功。退出在 dirty 检查后直接执行，重启动作由外部等待进程安排。
- 复测：外部等待助手在旧进程退出后启动 PID 28716；实际窗口/测试/Player 均确认 D3D11。工具回执仅作为请求被受理的证据。

## F-010：Unity 6.6 BuildSummary 错误/警告计数为 int

- 日期：2026-10-24；模块：S1-R01 独立 Player 验证工具。
- 现象：R01PlayerValidationBuild.cs 出现两条 CS0266（int 不能隐式转 uint）。
- 根因：报告 DTO 将 errors/warnings 定义成 uint，与本版本 BuildSummary 的实际属性类型不一致。
- 处理：DTO 改用 int；不以强转掩盖 API 类型差异。
- 复测：重新编译后错误列表为空；渲染测试通过，Windows 验证构建 Succeeded 且 0 errors。

## F-011：验证 Player 构建成功但存在环境/依赖警告

- 日期：2026-10-24；模块：S1-R01；构建结果 Succeeded、0 errors、4 warnings。
- 观测：两条 MCPForUnity.Runtime 的 UAC0005（AppDomain.GetAssemblies）、一条 Unity Services 项目未关联提示、一条 System.Windows.Forms.dll 在当前平台不受支持的构建警告。控制台与构建汇总分别保存，不能保证二者计数口径完全相同。
- 检查：项目 Assets/Packages 源码无直接 System.Windows.Forms 引用；ManagedStripped 内 System.dll、System.Configuration.Install.dll、System.ServiceProcess.dll 含对应元数据。准确依赖引入根因尚未确认。
- 结果：独立 Player 退出码 0，D3D11/HDR/SDR 验证通过；警告不能因此标记已修复。
- 后续：作为依赖/API 兼容性清理项跟踪；不为消除 Services 提示擅自关联云项目，也不在渲染首模块内自行切换 .NET 兼容性配置或修改第三方包。正式打包前必须完成兼容性调查与相关回归。

## F-012：URP 构建更新派生变体过滤字段，继承参数测试误报

- 日期：2026-10-24；模块：S1-R01；构建后回归 12 项中 T08 失败，首个差异为 m_PrefilteringModeAdditionalLight。
- 根因：已安装 URP 的 Editor/ShaderBuildPreprocessor.cs 在构建前调用 UpdateShaderKeywordPrefiltering 并保存资产。Runtime/Data/UniversalRenderPipelineAssetPrefiltering.cs 明确写入 m_Prefilter* 和两组 Light2D 派生字段，不能要求这些数据始终等于尚未构建的模板。
- 处理：这些字段仍完整记录，但不作为用户输入参数做继承相等断言；Forward/HDR、renderScale、光照/阴影/批处理等真实配置仍检查。增加 renderScale=0.5 的负向测试，证明真正漂移仍会失败。
- 复测：最终 13/13 通过，包含真实 renderScale 漂移负向测试；不修改渲染需求或实际渲染断言，保留失败的 EditMode-Final.json。

## F-013：PowerShell 中嵌套 Python 双引号不能使用反斜杠转义

- 日期：2026-10-24；范围：交付前 Markdown 链接与证据校验。
- 现象：长 python -c 参数嵌套正则双引号，PowerShell 在调用 Python 前报 ParserError。
- 原因：PowerShell 的双引号转义规则不同于 Python/JavaScript，反斜杠不是其通用转义符。
- 处理：多行验证代码使用单引号 here-string，经标准输入交给 Python，避免多层引号插值。
- 复测：6 份 Markdown 文档的本地链接均有效，13/13 与 Player 证据断言通过；该错误未修改 Unity 文件或既有测试结果。

## F-014：HLSL 的 line 是保留标识符

- 日期：2026-10-25；模块：S1-R02 测试图卡。
- 现象：D3D11 编译测试 Shader 报 unexpected token 'line'，并连带报告 float2 构造参数错误。
- 根因：测试字形行号变量使用了 HLSL 的 line 标识符。
- 处理：改为 glyphLine；创建模板入口增加 Shader 编译错误检查，不把 Shader.Find 能找到对象当作编译成功。
- 复测：修正后Unity错误列表为空；ShaderHasError断言通过，真实图卡正常渲染；Images-Retest.json 中21/21通过。

## F-015：EditMode 尚未渲染时 VolumeManager 未初始化

- 日期：2026-10-25；模块：S1-R02。
- 现象：首轮 20 项中 R01 的13项通过，R02的7项在 SetUp 报 CreateStack cannot be called before VolumeManager is initialized。
- 根因：EditMode 运行开始不保证发生过相机渲染，URP 所管理的 VolumeManager 尚未初始化。
- 处理：测试夹具先对隔离相机提交一次16×16真实URP渲染请求，让管线完成初始化，再创建独立栈；不手工篡改引擎初始化标记。
- 复测：Configuration-Retest.json 中20/20通过，含R02的7项及R01的13项；首轮失败保留在 Configuration-FirstRun.json。

### 工具查找补充（2026-10-25）

- F-008 同类：读取测试夹具 asmdef 时名称误写复数，随后用 rg --files 找到真实的 EchoStation.Rendering.TestFixture.asmdef；未改动工程。
- F-004 同类：再次把 PackageCache 通配路径传给 rg 触发 OS123；后续仅使用已解析的实际包目录或从 PackageCache 根目录配合文件过滤搜索。
- 工程没有 .git；git status 无法使用。保护验证使用文件哈希和序列化配置比较，不创建仓库。

## F-016：SingleCameraRequest 不更新 Volume，EditMode 帧号不前进

- 日期：2026-10-25；模块：S1-R02图像测试。
- 现象：21项中20项通过，实际图像测试失败；独立栈正确，但ACES/颗粒/色差图像差异全部为0；8次颗粒采样frameCount均为7。
- 根因：本地 URP 17.6 ProcessRenderRequests 的 SingleCameraRequest 分支直接调用 RenderSingleCameraInternal，没有调用 UpdateVolumeFramework；StandardRequest 分支执行完整 RenderCameras 流程。另外 EditMode yield null 不保证游戏时间/帧号推进，而URP颗粒随机偏移以Time.frameCount为种子。
- 处理：图像采样改用完整 StandardRequest，并在真正PlayMode中运行；参数单测仍独立执行。保持8个不同帧号、实际图像差异和Response断言，不以弱化断言通过。
- 复测：Images-Retest.json 中21/21通过，灰阶ACES差异2.0865、CA边缘差异0.003999、8个不同帧颗粒均有效且Response亮区差异减小，干扰Volume图像差异0；失败证据 Images-FirstRun.json 保留完整读数。

## F-017：验证图卡Shader在Player构建出现两项编译警告

- 日期：2026-10-25；模块：S1-R02；首构建Succeeded、0 errors、4 warnings。
- 现象：D3D11提示Glyph可能使用未初始化值、带符号整数modulus可能较慢；其余两条为已跟踪的Services/Windows.Forms依赖警告。
- 分析：字形函数动态索引本地常量数组，编译器无法证明所有路径的索引有效；字形序号使用带符号整数余数。
- 处理：字形改为初始化的位掩码分支、行号显式clamp，序号使用uint余数。只改验证图卡，不变产品Volume参数。
- 复测：第二次构建Succeeded、0 errors、1 warning（Services未关联），最终重构建Succeeded、0 errors、2 warnings（Services/Windows.Forms）；两条自有Shader警告消失，最终图像回归通过。首构建汇总保存为PlayerBuild-FirstRun.json。

## F-018：批处理Player的正常帧GPU计时无有效样本

- 日期：2026-10-25；模块：S1-R02诊断。
- 现象：首Player功能图像通过、退出0、FrameTimingEnabled=true，但360个候选正常帧均未通过有效时间戳/CPU/GPU非零检查；不能据此报告GPU耗时为0。
- 处理：保留Player/首轮数据；另以非batchmode窗口进程进行同样诊断，启动助手保持隐藏，不切换已确认的D3D11。若仍无数据，进一步记录原始计时字段，不伪造GPU测量。
- 参考：[Unity Frame Timing Manager](https://docs.unity3d.com/6000.0/Documentation/Manual/frame-timing-manager.html)；支持平台与计时有效性必须分别核验，不把支持声明当作本机数据。
- 复测：非batchmode但隐藏窗口同样GPU=0；补充原始字段确认CPU/时间戳有效、GPU为0。用户明确允许显示窗口后，最终取得279个有效CPU/GPU样本、81无效、0重复。可见/隐藏结果差异支持隐藏窗口缺少有效GPU渲染计时这一判断，不能推广为硬件不支持或GPU耗时为0。最终原始证据PlayerFinal/PlayerProbe.json。

## F-019：Player提示已剥离的禁用后处理Shader

- 2026-10-25 E12再次出现相同四条未启用DOF/Panini警告；已先查本条，保留日志。Windows实际波纹图像/普通帧计时通过，未为消提示改动第三方或开启多余后处理。

- 日期：2026-10-25；模块：S1-R02。
- 现象：运行日志四条Shader提示涉及Gaussian DOF（两次）、Bokeh DOF、Panini。三类效果按本模块批准要求关闭；ACES、颗粒、色差的实际图像全部通过。
- 本地源码证据：PostProcessUtils.LoadShader对shader.isSupported=false发Warning，相关Pass不执行。构建仅包含用到的Volume效果，不能因为该提示就认定全部后处理失败，也不能把提示隐藏后宣称无警告。
- 处理：保留日志，不为了消提示开启未批准的景深/Panini、关闭变体剥离或修改第三方URP。后续S1-P06正式启用景深时须重新构建并验证对应Shader保留与实际效果。
- 状态：功能未受影响，警告如实跟踪；尚未对引擎所有剥离路径做源码级根因证明。

## F-020：构建后初始化测试仍使用SingleCameraRequest导致空栈

- 日期：2026-10-25；模块：S1-R02；构建后回归14/21通过。
- 现象：7个配置测试的预热渲染报Render Graph Execution error；Editor.log的具体异常是SetupColorLut colorAdjustments cannot be null。图像测试与R01测试通过。
- 根因：配置测试预热残留SingleCameraRequest，管线重建后的默认Volume栈没有完成更新；与F-016同类。开始曾怀疑16×16尺寸，但具体异常指向空Volume组件，未把尺寸猜测当根因。
- 处理：配置测试预热也改为完整StandardRequest；不吞掉日志、不使用LogAssert.Expect规避异常。失败保存EditMode-AfterBuild-Failed.json。
- 复测：改用StandardRequest后7个配置测试与13个R01测试全部通过，Render Graph空栈异常消失；该轮图像测试另有F-022背景帧问题，未混记为完整通过。

## F-021：隐藏的窗口Player因失焦不推进帧

- 日期：2026-10-25；模块：S1-R02计时诊断。
- 现象：非batchmode隐藏进程初始化后没有产出报告，CPU无持续增长，项目原runInBackground关闭。
- 处理：仅带--r02-report的诊断进程在BeforeSceneLoad设置Application.runInBackground=true，不改项目PlayerSettings。识别自建测试进程的路径后停止该次挂起进程；保留PlayerWindowed初次日志。后续启动有超时并保存退出状态。
- 复测：后台运行修正后隐藏进程正常退出0并完成图像检查，但正常帧GPU仍为0；显示窗口经用户授权后GPU采样有效。未改项目runInBackground，不擅改图形API。

## F-022：失焦Editor的测试枚举器推进但PlayMode帧不前进

- 日期：2026-10-25；模块：S1-R02最终回归；20/21通过，8个颗粒样本均frame=2。
- 证据：ACES/CA/隔离图像数值有效，但不同帧号断言失败；此前同样代码曾21/21通过，不能视为稳定测试。
- 分析与处理：项目关闭runInBackground，工具运行时Editor经常失焦；图像测试临时打开Application.runInBackground，finally恢复。颗粒每次采样后等待真实frameCount变化，并设5秒超时；不把枚举器yield次数当游戏帧数。
- 复测：修正后构建前21/21、最终Player构建后21/21通过，8个不同帧号及颗粒/Response图像断言通过；失败保留EditMode-BackgroundFrame-Failed.json。测试finally恢复后台运行标记，不降低8个独立帧的要求。

### 2026-10-25最终工具记录补充

- 查询测试任务时一次填错job_id，MCP返回Unknown job_id；改用run_tests实际返回的1d5ccd21371443cb8a5f823804ef6b99后取得21/21最终结果。此为查询输入错误，无工程修改；后续从已保存回执读取ID，不手工猜写。

### 2026-10-25 R03烘焙环境日志

- 首次CPU烘焙完成、数据检查通过并恢复场景后，Editor.log出现Curl error 35 / UnityTls error code 7。与既有网络F-002同类；未见本模块烘焙失败，BakeResult.json为passed=true。作为环境网络提示记录，不为消提示关联云服务或修改TLS/全局网络设置。

## F-023：NUnit Is.Zero不等同于枚举的零值

- 日期：2026-10-25；模块：R03-A；首轮6项中5通过。
- 现象：动态代理StaticEditorFlags断言显示Expected 0 / But was 0仍失败。
- 根因：Is.Zero按数值约束比较，枚举类型的零值不应直接使用该约束。
- 处理：断言改为Is.EqualTo((StaticEditorFlags)0)，仍要求所有Static标记关闭，不修改代理配置。
- 复测：A-Retest.json中6/6通过；A-FirstRun.json保留首轮失败。

## F-024：动态受光图像夹具的投影与环境回退对照不充分

- 日期：2026-10-25；模块：R03-B。
- 现象：静态Lightmap A/B与49点SH都有效，但部分动态代理ROI为0；Off探针后图像仍有照明，整体图像测试未通过。
- 证据：B-FirstRun.json；代理真实截图在边缘被严重裁切；Off不是“清零所有SH”的控制变量，仍存在环境/默认光照回退。
- 处理：采样运行期显式固定Camera.aspect=16/9（不依赖编辑器GameView），记录并断言Viewport中心在画面内；保留Off实测值，新增CustomProvided全零SH的独立控制图，比较生产BlendProbes与明确零值控制，原有亮度/静态/连续性阈值不降低。材质PropertyBlock和相机宽高比最终恢复。
- 复测：待执行；此问题不要求重新烘焙，不把夹具假设错误当作烘焙失败。

## F-025：SH数组属性不适用于当前非Instanced的零值对照

- 日期：2026-10-25；模块：R03-B。
- 现象：固定16:9后九个代理ROI全部位于画内且有照明；但CustomProvided零值与Off回退读数相同，测试仍失败。
- 根因证据：本地CoreModule XML明确CopySHCoefficientArraysFrom生成7组Vector4数组供instanced rendering使用；当前是非Instanced MeshRenderer。
- 处理：零值控制使用7个同名标量Vector4全零，而非数组；切换每个受光模式后经过真实游戏帧再渲染；原PropertyBlock为空时用null恢复，避免残留覆盖影响SRP Batcher。生产代理仍为BlendProbes。
- 复测：B-Retest2.json仍失败，标量零SH也未令此MeshRenderer图像归零；不能把数组类型认定为唯一根因，也不声称该修正已解决。保留两次失败。后续改用直接清零实际bakedProbes的同一BlendProbes通路对照，原始93组SH最终恢复，同时断言插值系数与图像都归零；移除不可靠的CustomProvided测试分支。生产代码不依赖CustomProvided，未改第三方引擎。

## F-026：探针对照必须排除材质环境反射

- 日期：2026-10-25；模块：R03-B。
- 现象：B-Retest3的实际烘焙探针系数已全部清零（逐项插值检查0），代理仍有与Off相同的残余照明；因此不能把残余认定为探针未清零或CustomProvided失效。
- 源码证据：URP GlobalIllumination.hlsl的常规环境反射路径可采默认Cube；ENVIRONMENTREFLECTIONS_OFF才使用_GlossyEnvironmentColor，UniversalRenderPipeline.cs显式用ambientSH×reflectionIntensity计算此值。仅Renderer.reflectionProbeUsage=Off不等同于材质关闭环境反射。
- 处理：为动态受光验证代理提供独立URP Lit漫反射测量材质，显式关闭材质Environment Reflections，保留中性灰/Metallic0/Smoothness0.15。静态房间材质不改；这只是测试夹具隔离反射，不是替正式PBR场景禁用反射。材质资产显式引用进场景，保证Player保留对应Shader变体。
- 复测：B-Retest4中反射隔离材质配置通过但图像控制仍有残余，不能断言反射是唯一来源。继续检查逐帧环境探针状态；保持零SH数据与图像双断言，不把未经证实的分析写成根因。F-025类型差异也不能解释全部残余亮度。

### F-024至F-026补充诊断

- 初始只在构造时清零RenderSettings.ambientProbe，而测试跨过多个正常游戏帧；改为每次受控渲染前重新清零环境探针，并记录控制图渲染后的环境L0系数，检验LightingData/帧更新导致环境回退恢复的可能性。此为待验证处理，尚未声称已解决。

- B-Retest5确认控制时环境L0读数为0、探针27项系数也为0，但图像仍有残余。上述反射/环境属性假设均不足以完整解释残余，未定位到引擎内的全部来源，不能标记“全黑控制”修复成功。
- 测试设计修正：用户要求动态受光有效、移动连续；原“探针系数全零则整个PBR像素必须全黑”混合了其他照明分量，属于夹具额外假设。改为更直接的0/1/2倍烘焙SH三点因果对照：要求数据归零、九处像素逐级增加且增量超过0.0001，同时保持原路径非黑、49点连续性、灯关闭/Lightmap移除断言。残余照明单列为诊断限制，不冒充探针贡献；这是修正控制变量定义，不宣称未知来源已经解决。待实际三点对照复测。

- 最终B-Final.json图像测试通过；九处0/1/2倍像素均单调增加。x=-2.4m读数0.0802993/0.0993258/0.1198550，零系数及环境L0均为0。残余照明仍未完整归因，按上述诊断限制保留；不把“测试通过”写成所有假设均获证实。

## F-027：R03需要复用真实帧等待，不能只yield null

- 日期：2026-10-25；模块：R03-B；相关既有经验F-022。
- 三点对照B-Retest6已通过，但进一步审查发现切换SH/Renderer状态时仍只yield null，没有像R02颗粒测试一样保证Time.frameCount实际推进；这会令逐帧渲染器缓存的验证含糊。
- 处理：复用带5秒超时的真实帧等待；每个位置记录with/off/zero/double四个帧号并断言严格递增。恢复原探针与位置后再等真实帧拍最终场景图。保留三点因果、静态A/B与路径连续性断言。
- 复测：B-Final.json通过，各位置四个帧号严格递增；第一处3/4/5/6。残余亮度仍在，说明仅真实帧推进不能解释全部来源；没有将该假设标为根因修复。

## F-028：Player静态图像精确比较受继承SSAO噪声干扰

- 日期：2026-10-25；模块：R03-C。
- 现象：完整Editor28/28通过；首Player退出1，所有动态/贴图数据均有效，但静态灯开关图像平均差3.7684e-5，高于1e-6。原始记录保留PlayerFirstRun。
- 检查：R01保留的Renderer含活动SSAO，后处理Volume的Grain=0并不会禁用SSAO的采样噪声。
- 处理：量化A/B只在运行期暂时停用已识别SSAO Feature，保留原active值并在最终展示图与Dispose前恢复；不保存共享Renderer资产、不调SSAO参数，不降低静态比较阈值。测试显式传入实际RendererData引用，避免操作错误Renderer。
- 复测：最终Player退出0，static lightsDisabledDifference=0，SSAO隔离标记为true；构建后28/28通过，共享Renderer文件哈希仍与原快照相同。静态差异随运行期SSAO隔离消失，支持该采样干扰判断；未放宽1e-6断言或变更产品SSAO设置。

### R03最终跨平台对照补充

- 最终独立Player的零SH控制九处均为0，原值/双倍响应与49点路径均通过；Editor附加加载路径的残余依然存在。故F-024至F-026的未完整归因残余限定为Editor测试环境问题，不宣称所有环境一致或已定位到引擎根因。真实运行判定以独立Player证据为准，保留两类原始数据。

F061复测补充：P03/R01-BindingRegression 13/13、B-IsolatedFlashlight 1/1、E12-RestoreCycle1/1、最终RenderSafety/13份保护SHA通过。每次module_tests启动新增新鲜RenderSafety证据，空绑定/非Forward/HDR或API漂移立即拒绝启动。最初漂移产生时点仍未证实，后续继续监测，不把未知写成已锁定根因。

F051补充：P04字体字节/许可/哈希/元数据已成功保存，最后把版权©打印到Windows GBK stdout时报UnicodeEncodeError。文件UTF-8与字体数据没有失败；控制台改用ASCII转义JSON或python -X utf8，不重复下载/覆盖正确资产。

F064补充：A-JournalData五项通过，NativePreflight有实际空闲证明。测试/菜单共用Tools/MCP/preflight.py；只对唯一stale_status使用主线程回读，菜单助手同样保存原生预检，其原有错误门禁及各编辑器配置入口的干净场景约束保持。

F052补充：P05安全菜单脚本补丁因预期空行与原文件不同而被拒绝，确认整个补丁未应用后按实际结构锚点修改，未把工具成功/失败回执当作文件已更新。

F069补充（S2-W01B）：新Station测试访问Knowledge.IsReady，首次缺Memory.Integration直接程序集引用CS0012；保存B-CompilationErrors.log，补直接引用后再编译，未运行旧DLL。只看Station.Runtime引用不足以替代测试直接依赖。

## F-088：复用日志面板API必须按实际方法名

W02测试初次用了不存在的JournalPanel.Select，CS1061。查询实际源码为SelectEntry，修正后再次刷新；菜单Console门禁已拒绝旧DLL，没有创建或测试旧版本。CompilationErrors.log留存，后续先查接口再写调用。

## F-089：EnterPlayMode必须在测试根枚举器切换，失败前不得修改编辑器标志

W02首轮2/2在SyntheticInputScope报Requires PlayMode；嵌套Boot内yield EnterPlayMode没有完成预期模式切换。Boot在此之前设置Application.runInBackground=true，EditMode下连带PlayerSettings/磁盘runInBackground变1，旧Cleanup只在PlayMode还原，留下副作用。FailedFixtureFlags.json实测playing=false/background=true/playerSetting=true。按此前F021/F022明确的项目false基线还原并保存，RestoredFixtureFlags.json验证false；不将未知原因称场景故障。

把模式指令移到每个UnityTest根，Boot先断言已播放再改标志，Cleanup另加非播放失败还原分支。后续所有复用夹具遵循同样顺序；最终测试及磁盘/原生标志均须复查。

F087复测：W01 B-Retest实际3/3通过；F088/F089复测：W02 B-ValleyRetest 2/2通过，FinalFixtureFlags原生/磁盘背景false，工作区还原。

F069补充（P02B）：测试新增直接调用Commands/Integration/Knowledge三程序集，首轮缺直接引用CS0234。补三个测试asmdef引用并保留B-CompilationErrors.log，重新编译后才运行；生产终端代码未依赖LLM或新增包。

## F246 合并浮点坐标不能用小数字符串严格相等
R02a-1首轮Geometry用F4字符串集合，CombineMesh矩阵浮点误差跨过舍入边界（24.0108/24.0109）导致1失败；实际三区画面对照已过。改为材质/Atlas分桶逐角一对一匹配，位置50微米/UV5e-6容差，三角形角数完全相等；不放宽图像阈值、不改产品合并。重跑后记录结果。


## F247 美术布局夹具不能把未合批Renderer数量作为永久验收
静态合并142→37后Saved_A11/12，旧Layout仍要求每区>40Renderer，实测13。将已合并路径改为对照合并前独立Geometry回执的完整角数与输出组数；未合并路径保留旧检查。实际导航/光照11项已通过，重跑全部12项。


## F248 实例模型面数与原生计数采样
Decor_A7/9：LOD0原模型360顶点实际204三角形，不能按顶点数推算120；独立断言更正为真实输入204。实际开关图像差0.0061787且一次提交5实例，但SubmitRenderRequest返回的同帧UnityStats标准实例计数仍0，不能就此称原生计数通过。增加五个完整播放帧采样并保留所有原始计数，若仍0继续定位计数口径，不降验收。实际导航/六货物操作/扫描7项已通过。


F248补充：Decor_B配置通过，五完整帧UnityStats各实例计数仍0（图像差0.0061787），该口径在当前URP请求下不足验收。复用已在S1实测的FrameDebuggerEvidence，并在新模块读取实际事件m_InstanceCount/Shader/InstancedMesh；不修改旧工具。之前猜错FrameDebugger命名空间/类型后原生反射确认UnityEditorInternal.FrameDebuggerInternal，旧工具已用正确路径。


F248补充2：Decor_C实际FrameDebugger已记录InstancedMesh事件21，首详细读取仅末尾UberPost25有效，不能把GetFrameEventData false当作没有实例绘制。新版工具选中limit=index+1并同步重渲染该事件后再读详情，所有无详情行仍保留，finally还原limit/调试窗口/暂停状态。


## F249 正常域重载下新遮挡夹具共享字段空引用
Visibility_A2/3：真实货物SRP事件4→6、移动图差.00013433及PVS文件通过；遮挡测试正常域重载后Boot返回即camera字段Null，尚未开关图像采集。先重取实际已加载新场景/Camera并加阶段断言定位，不当作PVS失败、不修改产品。仍保留正常域重载及SessionState还原，后续以真实重测为准。


F249补充：Visibility_B仍在首行阶段空引用，重取场景并未触及断言，原先camera字段丢失解释未获证实。进一步怀疑EnterPlayMode完整域重载后的迭代器this未恢复；改为无实例捕获的静态UnityTest/Boot与测试域状态，所有运行状态在进入后初始化、SessionState选项仍finally还原。等待新实测，不改产品与PVS门槛。


F249定位：Visibility_D阶段日志证明Boot/Root/Camera都存在，精确报错在JArray a=null,b=null,c=null赋值；这些变量被后面回调lambda捕获，编译器在EnterPlayMode之前建立的DisplayClass在正常域重载后丢失。此前this/camera假设均未解决。去掉跨Enter的捕获闭包，改为静态Received回调＋普通局部引用；不取消正常域重载，也不修改产品。下一轮验证。


## F250 PVS对照必须先稳定离线请求并隔离旧穿墙标签
F249移除跨域闭包后Visibility_E终于完整采集：真实Opaque50→20、背向0，但图差.0001556超过1e-6，未验收。已看Off/On：前图仍有已禁用实例装饰和旧TextMesh穿墙文字，后图没有；不能把这些非静态控制差异说成PVS几何无损。按既有F077补Warm/Off/Off稳定基线，静态墙体因果对照中同时隐藏文本/货物/Echo，独立动态SRP测试仍保留真实物品。后续产品可读性须真实修复旧文本，不能仅靠本夹具隐藏。阈值不变，先重测证明。


## F251 新Editor模块直接使用PlayerInputReader需显式程序集引用
NextEffects首编译CS0234，Editor asmdef只有SalvageNext.Runtime的间接Player引用。沿用F069显式依赖原则，新增仅自有Editor程序集的EchoStation.Player.Runtime引用；Console门禁已阻止Prepare，没有运行旧DLL。重新编译无误后才准备资源/测试。


## F252 粒子自动归还要用实际生命周期验证
Effects_A2/3：真实新Rover扫描图差.0079965、一次全屏Pass、扣8.065电/信息权限/1000状态操作0B均过；Pool用Simulate(1)推进后只等两帧仍有1活跃，未当作自动回收通过。改用真实播放等待≤2秒（源duration.5＋lifetime.35），不模拟该阶段，超时输出IsAlive/playing/paused/time/count证据再定位；源实现/时间阈值和并发预算未放宽。此前并发第三拒绝、代次和24次复用已执行，仍需完整重测。


## F253 解除菜单阻塞不等于重新捕获指针
Effects_B生命周期真实等待通过，后续TryBurst期待true却false。查PlayerInputReader.SetUiBlocked(true)会释放指针，false不会自动捕获；这是正确CanPlay门禁。夹具增加未捕获断言，然后按实际关面板流程显式SetPointerCaptured(true)再测试距离/禁用，沿用F067焦点规则，不绕过产品输入条件。


## F254 直接移动子相机后需要还原其局部位置
Effects_C仍在池用例后段ExpectTrue失败；检查夹具远距离控制直接改camera.transform.position，后续Motor.RestorePose只恢复玩家/视角旋转，不还原相机localPosition。因此之后相机仍在远处，拒绝火花符合12m规则。夹具保存/还原localPosition再回近处，增加阶段说明；源距离/输入门禁不改。需要重跑池实测，不因前两项通过跳模块。


F138复用（R02c）：Effects_D启动前唯一既有Purchasing Editor TokenExchange空响应已完整存Effects_D_ExistingIAP.json，核对无C#或产品错误后清理该Console缓存，未生成测试Job，不改服务/网络/包。随后以同一未使用标签重走全部门禁。


## F255 依赖保护测试的断言括号错误
NextPackageTests新增Manifest深比较多一个右括号导致编译错误，尚未运行测试；修正自有测试语法并重新编译，仍需Console0和实际安装验收。包安装自身已成功，73旧锁条目版本/来源/Hash/依赖审计全部保持。


## F256 安装来源断言只用于新增Registry包
Package_A1/2：旧73锁条目和Manifest保护均过，SDK版本正确，但测试误假定URP/Test等全部旧Unity包也标Registry，实际存在BuiltIn。只对本次新增Addressables/SBP要求官方Registry，旧包依然验证精确版本与原锁来源/Hash/依赖保持；不是修改包或放宽安装版本。重测。


## F257 Addressables首次内容构建的可选报告模态会阻塞MCP执行
NextAddressables.ScheduleBuild已受理，但无回执/缓存输出，execute_code（含纯constant/显式CodeDom/get_history）返回false/null，不能归因于编译器或重复构建。原生UI实际看到Addressables Build Report要求启用Debug Build Layout。computer-use选择No保留原偏好，继续原构建，不重放Schedule。以后SDK首次构建若MCP空回执/未知状态，立即检查真实模态，优先复用F237；包构建完成与保存范围要单独核验。


F257操作补充：前两次UIA索引157均返回not available，未关闭弹窗；重新截图后用最新主窗口screenshot-0坐标(983,503)实际点击No，已观察弹窗消失并进入Compiling Scripts。此前文中的选择No以此步为实际完成，非索引尝试。构建未重复启动。


F257完成：原始构建回执Build_0b5f…为passed、1个1288B实际Bundle＋runtime settings，627.232s包含报告弹窗等待/首次脚本编译，不能拿这个值当流送加载性能。保存scope单独回执已生成；没有重启或重复构建。

## F258 真实分区包的首轮光照图对照几乎全黑
Presentation_A未通过：37Renderer的全局槽、Start后绑定、相机和独立探针均过，但第一区Shared与Partitioned图平均亮度仅1.07e-6。未把黑图当成功，不能因两图相同而通过。保留PNG及失败证据；下一步区分Bundle Shader变体与运行时光照数据：同一实际Bundle渲染器仅运行时克隆材质换回编辑器URP Lit再对照，不修改源材质/全局项目设置。尚未证实根因。

F258定位：Presentation_B运行时材质换回编辑器同名URP Lit后平均亮度.0092997、图差.0092987，几何/Atlas不变；Bundle的Shader AssetPath为空且isSupported=true，支持并不等于所需变体存在。检查官方SBP：GlobalUsage来自GraphicsSettings及场景使用；三个初始inactive视觉Scene没有LightingData，自动剔除无法推断运行时Lightmap。新增自有构建scope仅在内存临时设LightmapStripping=Custom，保留Plain/Directional；不写Graphics磁盘，finally验证原生JSON/字节/dirty完全还原。先独立测试成功/异常恢复，再真实重建并用原亮度/图差门槛重测；不修改SDK、不永久关闭所有Shader剔除。

F221复用：本轮读取两次猜测文件名未命中、Windows原生命令通配路径失败，均只读且无改动；改用rg --files/已有完整路径定位后再读，避免把失败输出当资料。

## F259 Manual保留了多余GI模式导致首次Shader变体编译过大
Build_29b…原生进度16:53 ForwardLit1486/5376，缓存持续增长非死锁。Custom仍继承DynamicPlain/DynamicDir/ShadowMask/Subtractive=true，超出新版只用静态方向光图/探针的需求。通过实际进度窗口Cancel取消本轮可重建内容构建，回执SBP ErrorCanceled/1043.95s，lightmap-scope原生JSON/字节/dirty与SaveScope均完全还原，无磁盘配置写入。保留已有Shader缓存。收紧自有scope仅Plain/Dir，其余四GI模式临时false，仍finally逐项还原六bool和模式；先重跑恢复测试再重建，不永久关闭全局剔除，不取消或删除旧版内容。

F259实际新进度：Build_0b920…ForwardLit总量从5376降至1344（窗口04:57时903/1344），证实范围确有收紧；缓存复用但仍需等待真实构建和图像验收。Sky若主窗口激活超时，当前编译进度是独立原生窗口，重新list_windows选择其ID，先get_window_state(include_text:true)再activate该窗口。截图主窗可能仍被Codex遮挡，进度自身的related截图可见；只依据实际进度截图操作。get_window_state不可两个include都false，空请求失败无状态变更。

F258补充：收紧变体的Build_0b920…真实构建passed396.49s，两个scope均恢复；Presentation_C三房图的亮度/原始两图差均已通过，但同帧Phase.Empty就断言Native Mesh/Map已销毁失败。不能把异步卸载句柄完成等同于原生引用当帧全部回收；参照b3a已验证做法，等待Resources.UnloadUnusedAssets和实际对象销毁（仍12s门槛），增加具名阶段与逐房图差文件，不放宽亮度/图差阈值。C整例仍失败保留，需完整重测。

## F260 新主场景接入异步流送后，旧探针夹具不能立即ExitPlayMode
Visibility_A6项中5通过：多Scene PVS实际NativeDraws36→20/背向0、图差0已执行；旧NextRoomPartitionTests的探针用例在Main卸载后立刻退出播放，SDK ResourceManager停止时晚加载回调进入SceneProvider.InternalLoadScene:131空引用，整组仍失败。Main现在含真实异步服务，夹具也需捕获其Owner，Main卸载后等Owner.IsClosed与NativeSweep/pump销毁，再ExitPlayMode；正常与失败TearDown都等。不修改SDK或忽略异常，不用LogAssert.Expect掩盖，重跑受影响6项。

## F261 新压力夹具误写Carry诊断属性
压力测试首次编译CS1061，SalvageCarry实际属性为LastMessage，不是LastError。门禁拦截，未运行旧DLL测试；只修正自有夹具诊断引用，再编译/Console0后执行。以后引用既有API先读取实际声明，避免按相邻类型猜名。

## F262 浮点三元表达式的可读性审查
新世界标签的条件与.006字面量连写成Target?.006，审查时主动改为括号与空格；未捕获对应编译错误，不能将其记作已证实的语法故障。后续检索已有已编译代码也有同类紧连写，原先“非法token”的判断缺少证据，现更正为可读性改动。真正编译错误见F261，不把格式改动当运行修复或测试证据。

F261补充（标签夹具）：编译门禁又发现两个引用错误：allowHDROutput实际属于URP UniversalAdditionalCameraData，而非Camera；旧Carry.Actor是internal，Next测试程序集无访问权。查官方已装源码/既有实现，改为GetUniversalAdditionalCameraData().allowHDROutput与本场景按ID查询CargoActor，不修改旧程序集可见性。尚未挂载/执行标签产品，修复后重新编译。

## F263 TextMesh.characterSize不是直接的字形世界高度
Labels_A2/3：真实原生字体重建1次、Atlas256→1024，图差9.12e-9/字体SHA保持；Main合并标记/手持隐藏/恢复均过。但遮挡夹具前景可读性失败（图差7.7e-6），已看实际图，文字过小。原实现.006/.008把characterSize误当实际字高。改按Noto原生fontSize与约1/10几何单位换算，目标货物/危险字高.14m、区域.18m；保持原图差/Bounds门槛，重新实图验证，不降低标准。字体重建用例改为可重复冷/热缓存：图集对象或尺寸改变必须有通知，始终验证匹配/实际图差/源SHA；冷A已实际1次证明不能当热运行也发生新重建。

F263完成：Labels_B3/3，真实墙后图差5.25e-8、旧材质.00030009、前景.0026961、单行Renderer.Bounds高.26064m，原生atlas事件1次256→1024/前后2.45e-7；已看Main货物图。一次重复write_stdin已完成exec会话返回Unknown process id，无项目改动，改用已有Job文件直接module_tests poll，B完整通过。

F263视觉复查：B标签已可读但三行字比工具箱本体更大；将目标/区域参数.14/.18收敛到.08/.14，保持所有原测试阈值，C完整复测后才继续。属新UI普通微调，不把旧B结果直接当修改后通过。

## F265 新显示后端缺少运行选项命名空间
首次编译CS0103，GameRuntimeOptions定义在EchoStation.Echo，而非Salvage；补实际命名空间后重新编译/门禁，不运行旧程序集冒充新后端验证。另PowerShell不展开rg参数内通配目录，后续用实际枚举目录或rg -g过滤。

## F264 参数与权限通过后仍需检查模型实际朝向
Hazards_A5/5技术用例过，但实图配电设备为立柱轮廓、管段Fan从接近方向看成侧面，尚不适合作为最终外观。改Computer控制台朝既有西侧Approach、Fan转向西、补原碰撞范围内底座与橙色指示条；不变Collider/Nav/权限，保存新备份并重跑全部受影响5项。另源ES_Light虽有HDR颜色，实际关键词缺_EMISSION，说明颜色有值不等于发光；新副本同时设非EmissiveIsBlack flags、关键字，再以实际图像验证，不修改源材质。


## F266 Windows验证辅助方法遮蔽枚举名
NextWindowsProbe.Key(Key)使方法体外Key.F10解析为方法，编译CS0119。辅助函数改名KeyPress，枚举保持UnityEngine.InputSystem.Key；不进入BuildPlayer直到Console无编译错误。

## F267 BuildPlayer回收使原管线Unity对象引用失效
首个e4fa构建本身Succeeded/0errors/116.388s，但scope还原Graphics原生JSON失败；磁盘/Player/Quality/dirty检查均保持，Lightmap scope通过。实际导出Graphics显示m_CustomRenderPipeline=null，原磁盘与新鲜前置均为S2。根因是只缓存原管线对象，BuildPlayer原生UnusedAssets后对象已失效。改为记录资产路径并在finally重新LoadAssetAtPath恢复；增加实际EditorUtility.UnloadUnusedAssetsImmediate用例，不能只沿用前两项未发生回收的测试。首次构建不记交付通过，模型副本仍只在新输出，修复后重新构建并核验原生/磁盘/dirty。

## F268 Unity构建服务将未变的UnityConnectSettings标脏
PlayerScope_B3/3全部被保存scope前置拒绝，原因UnityConnectSettings脏；没有运行配置变更。初次BuildPlayer服务警告将其标脏，而第一次scope接受证明构建前干净。导出原生JSON，与未变磁盘YAML全部字段/类型语义逐项比对完全相同（含每层字段集合），归档ValueComparison_A；再次核对当前JSON和磁盘SHA后仅ClearDirty，不保存或丢弃值。构建scope随后把UnityConnect加入原生JSON/磁盘/dirty快照并finally恢复，避免放宽未知脏设置门禁。

## F269 Windows显示的请求值不等于稳定接受
Windows_A显示脚本报告通过，但日志记录独占全屏DXGI失败/回退。原检查紧接Screen.SetResolution只读到请求状态，不能证明稳定原生切换。应用事务增加连续1秒匹配（3秒总应用预算不变），新增抖动状态用例；窗口验证区分独占成功与系统拒绝后还原，不把后者标成成功切换。原A显示结果仅中间证据，最终需重跑。

## F270 新场景完整读档路线目标瞄准失败
Windows_A startup/write通过，read在D处失败：玩家(25,0.03,11.91)，固定向东/35度不能确认真实PointedId。保留结果；新夹具改按实际CargoCollider中心计算视线，并为D采用原路线已有南侧站位，失败导出实际首碰撞与截图。不修改射线穿墙规则、扫描权限或货物碰撞；受影响跨进程/主路线/短路均须重跑。

F270第二轮：Windows_B display稳定/后处理、write通过，D在南侧站位(27,0,10)挡住机器人原Approach(27,0,10.5)，机器人停(27.06,0.02,10.86)，3秒无进展失败。这是夹具站位错误，不能改AI权限或忽略失败。实际D Collider中心(27,.23,12)、尺寸(.23,.45,.18)，A的固定朝向在z11.908擦过窄物体边缘；回到原西侧站位，仅按实际中心瞄准，保留空的Approach。先增加真实初始/读档后D扫描独立用例，再重建重跑。未提前认定修复通过。

## F271 URP未使用的Panini shader在Windows中被剥离
已装URP PostProcessUtils.LoadShader明确以Warning记录不支持的Shader并仅返回该pass的null材质。Windows_A出现PaniniProjection（本项目不启用）警告，不能误称整个Volume失效，也不能忽略实际启用的效果。保留日志，继续验证已用UberPost支持和实际后处理开关图差；不修改旧全局设置或为未使用效果关闭整个剥离优化。

F271复测：Windows_B实际UberPost支持、相机内部HDR/SDR，后处理开关全屏平均图差.0798009221，照片Post_On/Off已归档。Panini未使用警告与真正已用后处理失效有区别，保留日志，不把警告计作整个Volume失败。

## F272 Windows防火墙权限窗口阻塞测试帧
DRoute_A原生状态running/completed0，工具报告editor_unfocused/run_in_background=true/stuck_suspected。Sky只读截图发现Windows安全中心为本轮新exe请求公共/专用网络访问，覆盖桌面；不是模型或Nav死循环。电脑操作技能guidance明确禁止代操作安全/隐私权限弹窗，已请用户手动取消；不开放网络、不改全机设置、不通过其他工具绕过。未操作弹窗、不记本组通过。处理后先检查现有Job是否超时/清理，再用新标签复测，不能直接重放仍活跃的测试。

## F273 Sky截图状态不要整体写出
get_window_state默认自动显示截图；将整个返回对象nodeRepl.write会把截图data URL写成巨大文本。后续只输出accessibility/tree或短窗口元数据，图像仅用工具原生自动显示，不重复打印/解码/保存截图payload。

## F274 D扫描夹具重犯嵌套EnterPlayMode（F048/F199）
用户处理防火墙后，DRoute_A最终0/2、各180秒超时。查既有F048/F199与代码，EnterPlayMode仍在Run子迭代器，测试未正确切入播放；所以此前F272把失焦提示当作唯一阻塞原因不完整。防火墙弹窗是实际存在的独立系统权限事件，但不能据此推断测试根因。改为每个UnityTest根直接EnterPlayMode；Run首行断言Application.isPlaying，加载等待有20秒墙钟门禁，不提高总超时。回收/扫描/导航尚未在该失败组执行通过。

接续时当前干净场景已为用户选中的ES_S3_AI_Demo（selection同名），不强行覆盖为旧VC快照；建立R3R4_E记录最新布局，最终优先还原此快照。原D/VC快照保留。磁盘PlayerSettings SHA仍与3610保护基线相同、runInBackground=0；原生Application后台标记为true需核验/恢复，不能误称磁盘已损坏。

F274原生标志补充：DRoute_B正确进入播放后0/2，均在实际D扫描20秒超时，已经区别于A的模式切换故障。原生PlayerSettings唯一差异为runInBackground=true，磁盘仍精确基线false；修改该原生字段后全部JSON与构建前快照精确相同、磁盘未变，再清dirty，BackgroundRecovery_C保留完整证据。B未证明D可扫，后续增加结束时导航/传感器/输入真实状态诊断，不放宽门槛。

F274诊断记录：C只跑初始用例仍失败，try/finally包住嵌套Wait子迭代器没有产生预期诊断文件，不能认为已取到故障状态。将同一20秒等待直接放在Run内，先写实际状态再断言，避免子迭代器异常中断父迭代器记录。未改产品或提高超时。

F270真实根因细化：DRoute_D导出：timeScale1、输入focus/enabled正常且无UI/加载阻塞，Echo已初始化/受传统树控制；Scanner一直Idle、未收费。机器人停在(24.1656,.02,11.68)，目标(27,.02,10.5)，玩家站(25,.03,11.908)挡住门口；树因3秒无进展回Wait。除南侧占Approach外，西侧近物站位也挡入门路径。夹具发出指令后去(26,0,14)留通道，扫描后再返回中心瞄准取物；Windows版本必须真实Walk，不能靠传送宣称完整路线。先重跑两个独立用例，不更改产品寻路/碰撞/权限/超时。

## F275 窗口验证不能只等待既存Phase
Windows_C启动通过，display在原“15秒超时”记录之后失败，全进程18秒不足以证明此前所有切换加15秒已完成；错误保留“已取消…已还原”。原鼠标辅助只投递三帧后返回，没有核对实际onClick发生，Apply等待也可能读旧Phase。增加真实事件回执（只观察，不直接Invoke）、布局稳定200ms、每次新请求的Idle→Applying/Confirming与Requested一致检查，以及至少14.5秒确认计时门槛；不将原C视作完整显示通过。Windows_B仍为当时固定功能事实，最终修订需重新验证。产品未改，Gameplay_D先以a7b6构建独立验证回收/存档/性能；Editor编译仅发生在起始写档阶段，性能阶段须在编译结束后采样。

## F276 首轮Windows性能没有有效GPU数据
Gameplay_D write/read/route/alternate全部通过，performance在Low/zone0有效GPU帧不足即失败，全进程约5秒。原预热120帧在极高帧率/窗口被遮挡时不等于足够时间；未保留部分samples也妨碍诊断。改为10秒墙钟预热、每区1秒稳定，失败也保留已采样帧；之后用已授权可见窗口置前实测，不降低160/180有效帧门槛。无效GPU数据不能写成达标。

首轮Editor闲置资源快照实际在性能失败之后采集，原误命名Before已更正为EditorIdleAfterPerformanceFailure.json，不能以文件名伪称预先采集；其中last_compile_finished原生时间可供单独核对，最终样本仍须新鲜实测。

Perf_E诊断已保留180条：GPU实际152条非零、28条零、时间戳全唯一，未达到160条，不能说全部GPU为零；actual_player_foreground门禁通过。窗口17秒结束导致Sky选到的旧句柄已消失，无点击/置前操作成功，不把它记成UI助手故障。参考旧StationSession原有60FPS节奏，下一测量仅在验证作用域设置60FPS/vSync0，finally恢复，产品默认帧率不改；仍需原GPU非零/唯一门槛。Unity官方说明计时并非帧末立即可用：https://docs.unity3d.com/cn/2023.2/Manual/frame-timing-manager.html 。这是采用稳定节奏复测的依据，不是证明本机零值的唯一根因。

F276完成：Perf_F前台可见、53.078秒，9组全部180/180唯一非零GPU/CPU，共1620/1620；Profiler与临时60FPS/vSync0/输入均恢复。Unity已用内存峰599.1MiB、GPU P95最高3.441ms，本开发机不替代目标机60FPS。原始记录与PerformanceSummary_F保留。

## F277 负向筛选遗漏Fixture名称末尾
FullNext_A191项实际188通过、2失败、1忽略。原排除条件只匹配“类名+点”，Fixture组名以类名结束时也匹配了整体表达式，进而带入两个NextMeshMergeProbe旧准备探针；它们在Already merged守卫拒绝，未重新合并或保存产品资产。Bounded模型探针同样被选中，但明确Skipped:Ignored、No one-use explicitly admitted bounded experiment configuration，0.209秒，在创建Owner前退出，没有新模型实验。初次全回归仍记失败，不事后抹掉两项。

排除表达式改为类名后“点或字符串末尾”，先前计划排除范围不变，重新执行须188项/0失败/0忽略。尝试官方CancelTestRun时未先核验签名而传空参数数组，工具返回TargetParameterCountException、未取消；已查已装API定义，后续先查签名不猜参。该轮自然结束、Console0，保留全部原始记录。

## F278 临时播放选项值恢复但脏标记未恢复
FullNext_B正确筛选188项：184通过、4保存/构建scope测试被未知脏EditorSettings门禁拒绝。查NextVisibilityTests：为F249临时正常域重载，SessionState保存/恢复两个播放选项，却没保存dirty。A的Visibility在保存scope测试后留下脏标记，B前置如实拒绝，没有放宽保护或写旧设置。

导出B后原生EditorSettings，与未变磁盘YAML逐层所有字段/值比对（仅Unity原生null对象instanceID0与磁盘fileID0、布尔/版本等同一语义规范化）完全一致；再次核对当前JSON与磁盘SHA后仅清该未变脏标记，ValueComparison_B留存。夹具改为跨域SessionState保存全部原生JSON及原dirty；退出选项恢复后断言整份JSON精确相同，再恢复dirty。不取消正常域重载、保存门禁或渲染断言，先独立重测相关用例后再全回归。

F278独立修复完成：OptionsRestore_C3/3/17.956秒，后置原生dirty=false、enabled=true/flags1，Console0；原生JSON一致已在跨域夹具实际断言，保存保护未放宽。继续正确筛选188项全回归。

F278最终：FullNext_C188/188、零失败零跳过、36.685秒；未降低保护门槛，旧错误轮次保留。

## F279 原始设置候选的只读PC分段错误
只读生成恢复候选时，首个正则从Mobile块跨到PC块，AA计数断言拒绝，未写ProjectSettings；Graphics候选已精确原SHA保留。改用唯一name: PC起点到下一质量块/尾部的明确区间，仅PC AA0→4。最终Graphics与Quality候选整体SHA均等于任务前ProtectedBefore，才可后续恢复；不通过局部看似正确就覆盖未知用户改动。

## F280 交付收据的归档引用路径
最终只读汇总误将Restored_F.json放在R4-Final根读取，Get-Content报告文件不存在；没有修改资产或重跑恢复。用rg --files查到实际OriginalSettingsCandidates/Restored_F.json后读取并引用。交付材料使用实际归档路径，发行清单再逐文件核验，不从记忆拼接证据位置。


## F281 发行清单只统计许可证顶层
生成最终发行清单时len(notices)>=10断言拒绝：代码用iterdir只统计顶层文件，许可证实际含子目录，未写清单、未修改包内容。改为rglob递归并保留相对路径，重新核验同一包及646项产品SHA；不能为凑数量新增重复许可。最终DeliveryManifest_G实际通过才记清单完成。


## F282 反馈调研中的路径与页面查找失败
本轮只读调查猜测NextPost.cs/NextRoomLightingTests.cs等文件名而出现文件不存在；正确入口是NextPostProcessing.cs/NextRoomPresentationTests.cs，FrameCapture位于Salvage/Editor。已改用rg --files发现后读取。网上猜测的mixandjam Death-Stranding-Walking仓库404，Kenney部分页面受访问限制；不将未读到内容的仓库或页面当已核实推荐。没有因这些失败写入Unity资源。


## F283 亮度测试缺少频闪组件命名空间
NextBrightnessTests引用FaultLightFlicker时缺少EchoStation.Rendering，CS0246使宿主安全入口拒绝，场景未切换、未运行测试。补入现有命名空间后重新编译，空Console错误后才进入测试。该失败为测试代码编译错误，未影响已交付Player。


## F284 亮度夹具查找了错误的组件层级
Visibility_A在外部变更待刷新门禁拒绝，无测试启动；仅刷新资产后Visibility_B实际7项6过1失败。新亮度夹具从World根GetComponent<NextSession>为空，实际组件在玩家子对象，失败堆栈指向第57行。改GetComponentInChildren并显式非空断言，原6项既有后处理/渲染测试通过、Console0，独立重跑亮度与受影响用例，不降低图像门槛。

## F285 外部资源返回未通过ZIP签名门禁
Kenney UI Sci-Fi与Space Station官网下载链接获取后，urllib响应未通过PK签名断言，未落盘ZIP、未解压或导入Unity；作者页的CC0标记可确认，但原始包接收/内容仍未验收。LLMUnity GitHub API树请求25秒超时，改用已成功读取的仓库README作功能依据，不捏造commit或代码检查结论。


## F286 原生恢复片段扩展方法与URP AA副作用
恢复片段CodeDom未引入Newtonsoft扩展方法命名空间，JToken.Value<T>()编译拒绝，未执行修改；依赖的leave被误继续调度，布局已还原但绑定核验失败。改显式JToken转换。随后全字段比较发现仅Quality PC AA4→0和已知S2临时绑定差异，保护门禁先拒绝；已读取安装URP UniversalRenderPipeline.cs第389行构造时会写QualitySettings.antiAliasing，完整差异与磁盘原字节保留。只允许确认的三处临时字段差异后还原原始JSON，最终Graphics/Quality/EditorSettings native/disk/dirty全部精确相同、VC/AA4。再次leave走AlreadyOriginal只读核验通过，不保存/丢弃用户场景。

## F287 反馈02测试文件路径误读
只读查找NextLayoutTests时误用Editor/Tests目录，文件不存在；未修改资产/执行测试。随后用rg --files查实际Tests目录，不凭旧模块路径拼接。大批读取导致输出截断，后续只读取目标代码段，不能把截断部分当已读。

## F288 编辑器后台无响应与延迟执行
反馈02原生快照调用返回success=false/null，read_console随后ping无应答，系统Responding=false。没有重复执行写入或强制结束；经computer-use选中真实Unity窗口并前台激活后恢复，原排队快照才落盘。重新只读检查证实S3场景干净、编译空闲，才继续。未知调用结果先查文件/状态，避免重放造成重复写入；后台卡顿不等于操作绝对未执行。

## F289 地标警示横条下缘低于净空门槛
Dressing_A实际2项1通过1失败，检修间WarningRail中心2.15m、厚.12m，实际下缘2.09m低于2.1m。未放宽断言；中心调整为2.2m，下缘2.14m，再独立复测。失败保留V02/Dressing_A.json。资源官网下载旧直链已改为作者网页实际使用的download-link签名端点，三包ZIP/随包CC0均通过，未绕过权限或购买。

## F290 无测试Job时误轮询
Bake_A在域重载/状态不完整门禁拒绝，没有Job。随后误poll导致FileNotFound，未执行测试。改用新鲜状态/空错误/不追加编译刷新后新Bake_B，实际1/1通过。后续依赖操作必须检查前一步成功，不能将启动命令已退出当作测试启动成功。

## F291 几何/亮度断言通过仍存在视觉问题
第一次25.52秒烘焙和Bake_B1/1通过，但实际三房截图显示检修区标被线缆遮挡、排风机朝向侧面，整体仍偏暗；不能把自动阈值通过当视觉验收。保留Lit_Room0–2与FanProbe0–2，90°俯仰探针确认风机应面向室内。移区标到线缆前、风机转向；主烘焙灯提高至160/150/170、墙顶补光25，重烘焙独立Probes_B与Merged_B，再复测几何/截图/合并。A合并场景仅为本轮历史草稿，不作为正式入口。

## F292 生成记录与测试轮询记录文件重名
V04生成器写Partition_A.json后，同名模块轮询覆盖了该生成记录。真实场景与独立PartitionValidation_A.json仍在，不能伪造被覆盖的原始收据。后续生成器改为PartitionCreated_A.json，测试记录继续独立标签。

## F293 导航夹具错误假定播放场景只有一个根对象
Streaming_A实际6项5通过1失败；NextCommandPanel会创建独立EventSystem，导航夹具无筛选Single()报多元素。失败发生在stream引用赋值前，清理未等迟到加载结束便退出PlayMode，另触发Addressables SceneOp空引用。改按世界根名称选择，并在异常清理中从仍在场景的组件追回Owner后等待关闭；不忽略异常、不修改产品寻路参数。失败收据保留，再复测。

## F294 HUD实测中文行高与低电夹具参数
Hud_A 0/2。Noto中文24px实际行高35px，初版30px标签空间不足；扩大各区文本槽与信息卡并重新布局，不能只允许溢出或降低断言。低电Restore(18,false)违反既有低电告警快照契约，夹具改18,true；产品电量逻辑未改。旧失败保留，再跑实际UGUI图片/状态用例。
F290补记（HUD_B）：启动再次因域重载拒绝，无Job；误轮询产生FileNotFound。未运行测试。改新鲜就绪状态后用HUD_C，必须收到启动成功再轮询。

## F295 长时测试后磁盘临时管线绑定变化
反馈02打包前3610旧文件SHA检查除原三项授权例外外，Graphics/Quality各出现一行VC→S2临时绑定；与NativeBefore_A原字节逐行比较没有其他变化。保存来源未单独定位，不声称磁盘始终未改。最终须在全量回归后只恢复已确认的临时字段/原字节，再验证完整native+disk+dirty以及3610保护清单。构建19e0fe自身scope已逐项精确恢复构建前状态。

反馈02构建19e0fe为0错误/5警告：门框碰撞预烘焙未来兼容提醒、Unity Services未关联项目、两条验证代码DEVELOPMENT_BUILD弃用提醒、既有System.Windows.Forms平台依赖提醒。未据此升级包/启用服务。退出日志可见Unity网络Callback aborted/少量MemoryLeaks诊断，完整保留；实际应用错误标记与跨进程结果分别验收，不把警告抹去或宣称根因全部解决。

## F296 全回归中样式夹具依赖编辑器GameView尺寸
FullNext_A实际208项207过1失败，栈指向NextPanelStyleTests第28行display.Open；最后图片已完成任务板，尚未进入设置图片。失败后原生只读检查GameView为883×437，而UnityDisplayBackend.Capture构造的DisplaySelection最低高度480，会拒绝这个编辑器布局。独立U02时640×480故未暴露。呈现夹具改注入固定1080p只读显示后端，不改变真实显示边界/产品代码；Windows720/900/1080、原生点击、确认/取消/失焦/超时已另有真实通过证据。保留FullNext_A，复测样式及全回归。

反馈02最终复测：F291 Lighting_C4/4、F293 Navigation_B1/1、F294 Hud_D2/2、F296 Panels_B1/1后FullNext_B208/208。F295完整native/disk/dirty已恢复，3610保护仅原三项授权例外，证据NativeRestored_A/FinalWorkspace_A/Audit_Feedback02_Final_A。

## F297 Windows下rg路径参数中的通配符未展开

2026-10-31编写新手引导时，给rg直接传入`Assets/EchoStation/SalvageNext/Editor/NextArtV2*.cs`得到os error 123。改为传目录并用`-g 'NextArtV2*.cs'`筛选后正常搜索，无匹配不代表执行异常。本次只读检索错误，没有修改场景或产品代码；后续Windows检索通配文件名统一使用rg的glob参数。

## F298 玩法研究的未核实仓库地址与GitHub匿名API限流

2026-10-31猜测的DavidFDev/Unity-Interaction-System地址返回404，已排除，不作为有效推荐。尝试读取Unitystation、Mix and Jam、GOAP的仓库信息和递归树时均收到匿名API rate limit exceeded；PowerShell默认非终止错误还让依赖树请求继续执行，最终空files/null并非仓库不存在或没有代码。停止API重试，复用正常可读的作者站点/仓库README与许可声明，研究报告明确未完成代码树审计。后续此类脚本使用ErrorAction Stop，只有上一步成功且branch非空才执行依赖请求；优先查本文件已有GitHub共享限额记录，不申请账号权限或改变网络设置。

## F299 扩展研究的网页抓取限制

2026-10-31 Kenney Conveyor主站和两个背包仓库子目录返回restricted URL，Factory主站正文受robots限制；没有据此判断资源不存在或绕过限制。Conveyor使用作者自己的itch作品页确认CC0与用途，Factory仅注明官方搜索摘要、未完整正文核验；背包仓库README/实际LICENSE可读，子目录实现未审计。没有将错误响应保存为资源文件、下载第三方镜像或引入Unity。本轮复用F298经验，未重试匿名GitHub树API。

## F300 B01预检的错误文件名与Unity后台无响应

2026-10-31只读查询误用不存在的RenderSafetyGuard.cs；用rg发现实际入口为RenderBindingValidation.cs。初次execute_code返回success=false/null，Unity19848 Responding=false；未保存/丢弃场景或重启。按computer-use技能选取真实Unity窗口，第一次activate_window超时，刷新窗口后重试激活成功，原生干净场景与D3D11/VC绑定检查恢复，NativeBefore_A快照成功。一次截图与所选Unity窗口不符、显示Codex内容，未据此点击或键入；只重获Unity窗口并激活。后续查询未知结果先确认状态，禁止盲目重放写操作。

B01结果补记：刷新/域重载期间资源短暂no_unity_session，未将其当测试启动成功或追加编译请求；待新DLL、原生空闲及空Console恢复后启动Geometry_A，22/22通过。测试临时ES_Gameplay管线已完整native/disk/dirty还原，原S3布局恢复、3767保护0差异。恢复原VC后RenderSafety白名单为false是已知路径限制，不改白名单，也未在此状态继续启动测试。

F300补记：B03读取SyntheticInputScope时误把PlayerFixtures当目录，实际rg发现入口为Tests/PlayerEditor/SyntheticInputScope.cs。已改读实际路径，并复用其已验证的输入设置克隆/还原逻辑，避免另写不完整输入设置切换。无产品场景或配置写入。

F300补记：B05候选AtomicJsonStore.cs名称不存在，未继续按猜测路径读取；已有实际NextSaveStore严格编码/原子提交实现已读，Expansion据此在新自有文件适配独立格式与槽，原文件保留。

## F301 B03指针旋转测试未计入抓取格偏移

Ui_A实际9项8通过1失败：竖直1×3扳手抓取顶部(0,0)后顺时针旋转，按已约定保持抓取格在鼠标下，抓取格变为(2,0)。鼠标放在列3时左端应是列1，测试却仍要求左端列3；实际得到1。保留失败收据与截图，修测试固定断言为GrabX=2、最终X=1，并重跑同一UI/拖拽范围；不修改产品旋转行为或放置边界。

## F302 共享UnityTearDown协程嵌套退出播放模式

B05 Save_A实际16项12通过4失败，4项均为TearDown的“Nested enumerators are not allowed to yield ExitPlayMode”。共享B04清理协程包含ExitPlayMode，B05再yield这个协程，违反UnityTestRunner的退出约束。将共享清理限定为卸载自有场景/还原输入与时间；由每个最外层UnityTearDown直接yield ExitPlayMode。保留失败结果，重跑B05与受影响B04同范围，不改产品存档校验/回滚门槛。
