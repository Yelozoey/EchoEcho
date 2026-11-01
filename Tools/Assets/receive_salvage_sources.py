"""Development-only, official free source intake. Powered by Poly Haven.

Does not extract archives, execute downloaded files, import Unity assets, pay,
log into accounts, or persist anonymous itch.io session/download tokens.
"""
import argparse
import os
import subprocess
import hashlib
import http.client
import http.cookiejar
import json
import re
import stat
import time
import threading
import urllib.error
import urllib.parse
import urllib.request
import zipfile
from concurrent.futures import ThreadPoolExecutor, as_completed
from datetime import datetime, timezone
from html.parser import HTMLParser
from pathlib import Path, PurePosixPath, PureWindowsPath

ROOT = Path(__file__).resolve().parents[2]
BASE = ROOT / "Docs/Assets/Sources/SalvagePrototype"
MANIFEST = BASE / "Manifest.json"
MAX_BYTES = 512 * 1024**2
MAX_EXPANDED = 2 * 1024**3
UA = "EchoStation-asset-preparation/1.0 (offline Unity portfolio; Powered by Poly Haven)"
OPENER = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))


class Metadata(HTMLParser):
    def __init__(self):
        super().__init__()
        self.csrf = None
        self.uploads = []

    def handle_starttag(self, tag, attrs):
        attrs = dict(attrs)
        value = attrs.get("value") or attrs.get("content")
        if attrs.get("name") == "csrf_token" and value:
            self.csrf = value
        if "data-upload_id" in attrs:
            self.uploads.append(attrs["data-upload_id"])


def sha(path):
    value = hashlib.sha256()
    with path.open("rb") as source:
        for chunk in iter(lambda: source.read(1024**2), b""):
            value.update(chunk)
    return value.hexdigest()


def request(url, data=None, referer=None):
    if urllib.parse.urlsplit(url).scheme != "https":
        raise ValueError("HTTPS required")
    headers = {"User-Agent": UA}
    if referer:
        headers["Referer"] = referer
    encoded = urllib.parse.urlencode(data).encode() if data is not None else None
    return OPENER.open(urllib.request.Request(url, data=encoded, headers=headers), timeout=45)


def small(url, data=None, referer=None):
    for attempt in range(3):
        try:
            with request(url, data, referer) as response:
                raw = response.read(8 * 1024**2 + 1)
            if len(raw) > 8 * 1024**2:
                raise ValueError("Metadata exceeds bound")
            return raw
        except urllib.error.HTTPError as error:
            if error.code not in {429, 500, 502, 503, 504} or attempt == 2:
                raise
        except (OSError, ConnectionError, http.client.IncompleteRead):
            if attempt == 2:
                raise
        time.sleep(attempt + 1)


def binary_transport(url, output, byte_range=None, max_bytes=MAX_BYTES):
    """Use the installed Windows HTTPS stack; certificate verification stays on."""
    curl = Path(os.environ.get("WINDIR", "C:/Windows")) / "System32/curl.exe"
    if not curl.is_file():
        raise RuntimeError("Native curl unavailable; no dependency installed")
    args = [str(curl), "--disable", "--silent", "--show-error", "--fail", "--location",
            "--proto", "=https", "--connect-timeout", "15", "--max-time", "240" if byte_range is None else "120",
            "--max-filesize", str(max_bytes), "--user-agent", UA,
            "--dump-header", "-", "--output", str(output)]
    if byte_range is not None:
        args += ["--range", f"{byte_range[0]}-{byte_range[1]}"]
    result = subprocess.run(args + [url], capture_output=True, timeout=250)
    if result.returncode:
        raise ConnectionError("Verified HTTPS transfer failed; native curl exit=" + str(result.returncode))
    return result.stdout.decode("iso-8859-1")


def json_save(path, data):
    path.parent.mkdir(parents=True, exist_ok=True)
    pending = path.with_suffix(path.suffix + ".tmp")
    pending.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    pending.replace(path)


