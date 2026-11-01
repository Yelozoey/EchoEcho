# S2-W01 五区基础测试报告

2026-10-25；Unity6000.6.7f1、Windows11开发机i7-12650H/RTX4060Laptop/16GB，D3D11/普通Forward/内部HDR→SDR，UseLLM=false。最低/推荐目标硬件1080p60FPS尚未实测。

A拓扑3/3先独立通过并归档。B新增独立场景/NavMesh、既有玩家/Echo预制体、摘要桥/脚本知识与文字输入、区域记忆、Tab指令与显示入口。

B首次3项中1通过：加载夹具未等待isLoaded，F087；另首次编译缺直接Memory.Integration引用见F069，未启动旧DLL。B-Retest最终实际3/3、0失败/跳过、框架跨模式耗时3.664383秒（非墙钟游玩时间）。../Evidence/S2-W01/B-Retest.json与门禁/工作区还原证据留存。

测试检查独立NavMesh路径、五区根节点/玩家无S1存档组件/相机HDR与SDR；实际PlayMode成功绑定Echo、空线索记忆、五区路径双向PathComplete、Echo实际跟随移动>0.2米；区域变化产生正确状态和5个首次访问事件，未产生线索；对话/Tab/日志互斥。NavMesh生成335顶点，专用Agent -1372625422。

回归宿主已退出，原打开场景/播放选项/管线还原；475个S1资产及交付文件SHA全部不变（S1-Protected-Verified.json）。当前地板仍为占位，各区正式灰盒与谜题尚未验收；查询可达不等于玩家已完整走通，不把该结果当25–40分钟或性能验收。
