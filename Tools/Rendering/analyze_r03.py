"""Summarize R03 actual evidence, including residual lighting and controls."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
EVIDENCE = ROOT / "Docs/Tests/Stage1/Evidence/S1-R03"


def analyze():
    result = {}
    for name, relative in [("editor", "Editor/LightingReport.json"), ("player", "Player/PlayerProbe.json")]:
        path = EVIDENCE / relative
        if not path.exists():
            result[name] = {"status": "not executed"}
            continue
        raw = json.loads(path.read_text(encoding="utf-8-sig"))
        r = raw if name == "editor" else raw["lighting"]
        rows = r["dynamicImages"]
        result[name] = {"passed": raw["passed"], "source": relative, "atlasCount": r["atlasCount"],
                        "probeCount": r["probeCount"], "lightmapRuntimeBytes": r["lightmapRuntimeBytes"],
                        "lightsDisabledDifference": r["lightsDisabledDifference"],
                        "lightmapsRemovedLuminance": r["lightmapsRemovedLuminance"],
                        "maxNormalizedProbeStep": r["maxNormalizedProbeStep"],
                        "pathSampleCount": len(r["interpolated"]), "actualImagePositions": len(rows),
                        "minimumOriginalProxyLuminance": min(s["withProbes"] for s in rows),
                        "minimumOneMinusZero": min(s["withProbes"] - s["zeroBakedProbe"] for s in rows),
                        "minimumTwoMinusOne": min(s["doubleBakedProbe"] - s["withProbes"] for s in rows),
                        "maxZeroControlSHMagnitude": max(s["zeroControlSHMagnitude"] for s in rows),
                        "zeroControlResidualLuminanceRange": [min(s["zeroBakedProbe"] for s in rows), max(s["zeroBakedProbe"] for s in rows)],
                        "strictFrameOrder": all(s["withFrame"] < s["offFrame"] < s["zeroFrame"] < s["doubleFrame"] for s in rows)}
    result["limits"] = "Diagnostic fixture only. Editor additive zero-SH control retains residual lighting; Player control is reported separately. Only controlled incremental contribution is attributed to probes. No target-hardware FPS or complete scene art acceptance."
    (EVIDENCE / "MetricsSummary.json").write_text(json.dumps(result, indent=2), encoding="utf-8")
    print(json.dumps(result, indent=2))


if __name__ == "__main__":
    analyze()