def archive_audit(path):
    entries, licenses, expanded = [], [], 0
    with zipfile.ZipFile(path) as archive:
        if len(archive.infolist()) > 20000:
            raise ValueError("Too many archive entries")
        for entry in archive.infolist():
            normalized = entry.filename.replace("\\", "/")
            posix, windows = PurePosixPath(normalized), PureWindowsPath(normalized)
            if posix.is_absolute() or windows.is_absolute() or windows.drive or ".." in posix.parts:
                raise ValueError("Unsafe archive path")
            if stat.S_ISLNK(entry.external_attr >> 16):
                raise ValueError("Archive contains a symlink")
            expanded += entry.file_size
            if expanded > MAX_EXPANDED:
                raise ValueError("Archive expanded-size bound exceeded")
            entries.append({"path": normalized, "bytes": entry.file_size, "crc32": entry.CRC})
            if not entry.is_dir() and "licen" in posix.name.lower() and posix.suffix.lower() in {".txt", ".md", ".html"}:
                if entry.file_size > 256 * 1024:
                    raise ValueError("License file exceeds bound")
                raw = archive.read(entry)
                text = raw.decode("utf-8-sig", errors="replace")
                licenses.append({"path": normalized, "sha256": hashlib.sha256(raw).hexdigest(), "text": text})
        bad = archive.testzip()
        if bad:
            raise ValueError("Archive CRC mismatch")
    record = {"entries": entries, "entryCount": len(entries), "expandedBytes": expanded,
              "crcPassed": True, "safePaths": True, "extracted": False, "licenses": licenses}
    json_save(path.with_suffix(".inventory.json"), record)
    return {key: record[key] for key in ["entryCount", "expandedBytes", "crcPassed", "safePaths", "extracted"]} | {
        "licenseFiles": [x["path"] for x in licenses],
        "inventory": path.with_suffix(".inventory.json").relative_to(ROOT).as_posix()}


def ranged_zip(url, destination, refresh_url):
    """Bounded HTTP Range transfer, using only the same official free CDN URL."""
    req = urllib.request.Request(url, headers={"User-Agent": UA, "Range": "bytes=0-0", "Accept-Encoding": "identity"})
    with urllib.request.urlopen(req, timeout=30) as response:
        header = response.headers.get("Content-Range", "")
        match = re.fullmatch(r"bytes 0-0/(\d+)", header)
        if response.status != 206 or not match or len(response.read(2)) != 1:
            raise ValueError("Official CDN did not honor bounded Range request")
        total = int(match.group(1))
        host = urllib.parse.urlsplit(response.geturl()).hostname
    chunks = destination.parent / (destination.stem + ".ranges")
    chunks.mkdir(exist_ok=True)
    used = sum(x.stat().st_size for x in BASE.rglob("*") if x.is_file())
    cached = sum(x.stat().st_size for x in chunks.glob("*.bin"))
    if total <= 0 or used + max(0, total - cached) + total > MAX_BYTES:
        raise ValueError("Range transfer peak budget exceeded")
    block = 512 * 1024
    link = {"url": url, "created": time.monotonic()}
    link_lock = threading.Lock()

    def fresh_url():
        with link_lock:
            # This official provider issues public CDN links valid for 60 seconds.
            if time.monotonic() - link["created"] > 25:
                link["url"] = refresh_url()
                link["created"] = time.monotonic()
            return link["url"]

    def receive(index):
        start, end = index * block, min((index + 1) * block, total) - 1
        path = chunks / (str(index) + ".bin")
        if path.exists():
            if path.stat().st_size != end - start + 1:
                raise ValueError("Incomplete prior chunk retained")
            return index
        for attempt in range(3):
            try:
                pending = path.with_suffix(".pending")
                headers = binary_transport(fresh_url(), pending, (start, end), end - start + 1)
                ranges = re.findall(r"(?im)^content-range:\s*([^\r\n]+)", headers)
                if not ranges or ranges[-1] != f"bytes {start}-{end}/{total}":
                    raise ValueError("Exact Content-Range mismatch")
                raw = pending.read_bytes()
                if len(raw) != end - start + 1:
                    if attempt == 2:
                        raise ValueError("Range length mismatch")
                    time.sleep(1)
                    continue
                with path.open("xb") as output:
                    output.write(raw)
                pending.unlink()
                return index
            except urllib.error.HTTPError as error:
                if error.code not in {429, 500, 502, 503, 504} or attempt == 2:
                    raise
                time.sleep(2 * (attempt + 1))
            except (urllib.error.URLError, TimeoutError, ConnectionError):
                if attempt == 2:
                    raise
                time.sleep(1)
        raise RuntimeError("Range unavailable")

    count = (total + block - 1) // block
    completed = 0
    with ThreadPoolExecutor(max_workers=4) as pool:
        futures = [pool.submit(receive, index) for index in range(count)]
        for future in as_completed(futures):
            future.result(); completed += 1
            if completed % 16 == 0 or completed == count:
                print(json.dumps({"file": destination.name, "verifiedRanges": completed, "totalRanges": count}), flush=True)
    staging = destination.with_suffix(destination.suffix + ".assembled.part")
    if staging.exists():
        raise FileExistsError("Prior assembled file retained")
    with staging.open("xb") as output:
        for index in range(count):
            with (chunks / (str(index) + ".bin")).open("rb") as source:
                output.write(source.read())
    if staging.stat().st_size != total:
        raise ValueError("Assembled archive length mismatch")
    # Validate CRC before publishing the final source ZIP or removing owned chunks.
    with zipfile.ZipFile(staging) as archive:
        if archive.testzip():
            raise ValueError("Assembled archive CRC mismatch")
    staging.replace(destination)
    record = {"path": destination.relative_to(ROOT).as_posix(), "bytes": total,
              "sha256": sha(destination), "finalHost": host, "upstreamMd5Verified": False,
              "transfer": "exact HTTP Range, max 4 connections", "rangeCount": count,
              "archiveAudit": archive_audit(destination)}
    for index in range(count):
        (chunks / (str(index) + ".bin")).unlink()
    chunks.rmdir()
    previous = destination.with_suffix(destination.suffix + ".part")
    if previous.exists():
        with destination.open("rb") as source:
            same_prefix = hashlib.sha256(source.read(previous.stat().st_size)).hexdigest() == sha(previous)
        if same_prefix:
            record["retiredMatchingInitialPartialBytes"] = previous.stat().st_size
            previous.unlink()
    return record


