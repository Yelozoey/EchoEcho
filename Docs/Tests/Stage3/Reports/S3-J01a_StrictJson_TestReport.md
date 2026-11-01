# S3-J01a 严格解析、Schema与离线校验

2026-10-26，通过：Unity64/64，1.691秒，job `2a33fa2f393b45f3a8fc5b12b3d8dd4d`，J01a_A；独立Python61/61（PythonParser_C.json），两语言从相同UTF16单元恢复原始输入，无JSON库替换坏代理项的夹具误差。a通过后进入b结构约束，J01整体未完成。

Unity6000.6.7f1/Win x64/D3D11/1080p普通Forward内部HDR-SDR、i7-12650H/RTX4060Laptop/16GB；最低i5-8400/GTX1060 6GB/16GB中档1080p60及推荐i5-12400/RTX3060 6GB/16GB高档1080p60不在纯解析测试验收。干净场景/Console/实际RenderSafety通过。

入口module_tests.py，assembly EchoStation.AI.Editor.Tests，group `^EchoStation.Tests.AI.IntentJsonParserTests`；Python `python -X utf8 Tools/Stage3/validate_intent.py --self-test --report 新路径.json`。独立单文件可用`python -X utf8 Tools/Stage3/validate_intent.py intent.json`，无网络/外部依赖。Schema位于Docs/Stage3/intent.schema.json。

61项中16合法、45非法，完整清单位于Evidence/S3-J01/ParserCases.json（display及utf16Units可重放）：六意图、任意字段顺序、JSON空白、日期字符串、合法转义/Unicode对、280字符/140补充平面字符、64字符ID和4096字节边界接受；围栏/数组/双对象/注释/尾逗号/单引号/无引号/未知或错大小写/重复及转义同名键/多余或缺失字段/null及错类型/空白台词/281单元/141补充字符/坏ID/截断/NaN/裸控制字符/孤立代理项/BOM/4097字节拒绝。另3项验证精确DTO字段、固定兜底原句、六个封闭对象Schema分支。

复用已验证MemoryJson.ValidateLexemes挡JSON.NET宽松语法，读入前再验Unicode代理项，DuplicatePropertyNameHandling.Error，DateParseHandling.None，MaxDepth8。只输出纯值StructuredIntent，不引用Echo/Unity执行API。Schema标准maxLength按Unicode标量，与C# UTF16不同，附加x-maxUtf16Length注解并由运行校验器严格执行，不伪称标准Schema自身能表达该差异。

未修复/抽取任何模型raw；F120仍为真实模型格式失败，待b/c真实结构约束验证。语义/事实/权限和游戏行为尚不算通过。
