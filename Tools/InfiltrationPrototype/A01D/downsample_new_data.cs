EchoStation.Rendering.Editor.SceneSafetyGuard.RequireClean();
var rows=new Newtonsoft.Json.Linq.JArray();
foreach(string suffix in new[]{"Normal","ORM"})
{
 string path="Assets/EchoStation/InfiltrationRefinement/ThirdParty/T_Props_Crates_"+suffix+".png";
 var importer=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);if(!importer||importer.maxTextureSize!=1024)throw new System.InvalidOperationException("Expected owned 1K import");
 var bytes=System.IO.File.ReadAllBytes(path);UnityEngine.Texture2D input=null,output=null;UnityEngine.RenderTexture rt=null,previous=UnityEngine.RenderTexture.active;
 try
 {
  input=new UnityEngine.Texture2D(2,2,UnityEngine.TextureFormat.RGBA32,false,true);input.LoadImage(bytes);int oldWidth=input.width,oldHeight=input.height;if(oldWidth<=1024&&oldHeight<=1024)throw new System.InvalidOperationException("Preserve already converted data");input.filterMode=UnityEngine.FilterMode.Bilinear;
  rt=UnityEngine.RenderTexture.GetTemporary(1024,1024,0,UnityEngine.RenderTextureFormat.ARGB32,UnityEngine.RenderTextureReadWrite.Linear);UnityEngine.Graphics.Blit(input,rt);UnityEngine.RenderTexture.active=rt;output=new UnityEngine.Texture2D(1024,1024,UnityEngine.TextureFormat.RGBA32,false,true);output.ReadPixels(new UnityEngine.Rect(0,0,1024,1024),0,0);output.Apply();var result=output.EncodeToPNG();System.IO.File.WriteAllBytes(path,result);UnityEditor.AssetDatabase.ImportAsset(path,UnityEditor.ImportAssetOptions.ForceSynchronousImport);
  rows.Add(new Newtonsoft.Json.Linq.JObject{{"path",path},{"beforeBytes",bytes.Length},{"afterBytes",result.Length},{"oldWidth",oldWidth},{"oldHeight",oldHeight},{"width",1024},{"height",1024},{"dataLinear",true},{"authorOriginalZipPreserved",true},{"intakeRecord","SelectedTextures_A.json is the preconversion snapshot"}});
 }
 finally{UnityEngine.RenderTexture.active=previous;if(rt)UnityEngine.RenderTexture.ReleaseTemporary(rt);if(input)UnityEngine.Object.DestroyImmediate(input);if(output)UnityEngine.Object.DestroyImmediate(output);}
}
System.IO.File.WriteAllText("Docs/Tests/InfiltrationPrototype/Evidence/A01D/DerivedDataTextures_A.json",rows.ToString());return new{textures=rows,lightingDataSetter=typeof(UnityEditor.Lightmapping).GetProperty("lightingDataAsset").CanWrite};
