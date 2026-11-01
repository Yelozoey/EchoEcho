# A00-B 场景、烘焙与实际图像记录

日期2026-11-01；环境Unity6000.6.7f1、Windows11 10.0.26300、D3D11，开发机i7-12650H/RTX4060 Laptop/16122MiB内存。最低i5-8400/GTX1060 6GB/16GB与推荐i5-12400/RTX3060 6GB/16GB的1080p60FPS仍仅为目标，本项没有目标机性能验收。

A00-A资源已先独立通过8/8并归档。新场景仅自有目录，约8×12m、净空4.5m，现含接待台/终端/工具箱/桌椅、门卫与旧Rover外观、可走入转角；角色不执行AI。建筑主体来自CC0模块，墙地门与关键道具不以临时彩色方块代替；小型设计状态灯和不可见碰撞体不作占位判定。

## 实际过程

- Bake_A在NUnit内部未命名场景保存提示阻塞，取消后实际1/1失败，未烘焙；轮询空结果另留存，见F-338。
- 隔离BakeJob_B实际4.761秒完成：1张实际Lightmap、27探针、0错误，保存后宿主还原；SaveScope阻止QualitySettings保存，原设置磁盘未变。
- Scene_A实际5/5，但四张3bc3abeb图存在巨字/镜像和过强明暗反差。未据自动通过交付；初版场景/Lightmap另存ArtDraft_A，见F-339。
- 修改本轮材质涂装遮罩/中性色图集、灯能量/曝光、文字尺寸/深度/朝向、隔墙双面和门楣后，BakeJob_C实际2.976秒完成，同为1张Lightmap/27探针/0错误/宿主恢复，保存Scope通过。
- 受影响资源独立Assets_D 8/8通过；随后Scene_B独立5/5、0跳过、8.118秒，覆盖实际Lightmap/探针、关键模型与门框净空、Forward管线HDR/SDR与ACES、四个实际1080p第一人称相机渲染视角。

代理已查看View_0–3_b407a87f.png，文字正常朝向、不过大，材质和道具可辨；这是代理视觉复核，不是用户接受美术风格。四张图来自实际PlayMode相机离屏渲染，包含场景实际模型与照明；不是AI概念图，也不称为带编辑器UI的原生Game截图，后者在C单独采集。

当前仅场景工程和图像门禁通过；是否达到用户希望的求职视觉底线待本人Editor亲测。尚未验证原生WASD/鼠标实际操作、完整潜入、LLM协作或发行包。

证据相对Docs/Tests/InfiltrationPrototype：Evidence/A00/Bake_A_AfterCancel.json、BakeJob_B/C.json与scope、VisualRefinement_B.json、Assets_D.json、Scene_A/B.json及各自门禁、ViewMetrics_3bc3abeb/b407a87f.json与PNG。预算在样板调整后19.716446/20GiB；最终设置还原/旧SHA留待C，不能提前标通过。
