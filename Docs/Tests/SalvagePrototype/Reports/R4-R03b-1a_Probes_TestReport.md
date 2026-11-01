# R4-R03b-1a 独立探针资产

2026-10-30，Probes_A2/2，0.328秒，完整module_tests门禁/Console0。

固定Unity6000.6.7f1实际支持LightProbes实例克隆与独立CreateAsset。派生ES_Next_Streamed_Probes保留180位置、全部4860个SH系数、840四面体单元；持久化重导入后仍匹配。资产依赖图仅自身，不含旧LightingData或任何Lightmap贴图。

关闭源单场景，在空宿主中单独设置该资产，三个世界点的27个SH系数与源场景采样最大差0。finally恢复原探针。源场景/Nav/GI没有改动。

此项只证明探针可脱离整个GI数据，尚未从正式主场景移除旧GI，也未实现三区异步流送或逐区Lightmap释放。
