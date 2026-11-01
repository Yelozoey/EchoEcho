using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using EchoStation.Memory;
using EchoStation.Salvage;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EchoStation.SalvageNext
{
    public enum RecoveryQuality { Low,Medium,High }
    public sealed class NextSnapshot
    {
        public SalvageSnapshot World { get; }
        public RecoveryPreference Preference { get; }
        public RecoveryQuality Quality { get; }
        public NextSnapshot(SalvageSnapshot world,RecoveryPreference preference,RecoveryQuality quality)
        {
            SalvageSnapshot.Validate(world);if(!Enum.IsDefined(typeof(RecoveryPreference),preference)||!Enum.IsDefined(typeof(RecoveryQuality),quality))throw new ArgumentException("Invalid explicit settings.");
            // Freeze a defensive copy of mutable world fields at the serialization boundary.
            World=SalvageSaveStore.Decode(SalvageSaveStore.Encode(world));Preference=preference;Quality=quality;
        }
    }
    public sealed class NextSaveStore
    {
        const int MaximumBytes=98304;static readonly UTF8Encoding Utf8=new UTF8Encoding(false,true);
        readonly string directory;public string Primary=>Path.Combine(directory,"slot1.json");public string Backup=>Primary+".bak";
        public NextSaveStore(string folder){if(string.IsNullOrWhiteSpace(folder))throw new ArgumentException();directory=Path.GetFullPath(folder);}
        static string Hash(string value){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Utf8.GetBytes(value))).Replace("-","").ToLowerInvariant();}
        static void Fields(JObject value,params string[] names){if(value==null||value.Count!=names.Length||names.Any(n=>value.Property(n,StringComparison.Ordinal)==null))throw new ArgumentException("Missing or extra save fields.");}
        public static string Encode(NextSnapshot snapshot)
        {
            if(snapshot==null)throw new ArgumentNullException(nameof(snapshot));if(!Enum.IsDefined(typeof(RecoveryPreference),snapshot.Preference)||!Enum.IsDefined(typeof(RecoveryQuality),snapshot.Quality))throw new ArgumentException("Invalid settings.");
            var payload=new JObject{{"world",JObject.Parse(SalvageSaveStore.Encode(snapshot.World))},{"preference",(int)snapshot.Preference},{"quality",(int)snapshot.Quality}};
            var result=new JObject{{"format","echostation.salvage-next"},{"version",1},{"payload",payload},{"sha256",Hash(payload.ToString(Formatting.None))}}.ToString(Formatting.None);if(Utf8.GetByteCount(result)>MaximumBytes)throw new ArgumentException("Save exceeds byte budget.");return result;
        }
        public static NextSnapshot Decode(string text)
        {
            if(string.IsNullOrWhiteSpace(text)||Utf8.GetByteCount(text)>MaximumBytes)throw new ArgumentException("Save empty or exceeds byte budget.");MemoryJson.ValidateLexemes(text);JObject root;
            using(var reader=new JsonTextReader(new StringReader(text)){MaxDepth=20,DateParseHandling=DateParseHandling.None}){root=JToken.ReadFrom(reader,new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error}) as JObject;if(root==null||reader.Read())throw new ArgumentException("One save object required.");}
            Fields(root,"format","version","payload","sha256");if(root["format"].Type!=JTokenType.String||root["format"].Value<string>()!="echostation.salvage-next"||root["version"].Type!=JTokenType.Integer)throw new ArgumentException("Save header mismatch.");
            if(root["version"].Value<long>()!=1)throw new NotSupportedException("Unsupported new-prototype save version; original retained.");
            var data=root["payload"] as JObject;Fields(data,"world","preference","quality");if(root["sha256"].Type!=JTokenType.String||root["sha256"].Value<string>()!=Hash(data.ToString(Formatting.None)))throw new ArgumentException("Checksum mismatch.");
            if(data["preference"].Type!=JTokenType.Integer||data["quality"].Type!=JTokenType.Integer||!(data["world"] is JObject))throw new ArgumentException("Invalid settings/world types.");
            var preference=(RecoveryPreference)data["preference"].Value<int>();var quality=(RecoveryQuality)data["quality"].Value<int>();return new NextSnapshot(SalvageSaveStore.Decode(data["world"].ToString(Formatting.None)),preference,quality);
        }
        static bool FileError(Exception e)=>e is IOException||e is UnauthorizedAccessException||e is ArgumentException||e is JsonException||e is OverflowException||e is FormatException;
        static NextSnapshot Read(string path){if(new FileInfo(path).Length>MaximumBytes)throw new ArgumentException("Oversized save file.");return Decode(Utf8.GetString(File.ReadAllBytes(path)));}
        public bool TryRead(out NextSnapshot snapshot,out string error)
        {
            snapshot=null;error="";foreach(var path in new[]{Primary,Backup})try{if(!File.Exists(path))continue;snapshot=Read(path);if(path==Backup)error="主档不可用，已读取有效新版备份。";return true;}catch(NotSupportedException e){error=e.Message;return false;}catch(Exception e)when(FileError(e)){error=e.Message;}
            if(error=="")error="没有新版回收存档。";return false;
        }
        public bool TryWrite(NextSnapshot snapshot,out string error)
        {
            error="";try
            {
                var bytes=Utf8.GetBytes(Encode(snapshot));bool primaryValid=false,backupValid=false;
                if(File.Exists(Primary))try{Read(Primary);primaryValid=true;}catch(Exception e)when(FileError(e)){}
                if(File.Exists(Backup))try{Read(Backup);backupValid=true;}catch(Exception e)when(FileError(e)){}
                if(File.Exists(Primary)&&!primaryValid&&!backupValid||!File.Exists(Primary)&&File.Exists(Backup)&&!backupValid)throw new IOException("损坏存档已保留，拒绝覆盖无有效副本的数据。");
                string temporary=Primary+".tmp";if(File.Exists(temporary))throw new IOException("遗留临时文件已保留，请先检查。");Directory.CreateDirectory(directory);
                if(File.Exists(Primary)&&!primaryValid)File.Copy(Primary,Primary+".rejected-"+Guid.NewGuid().ToString("N"));if(File.Exists(Backup)&&!backupValid)File.Copy(Backup,Backup+".rejected-"+Guid.NewGuid().ToString("N"));
                using(var stream=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None)){stream.Write(bytes,0,bytes.Length);stream.Flush(true);}
                if(File.Exists(Primary))File.Replace(temporary,Primary,primaryValid?Backup:null,true);else File.Move(temporary,Primary);return true;
            }
            catch(Exception e)when(FileError(e)||e is NotSupportedException){error=e.Message;return false;}
        }
    }
}
