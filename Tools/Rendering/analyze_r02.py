"""Summarize real R02 evidence; never synthesize missing GPU samples."""
import json
import statistics
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
EVIDENCE = ROOT / "Docs/Tests/Stage1/Evidence/S1-R02"


def stats(values):
    values = sorted(values)
    if not values:
        return {"count": 0, "status": "unavailable"}
    return {"count": len(values), "meanMs": statistics.mean(values),
            "medianMs": statistics.median(values), "minMs": min(values), "maxMs": max(values)}


def analyze():
    output = {}
    for name, relative in [("editor", "Editor/ImageReport.json"), ("player", "PlayerFinal/PlayerProbe.json")]:
        path = EVIDENCE / relative
        if not path.exists():
            output[name] = {"status": "not executed"}
            continue
        raw = json.loads(path.read_text(encoding="utf-8-sig"))
        image = raw if name == "editor" else raw["images"]
        samples = image["grainSamples"]
        item = {"passed": raw["passed"], "source": relative,
                "grainDistinctFrames": len({s["frame"] for s in samples}),
                "brightGrainRatioResponse08To0": statistics.mean(
                    s["response08BrightDifference"] / s["response0BrightDifference"] for s in samples)}
        for enabled in (False, True):
            key = "effectsOn" if enabled else "effectsOff"
            submit = [s for s in image["submitAndReadback"] if s["effects"] == enabled]
            item[key] = {"requestCpuSubmit": stats([s["submitMs"] for s in submit]),
                         "synchronousReadback": stats([s["readbackMs"] for s in submit])}
            if name == "player":
                frames = [s for s in raw["frames"] if s["effects"] == enabled]
                item[key]["normalFrameCpu"] = stats([s["cpuMs"] for s in frames])
                item[key]["normalFrameGpu"] = stats([s["gpuMs"] for s in frames])
        if name == "player":
            item["invalidTimings"] = raw["invalidTimings"]
            item["duplicateTimings"] = raw["duplicateTimings"]
            item["frameTimingEnabled"] = raw["frameTimingEnabled"]
            item["normalBlocks"] = [
                {"block": b, "effects": bool(b % 2),
                 "cpu": stats([s["cpuMs"] for s in raw["frames"] if s["block"] == b]),
                 "gpu": stats([s["gpuMs"] for s in raw["frames"] if s["block"] == b])} for b in range(4)]
            if item["effectsOn"]["normalFrameGpu"]["count"] and item["effectsOff"]["normalFrameGpu"]["count"]:
                item["gpuMedianDifferenceMs"] = (item["effectsOn"]["normalFrameGpu"]["medianMs"] -
                                                 item["effectsOff"]["normalFrameGpu"]["medianMs"])
                item["cpuMedianDifferenceMs"] = (item["effectsOn"]["normalFrameCpu"]["medianMs"] -
                                                 item["effectsOff"]["normalFrameCpu"]["medianMs"])
        output[name] = item
    output["limits"] = "Local diagnostic chart, Development build, short samples. Request submission/readback are not GPU frame time. CPU frame includes waits. No target hardware, game scene, stable 60FPS, or preset acceptance. Missing timings remain unavailable."
    (EVIDENCE / "MetricsSummary.json").write_text(json.dumps(output, indent=2), encoding="utf-8")
    print(json.dumps(output, indent=2))


if __name__ == "__main__":
    analyze()
