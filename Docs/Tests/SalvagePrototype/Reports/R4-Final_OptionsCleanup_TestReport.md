# 回归环境播放选项清理

2026-10-30，OptionsRestore_C 3/3、17.956秒，原生PVS/动态SRP用例完整重跑。跨正常域重载后，两项播放选项、全部EditorSettings原生JSON及原dirty都恢复，后置dirty=false/enabled=true/flags1，Console0。

修复只在Editor测试夹具，产品代码与Windows构建不变。FullNext_A筛选错误带入一次性探针，FullNext_B4项保存scope受遗留脏标记拒绝，失败记录均保留；此次独立清理模块通过后再执行正确188项全回归，不把B的184通过当全通过。
