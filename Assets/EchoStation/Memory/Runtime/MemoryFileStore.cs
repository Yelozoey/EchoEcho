using System;
using System.IO;
using System.Text;

namespace EchoStation.Memory
{
    public sealed class MemoryFileStore
    {
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);
        private readonly object gate = new object();
        public string DirectoryPath { get; }
        public string PrimaryPath => Path.Combine(DirectoryPath, "memory.json");
        public string BackupPath => PrimaryPath + ".bak";
        public string TemporaryPath => PrimaryPath + ".tmp";

        public MemoryFileStore(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("Memory directory required.", nameof(directory));
            DirectoryPath = Path.GetFullPath(directory);
        }

        public MemoryReadResult Read()
        {
            lock (gate)
            {
                var primary = ReadFile(PrimaryPath);
                if (primary.Status == MemoryReadStatus.Loaded || primary.Status == MemoryReadStatus.UnsupportedVersion || primary.Status == MemoryReadStatus.IoError) return primary;
                var backup = ReadFile(BackupPath);
                if (backup.Status == MemoryReadStatus.UnsupportedVersion || backup.Status == MemoryReadStatus.IoError) return backup;
                if (backup.Success) return new MemoryReadResult(MemoryReadStatus.Backup, backup.Store, "主记忆文件不可用，已读取上次有效备份。");
                if (primary.Status == MemoryReadStatus.Missing && backup.Status == MemoryReadStatus.Missing) return primary;
                return new MemoryReadResult(MemoryReadStatus.Invalid, detail: "记忆文件与备份不可用，原文件已保留。");
            }
        }

        public bool TryWrite(MemorySnapshot snapshot, out string error)
        {
            lock (gate)
            {
                error = "";
                try
                {
                    var bytes = Utf8.GetBytes(MemoryJson.Encode(snapshot));
                    var primary = ReadFile(PrimaryPath); var backup = ReadFile(BackupPath);
                    if (Protected(primary) || Protected(backup)) { error = "未来版本或无法访问的记忆文件已保留。"; return false; }
                    var preserveBackup = primary.Status == MemoryReadStatus.Invalid;
                    if (preserveBackup && !backup.Success || primary.Status == MemoryReadStatus.Missing && backup.Status == MemoryReadStatus.Invalid)
                    { error = "没有有效副本，拒绝覆盖损坏记忆文件。"; return false; }
                    Directory.CreateDirectory(DirectoryPath);
                    if (preserveBackup) PreserveRejected(PrimaryPath);
                    if (primary.Success && backup.Status == MemoryReadStatus.Invalid) PreserveRejected(BackupPath);
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

        private static bool Protected(MemoryReadResult result) => result.Status == MemoryReadStatus.UnsupportedVersion || result.Status == MemoryReadStatus.IoError;
        private static void PreserveRejected(string path) => File.Copy(path, path + ".rejected-" + DateTime.UtcNow.Ticks + "-" + Guid.NewGuid().ToString("N"));
        private static MemoryReadResult ReadFile(string path)
        {
            try
            {
                if (Directory.Exists(path)) return new MemoryReadResult(MemoryReadStatus.IoError, detail: "记忆文件路径被目录占用。");
                if (!File.Exists(path)) return new MemoryReadResult(MemoryReadStatus.Missing, detail: "没有记忆文件。");
                if (new FileInfo(path).Length > MemoryJson.MaximumBytes) return new MemoryReadResult(MemoryReadStatus.Invalid, detail: "记忆文件超过字节预算。");
                return MemoryJson.Decode(Utf8.GetString(File.ReadAllBytes(path)));
            }
            catch (DecoderFallbackException exception) { return new MemoryReadResult(MemoryReadStatus.Invalid, detail: exception.Message); }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            { return new MemoryReadResult(MemoryReadStatus.IoError, detail: exception.Message); }
        }
    }
}
