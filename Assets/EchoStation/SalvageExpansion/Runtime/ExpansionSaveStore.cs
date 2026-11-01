using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using EchoStation.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace EchoStation.SalvageExpansion
{
    public sealed class ExpansionSaveStore
    {
        const int MaximumBytes=131072;static readonly UTF8Encoding Utf8=new UTF8Encoding(false,true);
        readonly string directory;public string Primary=>Path.Combine(directory,"slot1.json");public string Backup=>Primary+".bak";
        public ExpansionSaveStore(string path){if(string.IsNullOrWhiteSpace(path))throw new ArgumentException("Save directory required.");directory=Path.GetFullPath(path);}
        static string Hash(string value){using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(Utf8.GetBytes(value))).Replace("-","").ToLowerInvariant();}
        static void Fields(JObject value,params string[] names){if(value==null||value.Count!=names.Length||names.Any(n=>value.Property(n,StringComparison.Ordinal)==null))throw new ArgumentException("Missing or extra snapshot fields.");}
        static JArray Point(Vector3 p)=>new JArray(p.x,p.y,p.z);
        static float Number(JToken token){if(token==null||token.Type!=JTokenType.Float&&token.Type!=JTokenType.Integer)throw new ArgumentException("Number required.");float value=token.Value<float>();if(!float.IsFinite(value))throw new ArgumentException("Finite number required.");return value;}
        static int Integer(JToken token){if(token==null||token.Type!=JTokenType.Integer)throw new ArgumentException("Integer required.");return token.Value<int>();}
        static string Text(JToken token){if(token==null||token.Type!=JTokenType.String)throw new ArgumentException("String required.");return token.Value<string>();}
        static Vector3 Point(JToken token){var array=token as JArray;if(array==null||array.Count!=3)throw new ArgumentException("Three coordinates required.");return new Vector3(Number(array[0]),Number(array[1]),Number(array[2]));}
        public static string Encode(ExpansionSnapshot snapshot)
        {
            if(snapshot==null)throw new ArgumentNullException(nameof(snapshot));
            var items=new JArray(snapshot.Inventory.Select(r=>new JObject{{"id",r.Id},{"location",(int)r.Location},{"x",r.X},{"y",r.Y},{"rotation",r.Rotation},{"slot",r.Slot==null?JValue.CreateNull():new JValue(r.Slot)}}));
            var positions=new JObject();foreach(var row in snapshot.Positions.OrderBy(p=>p.Key,StringComparer.Ordinal))positions[row.Key]=Point(row.Value);
            var payload=new JObject{{"items",items},{"positions",positions},{"player",new JObject{{"position",Point(snapshot.Player)},{"yaw",snapshot.Yaw},{"pitch",snapshot.Pitch}}},{"sections",snapshot.Sections}};
            string result=new JObject{{"format","echostation.salvage-expansion"},{"version",1},{"payload",payload},{"sha256",Hash(payload.ToString(Formatting.None))}}.ToString(Formatting.None);
            if(Utf8.GetByteCount(result)>MaximumBytes)throw new ArgumentException("Snapshot exceeds 128KiB.");return result;
        }
        public static ExpansionSnapshot Decode(string text)
        {
            if(string.IsNullOrWhiteSpace(text)||Utf8.GetByteCount(text)>MaximumBytes)throw new ArgumentException("Empty or oversized snapshot.");MemoryJson.ValidateLexemes(text);JObject root;
            using(var reader=new JsonTextReader(new StringReader(text)){MaxDepth=32,DateParseHandling=DateParseHandling.None}){root=JToken.ReadFrom(reader,new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error})as JObject;if(root==null||reader.Read())throw new ArgumentException("One JSON object required.");}
            Fields(root,"format","version","payload","sha256");if(Text(root["format"])!="echostation.salvage-expansion")throw new ArgumentException("Wrong save format.");if(Integer(root["version"])!=1)throw new NotSupportedException("Unsupported expansion save version; original retained.");
            var payload=root["payload"]as JObject;Fields(payload,"items","positions","player","sections");if(Text(root["sha256"])!=Hash(payload.ToString(Formatting.None)))throw new ArgumentException("Checksum mismatch.");
            var array=payload["items"]as JArray;if(array==null||array.Count!=ExpansionCatalog.Items.Count)throw new ArgumentException("Complete inventory required.");var items=new List<InventoryItemState>();
            foreach(var token in array){var row=token as JObject;Fields(row,"id","location","x","y","rotation","slot");if(row["slot"].Type!=JTokenType.Null&&row["slot"].Type!=JTokenType.String)throw new ArgumentException("Invalid slot type.");items.Add(new InventoryItemState(Text(row["id"]),(InventoryItemLocation)Integer(row["location"]),Integer(row["x"]),Integer(row["y"]),Integer(row["rotation"]),row["slot"].Type==JTokenType.Null?null:Text(row["slot"])));}
            var rawPositions=payload["positions"]as JObject;if(rawPositions==null)throw new ArgumentException("Positions required.");var positions=rawPositions.Properties().ToDictionary(p=>p.Name,p=>Point(p.Value),StringComparer.Ordinal);
            var player=payload["player"]as JObject;Fields(player,"position","yaw","pitch");return new ExpansionSnapshot(items,positions,Point(player["position"]),Number(player["yaw"]),Number(player["pitch"]),payload["sections"]as JObject);
        }
        static bool FileError(Exception e)=>e is IOException||e is UnauthorizedAccessException||e is ArgumentException||e is JsonException||e is OverflowException||e is FormatException;
        static ExpansionSnapshot Read(string path){if(new FileInfo(path).Length>MaximumBytes)throw new ArgumentException("Oversized file.");return Decode(Utf8.GetString(File.ReadAllBytes(path)));}
        public bool TryRead(out ExpansionSnapshot snapshot,out string message,Func<ExpansionSnapshot,string> validate=null)
        {
            snapshot=null;message="";foreach(var path in new[]{Primary,Backup})try{if(!File.Exists(path))continue;var candidate=Read(path);string error=validate?.Invoke(candidate);if(!string.IsNullOrEmpty(error))throw new ArgumentException(error);snapshot=candidate;if(path==Backup)message="主档不可用，已读取有效灰盒备份。";return true;}catch(NotSupportedException e){message=e.Message;return false;}catch(Exception e)when(FileError(e)){message=e.Message;}
            if(message=="")message="尚无环境回收存档。";return false;
        }
        public bool TryWrite(ExpansionSnapshot snapshot,out string error)
        {
            error="";try
            {
                var bytes=Utf8.GetBytes(Encode(snapshot));bool primaryValid=false,backupValid=false;
                if(File.Exists(Primary))try{Read(Primary);primaryValid=true;}catch(Exception e)when(FileError(e)){}
                if(File.Exists(Backup))try{Read(Backup);backupValid=true;}catch(Exception e)when(FileError(e)){}
                if(File.Exists(Primary)&&!primaryValid&&!backupValid||!File.Exists(Primary)&&File.Exists(Backup)&&!backupValid)throw new IOException("损坏文件已保留，拒绝覆盖无有效副本的数据。");
                string temporary=Primary+".tmp";if(File.Exists(temporary))throw new IOException("已有临时文件，保留并拒绝覆盖。");Directory.CreateDirectory(directory);
                if(File.Exists(Primary)&&!primaryValid)File.Copy(Primary,Primary+".rejected-"+Guid.NewGuid().ToString("N"));if(File.Exists(Backup)&&!backupValid)File.Copy(Backup,Backup+".rejected-"+Guid.NewGuid().ToString("N"));
                using(var stream=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None)){stream.Write(bytes,0,bytes.Length);stream.Flush(true);}
                if(File.Exists(Primary))File.Replace(temporary,Primary,primaryValid?Backup:null,true);else File.Move(temporary,Primary);return true;
            }
            catch(Exception e)when(FileError(e)||e is NotSupportedException){error=e.Message;return false;}
        }
    }
}
