# S1-P01-A 输入焦点与运动规则

2026-10-25，Unity6000.6.7f1/InputSystem1.20.1。虚拟键鼠实际InputAction+运动规则独立6/6通过（A-InputMotion-Retest.json）；首轮5/6保留，F056浮点精确比较已修正为.00001m容差后全量复测。

W/S/A/D分别对应四向，W+D长度1；鼠标(50,20)读取同帧delta，转换yaw+5°/pitch-2°；UI/失焦阻断保持的输入并释放捕获，关闭UI/重新聚焦不会自动恢复捕获。禁用时不能捕获/输入。运动3m/s，30与144步结果一致，斜向无加速，yaw90前进为+X；pitch±80，NaN/Infinity/负时间拒绝。

此处验证真实InputAction数据与计算契约，不代表实际CharacterController移动、产品键鼠窗口或跳跃已完成，B继续。渲染和性能硬件基线沿用总文档，目标1080p60FPS未验收。
