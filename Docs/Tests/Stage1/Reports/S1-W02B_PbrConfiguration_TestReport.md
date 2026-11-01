# S1-W02-B PBR与房间预制体配置

2026-10-25。前置A下载/许可通过后，配置首轮4/4；增加实际PNG全像素通道对照后5/5（B-ActualMasks.json），0失败/跳过。不是仅比较转换函数自身：重新解码两份原ARM与最终Mask文件，逐像素要求R=原B、G=原R、B=0、A=255-原G。

两套6张最终贴图1024、MipMaps/Repeat、CompressedHQ、运行时不可读；BaseColor sRGB，Normal为线性NormalMap、Mask线性。URP Lit实际绑定贴图与三个关键词，场景8个合并体实际使用两套PBR；30源Collider/有效UV2保留。按米UV0及重新计算Tangent，2m重复；未改UV2或几何。

Main/Storage/Foyer三个房间预制体均根scale1、实际Collider与材质有效，不包含相机/灯；lightmapIndex=-1，场景距离阴影相机引用不进入模板。场景继续持有有效烘焙引用，随后C重烘焙，避免将旧光照当作PBR重算结果。

Unity6000.6.7f1/URP17.6/Windows Editor D3D11，当前开发机配置见总文档；没有目标硬件性能验收。F-041遗漏命名空间修复后Console0错误再导入。B通过，允许C实际光照/图像复验。
