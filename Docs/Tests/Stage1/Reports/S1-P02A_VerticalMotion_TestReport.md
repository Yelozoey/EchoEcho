# S1-P02-A 垂直状态与Space输入测试

2026-10-25，Unity6000.6.7f1/InputSystem1.20.1，A-VerticalAndJumpInput.json独立4/4通过。接地初速sqrt(2×18×.85)，空中再次请求不能重置；自由落体速度封顶-30，碰顶清向上速度/落地-2贴地，非法时间拒绝且无状态污染。实际Space InputAction只在边沿有效，保持不会再次触发，UI/失焦拒绝跳跃。

状态模块未证明实际跳高或场景碰撞，B继续真实CharacterController。渲染和性能硬件基线沿用总文档，目标1080p60FPS尚未实机验收。
