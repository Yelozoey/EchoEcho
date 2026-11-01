using System;
using System.IO;
using System.Linq;
using EchoStation.Rendering.Editor;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.AddressableAssets;
namespace EchoStation.SalvageNext.Editor
{
    public static class NextAddressables
    {
        public const string Folder=FacilityAssets.Root+"/Addressables",SettingsPath=Folder+"/ES_Next_Addressables.asset",ProbePath=FacilityAssets.Generated+"/ES_Next_AddressableSmoke.txt",Address="es-next-smoke",Payload="EchoStation local Addressables 2.11.2 smoke",Evidence="Docs/Tests/SalvagePrototype/Evidence/R4-R03a-2";
        static bool pending;static int ticks;public static string Receipt{get;private set;}
        public static AddressableAssetSettings Settings=>AssetDatabase.LoadAssetAtPath<AddressableAssetSettings>(SettingsPath);
        public static void SaveOwned()
        {
            foreach(var path in AssetDatabase.FindAssets("",new[]{Folder}).Select(AssetDatabase.GUIDToAssetPath).Distinct())foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(path))if(asset&&EditorUtility.IsDirty(asset))AssetDatabase.SaveAssetIfDirty(asset);
        }
        public static void ConfigureLocal(AddressableAssetGroup group)
        {
            var schema=group.GetSchema<BundledAssetGroupSchema>();schema.UseDefaultSchemaSettings=false;schema.BuildPath.SetVariableByName(Settings,AddressableAssetSettings.kLocalBuildPath);schema.LoadPath.SetVariableByName(Settings,AddressableAssetSettings.kLocalLoadPath);schema.BundleMode=BundledAssetGroupSchema.BundlePackingMode.PackTogether;schema.Compression=BundledAssetGroupSchema.BundleCompressionMode.LZ4;schema.IncludeInBuild=true;schema.UseUnityWebRequestForLocalBundles=false;EditorUtility.SetDirty(schema);
        }
        public static void Prepare()
        {
            SceneSafetyGuard.RequireClean();if(AssetDatabase.LoadMainAssetAtPath(SettingsPath)||AddressableAssetSettingsDefaultObject.SettingsExists)throw new InvalidOperationException("Preserve existing Addressables settings.");Directory.CreateDirectory(Evidence);File.Copy("ProjectSettings/EditorBuildSettings.asset",Evidence+"/EditorBuildSettings.asset.before");var scenes=EditorBuildSettings.scenes.Select(s=>s.path+"|"+s.enabled).ToArray();var configs=EditorBuildSettings.GetConfigObjectNames();FacilityAssets.Folder(Folder);
            using(NextSdkSaveScope.Begin(Evidence+"/PrepareSaveScope_A.json","ProjectSettings/EditorBuildSettings.asset"))
            {
                var settings=AddressableAssetSettings.Create(Folder,"ES_Next_Addressables",false,true);settings.BuildRemoteCatalog=false;settings.DisableCatalogUpdateOnStartup=true;settings.BuildAddressablesWithPlayerBuild=AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;settings.ContentStateBuildPath="Library/com.unity.addressables/SalvageNextContentState";settings.profileSettings.SetValue(settings.activeProfileId,AddressableAssetSettings.kRemoteBuildPath,AddressableAssetSettings.kLocalBuildPathValue);settings.profileSettings.SetValue(settings.activeProfileId,AddressableAssetSettings.kRemoteLoadPath,AddressableAssetSettings.kLocalLoadPathValue);settings.ActivePlayerDataBuilderIndex=settings.DataBuilders.FindIndex(b=>b is BuildScriptPackedMode);settings.ActivePlayModeDataBuilderIndex=settings.DataBuilders.FindIndex(b=>b is BuildScriptPackedPlayMode);if(settings.ActivePlayerDataBuilderIndex<0||settings.ActivePlayModeDataBuilderIndex<0)throw new InvalidOperationException("SDK packed builders missing.");
                var defaults=ScriptableObject.CreateInstance<AddressableAssetSettingsDefaultObject>();AssetDatabase.CreateAsset(defaults,Folder+"/ES_Next_DefaultObject.asset");var so=new SerializedObject(defaults);so.FindProperty("m_AddressableAssetSettingsGuid").stringValue=AssetDatabase.AssetPathToGUID(SettingsPath);so.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssetIfDirty(defaults);EditorBuildSettings.AddConfigObject(AddressableAssetSettingsDefaultObject.kDefaultConfigObjectName,defaults,true);if(AddressableAssetSettingsDefaultObject.Settings!=settings)throw new InvalidOperationException("Own default mapping failed.");
                var group=settings.CreateGroup("ES_Next_Local",true,false,false,null,typeof(BundledAssetGroupSchema),typeof(ContentUpdateGroupSchema));ConfigureLocal(group);File.WriteAllText(ProbePath,Payload);AssetDatabase.ImportAsset(ProbePath,ImportAssetOptions.ForceSynchronousImport);settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(ProbePath),group).address=Address;EditorUtility.SetDirty(settings);SaveOwned();
            }
            if(!scenes.SequenceEqual(EditorBuildSettings.scenes.Select(s=>s.path+"|"+s.enabled)))throw new InvalidOperationException("Original build scene list changed.");if(!EditorBuildSettings.GetConfigObjectNames().OrderBy(s=>s).SequenceEqual(configs.Append(AddressableAssetSettingsDefaultObject.kDefaultConfigObjectName).OrderBy(s=>s)))throw new InvalidOperationException("Unexpected config mapping changes.");File.WriteAllText(Evidence+"/Prepared_A.json",new JObject{{"settings",SettingsPath},{"address",Address},{"originalBuildScenesUnchanged",true},{"addedConfig",AddressableAssetSettingsDefaultObject.kDefaultConfigObjectName},{"remoteCatalog",false},{"playMode","PackedPlayMode"},{"contentBuild","PackedMode"}}.ToString());
        }
        public static void ScheduleBuild()
        {
            SceneSafetyGuard.RequireClean();if(pending||BuildPipeline.isBuildingPlayer)throw new InvalidOperationException("Build busy.");if(!Settings||AddressableAssetSettingsDefaultObject.Settings!=Settings)throw new InvalidOperationException("Use own settings only.");Directory.CreateDirectory(Evidence);Receipt=Evidence+"/Build_"+Guid.NewGuid().ToString("N")+".json";pending=true;ticks=2;EditorApplication.update+=Queued;
        }
        static void Queued(){if(--ticks>0)return;EditorApplication.update-=Queued;EditorApplication.update+=Build;}
        static void Build()
        {
            EditorApplication.update-=Build;var row=new JObject{{"passed",false}};var timer=System.Diagnostics.Stopwatch.StartNew();
            try
            {
                SceneSafetyGuard.RequireClean();using(NextSdkSaveScope.Begin(Receipt+".save-scope.json"))using(new NextLightmapBuildScope(Receipt+".lightmap-flags.json")){SaveOwned();AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);row["error"]=result.Error;row["output"]=result.OutputPath;if(!string.IsNullOrEmpty(result.Error))throw new InvalidOperationException(result.Error);}var root=Path.GetFullPath(Addressables.BuildPath);if(!root.StartsWith(Path.GetFullPath("Library")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Unexpected bundle build destination.");var bundles=Directory.GetFiles(root,"*.bundle",SearchOption.AllDirectories);row["bundleRoot"]=root;row["bundles"]=new JArray(bundles.Select(p=>new JObject{{"path",p},{"bytes",new FileInfo(p).Length}}));row["runtimeSettingsExist"]=File.Exists(Path.Combine(root,"settings.json"));row["passed"]=bundles.Length>0&&row["runtimeSettingsExist"].Value<bool>();
            }
            catch(Exception e){row["error"]=e.ToString();}
            finally{pending=false;row["seconds"]=timer.Elapsed.TotalSeconds;File.WriteAllText(Receipt,row.ToString());}
        }
    }
}
