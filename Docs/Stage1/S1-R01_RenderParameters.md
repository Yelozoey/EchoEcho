# S1-R01 渲染参数逐项记录

生成器：Tools/Rendering/write_r01_inventory.py。以下为实际序列化值，嵌套值完整保留在同一行。

包含R02的HDR调色/中性Profile以及R04获授权的三个阴影输入（回归契约v0.3）；其他继承字段仍不代表最终画质或性能验收通过。

Unity 6000.6.7f1 会迁移旧模板字段。T08 比较加载后的有效属性和 Renderer Feature 参数；T07 单独保护原始模板文件哈希。

## ES_PC_ForwardHDR.asset

| 对象/字段 | 当前值 | 状态 |
| --- | --- | --- |
| `11400000/m_ObjectHideFlags` | `0` | 继承值，尚未验收 |
| `11400000/m_CorrespondingSourceObject` | `{fileID: 0}` | 继承值，尚未验收 |
| `11400000/m_PrefabInstance` | `{fileID: 0}` | 继承值，尚未验收 |
| `11400000/m_PrefabAsset` | `{fileID: 0}` | 继承值，尚未验收 |
| `11400000/m_GameObject` | `{fileID: 0}` | 继承值，尚未验收 |
| `11400000/m_Enabled` | `1` | 继承值，尚未验收 |
| `11400000/m_EditorHideFlags` | `0` | 继承值，尚未验收 |
| `11400000/m_Script` | `{fileID: 11500000, guid: bf2edee5c58d82540a51f03df9d42094, type: 3}` | 继承值，尚未验收 |
| `11400000/m_Name` | `ES_PC_ForwardHDR` | 项目专用身份/绑定 |
| `11400000/m_EditorClassIdentifier` | `` | 继承值，尚未验收 |
| `11400000/k_AssetVersion` | `13` | 继承值，尚未验收 |
| `11400000/k_AssetPreviousVersion` | `13` | 继承值，尚未验收 |
| `11400000/m_RendererType` | `1` | 继承值，尚未验收 |
| `11400000/m_RendererData` | `{fileID: 0}` | 继承值，尚未验收 |
| `11400000/m_RendererDataList` | `- {fileID: 11400000, guid: 1bf06c036c4ff8f42957c6d96f6e065a, type: 2}` | 项目专用身份/绑定 |
| `11400000/m_DefaultRendererIndex` | `0` | 继承值，尚未验收 |
| `11400000/m_RequireDepthTexture` | `1` | 继承值，尚未验收 |
| `11400000/m_RequireOpaqueTexture` | `1` | 继承值，尚未验收 |
| `11400000/m_OpaqueDownsampling` | `1` | 继承值，尚未验收 |
| `11400000/m_SupportsTerrainHoles` | `1` | 继承值，尚未验收 |
| `11400000/m_SupportsHDR` | `1` | R01 内部 HDR 已开启（1） |
| `11400000/m_HDRColorBufferPrecision` | `0` | 继承值，尚未验收 |
| `11400000/m_MSAA` | `1` | 继承值，尚未验收 |
| `11400000/m_RenderScale` | `1` | 继承值，尚未验收 |
| `11400000/m_UpscalingFilter` | `0` | 继承值，尚未验收 |
| `11400000/m_FsrOverrideSharpness` | `0` | 继承值，尚未验收 |
| `11400000/m_FsrSharpness` | `0.92` | 继承值，尚未验收 |
| `11400000/m_EnableLODCrossFade` | `1` | 继承值，尚未验收 |
| `11400000/m_LODCrossFadeDitheringType` | `1` | 继承值，尚未验收 |
| `11400000/m_ShEvalMode` | `0` | 继承值，尚未验收 |
| `11400000/m_LightProbeSystem` | `0` | 继承值，尚未验收 |
| `11400000/m_ProbeVolumeMemoryBudget` | `1024` | 继承值，尚未验收 |
| `11400000/m_ProbeVolumeBlendingMemoryBudget` | `256` | 继承值，尚未验收 |
| `11400000/m_SupportProbeVolumeGPUStreaming` | `0` | 继承值，尚未验收 |
| `11400000/m_SupportProbeVolumeDiskStreaming` | `0` | 继承值，尚未验收 |
| `11400000/m_SupportProbeVolumeScenarios` | `0` | 继承值，尚未验收 |
| `11400000/m_SupportProbeVolumeScenarioBlending` | `0` | 继承值，尚未验收 |
| `11400000/m_ProbeVolumeSHBands` | `1` | 继承值，尚未验收 |
| `11400000/m_MainLightRenderingMode` | `1` | 继承值，尚未验收 |
| `11400000/m_MainLightShadowsSupported` | `1` | 继承值，尚未验收 |
| `11400000/m_MainLightShadowmapResolution` | `2048` | 继承值，尚未验收 |
| `11400000/m_AdditionalLightsRenderingMode` | `1` | 继承值，尚未验收 |
| `11400000/m_AdditionalLightsPerObjectLimit` | `4` | 继承值，尚未验收 |
| `11400000/m_AdditionalLightShadowsSupported` | `1` | 继承值，尚未验收 |
| `11400000/m_AdditionalLightsShadowmapResolution` | `2048` | 继承值，尚未验收 |
| `11400000/m_AdditionalLightsShadowResolutionTierLow` | `256` | 继承值，尚未验收 |
| `11400000/m_AdditionalLightsShadowResolutionTierMedium` | `512` | 继承值，尚未验收 |
| `11400000/m_AdditionalLightsShadowResolutionTierHigh` | `1024` | 继承值，尚未验收 |
| `11400000/m_ReflectionProbeBlending` | `1` | 继承值，尚未验收 |
| `11400000/m_ReflectionProbeBoxProjection` | `1` | 继承值，尚未验收 |
| `11400000/m_ReflectionProbeAtlas` | `1` | 继承值，尚未验收 |
| `11400000/m_ShadowDistance` | `40` | R04 连续开发授权：40m / .1,.25,.5 / .1；配置已测，图像/性能验收见R04报告 |
| `11400000/m_ShadowCascadeCount` | `4` | 继承值，尚未验收 |
| `11400000/m_Cascade2Split` | `0.25` | 继承值，尚未验收 |
| `11400000/m_Cascade3Split` | `{x: 0.1, y: 0.3}` | 继承值，尚未验收 |
| `11400000/m_Cascade4Split` | `{x: 0.1, y: 0.25, z: 0.5}` | R04 连续开发授权：40m / .1,.25,.5 / .1；配置已测，图像/性能验收见R04报告 |
| `11400000/m_CascadeBorder` | `0.1` | R04 连续开发授权：40m / .1,.25,.5 / .1；配置已测，图像/性能验收见R04报告 |
| `11400000/m_ShadowDepthBias` | `0.1` | 继承值，尚未验收 |
| `11400000/m_ShadowNormalBias` | `0.5` | 继承值，尚未验收 |
| `11400000/m_AnyShadowsSupported` | `1` | 继承值，尚未验收 |
| `11400000/m_SoftShadowsSupported` | `1` | 继承值，尚未验收 |
| `11400000/m_ConservativeEnclosingSphere` | `1` | 继承值，尚未验收 |
| `11400000/m_NumIterationsEnclosingSphere` | `64` | 继承值，尚未验收 |
| `11400000/m_SoftShadowQuality` | `3` | 继承值，尚未验收 |
| `11400000/m_AdditionalLightsCookieResolution` | `2048` | 继承值，尚未验收 |
| `11400000/m_AdditionalLightsCookieFormat` | `3` | 继承值，尚未验收 |
| `11400000/m_UseSRPBatcher` | `1` | 继承值，尚未验收 |
| `11400000/m_SupportsDynamicBatching` | `0` | 继承值，尚未验收 |
| `11400000/m_MixedLightingSupported` | `1` | 继承值，尚未验收 |
| `11400000/m_SupportsLightCookies` | `1` | 继承值，尚未验收 |
| `11400000/m_SupportsLightLayers` | `1` | 继承值，尚未验收 |
| `11400000/m_StoreActionsOptimization` | `0` | 继承值，尚未验收 |
| `11400000/m_UseAdaptivePerformance` | `1` | 继承值，尚未验收 |
| `11400000/m_ColorGradingMode` | `1` | R02 经用户批准：HDR 调色（1）；R01 回归契约 v0.2 |
| `11400000/m_ColorGradingLutSize` | `32` | 继承值，尚未验收 |
| `11400000/m_AllowPostProcessAlphaOutput` | `0` | 继承值，尚未验收 |
| `11400000/m_UseFastSRGBLinearConversion` | `0` | 继承值，尚未验收 |
| `11400000/m_SupportDataDrivenLensFlare` | `1` | 继承值，尚未验收 |
| `11400000/m_SupportScreenSpaceLensFlare` | `1` | 继承值，尚未验收 |
| `11400000/m_GPUResidentDrawerMode` | `0` | 继承值，尚未验收 |
| `11400000/m_SmallMeshScreenPercentage` | `0` | 继承值，尚未验收 |
| `11400000/m_ShadowSmallMeshScreenPercentages` | `{x: 0, y: 0, z: 0, w: 0}` | 引擎迁移新增；未作功能/性能验收 |
| `11400000/m_GPUResidentDrawerEnableOcclusionCullingInCameras` | `0` | 继承值，尚未验收 |
| `11400000/m_ShadowType` | `1` | 继承值，尚未验收 |
| `11400000/m_LocalShadowsSupported` | `0` | 继承值，尚未验收 |
| `11400000/m_LocalShadowsAtlasResolution` | `256` | 继承值，尚未验收 |
| `11400000/m_MaxPixelLights` | `0` | 继承值，尚未验收 |
| `11400000/m_ShadowAtlasResolution` | `256` | 继承值，尚未验收 |
| `11400000/m_VolumeFrameworkUpdateMode` | `0` | 继承值，尚未验收 |
| `11400000/m_VolumeProfile` | `{fileID: 11400000, guid: 51b302884b33dcd42833187a008b8970, type: 2}` | R02 经用户批准：项目中性默认 Profile；R01 回归契约 v0.2 |
| `11400000/apvScenesData` | `obsoleteSceneBounds: m_Keys: [] m_Values: [] obsoleteHasProbeVolumes: m_Keys: [] m_Values:` | 继承值，尚未验收 |
| `11400000/m_PrefilteringModeMainLightShadows` | `3` | 引擎构建派生的 Shader 变体过滤值；非用户参数调优 |
| `11400000/m_PrefilteringModeAdditionalLight` | `3` | 引擎构建派生的 Shader 变体过滤值；非用户参数调优 |
| `11400000/m_PrefilteringModeAdditionalLightShadows` | `2` | 引擎构建派生的 Shader 变体过滤值；非用户参数调优 |
| `11400000/m_PrefilterXRKeywords` | `1` | 引擎构建派生的 Shader 变体过滤值；非用户参数调优 |
| `11400000/m_PrefilteringModeForwardPlus` | `0` | 引擎构建派生的 Shader 变体过滤值；非用户参数调优 |
| `11400000/m_PrefilteringModeDeferredRendering` | `0` | 引擎构建派生的 Shader 变体过滤值；非用户参数调优 |
| `11400000/m_PrefilteringModeScreenSpaceOcclusion` | `2` | 引擎构建派生的 Shader 变体过滤值；非用户参数调优 |
| `11400000/m_PrefilteringModeScreenSpaceReflection` | `0` | 引擎构建派生的 Shader 变体过滤值；非用户参数调优 |
| `11400000/m_PrefilterWriteSmoothness` | `1` | 引擎构建派生的 Shader 变体过滤值；非用户参数调优 |
| `11400000/m_PrefilterDebugKeywords` | `1` | 引擎构建派生的 Shader 变体过滤值；非用户参数调优 |
| `11400000/m_PrefilterWriteRenderingLayers` | `1` | 引擎构建派生的 Shader 变体过滤值；非用户参数调优 |
| `11400000/m_PrefilterHDROutput` | `1` | 引擎构建派生的 Shader 变体过滤值；非用户参数调优 |
| `11400000/m_PrefilterAlphaOutput` | `1` | 引擎构建派生的 Shader 变体过滤值；非用户参数调优 |
| `11400000/m_PrefilterSSAODepthNormals` | `0` | 引擎构建派生的 Shader 变体过滤值；非用户参数调优 |
| `11400000/m_PrefilterSSAOSourceDepthLow` | `1` | 引擎构建派生的 Shader 变体过滤值；非用户参数调优 |
| `11400000/m_PrefilterSSAOSourceDepthMedium` | `1` | 引擎构建派生的 Shader 变体过滤值；非用户参数调优 |
| `11400000/m_PrefilterSSAOSourceDepthHigh` | `1` | 引擎构建派生的 Shader 变体过滤值；非用户参数调优 |
| `11400000/m_PrefilterSSAOInterleaved` | `1` | 引擎构建派生的 Shader 变体过滤值；非用户参数调优 |
| `11400000/m_PrefilterSSAOBlueNoise` | `0` | 引擎构建派生的 Shader 变体过滤值；非用户参数调优 |
| `11400000/m_PrefilterSSAOSampleCountLow` | `1` | 引擎构建派生的 Shader 变体过滤值；非用户参数调优 |
| `11400000/m_PrefilterSSAOSampleCountMedium` | `0` | 引擎构建派生的 Shader 变体过滤值；非用户参数调优 |
| `11400000/m_PrefilterSSAOSampleCountHigh` | `1` | 引擎构建派生的 Shader 变体过滤值；非用户参数调优 |
| `11400000/m_PrefilterDBufferMRT1` | `1` | 引擎构建派生的 Shader 变体过滤值；非用户参数调优 |
| `11400000/m_PrefilterDBufferMRT2` | `1` | 引擎构建派生的 Shader 变体过滤值；非用户参数调优 |
| `11400000/m_PrefilterDBufferMRT3` | `1` | 引擎构建派生的 Shader 变体过滤值；非用户参数调优 |
| `11400000/m_PrefilterSoftShadowsQualityLow` | `1` | 引擎构建派生的 Shader 变体过滤值；非用户参数调优 |
| `11400000/m_PrefilterSoftShadowsQualityMedium` | `1` | 引擎构建派生的 Shader 变体过滤值；非用户参数调优 |
| `11400000/m_PrefilterSoftShadowsQualityHigh` | `1` | 引擎构建派生的 Shader 变体过滤值；非用户参数调优 |
| `11400000/m_PrefilterSoftShadows` | `0` | 引擎构建派生的 Shader 变体过滤值；非用户参数调优 |
| `11400000/m_PrefilterScreenCoord` | `1` | 引擎构建派生的 Shader 变体过滤值；非用户参数调优 |
| `11400000/m_PrefilterScreenSpaceIrradiance` | `1` | 引擎构建派生的 Shader 变体过滤值；非用户参数调优 |
| `11400000/m_PrefilterNativeRenderPass` | `1` | 引擎构建派生的 Shader 变体过滤值；非用户参数调优 |
| `11400000/m_PrefilterUseLegacyLightmaps` | `0` | 引擎构建派生的 Shader 变体过滤值；非用户参数调优 |
| `11400000/m_PrefilterBicubicLightmapSampling` | `1` | 引擎构建派生的 Shader 变体过滤值；非用户参数调优 |
| `11400000/m_PrefilterReflectionProbeRotation` | `0` | 引擎构建派生的 Shader 变体过滤值；非用户参数调优 |
| `11400000/m_PrefilterReflectionProbeBlending` | `0` | 引擎构建派生的 Shader 变体过滤值；非用户参数调优 |
| `11400000/m_PrefilterReflectionProbeBoxProjection` | `0` | 引擎构建派生的 Shader 变体过滤值；非用户参数调优 |
| `11400000/m_PrefilterReflectionProbeAtlas` | `1` | 引擎构建派生的 Shader 变体过滤值；非用户参数调优 |
| `11400000/m_PrefilterPointSamplingUpsampling` | `1` | 引擎构建派生的 Shader 变体过滤值；非用户参数调优 |
| `11400000/m_Light2DPrefilteringMode` | `0` | 引擎构建派生的 Shader 变体过滤值；非用户参数调优 |
| `11400000/m_Light2DKeptVariantCombos` | `[]` | 引擎构建派生的 Shader 变体过滤值；非用户参数调优 |
| `11400000/m_ShaderVariantLogLevel` | `0` | 继承值，尚未验收 |
| `11400000/m_ShadowCascades` | `0` | 继承值，尚未验收 |
| `11400000/m_Textures` | `blueNoise64LTex: {fileID: 2800000, guid: e3d24661c1e055f45a7560c033dbb837, type: 3} bayerMatrixTex: {fileID: 2800000, guid: f9ee4ed84c1d10c49aabb9b210b0fc44, type: 3}` | 继承值，尚未验收 |

