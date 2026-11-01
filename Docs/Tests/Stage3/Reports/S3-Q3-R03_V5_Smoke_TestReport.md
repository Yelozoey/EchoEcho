# Q3-R03 v5 小样本门禁通过

2026-10-27。Unity6000.6.7f1、ORT1.31 CPU2/inter1、Qwen3-0.6B INT4、官方非思考user包装、greedy。开发机与最低/推荐Windows1080p硬件基线同R01计划；目标设备60FPS未测。正式模型/Prompt、S1/S2仍不变。

本版从正式v2只读复制，保留原六种示例，在其前增加代解谜、设备资料提问、不存在目标三种对照；完整正文SHAf3524a85ad232bd4077da4237280e778d5b6cc3bc0fcc95d8c069480973bcbe1。输入669–704tokens，仍满足1536输入/256生成/2048上下文；18条输入和预期未改。Q3_V5_V2Contrast.txt及可复现创建脚本独立保存。

干净状态正常重启D3D11后，新鲜Scene/RenderSafety、Console门禁通过。作业556e49edd22742bebb07965ffc7124eb，实际1/1通过、128.479秒；完整结构18/18、原始意图及目标17/18，达到固定17/18小样本门槛。唯一错误是“调查不存在的设备”被错误替换为power_panel_01，仍计错，未用脚本纠正、未派发。其余raw逐项查看，未出现代操作承诺；设备记录回复较重复，不能视作最终台词质量验收。

冷准备11.141秒；P50/P95=6.288/7.935秒，P95达标但P50高于≤5秒，性能尚未验收。Owner创建/执行/释放、清理完成、mainThreadNativeCalls=0；30秒/20秒/300ms保持。OS内存采样启动晚于模型加载，整个Editor峰值Private4,553,011,200B、WorkingSet2,670,444,544B，采样增量237,568B不代表AI增量，不以此证明3GiB目标。

证据 `Evidence/S3-J01/CandidateQwen3/Tuning/V5_V2Contrast_A/`：Run/Prompt/Cases、V5_A-Job/V5_A.json、Scene/RenderSafety、Memory、Summary及UnityTuning_20261027T0037037720884Z_0ec195405d364715992d609c28fe2d0e.json，原始SHA46d01c213e2c99abe23ca34dfed0533ff507162e93ec0db6a572e8ffec9f64d6。失败的v3/v4均保留。

结论只为R03候选小样本通过，S3整体仍11/24、J01未总验收。固定v5进入R04独立100条和线程性能对照，必须测试新目标/措辞、原始准确率和台词边界；通过后才进行正式Qwen3分词/后端适配及后续权限/玩法整合。
