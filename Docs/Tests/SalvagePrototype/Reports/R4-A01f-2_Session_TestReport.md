# R4-A01f-2 新槽真实场景生命周期

2026-10-30，Session_A2/2通过、3.127秒，module_tests／NextSessionTests。实际加载新版三区场景，独立新槽写读；恢复已发现B、50条历史、HighScore／Low、Wait基础模式，进行中的调查已取消、代次推进、UseLLM保持false。原SalvagePersistence.Update禁用，只复用其Capture／TryRestore，不调用旧Save／Load。

非法几何快照与坏文件均拒绝：任务Generation／新Epoch／100条实时历史对象与序列化世界／显式设置保持不变，坏文件原字节保留。使用GUID隔离测试槽，不写用户旧槽。编译期F235修正不扩大内部传感器写权限。真实文件、场景恢复与取消通过不等于Windows跨进程与最终画质验收，后续仍需完整构建复测。
