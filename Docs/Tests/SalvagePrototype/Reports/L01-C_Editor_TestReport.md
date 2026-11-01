# L01-C 完整场景Editor实际报告

2026-10-31，按用户最新策略只在编辑器验证，没有打包或启动Windows新版。

最终验收：用户在修复后的Editor重新Play复测，明确回复 **“中文候选、发送和重开均正常”**。本人原生输入验收通过；结合下述独立自动验证，L01完成并停止。人工原话与范围留存于Evidence/LLMShortTasks/L01/EditorIME_UserAcceptance.json。当前通信仍为脚本，未启动L02、模型训练或飞行改造。本次仅归档用户反馈，没有操作其正在使用的Unity会话。

本人首次Editor试玩未通过：用户明确报告“仍只能输入英文，无法出现中文候选”。上一版合成中文自动测试不能代替此结论。F326记录源码中的Windows Player专用条件；扩展为Windows Editor Play真实键盘路径后，EditorIME_A新测试实际1/1通过，4.1569379秒，Job8e32b5d85d25417e9495ee866abbc878。验证原生键盘接入、引擎IME为On、关闭恢复此前模式，未执行真实候选输入。

修复后EditorIME_Regression_B实际 **24/24**，23.4589478秒，Job4e66e330972941f1b58c726c0b1f3d7c，统一module_tests门禁/fullName核验。范围：最终面板11项（含真实键盘模式与还原）、完整场景2项、原GameTextInput4项、原DialoguePanel4项、IME生命周期3项。没有跳过/未决用例。自动结果与上方独立的本人原生候选确认分别留存。

EditorFinal_A初始化超时，completed0/summarynull/Console0，F325留档，不计通过。激活编辑器后Editor_B实际 **12/12**，18.0112363秒，Job0386a0f04ccc471a9e3cc4f146ba31ff：重跑最终版本面板10项，加完整环境场景2项。

完整场景实测：运行时自动且只挂一个通信面板；真实T键/中文字符/Enter通过uGUI、查询留面板、待命关闭面板并释放暂停；文字输入阻断画质/背包/R，库存与设备未被代操作。调查准星阀门后Echo实际导航到达、提交扫描，按已发现目标名读取报告，阀门仍未关闭。新EventSystem归属世界对象，未新增场景根对象或修改场景文件。

此为Editor Play的受控InputSystem事件、原生键盘模式及真实场景执行证据，不是Windows独立包、原生微软拼音候选窗口或目标机性能验收。此前IME01原生拼音仅Windows Game_B单独通过，不能移作Editor证明。A21项、Echo6项既有独立通过；Compatibility_A旧场景结构/界面2/2通过。

修复测试作用域EditorIMEFix已关闭：先regression_host恢复环境回收场景，再受限还原已知AA副作用，module_scope三设置完整native/disk/dirty精确一致。EditorIMEFix/FinalRestored.json passed=true；交给本人复测时环境场景已打开、干净且未播放，原VC默认/PC/实际管线、AA4、D3D11/普通Forward/内部HDR/SDR保持。Console_EditorIME_Final零错误；Budget_EditorIME_Final为19.667064/20GiB。未构建或启动新发行版，未修改项目输入系统设置。此为交付前快照，本次收到人工反馈后未再次读取或更改Unity当前状态。

EditorIMEProtection_After通过：3810个原源码/配置实际SHA检查仅两项明确修改（ExpansionEcho只读通信接口、GameTextInput编辑器IME）。原发行和模型仅核验存在且冻结基线后无写入，本轮没有重做旧二进制全量SHA，不将元数据核验冒充新的封包验收。
