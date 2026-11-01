"""Summarize retained native samples without discarding spikes or invalid rows."""
import hashlib
import json
import math
import sys
from pathlib import Path

path = Path(sys.argv[1])
data = json.loads(path.read_text(encoding="utf-8-sig"))
samples = data["samples"]
def stats(values):
    values = sorted(values)
    def percentile(q):
        return values[max(0, math.ceil(len(values) * q) - 1)]
    return {"p50": percentile(.5), "p95": percentile(.95), "p99": percentile(.99), "max": values[-1]}
summary = {"source": str(path), "passed": data["passed"], "rawSha256": hashlib.sha256(Path(data["rawFile"]).read_bytes()).hexdigest(),
           "frames": len(samples), "validFrames": data["validFrames"], "invalidFrames": data["invalidFrames"], "duplicates": data["duplicates"],
           "pvsDraws": data["pvsDraws"], "pvsDifference": data["pvsDifference"], "zones": []}
names = ["入口山谷", "地面观测小屋", "地下A实验室", "通风通道", "核心控制室"]
for i, name in enumerate(names):
    rows = [s for s in samples if s["zone"] == i]
    item = {"zone": name, "frames": len(rows), "gcNonzeroFrames": sum(s["gcBytes"] > 0 for s in rows),
            "deltaOver25ms": sum(s["deltaMs"] > 25 for s in rows), "deltaOver33ms": sum(s["deltaMs"] > 1000/30 for s in rows)}
    for field in ["deltaMs", "cpuMs", "gpuMs", "mainMs", "renderMs", "presentWaitMs", "gcBytes", "usedBytes", "drawCalls"]:
        item[field] = stats([s[field] for s in rows])
    summary["zones"].append(item)
out = path.parent / "Summary.json"
out.write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding="utf-8")
print(json.dumps(summary, ensure_ascii=False, indent=2))
if not data["passed"] or len(samples) != 1500 or data["invalidFrames"] or data["duplicates"]:
    raise SystemExit(1)
