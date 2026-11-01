using System;
using System.IO;
using System.Text;

namespace EchoStation.Persistence
{
    public sealed class StationFileStore
    {
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);
        private readonly object gate = new object();
        public string DirectoryPath { get; }
        public string PrimaryPath => Path.Combine(DirectoryPath, "slot1.json");
        public string BackupPath => PrimaryPath + ".bak";
        public string TemporaryPath => PrimaryPath + ".tmp";

        public StationFileStore(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("Memory directory required.", nameof(directory));
            DirectoryPath = Path.GetFullPath(directory);
        }

        public StationReadResult Read()
        {
            lock (gate)
            {
                var primary = ReadFile(PrimaryPath);
                if (primary.Status == StationReadStatus.Loaded || primary.Status == StationReadStatus.UnsupportedVersion || primary.Status == StationReadStatus.IoError) return primary;
                var backup = ReadFile(BackupPath);
                if (backup.Status == StationReadStatus.UnsupportedVersion || backup.Status == StationReadStatus.IoError) return backup;
                if (backup.Success) return new StationReadResult(StationReadStatus.Backup, backup.Data, "主进度存档文件不可用，已读取上次有效备份。");
                if (primary.Status == StationReadStatus.Missing && backup.Status == StationReadStatus.Missing) return primary;
                return new StationReadResult(StationReadStatus.Invalid, detail: "进度存档文件与备份不可用，原文件已保留。");
            }
        }

        public bool TryWrite(StationSaveData data, out string error)
        {
            lock (gate)
            {
                error = "";
                try
                {
                    var bytes = Utf8.GetBytes(StationSaveJson.Encode(data));
                    var primary = ReadFile(PrimaryPath); var backup = ReadFile(BackupPath);
                    if (Protected(primary) || Protected(backup)) { error = "未来版本或无法访问的进度存档文件已保留。"; return false; }
                    var preserveBackup = primary.Status == StationReadStatus.Invalid;
                    if (preserveBackup && !backup.Success || primary.Status == StationReadStatus.Missing && backup.Status == StationReadStatus.Invalid)
                    { error = "没有有效副本，拒绝覆盖损坏进度存档文件。"; return false; }
                    Directory.CreateDirectory(DirectoryPath);
                    if (preserveBackup) PreserveRejected(PrimaryPath);
                    if (primary.Success && backup.Status == StationReadStatus.Invalid) PreserveRejected(BackupPath);
                    using (var stream = new FileStream(TemporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
                    if (File.Exists(PrimaryPath)) File.Replace(TemporaryPath, PrimaryPath, preserveBackup ? null : BackupPath, true);
                    else File.Move(TemporaryPath, PrimaryPath);
                    return true;
                }
                catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
                { error = exception.Message; return false; }
                finally
                {
                    try { if (File.Exists(TemporaryPath)) File.Delete(TemporaryPath); }
                    catch (IOException) { } catch (UnauthorizedAccessException) { }
                }
            }
        }

        private static bool Protected(StationReadResult result) => result.Status == StationReadStatus.UnsupportedVersion || result.Status == StationReadStatus.IoError;
        private static void PreserveRejected(string path) => File.Copy(path, path + ".rejected-" + DateTime.UtcNow.Ticks + "-" + Guid.NewGuid().ToString("N"));
        private static StationReadResult ReadFile(string path)
        {
            try
            {
                if (Directory.Exists(path)) return new StationReadResult(StationReadStatus.IoError, detail: "进度存档文件路径被目录占用。");
                if (!File.Exists(path)) return new StationReadResult(StationReadStatus.Missing, detail: "没有进度存档文件。");
                if (new FileInfo(path).Length > StationSaveJson.MaximumBytes) return new StationReadResult(StationReadStatus.Invalid, detail: "进度存档文件超过字节预算。");
                return StationSaveJson.Decode(Utf8.GetString(File.ReadAllBytes(path)));
            }
            catch (DecoderFallbackException exception) { return new StationReadResult(StationReadStatus.Invalid, detail: exception.Message); }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            { return new StationReadResult(StationReadStatus.IoError, detail: exception.Message); }
        }
    }
}
