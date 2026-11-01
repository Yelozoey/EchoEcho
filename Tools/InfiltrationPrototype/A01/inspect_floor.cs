var source=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(EchoStation.InfiltrationArt.Editor.ArtSampleAssets.PathFor("Floor"));
var rows=new Newtonsoft.Json.Linq.JArray();
foreach(var f in source.GetComponentsInChildren<UnityEngine.MeshFilter>())
{
 var m=f.sharedMesh;var v=m.vertices;var tri=m.triangles;var matrix=source.transform.worldToLocalMatrix*f.transform.localToWorldMatrix;var faces=new Newtonsoft.Json.Linq.JArray();
 for(int i=0;i<tri.Length;i+=3){var a=matrix.MultiplyPoint3x4(v[tri[i]]);var b=matrix.MultiplyPoint3x4(v[tri[i+1]]);var c=matrix.MultiplyPoint3x4(v[tri[i+2]]);var n=UnityEngine.Vector3.Cross(b-a,c-a).normalized;faces.Add(new Newtonsoft.Json.Linq.JObject{{"y",(a.y+b.y+c.y)/3},{"normalY",n.y}});}
 rows.Add(new Newtonsoft.Json.Linq.JObject{{"name",m.name},{"triangles",faces},{"cull",f.GetComponent<UnityEngine.Renderer>().sharedMaterial.GetFloat("_Cull")}});
}
return rows;
