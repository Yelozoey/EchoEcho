# S2 D02 文字输入和消息面板

D01独立通过后按A/B/C进一步拆分，每步通过归档再推进。

## A 输入系统和文字编辑

当前工程activeInputHandler=1，仅Input System。现有uGUI InputField从IMGUI Event.PopEvent读取键入，并通过BaseInput访问旧Input的IME状态。为在既定输入系统内独立验证中文字符/IME及编辑，新增基于InputField的GameTextInput：文字来自Keyboard.onTextInput，编辑按键来自同一Keyboard；不同时读取旧文字事件，避免重复键入。复用uGUI的光标、选择、复制粘贴及字数限制。

IME composition通过Keyboard.onIMECompositionChange提供给临时BaseInput覆盖；候选确认的Enter不作为提交。覆盖和设备订阅只在输入框活动期间存在，关闭/销毁后恢复原模块覆盖与IME模式；不修改项目Input设置或第三方源码，不要求切换Both或重启Unity。

A实际PlayMode用例：Input System文字事件输入中文、退格/箭头/选中替换、200字符限制、IME组合期间Enter不提交、明确Enter/Esc事件、关闭恢复输入覆盖及设备订阅。使用隔离工作区与既有SyntheticInputScope，测试后原配置恢复。

## B 面板和模态行为

玩家组件使用RuntimeUi既有字体/颜色/Overlay规范；T打开、Enter提交、Esc关闭。消息为左右区分的气泡，最多100行复用，中文正文和输入均关闭rich text。会话只提交玩家文本，D04再接执行器；不在UI层直接操作Echo或机关。

J日志/Tab控制面板与对话互斥，打开时释放鼠标并禁止移动/E/跳跃/手电快捷输入；关闭后需点击重新捕获。输入框聚焦时不将J/Tab/WASD/E/F解释为玩法操作。新消息在读到底部或玩家主动提交时滚到底部；阅读旧消息时保留位置。失焦禁止提交，禁用/销毁解绑。

B独立用例：真实T/Enter/Esc、中文消息与字面标签、空白拒绝、消息气泡/滚动/100行预算、焦点和各玩法键门禁、模态互斥、Session替换/生命周期；实际截图确认可读。

## C Windows 运行验证

建立独立测试场景与诊断构建，不覆盖S1包；1080p窗口验证实际输入链、气泡、滚动、中文截图与关闭释放，保留日志和退出码。诊断只在明确CLI启用，不作为产品默认运行逻辑。原生OS拼音候选及不同键盘的覆盖范围如未实测必须如实记录，不能以设置InputField.text代替实际输入事件。

2026-10-25 A4/4、B4/4、C最终Windows退出0并查看两张实际中文截图，均按序归档，见[汇总](../Tests/Stage2/Reports/S2-D02_DialoguePanel_TestReport.md)。35项保护SHA保持，原工作区/Gameplay绑定恢复。首夹具编译和Windows启动失败留档并复测。原生OS拼音候选及不同硬件键盘仍为覆盖限制；未将InputSystem事件验证写成OS输入法自动化验收。
