using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DS4Windows.DI;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase10-Step4d W1（Phase10-Step4d-Plan.md §3）: <see cref="IProfileIdentityService"/> の実装（DI の Singleton）。
    /// プロファイルのフォルダー（<see cref="IPathService.ProfilesPath"/>）の各ファイルの <c>&lt;ProfileId&gt;</c> を、同一 XML 排他ロックの下で読み書きする。
    /// 状態は持たない（そのつどファイルを調べる）。
    /// </summary>
    public sealed class ProfileIdentityService : IProfileIdentityService
    {
        private const string XmlExtension = ".xml";

        private readonly IPathService _pathService;
        private readonly ProfileIdFile _file = new ProfileIdFile();

        public ProfileIdentityService(IPathService pathService)
        {
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
        }

        private sealed class Entry
        {
            public string Name;
            public string Path;
            public string Id;
            public DateTime Created;
        }

        public IReadOnlyList<string> EnsureAllPersisted()
        {
            var failed = new List<string>();
            int written = 0;

            lock (ProfileXmlStore.XmlIoLock)
            {
                List<Entry> entries = ReadEntries();

                // 重複: 同じ ID を持つファイルのうち、作成日時が古いもの（同じなら名前順で先のもの）が ID を保つ。残りは新しい ID を振る
                foreach (var group in entries.Where(e => e.Id != null).GroupBy(e => e.Id).Where(g => g.Count() > 1))
                {
                    foreach (Entry loser in group.OrderBy(e => e.Created).ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase).Skip(1))
                        loser.Id = null;
                }

                foreach (Entry entry in entries.Where(e => e.Id == null))
                {
                    if (_file.WriteId(entry.Path, _file.NewId()))
                        written++;
                    else
                        failed.Add(entry.Name);
                }
            }

            if (written > 0)
                AppLogger.LogInfo($"[DI] ProfileIdentityService: wrote a profile ID to {written} profile(s)");
            if (failed.Count > 0)
                AppLogger.LogWarn($"[DI] ProfileIdentityService: could not write a profile ID to {failed.Count} profile(s): {string.Join(", ", failed)}");

            return failed;
        }

        public string GetId(string profileName)
        {
            string path = PathOf(profileName);
            if (path == null) return null;

            lock (ProfileXmlStore.XmlIoLock)
            {
                return _file.ReadId(path);
            }
        }

        public string GetName(string profileId)
        {
            if (!_file.IsValid(profileId)) return null;

            lock (ProfileXmlStore.XmlIoLock)
            {
                return ReadEntries().FirstOrDefault(e => string.Equals(e.Id, profileId, StringComparison.Ordinal))?.Name;
            }
        }

        public string EnsurePersisted(string profileName)
        {
            string path = PathOf(profileName);
            if (path == null || !File.Exists(path)) return null;

            lock (ProfileXmlStore.XmlIoLock)
            {
                string id = _file.ReadId(path);
                if (id != null) return id;

                id = _file.NewId();
                return _file.WriteId(path, id) ? id : null;
            }
        }

        public bool TryResolve(string profileId, string nameHint, out string profileName)
        {
            string byId = GetName(profileId);
            if (byId != null)
            {
                profileName = byId;
                return true;
            }

            string path = PathOf(nameHint);
            if (path != null && File.Exists(path))
            {
                profileName = nameHint;
                return true;
            }

            profileName = null;
            return false;
        }

        // 呼び出し側が XmlIoLock を取る
        private List<Entry> ReadEntries()
        {
            var entries = new List<Entry>();
            string folder = _pathService.ProfilesPath;
            string[] files;
            try
            {
                if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) return entries;
                files = Directory.GetFiles(folder, "*" + XmlExtension);
            }
            catch (Exception ex)
            {
                AppLogger.LogWarn($"[DI] ProfileIdentityService: cannot enumerate profiles: {ex.Message}");
                return entries;
            }

            foreach (string file in files)
            {
                DateTime created;
                try { created = File.GetCreationTimeUtc(file); }
                catch { created = DateTime.MaxValue; }

                entries.Add(new Entry
                {
                    Name = Path.GetFileNameWithoutExtension(file),
                    Path = file,
                    Id = _file.ReadId(file),
                    Created = created,
                });
            }

            return entries;
        }

        private string PathOf(string profileName)
        {
            if (string.IsNullOrWhiteSpace(profileName)) return null;

            string folder = _pathService.ProfilesPath;
            return string.IsNullOrEmpty(folder) ? null : Path.Combine(folder, profileName + XmlExtension);
        }
    }
}
