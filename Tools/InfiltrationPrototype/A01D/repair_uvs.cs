EchoStation.Rendering.Editor.SceneSafetyGuard.RequireClean();
var rows=new Newtonsoft.Json.Linq.JArray();
foreach(string kind in EchoStation.InfiltrationRefinement.Editor.RefinementAssets.Kinds)
{
 string path=EchoStation.InfiltrationRefinement.Editor.RefinementAssets.Prefab(kind);var root=UnityEditor.PrefabUtility.LoadPrefabContents(path);
 try
 {
  int index=0;bool changed=false;foreach(var f in root.GetComponentsInChildren<UnityEngine.MeshFilter>())
  {
   if(f.sharedMesh.uv2.Length==0)
   {
    var original=f.sharedMesh;var mesh=UnityEngine.Object.Instantiate(original);mesh.name="DR_"+kind+"_UV2_"+index;
    if(!UnityEditor.Unwrapping.GenerateSecondaryUVSet(mesh))throw new System.InvalidOperationException("Unwrap failed "+kind);
    string output=EchoStation.InfiltrationRefinement.Editor.RefinementScene.Generated+"/"+mesh.name+".asset";if(System.IO.File.Exists(output))throw new System.InvalidOperationException("Preserve mesh output");UnityEditor.AssetDatabase.CreateAsset(mesh,output);EchoStation.InfiltrationRefinement.Editor.RefinementScene.Save(mesh);f.sharedMesh=mesh;changed=true;rows.Add(new Newtonsoft.Json.Linq.JObject{{"kind",kind},{"sourceMesh",UnityEditor.AssetDatabase.GetAssetPath(original)},{"ownedMesh",output},{"uv2Count",mesh.uv2.Length},{"sourceMeshUnchanged",original.uv2.Length==0}});
   }
   index++;
  }
  if(changed&&!UnityEditor.PrefabUtility.SaveAsPrefabAsset(root,path))throw new System.IO.IOException("Save owned UV2 prefab");
 }
 finally{UnityEditor.PrefabUtility.UnloadPrefabContents(root);}
}
System.IO.File.WriteAllText("Docs/Tests/InfiltrationPrototype/Evidence/A01D/LightmapUvs_A.json",rows.ToString());return rows;
