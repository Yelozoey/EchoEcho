"""Fresh official free Standard retry; old received files are never replaced."""
import importlib.util
import json
from pathlib import Path
from datetime import datetime, timezone

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("source_intake", ROOT / "Tools/Assets/receive_salvage_sources.py")
intake = importlib.util.module_from_spec(spec)
spec.loader.exec_module(intake)
target = ROOT / "Docs/Assets/Sources/SalvagePrototype/R3R4_Retry"
receipt_path = target / "Receipt.json"
if receipt_path.exists():
    raise FileExistsError("Fresh retry already has a receipt; inspect it before repeating")
intake.BASE = target
target.mkdir(parents=True, exist_ok=True)
result = {"utc": datetime.now(timezone.utc).isoformat(), "scope": "official anonymous free Standard only; no purchase or TLS changes"}
try:
    record = intake.itch("modular-sci-fi-megakit", "Modular SciFi MegaKit[Standard].zip", None)
    old = ROOT / "Docs/Assets/Sources/SalvagePrototype/Quaternius/Modular SciFi MegaKit[Standard].zip"
    result.update(status="received", record=record, originalSha256=intake.sha(old))
    result["identicalToExisting"] = result["originalSha256"] == record["files"][0]["sha256"]
except Exception as error:
    result.update(status="failed", errorType=type(error).__name__)
    if hasattr(error, "code"):
        result["httpStatus"] = error.code
    # Never serialize exception URLs or anonymous session tokens.
finally:
    intake.json_save(receipt_path, result)
print(json.dumps(result, ensure_ascii=True))
if result["status"] != "received":
    raise SystemExit(1)
