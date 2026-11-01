EchoStation.Rendering.Editor.SceneSafetyGuard.RequireClean();
const string target="Assets/EchoStation/InfiltrationRefinement/Generated/DR_CrateMask.png";
string full=System.IO.Path.GetFullPath(target),owned=System.IO.Path.GetFullPath("Assets/EchoStation/InfiltrationRefinement/Generated")+System.IO.Path.DirectorySeparatorChar;
if(!full.StartsWith(owned,System.StringComparison.OrdinalIgnoreCase)||!System.IO.File.Exists(full))throw new System.InvalidOperationException("Preserve unknown texture");
long before=new System.IO.FileInfo(full).Length;UnityEngine.Texture2D source=null,packed=null;UnityEngine.RenderTexture rt=null,previous=UnityEngine.RenderTexture.active;
try
{
 source=new UnityEngine.Texture2D(2,2,UnityEngine.TextureFormat.RGBA32,false,true);source.LoadImage(System.IO.File.ReadAllBytes("Assets/EchoStation/InfiltrationRefinement/ThirdParty/T_Props_Crates_ORM.png"));source.filterMode=UnityEngine.FilterMode.Bilinear;
 rt=UnityEngine.RenderTexture.GetTemporary(1024,1024,0,UnityEngine.RenderTextureFormat.ARGB32,UnityEngine.RenderTextureReadWrite.Linear);UnityEngine.Graphics.Blit(source,rt);UnityEngine.RenderTexture.active=rt;
 packed=new UnityEngine.Texture2D(1024,1024,UnityEngine.TextureFormat.RGBA32,false,true);packed.ReadPixels(new UnityEngine.Rect(0,0,1024,1024),0,0);packed.Apply();var pixels=packed.GetPixels32();for(int i=0;i<pixels.Length;i++){var p=pixels[i];pixels[i]=new UnityEngine.Color32(p.b,p.r,0,(byte)(255-p.g));}packed.SetPixels32(pixels);packed.Apply();System.IO.File.WriteAllBytes(full,packed.EncodeToPNG());UnityEditor.AssetDatabase.ImportAsset(target,UnityEditor.ImportAssetOptions.ForceSynchronousImport);
 var row=new{beforeBytes=before,afterBytes=new System.IO.FileInfo(full).Length,width=1024,height=1024,rawAuthorOrmPreserved=true,engineeringChannelRepack="R=Metallic G=AO A=1-Roughness",oldProjectAssetsChanged=false};System.IO.File.WriteAllText("Docs/Tests/InfiltrationPrototype/Evidence/A01D/MaskOptimization_A.json",Newtonsoft.Json.JsonConvert.SerializeObject(row));return row;
}
finally{UnityEngine.RenderTexture.active=previous;if(rt)UnityEngine.RenderTexture.ReleaseTemporary(rt);if(source)UnityEngine.Object.DestroyImmediate(source);if(packed)UnityEngine.Object.DestroyImmediate(packed);}
