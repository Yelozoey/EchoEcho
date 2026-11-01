# S3-T01c v5新集C未达原始语义门槛

2026-10-27，Quality_C_A作业87fe240423bb41eeafa74139304c775e，真实测试0/1失败，465.0153635秒；原始100次全部完成/后台释放，JSON100/100、意图+目标89/100，低于90门槛，**T01未验收，S3仍17/24**。未放宽门槛、不把权限守卫纠正算模型正确。

原始`Evidence/S3-T01c/Game100_639286770254424229.json`，C集SHA55cfbb8fb89bd94ba6ef9dad2683c20b51edcfdfc45734f0352daff7639bc21b；Prompt v5 SHAf3524a85ad232bd4077da4237280e778d5b6cc3bc0fcc95d8c069480973bcbe1。冷11.268秒，完整请求nearest-rank P50/P95=4.1916964/6.0104938秒；Owner/Native1240，1947次Native均后台，completed/cleanup均true。

错误11条：3条停止/解除跟随误Follow；2条危险提醒误调查回路笔记；6条终端代输/按钮/操作终端/启动通风/开门误Answer或Follow。部分raw声称已输入代码/允许代启动，不能外发；J02仍应拒绝这些操作，T02须重放。未发生任何游戏派发，本轮只测原始模型。当前不更换模型/训练/上云，按用户最新授权迭代Prompt。

后续：C集转为诊断集，v6先改善停止/代操作/危险对象优先判断，保留v5可玩实现；完成预算/版本接口独立测试后真实诊断。若修改基于C，必须另冻结新的D集独立验证，不能复用C自称独立质量达标。资源/六意图/严格JSON/三层/权限/时间门槛保持。

Memory_C_A.json为OS whole-Editor采样，启动时模型已加载：maxPrivate6235852800B、WS3519266816B、晚启动delta65536B；**不证明AI增量RAM**。最低/推荐目标PC60FPS仍须对应实机。Unity6000.6.7f1 / Windows / D3D11普通Forward内部HDR/SDR，开发机i7-12650H/RTX4060 Laptop/16GB。
