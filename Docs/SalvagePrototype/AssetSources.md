# 回收原型资源来源与使用计划

日期：2026-10-28。首批7组/22个原始文件已完整接收，下载字节合计261,358,083B（约249.3MiB）。逐文件SHA、4个ZIP的CRC、18个Poly Haven文件的官方大小/MD5交叉核对通过；Unity尚未导入。实际记录见原始目录 Manifest.json/Verification.json 与 [R0-A01报告](../Tests/SalvagePrototype/Reports/R0-A01_SourceIntake_TestReport.md)。

## 选定的首批资源

| ID / 作者 | 官方来源与版本 | 用途与范围 |
| --- | --- | --- |
| modular-sci-fi-megakit / Quaternius | [作品页](https://quaternius.itch.io/modular-sci-fi-megakit)、[免费文件入口](https://quaternius.itch.io/modular-sci-fi-megakit/purchase)；仅 Standard | 设施主体。免费包不是完整付费 Source 工程，不以270+宣传总数充当免费实际模型数 |
| sci-fi-essentials-kit / Quaternius | [作品页](https://quaternius.itch.io/sci-fi-essentials-kit)、[免费文件入口](https://quaternius.itch.io/sci-fi-essentials-kit/purchase)；仅 Standard | 箱体、屏幕与设备候选；包内武器/敌人不作为玩法引入 |
| metal_toolbox / Mateusz Sadek，Poly Haven | [Metal Toolbox](https://polyhaven.com/a/metal_toolbox)；1K FBX及官网列出的依赖 | 手持回收物候选，需要后续尺寸、碰撞和材质适配 |
| metal_jerrycan_green / Ulan Cabanilla，Poly Haven | [Metal Jerrycan Green](https://polyhaven.com/a/metal_jerrycan_green)；1K FBX及依赖 | 回收物或仓储道具候选；不自动成为燃料系统设计 |
| barrel_03 / Serhii Khromov，Poly Haven | [Barrel 03](https://polyhaven.com/a/barrel_03)；1K FBX及依赖 | 设施填充物、视觉遮挡；不默认给Echo操作权限 |
| MetalPlates015A / ambientCG | [官方资源页](https://ambientcg.com/view?id=MetalPlates015A)；2K JPG | 统一金属板材质 |
| Concrete034 / ambientCG | [官方资源页](https://ambientcg.com/view?id=Concrete034)；2K JPG | 统一混凝土材质；与已存在小屋纹理比较后决定实际使用 |

## 许可与来源记录

Quaternius 作品页标注 CC0-1.0；Standard 公开免费下载，Pro/Source 为独立版本。只走作者公开免费入口，不提交付款或账号操作。

Poly Haven 的[资产许可](https://polyhaven.com/license)与[公共API条款](https://github.com/Poly-Haven/Public-API/blob/master/ToS.md)分别适用。开发工具使用可识别 User-Agent；Powered by Poly Haven。只保存资产 metadata、文件哈希、授权页 SHA 和 API 条款，不把官网宣传图当 CC0 资产随包分发。

ambientCG 的[官方许可](https://docs.ambientcg.com/license/)说明可下载资产为 CC0-1.0，允许商用、修改与随项目分发，不强制署名。本项目仍保留来源、作者与处理记录。

## 下载与导入边界

原始文件路径为 `Docs/Assets/Sources/SalvagePrototype/`，完整 manifest 保存真实字节数、SHA256、许可页证据、Standard 版本与免费状态。Poly Haven 文件另外与官方 metadata 大小/MD5核对。ZIP保留文件目录、CRC、安全路径与包内许可清单。

接收不解包、不执行下载文件、不写 Assets、不改 Unity 包/设置/场景。阶段 R0-A02 只先适配一个设施模块与一个回收物；之后筛选使用的模型再建立运行时副本、碰撞与材质。FBX/OBJ/glTF的存在不等于 URP/LOD/动画适配已通过。

## 风格与资源缺项

主场景优先使用同作者配套资源；写实 Poly Haven 道具只少量引入，后续统一材质色调、粗糙度与细节强度。Kenney、Mars Rover 和 HDRI 暂为备选，不同时导入多套风格。

Echo最终模型、独立轮子/探头部件、关键电池道具、资源交付点、UI图标和环境音仍需逐项审查；不能因拿到两个包就宣称美术已完整。角色行为、冷蓝/灰/橙红风格与性能目标继续沿用总计划。
