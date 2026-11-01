# R4-R03b-4a 构建光照变体保护子项

最新BuildFlags_B2/2、1.213秒：六bool逐项恢复，构建中仅Plain/Directional=true，Dynamic两类/ShadowMask/Subtractive=false。Build_29b…因F259编译范围过大主动Cancel，1043.95秒/失败保留；实际取消finally的原生JSON/磁盘字节/dirty全部恢复，SDK无范围外写盘。已归档CancelledBuild_Console.json后才清理两条已知取消Console错误，未把取消当成功。B通过后才允许新构建。

2026-10-30，BuildFlags_A真实EditMode 2/2，.274秒，完整module_tests门禁通过。正常及故意抛出构建异常两条路径均验证：内存Custom保留Plain/Directional；finally原生Graphics JSON、磁盘字节、原dirty标志完全恢复。SDK保存仍受NextSdkSaveScope范围约束，未永久修改全局渲染设置。此测试只证明scope恢复，不代表真实Bundle修复已通过。

Presentation_A/B黑图失败保留；B相同几何/贴图仅换编辑器Shader后亮度从1.07e-6恢复.00930，定位包内Shader变体。旧包已归档BeforeShaderFix_BuiltContent.zip。下一步真实重建和原门槛重测，不能跳过。
