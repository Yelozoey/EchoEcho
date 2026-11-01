"""File-level actual-source checks, not Unity compatibility or visual acceptance."""
import hashlib,json,unittest,zipfile
from pathlib import Path,PurePosixPath
import receive_rover as source
class RoverSourceTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.receipt=json.loads(source.RECEIPT.read_text(encoding='utf-8'))
        cls.verified=source.verify()
    def test_author_official_source_and_cc0_evidence(self):
        self.assertEqual(self.receipt['page'],source.PAGE);self.assertEqual(self.receipt['download'],source.URL)
        self.assertEqual(self.receipt['author'],'JamesWhite');self.assertEqual(self.receipt['license'],'CC0-1.0');self.assertTrue(self.receipt['licenseEvidence']['cc0Observed'])
    def test_full_bytes_sha_crc_and_safe_paths(self):
        self.assertTrue(self.verified['archiveAudit']['crcPassed']);self.assertTrue(self.verified['archiveAudit']['safePaths']);self.assertGreater(self.verified['bytes'],0)
        with zipfile.ZipFile(source.ARCHIVE) as z:self.assertIsNone(z.testzip())
    def test_has_source_model_and_no_execution(self):
        self.assertTrue(set(self.verified['formats'])&{'.blend','.fbx','.obj','.dae','.gltf','.glb'})
        self.assertFalse(self.verified['executed']);self.assertFalse(self.verified['imported'])
        self.assertFalse(set(self.verified['formats'])&{'.exe','.dll','.cs','.ps1','.bat'})
    def test_existing_megakit_is_unchanged_and_complete(self):
        root=source.ROOT/'Docs/Assets/Sources/SalvagePrototype'
        manifest=json.loads((root/'Manifest.json').read_text(encoding='utf-8'))
        file=next(r for r in manifest['assets'] if r['id']=='modular-sci-fi-megakit')['files'][0]
        archive=source.ROOT/file['path'];self.assertEqual(hashlib.sha256(archive.read_bytes()).hexdigest(),file['sha256'])
        with zipfile.ZipFile(archive) as z:self.assertIsNone(z.testzip());self.assertFalse(any(Path(p.filename).suffix.lower() in {'.unity','.prefab','.unitypackage'} for p in z.infolist()))
if __name__=='__main__':unittest.main(verbosity=2)
