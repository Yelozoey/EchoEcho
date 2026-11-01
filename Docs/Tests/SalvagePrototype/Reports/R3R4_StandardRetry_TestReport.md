# Standard 官网重试实际结果

2026-10-29，用户要求代理尝试下载。仅资源接收验证，不是Unity／场景验收。

官网免费Standard匿名入口，4连接512KiB精确Range，93/93全部接收，48,559,316字节；1195 ZIP条目CRC与安全路径检查通过，展开64,822,507字节。SHA256 `6fae60cf5189e44dff0bd91097f094a765acc6d57d64a85a0cc0dd56e03035e3`，与既有完整包相同。无购买、无关闭证书验证、无全机网络更改。

工具Tools/Assets/retry_megakit.py实际exit0，完整回执Docs/Assets/Sources/SalvagePrototype/R3R4_Retry/Receipt.json。旧包与清单未替换；新版重试副本暂留待本轮预算核算，使用旧已校验包即可。

历史F184确认官网签名链接仅60秒，接收器25秒刷新同一免费文件链接。此次成功不能证明用户浏览器“无权限”的唯一根因；没有读取其下载凭据或浏览器错误详情。Pro／Source未下载，用户未确认已购买。
