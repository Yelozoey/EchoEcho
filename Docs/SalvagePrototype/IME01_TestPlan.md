# IME01：旧回收版微软拼音修复

最终：本项已完成，本机微软拼音人工三项正常；实际结果见IME01_Input_TestReport.md与Native_C_Acceptance.json，自动17/17、Native_C exit0。下方为初始排查计划历史。此次实际验收聚焦单一T字段的原生输入/确认/发送/重开、焦点恢复与既有编辑/模态/终端回归；双文字框并存、延迟输入模块及真实键盘热插拔未单独执行，不宣称这些扩展覆盖或其他IME兼容性。

2026-10-31。用户已说“开始吧，一个一个来”。本次仅输入法：旧SalvageNext的T文字面板、微软拼音始终输入英文；完成独立测试/补丁交付后等待用户亲测，不继续LLM/训练/形态改造。

先使用相同GameTextInput的隔离Windows诊断，记录IME启用命令、焦点/键盘布局、组合与提交事件；定位后只修受影响文字输入。旧场景/存档/发行包保护，新补丁目录Builds/LLMShortTasks/IME01；内部Forward HDR/SDR、D3D11/1080不变。Windows构建临时22GiB、结束20GiB规则沿用，仅剥离本轮输出模型副本。

独立用例：真实焦点启用IME、输入模块延迟激活、应用焦点恢复、两个文字框交接、中文组合候选Enter不误发送/再Enter仅发送一次、Esc取消组合/关闭、中文长度限制及编辑/粘贴、关闭恢复原输入模块与IME、键盘重连。受控InputSystem事件与Windows原生微软拼音分别记录，不相互替代。最终受影响DialogueUi及NextCommandPanel/终端输入回归、原编辑器场景/native/disk/dirty恢复和旧包SHA核验。

当前：排查启动，未执行本轮测试，不记通过。原生输入无法由自动化有效复现时如实交用户复测，不能用注入汉字替代候选窗口验收。

诊断进展：Probe_A已实际构建并启动。真实焦点/中文布局/请求On均存在，但原生context=0、IME selected=false、compositionEvents=0；已复现失效状态。对照引擎Input.imeCompositionMode=On由用户在诊断窗口点击后复测，尚待结果。当前只有诊断产物，未修生产GameTextInput、未交付修复结论。
