using System;
using System.IO;
using System.Linq;
using EchoStation.Rendering.Editor;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
namespace EchoStation.SalvageNext.Editor
{
    public static class NextRoomBundles
    {
        public const string Evidence="Docs/Tests/SalvagePrototype/Evidence/R4-R03b-3a",RoomGroup="ES_Next_Rooms",SharedGroup="ES_Next_Shared";
        public static string Address(int room){if(room<0||room>2)throw new ArgumentOutOfRangeException();return "es-next-room-"+room;}
        public static void Prepare()
        {
            SceneSafetyGuard.RequireClean();var settings=NextAddressables.Settings;if(!settings||settings.groups.Any(g=>g&&(g.Name==RoomGroup||g.Name==SharedGroup)))throw new InvalidOperationException("Preserve existing room bundle configuration.");Directory.CreateDirectory(Evidence);File.WriteAllText(Evidence+"/GroupsBefore_A.json",new JArray(settings.groups.Where(g=>g).Select(g=>new JObject{{"name",g.Name},{"guid",g.Guid},{"entries",g.entries.Count}})).ToString());
            using(NextSdkSaveScope.Begin(Evidence+"/GroupSaveScope_A.json"))
            {
                var group=settings.CreateGroup(RoomGroup,false,false,false,null,typeof(BundledAssetGroupSchema),typeof(ContentUpdateGroupSchema));NextAddressables.ConfigureLocal(group);group.GetSchema<BundledAssetGroupSchema>().BundleMode=BundledAssetGroupSchema.BundlePackingMode.PackSeparately;var shared=settings.CreateGroup(SharedGroup,false,false,false,null,typeof(BundledAssetGroupSchema),typeof(ContentUpdateGroupSchema));NextAddressables.ConfigureLocal(shared);var dependencies=new System.Collections.Generic.Dictionary<string,int>(StringComparer.Ordinal);
                for(int i=0;i<3;i++){string path=NextRoomPartition.RoomPath(i);settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path),group).address=Address(i);foreach(var dependency in AssetDatabase.GetDependencies(path,true).Distinct()){if(!dependency.StartsWith("Assets/",StringComparison.Ordinal))continue;var type=AssetDatabase.GetMainAssetTypeAtPath(dependency);if(type!=typeof(Material)&&type!=typeof(Texture2D)&&type!=typeof(Mesh))continue;dependencies.TryGetValue(dependency,out int count);dependencies[dependency]=count+1;}}
                var common=dependencies.Where(pair=>pair.Value>1).Select(pair=>pair.Key).OrderBy(p=>p,StringComparer.Ordinal).ToArray();foreach(var path in common){string guid=AssetDatabase.AssetPathToGUID(path);settings.CreateOrMoveEntry(guid,shared).address="es-next-shared-"+guid;}EditorUtility.SetDirty(settings);NextAddressables.SaveOwned();File.WriteAllText(Evidence+"/Prepared_A.json",new JObject{{"rooms",3},{"roomPacking","PackSeparately"},{"sharedAssets",new JArray(common)},{"sharedCount",common.Length},{"remote",false}}.ToString());
            }
        }
    }
}
