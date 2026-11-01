# Q3-A03b 正式ORT后端数值/生命周期通过

2026-10-27，依赖真实参考和19项新旧纯契约已通过。新增独立Qwen3OnnxBackend，显式新release租约、CPU4/inter1无自旋、只允许Owner创建/运行/清KV/释放，IO使用28/8/128契约及全部float有限检查；未改旧后端，不含Unity对象。

Unity6000.6.7f1，作业8bd577dac44f4b2b9917ad9252feb9c0实际2/2通过、33.030秒。Unity主线程创建被立即拒绝；真实Owner286全流程32.945秒、冷10.152秒。首轮1/16/64/669/1536各三步连续解码，之后四轮64长度各三步；共27次Run，全部logits与短输入全部56组KV逐值maxabs=0，所有贪心token精确匹配独立Python参考（预设阈值1e-4未放宽）。长KV仅完整shape/finite与最终logits对照，不宣称未存的长KV逐值参考。

每轮56个空KV成对释放，输出集合分别15/3/3/3/3全部创建/释放成对，disposeThread均Owner286，主线程native0。每例Reset后Consumed=0；空prefill、负token、无prefill Step、非法CPU预算不进入native，外线程Dispose拒绝，释放后访问拒绝、Owner重复Dispose安全。Owner最终清理完成；主线程无Join/Abort/Dispose。

证据Evidence/S3-Q3-A03b/Backend_A-Job/Backend_A.json、安全记录及Backend_639286642716000665.json，独立参考SHA246e42b93d0b80c5eee890cf024093f140259ba250fa7b7c1dde87d782f04b4d。正式后端已通过数值/所有权；尚未接入获选v5实际准备、完整生成、事实守卫或游戏/Windows/目标硬件性能，不冒充S3交付。
