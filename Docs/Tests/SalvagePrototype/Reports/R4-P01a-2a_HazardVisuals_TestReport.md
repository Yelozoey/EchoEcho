# R4-P01a-2a 只读危险设备外观

最终Hazards_B5/5、32.079秒：已看实图，配电设备改为正面朝西的CC0 Computer控制台，管段Fan也面向接近方向；补充碰撞范围内底座与自发光指示条，无新增实时灯/Collider。Body/Nav/8目标/两权限/三房光照全量重测通过，新Native绘制49→20/背向0、图差0；指示条Emission关键字/GI非Black/HDR值实际验证。FaceApproach只改已验证的自有视觉子树并另存BeforeFacing，保存scope精确保护；下文A模型与45→20为中间版本。

2026-10-30，Hazards_A5/5、35.494秒，完整module_tests门禁/Console0。两危险外观/权限用例、三房光照与多Scene遮挡回归均通过；已查看两个实际设备图片。

配电箱复用CC0 AccessPoint，管段使用小型金属管体加CC0 Fan/Cable，旧橙色立方体Renderer禁用，原Collider仍启用。118 Collider、8 Target、原位置/尺度/唯一ID/探针/导航SHA保持，新外观不含Collider/动画或机关写入。几何落在既有逻辑碰撞内。两个对象实际可被准星指向，Investigate/Warn权限通过，Echo传统警告成功，交付数不变、默认脚本。

复用PBR已降金属掩码材质，橙色屏幕使用自有材质副本，HDR颜色/Emission关键字/GI flags实际验证；没有新增实时灯。场景/新材质保存scope仅允许自有范围，旧Graphics/Quality保存拦截，原dirty和设置磁盘保持。

静态因果对照先停新文字管理器再隐藏字，避免LateUpdate重新启用；三房光照图差仍0。加入动态模型后的实际Native绘制45→20/背向0、图差0，不沿用旧36→20作为新值。墙/PVS未改变，动态设备使用探针和距离阴影。下一灯具实例化与可见灯面，尚未最终Windows/目标机验收。
