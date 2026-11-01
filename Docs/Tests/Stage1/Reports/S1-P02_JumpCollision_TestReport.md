# S1-P02 跳跃重力与碰撞验收

2026-10-25，A垂直规则/真实Space InputAction4/4、B实际物理4/4按序通过；补坡面接地与穿透检测正控后，Player程序集最终17/17通过（FinalRegression.json）。

实现Space接地边沿跳跃，设计初速sqrt(2×18×.85)，重力18m/s²，终端下落30m/s，接地2m/s贴地；碰顶清上升速度、落地清下落，不缓冲/空中连跳。UI/失焦阻断主动输入，重力继续。实际60Hz半隐式积分跳高.804518m（设计参数.85m不等于离散积分精确峰值），落地误差约2.8e-8m，空中第2次请求未重置速度。

30°坡面实际位置Z5.300/Y3.252且接地；60°坡面止于Z-.300/Y.030，未爬越45°限制。低顶棚触发Above，最高Y.070；墙前止于X1.570。隔离小屋真实30个非Trigger BoxCollider：从门厅穿主门、再经侧门到储藏室X7.450/Z1.000；外墙止于X8.670，桌前Z1.570。参考Capsule几何逐步检测最大穿透0，使用真实Capsule Collider，并用深入地面.2m正控确认查询能检测重叠，未把CharacterController不支持的查询返回false当作证据。

ActualJump/ActualSlopes/ActualCeilingWall/ActualCabinPlayer.json与作业原始结果留档；P01输入/视角/真实Update回归保持通过。本模块无新增故障。Unity6000.6.7f1/URP17.6普通Forward HDR→SDR、D3D11，当前Win11/i7-12650H/RTX4060 Laptop/16GB。最低i5-8400/GTX1060 6GB/16GB中画质及推荐i5-12400/RTX3060 6GB/16GB高画质1080p稳定60FPS仍待实机验收；完整Windows产品整合在P09。
