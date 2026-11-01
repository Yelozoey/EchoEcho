# S1-R05 剔除、合并与动态SRP Batcher实际测试

2026-10-25，v0.1。**功能与持久化通过：最终63/63 Editor回归、Windows Player退出0、9项外部审计全部通过。** 不属于目标硬件60FPS或正式场景美术验收。

## 环境和顺序

Unity6000.6.7f1 / URP17.6普通Forward / Windows x64 D3D11 / 内部HDR→SDR /1920×1080。开发机Windows11/i7-12650H/RTX4060 Laptop 7956MB/16122MB RAM。用户目标：最低i5-8400/GTX1060 6GB/16GB中档60FPS；推荐i5-12400/RTX3060 6GB/16GB高档60FPS；这两套硬件未执行。

R01–04先前通过。R05-A静态合并12/12、B房间剔除2/2、C动态SRP Batcher2/2依次独立归档后才做D。最终63项含既有47及新16项，0失败/跳过，12.032秒。A/B/C独立报告与原始证据在同目录索引可查。

## 实际结果

| 验证 | 期望及实际 |
| --- | --- |
| 离线同室合并 | 三盒父变换后世界顶点/三角形保持，源Collider/对象保留；UV2按各自lightmapScaleOffset合并；已有资产拒绝覆盖；9种非法组无部分修改 |
| 持久化 | 输出Mesh资产重新导入有效；实际场景打包并在Windows重新加载后合并体仍正确绘制 |
| 实际遮挡烘焙 | Occluder2m/Hole.25m/Backface100；PVS5568字节，资产12881字节，场景引用有效，原布局恢复 |
| Editor真实绘制 | 遮挡Off2批→On1批、反转1批、入远室2批；遮挡开关图像差0，远室显隐图像差0.083635 |
| 动态真实SRP Batcher | 独立无PVS动态夹具：隐藏2批→出现3批→移动3批；位移后图像差.009640，非Static/无MPB/无Instancing |
| Windows正常帧Draw Calls | 未合并31→合并24（减少7）；遮挡18（减少13）；反向视锥7；进入远室20，恢复可见 |

Windows各状态预热10个真实游戏帧，随后16个不同帧号采样；5状态共80个样本全部有效且同状态计数一致。用`ProfilerRecorder`真实`Draw Calls Count`，未混入手工截图渲染、未把SRPBatch批次数冒充Draw Calls。上述值是此夹具整帧计数，不能推广成完整游戏收益或60FPS证明。

共享Pipeline/Renderer无输入改动，9份保护资产原SHA全部不变。测试临时SSAO隔离后恢复；最终EditorSettings API实际enabled=true/options=1，恢复测试前记录的配置。

## 证据与复测

`Docs/Tests/Stage1/Evidence/S1-R05/`含A/B/C独立JSON、`EditMode-Final.json`、`PlayerBuild.json`、`FinalAudit.json`、`EditorOptions-Final.json`；`Player/PlayerProbe.json`中保存全部计数与真实帧号，`Exit.json`退出0且未超时，`Player.log`完整保留；Editor/Dynamic/Player合计10张1080p PNG与Frame Debugger原始事件。

菜单`Tools/EchoStation/Stage 1/R05`：建夹具、隔离烘焙、动态夹具、Build Validation Player。测试程序集`EchoStation.Rendering.Editor.Tests`。Windows命令`Tools/Rendering/RunR05Player.ps1 -Visible`短暂显示经授权的1080p窗口并自动关闭，60秒超时只终止自己的进程。`python Tools/Rendering/audit_r05.py`验证实际交付数据。

构建26.379秒，0 errors/2 warnings，仍是Services未关联与Windows.Forms依赖（F-011）；运行四条禁用DOF/Panini Shader提示（F-019）。没有新增Shader编译警告或未处理运行异常。

## 故障和限制

F-033：native umbraDataSize统计0，但真实PVS与运行期剔除有效，不能当内存0。F-034命名空间编译错误修复后构建通过。

F-035：首完整回归62/63，Editor重复PlayMode的遮挡采集2→2。预热、卸载PVS、等待场景卸载、拆独立夹具均不足单独修复；临时正常域重载后严格2→1恢复并完整63/63通过。MCP测试服务也管理重载选项，最终同时备份/恢复enabled和flags并核对实际API值。具体缓存来源未全部归因，不冒充已定位Unity原生Bug；未来Addressables反复加载仍需独立实测。全部失败和修正版证据保留，未删除原断言。

静态合并为显式同室/材质/图集工具，不自动把门、交互件或动态物合并；多材质/LOD/MPB等不兼容输入拒绝。大于65535顶点分支有实现但本轮未压测，不宣称无限规模。测试场景材质为测量而使用Unlit/AlphaClip变体，正式美术遵守PBR策略。最终小屋必须实际复用这些优化并重新烘焙，不能仅引用本报告宣称小屋已优化。

结论：R05模板、真实执行路径、绘制计数与持久化通过，允许开始W01小屋灰盒；S1整体尚未完成。
