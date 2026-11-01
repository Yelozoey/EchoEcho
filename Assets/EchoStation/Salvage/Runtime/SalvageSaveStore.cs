using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using EchoStation.Memory;

namespace EchoStation.Salvage
{
    public sealed class SalvageSaveStore
    {
        const int MaximumBytes=65536;static readonly UTF8Encoding Utf8=new UTF8Encoding(false,true);
        readonly string directory;public string Primary=>Path.Combine(directory,"slot1.json");public string Backup=>Primary+".bak";public int LastReadVersion{get;private set;}
        public string LegacyBackup=>Primary+".r1-v1.json";
        public SalvageSaveStore(string folder){if(string.IsNullOrWhiteSpace(folder))throw new ArgumentException();directory=Path.GetFullPath(folder);}
        public static string Encode(SalvageSnapshot snapshot)
        {SalvageSnapshot.Validate(snapshot);var data=JObject.FromObject(snapshot);return new JObject{["format"]="echostation.salvage",["version"]=2,["payload"]=data,["sha256"]=Hash(data.ToString(Formatting.None))}.ToString(Formatting.None);}
        public static SalvageSnapshot Decode(string text)=>Decode(text,out _);
        static SalvageSnapshot Decode(string text,out int version)
        {
            version=0;
            if(string.IsNullOrWhiteSpace(text)||Utf8.GetByteCount(text)>MaximumBytes)throw new ArgumentException("Save empty or too large.");MemoryJson.ValidateLexemes(text);JObject root;
            using(var reader=new JsonTextReader(new StringReader(text)){MaxDepth=12,DateParseHandling=DateParseHandling.None}){root=JToken.ReadFrom(reader,new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error}) as JObject;if(root==null||reader.Read())throw new ArgumentException("One object required.");}
            Fields(root,"format","version","payload","sha256");if(root["format"].Type!=JTokenType.String||root["format"].Value<string>()!="echostation.salvage")throw new ArgumentException("Wrong save format.");
            if(root["version"].Type!=JTokenType.Integer)throw new ArgumentException("Invalid version.");version=root["version"].Value<int>();if(version!=1&&version!=2)throw new NotSupportedException("Unsupported salvage save version.");
            var data=root["payload"] as JObject;if(data==null||root["sha256"].Type!=JTokenType.String||Hash(data.ToString(Formatting.None))!=root["sha256"].Value<string>())throw new ArgumentException("Checksum mismatch.");
            if(version==1)Fields(data,"Player","Echo","Flashlight","Warned","Completed","Charge","Elapsed","Cargo");else Fields(data,"Player","Echo","Flashlight","Warned","Completed","Charge","Elapsed","Cargo","Collaboration");
            if(version==2)ValidateCollaborationJson(data["Collaboration"] as JObject);
            foreach(var name in new[]{"Flashlight","Warned","Completed"})if(data[name].Type!=JTokenType.Boolean)throw new ArgumentException("Boolean required.");
            Number(data["Charge"]);Number(data["Elapsed"]);
            foreach(var name in new[]{"Player","Echo"}){var p=data[name] as JObject;Fields(p,"X","Y","Z","Yaw","Pitch");foreach(var v in p.Properties())Number(v.Value);}
            var cargo=data["Cargo"] as JArray;if(cargo==null||cargo.Count!=7)throw new ArgumentException("Seven cargo required.");
            foreach(var token in cargo){var c=token as JObject;Fields(c,"Id","State","X","Y","Z");if(c["Id"].Type!=JTokenType.String||c["State"].Type!=JTokenType.Integer)throw new ArgumentException("ID/state type.");foreach(var name in new[]{"X","Y","Z"})Number(c[name]);}
            var result=data.ToObject<SalvageSnapshot>();SalvageSnapshot.Validate(result);return result;
        }
        public bool TryRead(out SalvageSnapshot snapshot,out string error)
        {
            snapshot=null;error="";LastReadVersion=0;
            foreach(var path in new[]{Primary,Backup})
            {try{if(!File.Exists(path))continue;snapshot=Read(path,out var version);LastReadVersion=version;if(path==Backup)error="主档不可用，已读取有效备份。";return true;}catch(NotSupportedException e){error=e.Message;return false;}catch(Exception e)when(IsFileError(e)){error=e.Message;}}
            if(error=="")error="没有回收原型存档。";return false;
        }
        public bool TryWrite(SalvageSnapshot snapshot,out string error)
        {
            error="";string temp=Primary+".tmp";
            try
            {
                var bytes=Utf8.GetBytes(Encode(snapshot));if(bytes.Length>MaximumBytes)throw new ArgumentException("Save budget exceeded.");bool primaryValid=false,backupValid=false;int primaryVersion=0,backupVersion=0;
                if(File.Exists(Primary)){try{Read(Primary,out primaryVersion);primaryValid=true;}catch(Exception e)when(IsFileError(e)){}}
                if(File.Exists(Backup)){try{Read(Backup,out backupVersion);backupValid=true;}catch(Exception e)when(IsFileError(e)){}}
                if(File.Exists(Primary)&&!primaryValid&&!backupValid||!File.Exists(Primary)&&File.Exists(Backup)&&!backupValid)throw new IOException("损坏存档已保留，拒绝覆盖无有效副本的数据。");
                Directory.CreateDirectory(directory);if(primaryValid&&primaryVersion==1)PreserveLegacy(Primary,LegacyBackup);if(backupValid&&backupVersion==1)PreserveLegacy(Backup,Backup+".r1-v1.json");if(File.Exists(temp))throw new IOException("遗留临时文件已保留，请先检查。");
                if(File.Exists(Primary)&&!primaryValid)File.Copy(Primary,Primary+".rejected-"+Guid.NewGuid().ToString("N"));
                if(File.Exists(Backup)&&!backupValid)File.Copy(Backup,Backup+".rejected-"+Guid.NewGuid().ToString("N"));
                using(var stream=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)){stream.Write(bytes,0,bytes.Length);stream.Flush(true);}
                if(File.Exists(Primary))File.Replace(temp,Primary,primaryValid?Backup:null,true);else File.Move(temp,Primary);return true;
            }
            catch(Exception e)when(IsFileError(e)||e is NotSupportedException){error=e.Message;return false;}
        }
        static SalvageSnapshot Read(string path,out int version){if(new FileInfo(path).Length>MaximumBytes)throw new ArgumentException("Save file exceeds byte budget.");return Decode(Utf8.GetString(File.ReadAllBytes(path)),out version);}
        static void PreserveLegacy(string source,string destination)
        {var bytes=File.ReadAllBytes(source);if(File.Exists(destination)){if(!File.ReadAllBytes(destination).SequenceEqual(bytes))throw new IOException("Existing R1 backup differs; original files preserved.");return;}using(var stream=new FileStream(destination,FileMode.CreateNew,FileAccess.Write,FileShare.None)){stream.Write(bytes,0,bytes.Length);stream.Flush(true);}}
        static void ValidateCollaborationJson(JObject data)
        {
            Fields(data,"BaseMode","Discovered","Messages");if(data["BaseMode"].Type!=JTokenType.Integer)throw new ArgumentException("Base mode type.");var ids=data["Discovered"] as JArray;var messages=data["Messages"] as JArray;if(ids==null||ids.Count>8||ids.Any(id=>id.Type!=JTokenType.String)||messages==null||messages.Count>50)throw new ArgumentException("Discovery/history array type or budget.");
            foreach(var item in messages){var m=item as JObject;Fields(m,"Number","Speaker","Text","Refusal");if(m["Number"].Type!=JTokenType.Integer||m["Speaker"].Type!=JTokenType.Integer||m["Text"].Type!=JTokenType.String||m["Refusal"].Type!=JTokenType.Boolean)throw new ArgumentException("Message field type.");}
        }
        static bool IsFileError(Exception e)=>e is IOException||e is UnauthorizedAccessException||e is ArgumentException||e is JsonException||e is OverflowException||e is FormatException;
        static void Fields(JObject obj,params string[] names){if(obj==null||obj.Count!=names.Length||names.Any(n=>obj.Property(n,StringComparison.Ordinal)==null))throw new ArgumentException("Missing or unknown fields.");}
        static void Number(JToken value){if(value.Type!=JTokenType.Float&&value.Type!=JTokenType.Integer)throw new ArgumentException("Numeric value required.");}
        static string Hash(string s){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Utf8.GetBytes(s))).Replace("-","").ToLowerInvariant();}
    }
}
