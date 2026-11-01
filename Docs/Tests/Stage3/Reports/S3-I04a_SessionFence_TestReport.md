# S3-I04a 会话围栏

2026-10-26，通过11/11，1.581秒，job `15ca8f12738c43bcb967941b76f8657f`，证据`Evidence/S3-I04/I04a_A*`。a通过后进入b宿主，不算I04整体完成。

Unity6000.6.7f1/Windows x64/D3D11/1080p普通Forward内部HDR-SDR，开发机i7-12650H/RTX4060Laptop/16GB；最低i5-8400/GTX1060 6GB中档60及推荐i5-12400/RTX3060 6GB高档60的1080p目标未在该数据模块验收。

独立入口module_tests.py，assembly EchoStation.AI.Editor.Tests，group `^EchoStation.Tests.AI.SessionFenceTests`。干净场景/Console/实际RenderSafety通过。

8个生命周期原因逐个证明：先签发/取走结果，再NewGame、LoadGame、ManualOverride、WorldChanged、Ending、SceneUnloaded、LLMDisabled、LLMEnabled，旧戳均失效，新戳有效；新局/读档换session，普通世界修改只递增revision；即使世界不变，新请求也使旧请求失效；未知原因抛错且不修改当前戳，默认戳无效。无Unity对象或执行接口。实际挂接生命周期和目标重验仍属后续集成模块。
