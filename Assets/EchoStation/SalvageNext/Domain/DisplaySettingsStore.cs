using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using EchoStation.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace EchoStation.SalvageNext
{
    public interface IDisplaySettingsWriter{bool TryWrite(DisplaySelection selection,out string error);}
    public sealed class DisplaySettingsStore:IDisplaySettingsWriter
    {
        const int MaximumBytes=4096;static readonly UTF8Encoding Utf8=new UTF8Encoding(false,true);
        readonly string directory;public string Primary=>Path.Combine(directory,"settings.json");public string Backup=>Primary+".bak";
        public DisplaySettingsStore(string folder){if(string.IsNullOrWhiteSpace(folder))throw new ArgumentException();directory=Path.GetFullPath(folder);}
        static string Hash(string text){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Utf8.GetBytes(text))).Replace("-","").ToLowerInvariant();}
        static void Fields(JObject data,params string[] names){if(data==null||data.Count!=names.Length||names.Any(n=>data.Property(n,StringComparison.Ordinal)==null))throw new ArgumentException("Display fields mismatch.");}
        public static string Encode(DisplaySelection selection)
        {
            if(selection==null)throw new ArgumentNullException(nameof(selection));var payload=new JObject{{"quality",(int)selection.Quality},{"width",selection.Width},{"height",selection.Height},{"mode",(int)selection.Mode}};
            return new JObject{{"format","echostation.salvage-next.settings"},{"version",1},{"payload",payload},{"sha256",Hash(payload.ToString(Formatting.None))}}.ToString(Formatting.None);
        }
        public static DisplaySelection Decode(string text)
        {
            if(string.IsNullOrWhiteSpace(text)||Utf8.GetByteCount(text)>MaximumBytes)throw new ArgumentException("Display data empty or oversized.");MemoryJson.ValidateLexemes(text);JObject root;
            using(var reader=new JsonTextReader(new StringReader(text)){MaxDepth=8,DateParseHandling=DateParseHandling.None}){root=JToken.ReadFrom(reader,new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error}) as JObject;if(root==null||reader.Read())throw new ArgumentException("One display object required.");}
            Fields(root,"format","version","payload","sha256");if(root["format"].Type!=JTokenType.String||root["format"].Value<string>()!="echostation.salvage-next.settings"||root["version"].Type!=JTokenType.Integer)throw new ArgumentException("Display header mismatch.");if(root["version"].Value<long>()!=1)throw new NotSupportedException("Unknown display settings version; original retained.");var data=root["payload"] as JObject;Fields(data,"quality","width","height","mode");if(data.Properties().Any(p=>p.Value.Type!=JTokenType.Integer)||root["sha256"].Type!=JTokenType.String||root["sha256"].Value<string>()!=Hash(data.ToString(Formatting.None)))throw new ArgumentException("Display type/checksum mismatch.");return new DisplaySelection((RecoveryQuality)data["quality"].Value<int>(),data["width"].Value<int>(),data["height"].Value<int>(),(DisplayWindowMode)data["mode"].Value<int>());
        }
        static bool FileError(Exception error)=>error is IOException||error is UnauthorizedAccessException||error is ArgumentException||error is JsonException||error is OverflowException||error is FormatException;
        static DisplaySelection Read(string path){if(new FileInfo(path).Length>MaximumBytes)throw new ArgumentException("Oversized display file.");return Decode(Utf8.GetString(File.ReadAllBytes(path)));}
        public bool TryRead(out DisplaySelection selection,out string error)
        {
            selection=null;error="";foreach(string path in new[]{Primary,Backup})try{if(!File.Exists(path))continue;selection=Read(path);if(path==Backup)error="显示主配置不可用，已读取有效备份。";return true;}catch(NotSupportedException e){error=e.Message;return false;}catch(Exception e)when(FileError(e)){error=e.Message;}if(error=="")error="没有已保存显示配置。";return false;
        }
        public bool TryWrite(DisplaySelection selection,out string error)
        {
            error="";
            try
            {
                byte[] bytes=Utf8.GetBytes(Encode(selection));bool primaryValid=false,backupValid=false;
                if(File.Exists(Primary))try{Read(Primary);primaryValid=true;}catch(Exception e)when(FileError(e)){}
                if(File.Exists(Backup))try{Read(Backup);backupValid=true;}catch(Exception e)when(FileError(e)){}
                if(File.Exists(Primary)&&!primaryValid&&!backupValid||!File.Exists(Primary)&&File.Exists(Backup)&&!backupValid)throw new IOException("损坏配置已保留，拒绝覆盖无有效副本的数据。");
                string temporary=Primary+".tmp";if(File.Exists(temporary))throw new IOException("显示临时文件已保留，请先检查。");Directory.CreateDirectory(directory);if(File.Exists(Primary)&&!primaryValid)File.Copy(Primary,Primary+".rejected-"+Guid.NewGuid().ToString("N"));if(File.Exists(Backup)&&!backupValid)File.Copy(Backup,Backup+".rejected-"+Guid.NewGuid().ToString("N"));
                using(var stream=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None)){stream.Write(bytes,0,bytes.Length);stream.Flush(true);}if(File.Exists(Primary))File.Replace(temporary,Primary,primaryValid?Backup:null,true);else File.Move(temporary,Primary);return true;
            }
            catch(Exception e)when(FileError(e)||e is NotSupportedException){error=e.Message;return false;}
        }
    }
}
