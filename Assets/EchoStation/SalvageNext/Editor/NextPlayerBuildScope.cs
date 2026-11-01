using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace EchoStation.SalvageNext.Editor
{
    public sealed class NextPlayerBuildScope:IDisposable
    {
        readonly UnityEngine.Object[] objects;readonly string[] json;readonly byte[][] disk;readonly bool[] dirty;
        static readonly string[] Paths={"ProjectSettings/ProjectSettings.asset","ProjectSettings/GraphicsSettings.asset","ProjectSettings/QualitySettings.asset","ProjectSettings/UnityConnectSettings.asset"};
        readonly string receipt;readonly int width,height;readonly bool resize,background,hdr,supportHdr,timing,autoApi;readonly FullScreenMode mode;readonly GraphicsDeviceType[] apis;readonly string pipelinePath;bool disposed;
        public NextPlayerBuildScope(string receipt)
        {
            this.receipt=receipt;objects=Paths.Select(p=>AssetDatabase.LoadAllAssetsAtPath(p).Single()).ToArray();json=objects.Select(EditorJsonUtility.ToJson).ToArray();disk=Paths.Select(File.ReadAllBytes).ToArray();dirty=objects.Select(EditorUtility.IsDirty).ToArray();
            width=PlayerSettings.defaultScreenWidth;height=PlayerSettings.defaultScreenHeight;mode=PlayerSettings.fullScreenMode;resize=PlayerSettings.resizableWindow;background=PlayerSettings.runInBackground;hdr=PlayerSettings.useHDRDisplay;supportHdr=PlayerSettings.allowHDRDisplaySupport;timing=PlayerSettings.enableFrameTimingStats;autoApi=PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64);apis=PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows64);pipelinePath=AssetDatabase.GetAssetPath(GraphicsSettings.defaultRenderPipeline);Directory.CreateDirectory(Path.GetDirectoryName(receipt));for(int i=0;i<Paths.Length;i++)File.WriteAllText(receipt+".before-"+Path.GetFileName(Paths[i])+".json",json[i]);
            try
            {
                PlayerSettings.defaultScreenWidth=1920;PlayerSettings.defaultScreenHeight=1080;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=false;PlayerSettings.useHDRDisplay=false;PlayerSettings.allowHDRDisplaySupport=false;PlayerSettings.enableFrameTimingStats=true;PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,new[]{GraphicsDeviceType.Direct3D11});
                var so=new SerializedObject(objects[2]);var list=so.FindProperty("m_QualitySettings");list.arraySize=3;for(int i=0;i<3;i++){var item=list.GetArrayElementAtIndex(i);item.FindPropertyRelative("name").stringValue="SalvageNext_"+(RecoveryQuality)i;item.FindPropertyRelative("customRenderPipeline").objectReferenceValue=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(NextRendering.PipelinePath(i));}so.ApplyModifiedPropertiesWithoutUndo();GraphicsSettings.defaultRenderPipeline=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(NextRendering.PipelinePath(1));
            }
            catch{Dispose();throw;}
        }
        public void Dispose()
        {
            if(disposed)return;disposed=true;PlayerSettings.defaultScreenWidth=width;PlayerSettings.defaultScreenHeight=height;PlayerSettings.fullScreenMode=mode;PlayerSettings.resizableWindow=resize;PlayerSettings.runInBackground=background;PlayerSettings.useHDRDisplay=hdr;PlayerSettings.allowHDRDisplaySupport=supportHdr;PlayerSettings.enableFrameTimingStats=timing;PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,autoApi);PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,apis);EditorJsonUtility.FromJsonOverwrite(json[2],objects[2]);GraphicsSettings.defaultRenderPipeline=string.IsNullOrEmpty(pipelinePath)?null:AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(pipelinePath);
            EditorJsonUtility.FromJsonOverwrite(json[3],objects[3]);var native=objects.Select((o,i)=>EditorJsonUtility.ToJson(o)==json[i]).ToArray();var bytes=Paths.Select((p,i)=>File.ReadAllBytes(p).SequenceEqual(disk[i])).ToArray();for(int i=0;i<objects.Length;i++)if(native[i]&&!dirty[i])EditorUtility.ClearDirty(objects[i]);var flags=objects.Select((o,i)=>EditorUtility.IsDirty(o)==dirty[i]).ToArray();Directory.CreateDirectory(Path.GetDirectoryName(receipt));File.WriteAllText(receipt,new JObject{{"nativeExact",new JArray(native)},{"diskExact",new JArray(bytes)},{"dirtyExact",new JArray(flags)},{"paths",new JArray(Paths)},{"threeOwnPipelinesBuildOnly",true}}.ToString());if(native.Any(x=>!x)||bytes.Any(x=>!x)||flags.Any(x=>!x))throw new InvalidOperationException("Player build scope restoration mismatch; see "+receipt);
        }
    }
}
