var go=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(EchoStation.InfiltrationArt.Editor.ArtSampleAssets.PathFor("Wall"));
var rows=new Newtonsoft.Json.Linq.JArray();
foreach(var f in go.GetComponentsInChildren<UnityEngine.MeshFilter>())
{
 var m=f.sharedMesh;var v=m.vertices;var tri=m.triangles;var mat=go.transform.worldToLocalMatrix*f.transform.localToWorldMatrix;double plus=0,minus=0,other=0;
 for(int i=0;i<tri.Length;i+=3){var a=mat.MultiplyPoint3x4(v[tri[i]]);var b=mat.MultiplyPoint3x4(v[tri[i+1]]);var c=mat.MultiplyPoint3x4(v[tri[i+2]]);var n=UnityEngine.Vector3.Cross(b-a,c-a);if(n.normalized.x>.15f)plus+=n.magnitude*.5;else if(n.normalized.x<-.15f)minus+=n.magnitude*.5;else other+=n.magnitude*.5;}
 rows.Add(new Newtonsoft.Json.Linq.JObject{{"mesh",m.name},{"vertices",m.vertexCount},{"plusX",plus},{"minusX",minus},{"other",other}});
}
return rows;
