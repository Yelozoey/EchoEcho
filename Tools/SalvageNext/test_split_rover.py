"""Independent source geometry conservation tests; no Unity compatibility claim."""
import hashlib,json,unittest
import split_rover as source
class RoverSplitTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.data=json.loads((source.BASE/'RoverParts.json').read_text(encoding='utf-8'))
        cls.v,cls.uv,cls.faces=source.obj((source.BASE/'Original/rover_mesh.obj').read_bytes())
    def test_original_source_license_and_hashes_preserved(self):
        self.assertEqual(self.data['license'],'CC0-1.0');self.assertEqual(self.data['author'],'JamesWhite')
        self.assertEqual(hashlib.sha256((source.BASE/'Original/rover_mesh.obj').read_bytes()).hexdigest(),self.data['originalObjSha256'])
        self.assertEqual(hashlib.sha256((source.BASE/'Original/rover_diffuse.png').read_bytes()).hexdigest(),self.data['textureSha256'])
    def test_exactly_eight_parts_and_all_polygons_accounted_once(self):
        self.assertEqual(len(self.data['parts']),8)
        indices=[i for part in self.data['parts'] for i in part['sourceFaces']]
        self.assertEqual(sorted(indices),list(range(len(self.faces))));self.assertEqual(len(self.faces),704)
        self.assertEqual(sum(p['name'].startswith('Wheel_') for p in self.data['parts']),6)
    def test_every_face_corner_position_and_uv_is_conserved(self):
        for part in self.data['parts']:
            raw=(source.ROOT/part['path']).read_bytes();self.assertEqual(hashlib.sha256(raw).hexdigest(),part['sha256'])
            v,uv,faces=source.obj(raw);self.assertEqual(len(faces),len(part['sourceFaces']))
            for converted,original_index in zip(faces,part['sourceFaces']):
                original=self.faces[original_index];self.assertEqual(len(converted),len(original))
                for (cv,ct),(ov,ot) in zip(converted,original):
                    for a in range(3):self.assertAlmostEqual(v[cv][a]/self.data['scale']+part['sourcePivot'][a],self.v[ov][a],places=6)
                    for a in range(2):self.assertAlmostEqual(uv[ct][a],self.uv[ot][a],places=8)
    def test_six_wheel_pivots_radius_and_measured_obj_handedness(self):
        self.assertEqual(self.data['axisSigns'],[-1,1,1])
        wheels=[p for p in self.data['parts'] if p['name'].startswith('Wheel_')]
        for wheel in wheels:
            self.assertGreater(wheel['faces'],0);self.assertGreater(wheel['radius'],.04);self.assertLess(wheel['radius'],.07)
            self.assertLess(abs(wheel['unityPosition'][0]),.3);self.assertGreater(wheel['unityPosition'][1],.04)
        self.assertEqual(len({round(p['unityPosition'][2],3) for p in wheels}),3)
        self.assertTrue(all(p['unityPosition'][0]<0 for p in wheels if '_Left_' in p['name']))
if __name__=='__main__':unittest.main(verbosity=2)
