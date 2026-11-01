"""Audit actual R04 execution artifacts; no inferred passes or target-hardware claims."""
import hashlib
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
EVIDENCE = ROOT / "Docs/Tests/Stage1/Evidence/S1-R04"


def read(name):
    return json.loads((EVIDENCE / name).read_text(encoding="utf-8-sig"))


def fields(text):
    # Only top-level serialized fields: retain nested lines for identity/reference checks.
    found = list(re.finditer(r"^  (\w+):", text, re.M))
    return {m.group(1): text[m.end(): found[i + 1].start() if i + 1 < len(found) else len(text)].strip()
            for i, m in enumerate(found)}


protected = read("ProtectedFiles.json")["templates"]
protection = [{"path": item["path"], "unchanged": hashlib.sha256((ROOT / item["path"]).read_bytes()).hexdigest() == item["sha256"]}
              for item in protected]
before = fields((EVIDENCE / "Pipeline.before.txt").read_text(encoding="utf-8-sig"))
after = fields((ROOT / "Assets/EchoStation/Rendering/Settings/ES_PC_ForwardHDR.asset").read_text(encoding="utf-8-sig"))
changes = [key for key in after.keys() | before.keys() if after.get(key) != before.get(key)]
derived = [key for key in changes if key.startswith("m_Prefilter") or key in {"m_Light2DPrefilteringMode", "m_Light2DKeptVariantCombos"}]
effective = sorted(set(changes) - set(derived))
player = read("Player/PlayerProbe.json")
exit_record = read("Player/Exit.json")
build = read("PlayerBuild.json")
tests = read("EditMode-Final.json")["structuredContent"]["result"]["data"]["result"]["summary"]
on = read("Editor/FrameDebugger-On.json")
off = read("Editor/FrameDebugger-Off.json")
result = {
    "protected": protection, "effectivePipelineChanges": effective, "engineDerivedChanges": derived,
    "tests": tests, "build": build, "playerPassed": player["passed"],
    "shadowSubmissionEventsOn": on["shadowSubmissionEvents"], "shadowSubmissionEventsOff": off["shadowSubmissionEvents"],
    "assertions": {
        "protectedUnchanged": all(row["unchanged"] for row in protection),
        "onlyApprovedPipelineInputs": effective == ["m_Cascade4Split", "m_CascadeBorder", "m_ShadowDistance"],
        "testsPassed": tests["total"] >= 47 and tests["passed"] == tests["total"] and tests["failed"] == tests["skipped"] == 0,
        "playerPassed": player["passed"] and exit_record["exitCode"] == 0 and not exit_record["timedOut"],
        "buildPassed": build["result"] == "Succeeded" and build["errors"] == 0,
        "actualShadowSubmissionContrast": on["shadowSubmissionEvents"] > 0 and off["shadowSubmissionEvents"] == 0,
    },
    "targetHardwarePerformanceVerified": False,
}
result["passed"] = all(result["assertions"].values())
(EVIDENCE / "FinalAudit.json").write_text(json.dumps(result, indent=2, ensure_ascii=False), encoding="utf-8")
print(json.dumps({"passed": result["passed"], "assertions": result["assertions"], "effectivePipelineChanges": effective}, indent=2))
raise SystemExit(0 if result["passed"] else 1)
