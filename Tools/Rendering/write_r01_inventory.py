"""Record every serialized top-level field, including nested YAML values."""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[2]
EVIDENCE = ROOT / "Docs/Tests/Stage1/Evidence/S1-R01"


def fields(text):
    result, section, key, lines = {}, "", None, []
    for line in text.splitlines() + ["--- end"]:
        match = re.match(r"  (\w+):(.*)", line)
        if line.startswith("---") or match:
            if key is not None:
                result[(section, key)] = " ".join(part.strip() for part in lines).strip()
            key, lines = None, []
            if line.startswith("---"):
                section = line
            elif match:
                key, lines = match.group(1), [match.group(2)]
        elif key is not None:
            lines.append(line)
    return result


def cell(value):
    return "`" + value.replace("|", "\\|").replace("`", "'") + "`"


lines = ["# S1-R01 渲染参数逐项记录", "",
         "生成器：Tools/Rendering/write_r01_inventory.py。以下为实际序列化值，嵌套值完整保留在同一行。", "",
         "包含R02的HDR调色/中性Profile以及R04获授权的三个阴影输入（回归契约v0.3）；其他继承字段仍不代表最终画质或性能验收通过。", "",
         "Unity 6000.6.7f1 会迁移旧模板字段。T08 比较加载后的有效属性和 Renderer Feature 参数；T07 单独保护原始模板文件哈希。", ""]
for original, current in [("PC_RPAsset.asset", "ES_PC_ForwardHDR.asset"),
                          ("PC_Renderer.asset", "ES_PC_ForwardRenderer.asset")]:
    before = fields((EVIDENCE / (original + ".before.txt")).read_text(encoding="utf-8-sig"))
    after = fields((ROOT / "Assets/EchoStation/Rendering/Settings" / current).read_text(encoding="utf-8-sig"))
    lines.extend(["## " + current, "", "| 对象/字段 | 当前值 | 状态 |", "| --- | --- | --- |"])
    for (section, key), value in after.items():
        previous = before.get((section, key))
        if key.startswith("m_Prefilter") or key in {"m_Light2DPrefilteringMode", "m_Light2DKeptVariantCombos"}:
            status = "引擎构建派生的 Shader 变体过滤值；非用户参数调优"
        elif key in {"m_Name", "m_RendererDataList"}:
            status = "项目专用身份/绑定"
        elif key == "m_RenderingMode":
            status = "R01 已设普通 Forward（0）"
        elif key == "m_SupportsHDR":
            status = "R01 内部 HDR 已开启（1）"
        elif key == "m_ColorGradingMode":
            status = "R02 经用户批准：HDR 调色（1）；R01 回归契约 v0.2"
        elif key == "m_VolumeProfile":
            status = "R02 经用户批准：项目中性默认 Profile；R01 回归契约 v0.2"
        elif key in {"m_ShadowDistance", "m_Cascade4Split", "m_CascadeBorder"}:
            status = "R04 连续开发授权：40m / .1,.25,.5 / .1；配置已测，图像/性能验收见R04报告"
        elif previous is None:
            status = "引擎迁移新增；未作功能/性能验收"
        elif previous != value:
            status = "引擎序列化/迁移差异；有效值以 T08 验证"
        else:
            status = "继承值，尚未验收"
        label = section.replace("--- !u!114 &", "") + "/" + key
        lines.append(f"| {cell(label)} | {cell(value)} | {status} |")
    removed = [(key, value) for key, value in before.items() if key not in after]
    if removed:
        lines.extend(["", "引擎重序列化后不再写入的旧字段（原值仍在 before 快照中）：", ""])
        lines.extend("- " + cell(key[1]) + ": " + cell(value) for key, value in removed)
    lines.append("")
destination = ROOT / "Docs/Stage1/S1-R01_RenderParameters.md"
destination.write_text("\n".join(lines), encoding="utf-8")
print(destination)
