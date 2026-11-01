"""File-level adversarial archive checks; never runs Unity or network requests."""
import importlib.util
import json
import stat
import struct
import tempfile
import unittest
import zipfile
from pathlib import Path

SPEC = importlib.util.spec_from_file_location("salvage_intake", Path(__file__).with_name("receive_salvage_sources.py"))
INTAKE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(INTAKE)
EVIDENCE = INTAKE.ROOT / "Docs/Tests/SalvagePrototype/Evidence/R0-A01"


class ArchiveChecks(unittest.TestCase):
    def setUp(self):
        EVIDENCE.mkdir(parents=True, exist_ok=True)
        self.temp = tempfile.TemporaryDirectory(prefix="archive-check-", dir=EVIDENCE)
        self.folder = Path(self.temp.name)

    def tearDown(self):
        self.temp.cleanup()

    def create(self, name, content=b"sample"):
        path = self.folder / "sample.zip"
        with zipfile.ZipFile(path, "w", compression=zipfile.ZIP_STORED) as output:
            output.writestr(name, content)
        return path

    def test_valid_archive_keeps_original_files_unextracted(self):
        path = self.create("asset/License.txt", b"CC0 1.0 Universal")
        result = INTAKE.archive_audit(path)
        self.assertTrue(result["crcPassed"])
        self.assertEqual(result["licenseFiles"], ["asset/License.txt"])
        self.assertFalse(result["extracted"])
        self.assertFalse((self.folder / "asset").exists())

    def test_parent_escape_rejected(self):
        with self.assertRaisesRegex(ValueError, "Unsafe archive path"):
            INTAKE.archive_audit(self.create("../escaped.txt"))

    def test_windows_absolute_path_rejected(self):
        with self.assertRaisesRegex(ValueError, "Unsafe archive path"):
            INTAKE.archive_audit(self.create("C:/escaped.txt"))

    def test_unix_absolute_path_rejected(self):
        with self.assertRaisesRegex(ValueError, "Unsafe archive path"):
            INTAKE.archive_audit(self.create("/escaped.txt"))

    def test_symbolic_link_rejected(self):
        path = self.folder / "link.zip"
        entry = zipfile.ZipInfo("asset/link")
        entry.create_system = 3
        entry.external_attr = (stat.S_IFLNK | 0o777) << 16
        with zipfile.ZipFile(path, "w") as output:
            output.writestr(entry, "../../escaped")
        with self.assertRaisesRegex(ValueError, "symlink"):
            INTAKE.archive_audit(path)

    def test_damaged_content_crc_rejected(self):
        path = self.create("asset/data.bin", b"meaningful payload")
        raw = bytearray(path.read_bytes())
        name_size, extra_size = struct.unpack_from("<HH", raw, 26)
        raw[30 + name_size + extra_size] ^= 1
        path.write_bytes(raw)
        with self.assertRaises((ValueError, zipfile.BadZipFile)):
            INTAKE.archive_audit(path)

    def test_empty_form_field_does_not_clear_public_meta_value(self):
        fields = INTAKE.Metadata()
        fields.feed('<meta name="csrf_token" value="anonymous-test-value"><input name="csrf_token" value="">')
        self.assertEqual(fields.csrf, "anonymous-test-value")

    def test_no_token_is_not_invented(self):
        fields = INTAKE.Metadata()
        fields.feed('<input name="csrf_token" value="">')
        self.assertIsNone(fields.csrf)

    def test_missing_selected_assets_cannot_pass_verification(self):
        with self.assertRaisesRegex(ValueError, "seven selected"):
            INTAKE.verify({"assets": []})


if __name__ == "__main__":
    result = unittest.TextTestRunner(verbosity=2).run(unittest.defaultTestLoader.loadTestsFromTestCase(ArchiveChecks))
    record = {"testsRun": result.testsRun, "failures": len(result.failures), "errors": len(result.errors),
              "successful": result.wasSuccessful(), "scope": "Local adversarial archives; no network, extraction, or Unity import"}
    (EVIDENCE / "ArchiveSafety_A.json").write_text(json.dumps(record, indent=2), encoding="utf-8")
    raise SystemExit(0 if result.wasSuccessful() else 1)
