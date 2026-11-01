# S1-E13 粒子池模块验收

2026-10-25，A契约3/3、B真实播放/生命周期3/3依序通过，A-PoolContract/B-ActualPool.json留档。真实四个64粒子burst峰值256，第5个请求拒绝，非循环系统自然结束后预算回到0并可复用。两个池全局并发仍最多4；旧租约不能停止复用的新实例，禁用池释放预算。1000轮共4000次播放/停止，预热对象始终8→8，未扩容。单独销毁池组件后预算0、拥有实例清除、旧租约失效。

实现ParticleEffectPool/ParticleLease/ParticleBudget，可在Inspector指定符合契约的ParticleSystem预制体、容量1–4，或运行期Initialize一次。TryPlay成功返回租约，Stop只影响同代实例；不活跃/超预算/非法空间参数返回false。Update自然回收，OnDisable清场，OnDestroy清预热对象。每系统64、全局4系统的理论粒子上限256，禁止循环/自动播放Destroy/子粒子/子发射器/碰撞与触发回调来绕开池。请求过程不Instantiate。

ActualParticleBudget.json是峰值/第5个/自然回收证据；PoolStress.json是全局/租约/4000次/对象数证据，其中其他共享DTO的默认字段不属于该用例断言。ProductParticleAudit.json对保存场景/Prefab扫描无粒子组件；未额外向产品加入效果。后续有实际粒子需求必须接入本池和独立视觉验收。

Unity6000.6.7f1、普通Forward内部HDR/SDR、D3D11；当前Win11/i7-12650H/RTX4060 Laptop/16GB。最低i5-8400/GTX1060 6GB/16GB中画质及推荐i5-12400/RTX3060 6GB/16GB高画质1080p稳定60FPS尚未实机验收；本模块不把测试夹具当作产品效果或完整性能达标。
