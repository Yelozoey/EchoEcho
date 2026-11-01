# R03-A 配置与几何独立测试记录

日期：2026-10-25（Asia/Shanghai）；Unity6000.6.7f1 / URP17.6 / Windows11 / D3D11。

**配置组6/6通过**，0跳过；证据：[A-Retest.json](../Evidence/S1-R03/A-Retest.json)。这仅是R03-A配置与几何检查通过，尚不代表实际烘焙或R03整模块完成。

| 用例 | 实际结果 |
| --- | --- |
| A01 | Progressive CPU、手动Baked GI、20texels/m、1024、Padding4、Directional、HighQuality压缩、64/256/64、2次反弹与Gaussian滤波配置一致；场景引用正确 |
| A02 | 八个静态实体有完整、有限、非退化UV2；ContributeGI/ReceiveGI.Lightmaps明确；地板、顶板、前后墙边界正确 |
| A03 | 分层探针避开静态实体，覆盖动态代理路径 |
| A04 | 动态代理无Static标记、BlendProbes；两盏灯均Baked；相机HDR/SDR与Volume Mask正确 |
| A05 | 先前模块九个受保护文件SHA256未变 |
| A06 | 重复创建不重写场景/参数/材质，原打开场景dirty状态不变 |

首轮5/6，枚举零值断言失败；改用同类型枚举比较后6/6。原始失败保留A-FirstRun.json，详见故障F-023。参数来自用户授权的[R03推荐值](../../../Stage1/S1-R03_LightingParameters.md)。

当前允许开始R03-B实际烘焙。目标硬件性能、正式场景视觉、实时阴影均未验收。
