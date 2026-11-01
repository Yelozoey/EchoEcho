# Q3-A01a 有界已验证文本读取通过

2026-10-27，前置Q3隔离调优R01–R04通过，按Q3-A01_PackagePlan开始正式适配。Unity6000.6.7f1 / EditMode，新DLL233472B、编译/域重载完成；新鲜Scene/RenderSafety、空Console与空闲门禁通过。作业bb81397b304e491ca92dc2661f91c95b，实际8/8通过、0.471905秒。

VerifiedModelPackage.ReadUtf8旧调用默认8MiB不变；新增显式maximumBytes参数，只接受1..16MiB。依然只读取已验SHA并持有租约的文件、读取前检查实际长度、严格UTF8解码、同流复读前归零。未扩大模型总量1GiB或修改旧catalog/模型包。

独立测试使用微型非ONNX文件和9MiB文本，不加载任何模型权重。覆盖默认拒绝9MiB但显式16MiB可完整复读；中文/emoji小文件与写锁兼容、释放后可写；0/-1/>16MiB拒绝；超出调用方更小预算拒绝；非法UTF8字节C3/28、释放后调用拒绝；提高预算也不能读未验证文件。临时夹具位于核验绝对路径后的Library/EchoStation/S3Tests/Q3Text，只逐文件清理，不递归删除用户目录。

证据Evidence/S3-Q3-A01a/Text_A-Job/Text_A.json及Scene/RenderSafety。只通过读取契约；尚未安装/验证正式Qwen3包、分词或接入游戏。下叶才组装独立新模型包元数据与编译catalog，之后真实包后台全SHA测试。
