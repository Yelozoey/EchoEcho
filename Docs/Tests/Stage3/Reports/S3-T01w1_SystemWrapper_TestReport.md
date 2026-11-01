# S3-T01w1 官方system/user包装

2026-10-27，Wrapper_A实际19/19通过，3.018462秒，job `5d9556b9d92649908358c207c0059691`，Evidence/S3-T01w1。

新增WrapSystemUser，采用固定官方enable_thinking=false的system→user→assistant空think闭合顺序。项目隔离环境、本地官方tokenizer.apply_chat_template生成8组参考（无模型/网络），配置SHA `d5d09f07b48c3086c508b30d1c9114bd1189145b74e982a265350c923acd8101`，golden SHA `4d2715736d98b86219b0aea12e5f95418364c3012afbbf7d2898e0c59540f456`。

新3项实际逐token比对8序列、角色/EOS文字注入不得增角色、空值/孤立代理项/超长拒绝；原Profile/Preparation16项回归通过。全程未加载权重，尚未改变profile包装或选中默认，不算模型语义达标。后续独立profile/预算后再真实试验。