## ES_PC_ForwardRenderer.asset

| 对象/字段 | 当前值 | 状态 |
| --- | --- | --- |
| `11400000/m_ObjectHideFlags` | `0` | 继承值，尚未验收 |
| `11400000/m_CorrespondingSourceObject` | `{fileID: 0}` | 继承值，尚未验收 |
| `11400000/m_PrefabInstance` | `{fileID: 0}` | 继承值，尚未验收 |
| `11400000/m_PrefabAsset` | `{fileID: 0}` | 继承值，尚未验收 |
| `11400000/m_GameObject` | `{fileID: 0}` | 继承值，尚未验收 |
| `11400000/m_Enabled` | `1` | 继承值，尚未验收 |
| `11400000/m_EditorHideFlags` | `0` | 继承值，尚未验收 |
| `11400000/m_Script` | `{fileID: 11500000, guid: de640fe3d0db1804a85f9fc8f5cadab6, type: 3}` | 继承值，尚未验收 |
| `11400000/m_Name` | `ES_PC_ForwardRenderer` | 项目专用身份/绑定 |
| `11400000/m_EditorClassIdentifier` | `` | 继承值，尚未验收 |
| `11400000/debugShaders` | `debugReplacementPS: {fileID: 4800000, guid: cf852408f2e174538bcd9b7fda1c5ae7, type: 3} hdrDebugViewPS: {fileID: 4800000, guid: 573620ae32aec764abd4d728906d2587, type: 3} probeVolumeSamplingDebugComputeShader: {fileID: 7200000, guid: 53626a513ea68ce47b59dc1299fe3959, type: 3}` | 继承值，尚未验收 |
| `11400000/probeVolumeResources` | `probeVolumeDebugShader: {fileID: 4800000, guid: e5c6678ed2aaa91408dd3df699057aae, type: 3} probeVolumeFragmentationDebugShader: {fileID: 4800000, guid: 03cfc4915c15d504a9ed85ecc404e607, type: 3} probeVolumeOffsetDebugShader: {fileID: 4800000, guid: 53a11f4ebaebf4049b3638ef78dc9664, type: 3} probeVolumeSamplingDebugShader: {fileID: 4800000, guid: 8f96cd657dc40064aa21efcc7e50a2e7, type: 3} probeSamplingDebugMesh: {fileID: -3555484719484374845, guid: 57d7c4c16e2765b47a4d2069b311bffe, type: 3} probeSamplingDebugTexture: {fileID: 2800000, guid: 24ec0e140fb444a44ab96ee80844e18e, type: 3} probeVolumeBlendStatesCS: {fileID: 7200000, guid: b9a23f869c4fd45f19c5ada54dd82176, type: 3}` | 继承值，尚未验收 |
| `11400000/m_RendererFeatures` | `- {fileID: 7833122117494664109}` | 继承值，尚未验收 |
| `11400000/m_RendererFeatureMap` | `ad6b866f10d7b46cl` | 引擎序列化/迁移差异；有效值以 T08 验证 |
| `11400000/xrSystemData` | `{fileID: 0}` | 引擎迁移新增；未作功能/性能验收 |
| `11400000/postProcessData` | `{fileID: 11400000, guid: 41439944d30ece34e96484bdb6645b55, type: 2}` | 继承值，尚未验收 |
| `11400000/m_AssetVersion` | `3` | 引擎序列化/迁移差异；有效值以 T08 验证 |
| `11400000/m_PrepassLayerMask` | `serializedVersion: 2 m_Bits: 4294967295` | 引擎迁移新增；未作功能/性能验收 |
| `11400000/m_OpaqueLayerMask` | `serializedVersion: 2 m_Bits: 4294967295` | 继承值，尚未验收 |
| `11400000/m_TransparentLayerMask` | `serializedVersion: 2 m_Bits: 4294967295` | 继承值，尚未验收 |
| `11400000/m_DefaultStencilState` | `overrideStencilState: 0 stencilReference: 1 stencilCompareFunction: 3 passOperation: 2 failOperation: 0 zFailOperation: 0` | 继承值，尚未验收 |
| `11400000/m_ShadowTransparentReceive` | `1` | 继承值，尚未验收 |
| `11400000/m_RenderingMode` | `0` | R01 已设普通 Forward（0） |
| `11400000/m_DepthPrimingMode` | `0` | 继承值，尚未验收 |
| `11400000/m_CopyDepthMode` | `0` | 继承值，尚未验收 |
| `11400000/m_DepthAttachmentFormat` | `0` | 引擎迁移新增；未作功能/性能验收 |
| `11400000/m_DepthTextureFormat` | `0` | 引擎迁移新增；未作功能/性能验收 |
| `11400000/m_AccurateGbufferNormals` | `0` | 继承值，尚未验收 |
| `11400000/m_IntermediateTextureMode` | `0` | 继承值，尚未验收 |
| `11400000/m_TileOnlyMode` | `0` | 引擎迁移新增；未作功能/性能验收 |
| `7833122117494664109/m_ObjectHideFlags` | `1` | 引擎序列化/迁移差异；有效值以 T08 验证 |
| `7833122117494664109/m_CorrespondingSourceObject` | `{fileID: 0}` | 继承值，尚未验收 |
| `7833122117494664109/m_PrefabInstance` | `{fileID: 0}` | 继承值，尚未验收 |
| `7833122117494664109/m_PrefabAsset` | `{fileID: 0}` | 继承值，尚未验收 |
| `7833122117494664109/m_GameObject` | `{fileID: 0}` | 继承值，尚未验收 |
| `7833122117494664109/m_Enabled` | `1` | 继承值，尚未验收 |
| `7833122117494664109/m_EditorHideFlags` | `0` | 继承值，尚未验收 |
| `7833122117494664109/m_Script` | `{fileID: 11500000, guid: f62c9c65cf3354c93be831c8bc075510, type: 3}` | 继承值，尚未验收 |
| `7833122117494664109/m_Name` | `ScreenSpaceAmbientOcclusion` | 项目专用身份/绑定 |
| `7833122117494664109/m_EditorClassIdentifier` | `` | 继承值，尚未验收 |
| `7833122117494664109/m_Active` | `1` | 继承值，尚未验收 |
| `7833122117494664109/m_Settings` | `AOMethod: 0 Downsample: 0 AfterOpaque: 0 Source: 1 NormalSamples: 1 Intensity: 0.4 DirectLightingStrength: 0.25 Radius: 0.3 Samples: 1 BlurQuality: 0 Falloff: 100 SampleCount: -1` | 继承值，尚未验收 |

引擎重序列化后不再写入的旧字段（原值仍在 before 快照中）：

- `m_UseNativeRenderPass`: `1`
- `m_BlueNoise256Textures`: `- {fileID: 2800000, guid: 36f118343fc974119bee3d09e2111500, type: 3} - {fileID: 2800000, guid: 4b7b083e6b6734e8bb2838b0b50a0bc8, type: 3} - {fileID: 2800000, guid: c06cc21c692f94f5fb5206247191eeee, type: 3} - {fileID: 2800000, guid: cb76dd40fa7654f9587f6a344f125c9a, type: 3} - {fileID: 2800000, guid: e32226222ff144b24bf3a5a451de54bc, type: 3} - {fileID: 2800000, guid: 3302065f671a8450b82c9ddf07426f3a, type: 3} - {fileID: 2800000, guid: 56a77a3e8d64f47b6afe9e3c95cb57d5, type: 3}`
- `m_Shader`: `{fileID: 4800000, guid: 0849e84e3d62649e8882e9d6f056a017, type: 3}`
