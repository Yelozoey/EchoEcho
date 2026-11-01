"""Protect pre-existing project/build files and account for all owned resources."""
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
BASE = ROOT / "Docs/Tests/SalvagePrototype/Evidence/R3R4/ProtectedBefore.json"
def digest(path):
    h = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()
def write(path, record):
    path.parent.mkdir(parents=True, exist_ok=True)
    if path.exists():
        raise FileExistsError("Evidence must be fresh")
    path.write_text(json.dumps(record, ensure_ascii=False, indent=2), encoding="utf-8")
parser = argparse.ArgumentParser()
parser.add_argument("action", choices=["capture", "audit"])
parser.add_argument("--label", default="A")
args = parser.parse_args()
if args.action == "capture":
    old = json.loads((ROOT / "Docs/Tests/SalvagePrototype/Evidence/R0-A02/ProtectedFiles_Before_A.json").read_text(encoding="utf-8"))
    paths = {ROOT / p for p in old}
    for rel in ["Assets/EchoStation/Salvage", "Assets/EchoStation/Tests/SalvageEditor", "Assets/EchoStation/Tests/SalvageFixtures", "Builds/Salvage"]:
        paths.update(p for p in (ROOT / rel).rglob("*") if p.is_file())
    write(BASE, {p.relative_to(ROOT).as_posix(): digest(p) for p in sorted(paths)})
    print(json.dumps({"captured": len(paths)}))
else:
    baseline = json.loads(BASE.read_text(encoding="utf-8"))
    changed = [p for p, h in baseline.items() if not (ROOT / p).is_file() or digest(ROOT / p) != h]
    old_budget = json.loads((ROOT / "Docs/Tests/SalvagePrototype/Evidence/R2-V01b/FinalAudit_B.json").read_text(encoding="utf-8"))
    paths = [ROOT / p["path"] for p in old_budget["parts"]]
    paths += [ROOT / p for p in ["Assets/EchoStation/SalvageNext", "Assets/EchoStation/Tests/SalvageNextEditor", "Assets/EchoStation/Tests/SalvageNextFixtures", "Tools/SalvageNext", "Builds/SalvageNext", "Library/EchoStation/SalvageNext", "Docs/Assets/Sources/SalvageNext", "Library/com.unity.addressables", "Library/BuildCache", "Tools/Assets/retry_megakit.py"]]
    for package in ("com.unity.addressables@*", "com.unity.scriptablebuildpipeline@*"):
        paths.extend((ROOT / "Library/PackageCache").glob(package))
    selected = []
    for p in sorted(set(p.resolve() for p in paths), key=lambda p: (len(p.parts), str(p).casefold())):
        if not any(parent == p or parent in p.parents for parent in selected):
            selected.append(p)
    parts = [{"path": p.relative_to(ROOT).as_posix(), "bytes": p.stat().st_size if p.is_file() else sum(x.stat().st_size for x in p.rglob("*") if x.is_file()) if p.exists() else 0} for p in selected]
    total = sum(p["bytes"] for p in parts)
    record = {"protectedFiles": len(baseline), "changedProtectedFiles": changed, "parts": parts, "combinedBytes": total, "combinedGiB": total / 1024**3, "underLimit": total <= 20*1024**3,
              "scope": "Old S3+Salvage roots and new SalvageNext roots; shared Unity compiler/import cache excluded as before. New owned caches included. Manifest/lock changes require explicit reviewed exception."}
    write(BASE.parent / ("Audit_" + args.label + ".json"), record)
    print(json.dumps({k: record[k] for k in ["protectedFiles", "changedProtectedFiles", "combinedGiB", "underLimit"]}))
    if not record["underLimit"]:
        raise SystemExit(1)
