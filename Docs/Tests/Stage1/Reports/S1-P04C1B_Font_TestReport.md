# S1-P04-C1B 随包中文字体与面板复测

2026-10-25，C1-BundledFontAndOverlay.json独立2/2通过，包含实际Canvas生命周期及Resources动态中文字体/关键字形/Importer包含源字形数据。未修改Noto Sans CJK SC Regular Version2.004，16437364字节，SHA256 2c76254f6fc379fddfce0a7e84fb5385bb135d3e399294f6eeb6680d0365b74b。静态CFF1、无CFF2/fvar，按实际文字动态出图，不预烘焙全部中文。

SIL OFL1.1完整许可、Adobe版权和官方来源/name元数据见Assets/EchoStation/ThirdParty/Noto及Docs/Assets/Sources/Noto。运行期不联网、不依赖Windows语言包。网络/API及GBK打印问题F063/F051留档；资产数据/哈希已验证，未复制微软系统字体。C2继续Windows实际屏幕图像，不把字形HasCharacter或Text.text当作已渲染屏幕证明。目标性能硬件未验收。