def fetch_file(url, destination, expected=None, refresh_url=None):
    destination = destination.resolve()
    if not destination.is_relative_to(BASE.resolve()):
        raise ValueError("Destination outside owned source directory")
    destination.parent.mkdir(parents=True, exist_ok=True)
    if destination.exists():
        if expected and destination.stat().st_size == expected["size"] and hashlib.md5(destination.read_bytes()).hexdigest() == expected["md5"]:
            return {"path": destination.relative_to(ROOT).as_posix(), "bytes": destination.stat().st_size, "sha256": sha(destination), "upstreamMd5Verified": True}
        raise FileExistsError("Refusing to replace existing unverified file")
    if destination.suffix.lower() == ".zip" and destination.parent.name == "Quaternius":
        if refresh_url is None:
            raise ValueError("Official short-lived download requires a link renewer")
        return ranged_zip(url, destination, refresh_url)
    staging = destination.with_suffix(destination.suffix + ".part")
    retained = []
    for attempt in range(3):
        if staging.exists():
            snapshot = staging.with_name(staging.name + ".incomplete-" + str(time.time_ns()))
            staging.rename(snapshot)
            retained.append({"path": snapshot.relative_to(ROOT).as_posix(), "bytes": snapshot.stat().st_size, "sha256": sha(snapshot)})
        used = sum(x.stat().st_size for x in BASE.rglob("*") if x.is_file())
        count, md5, digest = 0, hashlib.md5(), hashlib.sha256()
        try:
            headers = binary_transport(url, staging, max_bytes=MAX_BYTES - used)
        except ConnectionError:
            if attempt == 2:
                raise
            continue
        final_host = urllib.parse.urlsplit(url).hostname
        lengths = re.findall(r"(?im)^content-length:\s*(\d+)", headers)
        declared = lengths[-1] if lengths else None
        raw = staging.read_bytes(); count = len(raw); md5.update(raw); digest.update(raw)
        valid = count > 0 and (not declared or count == int(declared))
        valid = valid and (not expected or count == expected["size"] and md5.hexdigest() == expected["md5"])
        if valid:
            break
        if attempt == 2:
            raise ValueError("HTTP size or official metadata integrity mismatch")
        time.sleep(1)
    staging.replace(destination)
    record = {"path": destination.relative_to(ROOT).as_posix(), "bytes": count,
              "sha256": digest.hexdigest(), "finalHost": final_host,
              "upstreamMd5Verified": bool(expected)}
    if retained:
        record["retainedIncompleteFiles"] = retained
    if destination.suffix.lower() == ".zip":
        record["archiveAudit"] = archive_audit(destination)
    return record


def license_evidence(url):
    raw = small(url)
    if b"CC0" not in raw:
        raise ValueError("CC0 not present in official source page")
    return {"url": url, "pageSha256": hashlib.sha256(raw).hexdigest(), "cc0Observed": True}


