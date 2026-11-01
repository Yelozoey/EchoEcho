using System;
using System.IO;
using System.Text;

namespace EchoStation.Player
{
    public sealed class LocalSaveStore
    {
        private static readonly UTF8Encoding Utf8=new UTF8Encoding(false,true);
        private readonly object gate=new object();
        public string DirectoryPath{get;}public string PrimaryPath=>Path.Combine(DirectoryPath,"slot1.json");public string BackupPath=>PrimaryPath+".bak";public string TemporaryPath=>PrimaryPath+".tmp";
        public LocalSaveStore(string directory){if(string.IsNullOrWhiteSpace(directory))throw new ArgumentException("Save directory required.");DirectoryPath=Path.GetFullPath(directory);}
        public SaveReadResult Read()
        {
            lock(gate){var primary=ReadFile(PrimaryPath);if(primary.Status==SaveStatus.Loaded||primary.Status==SaveStatus.UnsupportedVersion||primary.Status==SaveStatus.IoError)return primary;var backup=ReadFile(BackupPath);if(backup.Status==SaveStatus.Loaded)return new SaveReadResult(SaveStatus.Backup,backup.Data,"主存档不可用，已读取上次有效备份。");if(backup.Status==SaveStatus.UnsupportedVersion)return backup;if(primary.Status==SaveStatus.Missing&&backup.Status==SaveStatus.Missing)return primary;return new SaveReadResult(SaveStatus.Invalid,detail:"存档及备份不可用，原文件已保留。");}
        }
        public bool TryWrite(SavePayload data,out string error)
        {
            lock(gate)
            {
                error="";
                try
                {
                    var bytes=Utf8.GetBytes(SaveCodec.Encode(data));Directory.CreateDirectory(DirectoryPath);var primary=ReadFile(PrimaryPath);var preserveBackup=false;
                    if(primary.Status==SaveStatus.UnsupportedVersion||primary.Status==SaveStatus.IoError){error=primary.Detail;return false;}
                    if(primary.Status==SaveStatus.Invalid)
                    {
                        var backup=ReadFile(BackupPath);if(backup.Status!=SaveStatus.Loaded){error="损坏存档没有可用备份，拒绝覆盖原文件。";return false;}File.Copy(PrimaryPath,PrimaryPath+".rejected-"+DateTime.UtcNow.Ticks+"-"+Guid.NewGuid().ToString("N"));preserveBackup=true;
                    }
                    using(var stream=new FileStream(TemporaryPath,FileMode.Create,FileAccess.Write,FileShare.None)){stream.Write(bytes,0,bytes.Length);stream.Flush(true);}
                    if(File.Exists(PrimaryPath))File.Replace(TemporaryPath,PrimaryPath,preserveBackup?null:BackupPath,true);else File.Move(TemporaryPath,PrimaryPath);return true;
                }
                catch(Exception exception)when(exception is IOException||exception is UnauthorizedAccessException||exception is ArgumentException){error=exception.Message;return false;}
                finally{try{if(File.Exists(TemporaryPath))File.Delete(TemporaryPath);}catch(IOException){}catch(UnauthorizedAccessException){}}
            }
        }
        private static SaveReadResult ReadFile(string path)
        {
            try{if(!File.Exists(path))return new SaveReadResult(SaveStatus.Missing,detail:"没有存档。");if(new FileInfo(path).Length>SaveCodec.MaximumBytes)return new SaveReadResult(SaveStatus.Invalid,detail:"Save file exceeds byte budget.");return SaveCodec.Decode(File.ReadAllText(path,Utf8));}
            catch(DecoderFallbackException exception){return new SaveReadResult(SaveStatus.Invalid,detail:exception.Message);}
            catch(Exception exception)when(exception is IOException||exception is UnauthorizedAccessException){return new SaveReadResult(SaveStatus.IoError,detail:exception.Message);}
        }
    }
}
