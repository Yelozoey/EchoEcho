# P01c-3 两条Windows完整路线

2026-10-30，Build_a7b6 Gameplay_D主路线60.042秒、另一返回路线69.038秒，两个可见Windows独立进程均exit0，输入临时设置实际恢复。证据在R4-P01c/Windows/Gameplay_D。

主路线A+B+D；另一条C+F+B，通过不同连接/返回短路。均实际步行、准星调查、等待真实扫描、E拾取、携带减速、确认交付达到3件/12分；验证取消调查/取消交付/取消结算不错误提交，再主动结算。D下令后实际Walk让出门口，扫描后返回取物，另验证放下重拿与已知扫描不重复收费。R/T中文输入、文字电量问答及面板关闭也实际执行。

结算截图先留存，再确认重开，交付/手持/电量/发现/历史归初始值，因此最终JSON completed=false/count0是重开后状态；actions的explicit_completion/restart_verified和Complete截图记录此前成功结算，不拿重开状态冒充未结算。

默认UseLLM=false、无模型推理；Echo未代搬或改设备。全程D3D11/1080p/内部HDR转SDR。此为自动化真实输入与控制器步行功能测试，不证明外部玩家5分钟体验、最低硬件60FPS或有效GPU性能。本轮随后performance因无足够有效GPU帧而失败，单独记录，不能将四项流程通过混成性能通过。