def itch(slug, filename, old):
    base = "https://quaternius.itch.io/" + slug
    license_record = license_evidence(base)
    if old and old.get("status") == "received":
        return old
    purchase = small(base + "/purchase").decode("utf-8")
    options = re.search(r"init_GamePurchase\([^,]+,\s*(\{.*?\})\);", purchase)
    if not options or json.loads(options.group(1))["min_price"] != 0:
        raise ValueError("Official free purchase option not confirmed")
    fields = Metadata(); fields.feed(purchase)
    if not fields.csrf:
        raise ValueError("Nonempty public CSRF metadata missing")
    result = json.loads(small(base + "/download_url", {"csrf_token": fields.csrf}, base + "/purchase"))
    page_url = result.get("url", "")
    if urllib.parse.urlsplit(page_url).hostname != "quaternius.itch.io":
        raise ValueError("Unexpected anonymous download-page origin")
    download_page = small(page_url).decode("utf-8")
    fields = Metadata(); fields.feed(download_page)
    # Only the publicly free Standard upload can appear in this anonymous flow.
    if filename not in download_page or len(fields.uploads) != 1 or not fields.csrf:
        raise ValueError("Ambiguous or non-Standard free download")
    origin = {"host": None, "path": None}

    def free_file_url():
        source = json.loads(small(base + "/file/" + fields.uploads[0], {"csrf_token": fields.csrf}, page_url))
        if source.get("errors") or not source.get("url") or source.get("external"):
            raise ValueError("Free file endpoint did not yield a normal downloadable file")
        parsed = urllib.parse.urlsplit(source["url"])
        if parsed.scheme != "https":
            raise ValueError("Unexpected file URL scheme")
        if origin["host"] is None:
            origin.update(host=parsed.hostname, path=parsed.path)
        elif (parsed.hostname, parsed.path) != (origin["host"], origin["path"]):
            raise ValueError("Official upload changed during transfer")
        return source["url"]

    file_record = fetch_file(free_file_url(), BASE / "Quaternius" / filename, refresh_url=free_file_url)
    return {"id": slug, "provider": "Quaternius", "page": base, "license": "CC0-1.0",
            "licenseEvidence": license_record, "edition": "Standard", "pricePaid": 0,
            "status": "received", "files": [file_record]}


def poly(asset, old):
    if old and old.get("status") == "received":
        return old
    folder = BASE / "PolyHaven" / asset
    evidence = license_evidence("https://polyhaven.com/license")
    info = json.loads(small("https://api.polyhaven.com/info/" + asset))
    files = json.loads(small("https://api.polyhaven.com/files/" + asset))
    json_save(folder / "info.json", info); json_save(folder / "files.json", files)
    row = files["fbx"]["1k"]["fbx"]
    selection = [(asset + "_1k.fbx", row)] + list(row.get("include", {}).items())
    arm = files["arm"]["1k"]["png"]
    selection.append(("textures/" + Path(urllib.parse.urlsplit(arm["url"]).path).name, arm))
    received = []
    for name, record in selection:
        received.append(fetch_file(record["url"], folder / name, record))
    return {"id": asset, "provider": "Poly Haven", "credit": "Powered by Poly Haven",
            "page": "https://polyhaven.com/a/" + asset, "license": "CC0-1.0", "licenseEvidence": evidence,
            "resolution": "1k", "status": "received", "files": received}


def ambient(asset, old):
    if old and old.get("status") == "received":
        return old
    page = "https://ambientcg.com/view?id=" + asset
    evidence = license_evidence("https://docs.ambientcg.com/license/")
    body = small(page).decode("utf-8")
    filename = asset + "_2K-JPG.zip"
    if filename not in body:
        raise ValueError("Requested free material not listed on official page")
    url = "https://ambientcg.com/get?file=" + filename
    record = fetch_file(url, BASE / "ambientCG" / filename)
    return {"id": asset, "provider": "ambientCG", "page": page, "license": "CC0-1.0",
            "licenseEvidence": evidence, "resolution": "2K-JPG", "status": "received", "files": [record]}


