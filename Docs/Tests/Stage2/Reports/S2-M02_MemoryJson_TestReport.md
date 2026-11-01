# S2 M02 记忆 JSON 测试报告

2026-10-25，通过。M01 独立通过后新增严格 JSON 数据编解码，复用已安装 Json.NET 13.0.2，没有升级依赖、场景接线或磁盘访问。

Unity 6000.6.7f1、Windows 11 开发机（i7-12650H/RTX 4060 Laptop/约16GB）。前后场景 ES_Cabin_Greybox 干净；普通 Forward、内部 HDR/SDR、D3D11、默认/PC/实际 Gameplay 绑定保持，Console 后置零错误。目标最低/推荐硬件及 1080p 60 FPS 基准沿用总文档，本模块没有渲染性能验收。

## 实际执行

`module_tests.py --evidence Docs/Tests/Stage2/Evidence/S2-M02 --label B-JsonAndContract --assembly EchoStation.Memory.Editor.Tests --group ^EchoStation.Tests.Memory`

**26/26 通过、0 失败、0 跳过，框架用时 0.5390876 秒**；其中 M01 回归 14 项、M02 新增 12 项。首编译因测试缺 Json.NET 直接预编译引用失败，实际 .rsp 对照后补显式引用，记录 F083；另一次启动遇域重载未完成被门禁拒绝，没有启动或保存场景，重读实际空闲后重试。

| 用例 | 具体操作与实际结果 | 状态 |
| --- | --- | --- |
| T01 | 三类中文/引号/换行、顺序、时间、密码/标记/空可选字段往返后 JSON 相同 | 通过 |
| T02 | 空记忆库往返，null 编码拒绝 | 通过 |
| T03 | 改密码但不改校验、错误校验串均拒绝 | 通过 |
| T04 | 封装/数据/三类条目各删必要字段或加未知字段，重算校验后仍拒绝 | 通过 |
| T05 | 根与嵌套重复字段均拒绝 | 通过 |
| T06 | 时间类型/负数、非法枚举、关联 ID、超长摘要、数组类型均拒绝 | 通过 |
| T07 | 不连续编号、事件/线索/角色重复 ID 均拒绝 | 通过 |
| T08 | 版本 2 和超 long 正整数未来版本即使带未知新字段也返回 UnsupportedVersion；0/字符串版本非法 | 通过 |
| T09 | 空/非 JSON、多根、前导零、错误标点、无引号/单引号字段、注释、NaN 拒绝 | 通过 |
| T10 | 2MiB UTF-8 与 16 层深度预算超限拒绝 | 通过 |
| T11 | 512/128/64 满容量，120 字符摘要及已知事实往返；额外条目拒绝 | 通过 |
| T12 | 后段非法和外部类型元数据均不返回部分库；已存在的原存储不变 | 通过 |

## 证据和边界

[实际原始结果](../Evidence/S2-M02/B-JsonAndContract.json)、前后 SceneSafety/RenderSafety、Post-Console、A-CompilationErrors.log 均保留。本模块返回新的隔离存储，不会直接修改运行中的记忆；SHA256 只用于损坏检测，不是加密或防作弊。M03 再验证真实磁盘与备份，M07/F03 再验证应用与全进度一致性。

M02 独立通过，当前 2/30，可进入 M03。