def verify(manifest):
    required = {"modular-sci-fi-megakit", "sci-fi-essentials-kit", "metal_toolbox", "metal_jerrycan_green", "barrel_03", "MetalPlates015A", "Concrete034"}
    if len(manifest["assets"]) != len(required) or {x["id"] for x in manifest["assets"]} != required:
        raise ValueError("Exactly seven selected asset groups required")
    count, total, expanded, seen = 0, 0, 0, set()
    for asset in manifest["assets"]:
        if asset["status"] != "received":
            raise ValueError("Unreceived asset remains")
        if asset["license"] != "CC0-1.0" or not asset["licenseEvidence"]["cc0Observed"]:
            raise ValueError("License evidence missing")
        if asset.get("provider") == "Quaternius" and (asset.get("edition") != "Standard" or asset.get("pricePaid") != 0):
            raise ValueError("Only free Standard packages are allowed")
        for item in asset["files"]:
            path = (ROOT / item["path"]).resolve()
            if path in seen:
                raise ValueError("Duplicate resource path")
            seen.add(path)
            if not path.is_relative_to(BASE.resolve()) or not path.is_file() or path.stat().st_size != item["bytes"] or sha(path) != item["sha256"]:
                raise ValueError("Local file integrity mismatch")
            if path.suffix.lower() == ".zip":
                expanded += archive_audit(path)["expandedBytes"]
            count += 1; total += item["bytes"]
    if expanded > MAX_EXPANDED:
        raise ValueError("Combined expanded-size bound exceeded")
    actual = sum(x.stat().st_size for x in BASE.rglob("*") if x.is_file())
    if actual > MAX_BYTES:
        raise ValueError("Actual source directory exceeds intake budget")
    return {"assetGroups": len(manifest["assets"]), "files": count, "downloadBytes": total,
            "combinedArchiveExpandedBytes": expanded,
            "actualSourceDirectoryBytes": actual,
            "integrityPassed": True, "importedIntoUnity": False, "executedDownloadedFiles": False,
            "unityCompatibilityPassed": False}


def main():
    args = argparse.ArgumentParser(); args.add_argument("--verify", action="store_true")
    args.add_argument("--only", nargs="+", help="Receive only selected IDs; do not claim complete intake")
    options = args.parse_args()
    BASE.mkdir(parents=True, exist_ok=True)
    manifest = json.loads(MANIFEST.read_text(encoding="utf-8")) if MANIFEST.exists() else {
        "version": 1, "receivedUtc": datetime.now(timezone.utc).isoformat(), "assets": [],
        "scope": "Official free raw sources only; no import or gameplay changes", "intakeLimitBytes": MAX_BYTES}
    if not options.verify:
        terms_url = "https://raw.githubusercontent.com/Poly-Haven/Public-API/master/ToS.md"
        terms = small(terms_url)
        (BASE / "PolyHaven-API-Terms.txt").write_bytes(terms)
        manifest["polyHavenApiTerms"] = {"url": terms_url, "sha256": hashlib.sha256(terms).hexdigest()}
        tasks = [("modular-sci-fi-megakit", lambda old: itch("modular-sci-fi-megakit", "Modular SciFi MegaKit[Standard].zip", old)),
                 ("sci-fi-essentials-kit", lambda old: itch("sci-fi-essentials-kit", "Sci-Fi Essentials Kit[Standard].zip", old))]
        tasks += [(name, lambda old, name=name: poly(name, old)) for name in ["metal_toolbox", "metal_jerrycan_green", "barrel_03"]]
        tasks += [(name, lambda old, name=name: ambient(name, old)) for name in ["MetalPlates015A", "Concrete034"]]
        if options.only:
            if not set(options.only).issubset({name for name, _ in tasks}):
                raise ValueError("Unknown selected asset ID")
            tasks = [(name, task) for name, task in tasks if name in options.only]
        for name, task in tasks:
            print(json.dumps({"asset": name, "status": "receiving"}), flush=True)
            old = next((x for x in manifest["assets"] if x["id"] == name), None)
            try:
                record = task(old)
            except Exception as error:
                # Do not print URLs containing anonymous download tokens.
                record = {"id": name, "status": "failed", "errorType": type(error).__name__,
                          "error": str(error) if not isinstance(error, urllib.error.URLError) else "Official network request failed"}
                if isinstance(error, urllib.error.HTTPError):
                    record["httpStatus"] = error.code
                print(json.dumps(record), flush=True)
            manifest["assets"] = [x for x in manifest["assets"] if x["id"] != name] + [record]
            json_save(MANIFEST, manifest)
            print(json.dumps({"asset": name, "status": record["status"]}), flush=True)
        if options.only:
            print(json.dumps({"partialIntakeOnly": True, "selected": options.only}), flush=True)
            return
    result = verify(manifest)
    json_save(BASE / "Verification.json", result)
    print(json.dumps(result), flush=True)


if __name__ == "__main__":
    main()
