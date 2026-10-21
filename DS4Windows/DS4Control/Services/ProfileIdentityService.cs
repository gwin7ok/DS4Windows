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
    /// <para>
    /// Phase10-Step4d W5a: ID ↔ 名前の<b>メモリの索引</b>を持つ（実行時に頻繁に引くため。挙動は変えない）。索引の鮮度は、プロファイルのフォルダーの最終更新時刻で判定する
    /// （ファイルの追加・削除・名前変更・コピーはフォルダーの時刻を変える）。フォルダーの時刻が変わった、またはフォルダーのパスが変わったら、作り直す。
    /// ファイルシステムの時刻の粗さで直後の変更を見逃さないよう、フォルダーの時刻が <see cref="TrustAfter"/> 以内に新しいあいだは、索引を信用せず、毎回作り直す。
    /// 例外: ファイルの中身だけを外から書き換えた場合（ID の手編集）は、フォルダーの時刻が変わらないため、次にフォルダーが変わるまで反映されない
    /// （アプリ自身の書き込みは、その場で索引を更新する）。排他は <c>ProfileXmlStore.XmlIoLock</c> → 索引のロックの順（デッドロック防止）。
    /// </para>
    /// </summary>
    public sealed class ProfileIdentityService : IProfileIdentityService
    {
        private const string XmlExtension = ".xml";

        /// <summary>索引を信用するために、フォルダーの最終更新時刻が、索引を作った時点より、これ以上古い必要がある。</summary>
        internal static readonly TimeSpan TrustAfter = TimeSpan.FromSeconds(1);

        private readonly IPathService _pathService;
        private readonly ProfileIdFile _file = new ProfileIdFile();

        // 索引（_gate で保護する）
        private readonly object _gate = new object();
        private readonly Dictionary<string, string> _idToName = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _nameToId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private bool _built;
        private bool _trusted;
        private string _indexedFolder;
        private DateTime _indexedStamp;

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
                    string id = _file.NewId();
                    if (_file.WriteId(entry.Path, id))
                    {
                        entry.Id = id;
                        written++;
                    }
                    else
                    {
                        failed.Add(entry.Name);
                    }
                }

                lock (_gate)
                {
                    Rebuild(entries);
                }
            }

            if (written > 0)
                AppLogger.LogInfo($"[DI] ProfileIdentityService: wrote a profile ID to {written} profile(s)");
            if (failed.Count > 0)
                AppLogger.LogWarn($"[DI] ProfileIdentityService: could not write a profile ID to {failed.Count} profile(s): {string.Join(", ", failed)}");

            return failed;
        }

        public void Refresh()
        {
            lock (ProfileXmlStore.XmlIoLock)
            {
                lock (_gate)
                {
                    Rebuild(ReadEntries());
                }
            }
        }

        public string GetId(string profileName)
        {
            if (string.IsNullOrWhiteSpace(profileName)) return null;

            // 速い経路: 索引が有効なら、索引のロックだけで引く（実行時の読み取りが、長い XML の読み書き［XmlIoLock］を待たないように）
            lock (_gate)
            {
                if (IsFresh()) return _nameToId.TryGetValue(profileName, out string fast) ? fast : null;
            }

            lock (ProfileXmlStore.XmlIoLock)
            {
                lock (_gate)
                {
                    EnsureIndex();
                    return _nameToId.TryGetValue(profileName, out string id) ? id : null;
                }
            }
        }

        public string GetName(string profileId)
        {
            if (!_file.IsValid(profileId)) return null;

            lock (_gate)
            {
                if (IsFresh()) return _idToName.TryGetValue(profileId, out string fast) ? fast : null;
            }

            lock (ProfileXmlStore.XmlIoLock)
            {
                lock (_gate)
                {
                    EnsureIndex();
                    return _idToName.TryGetValue(profileId, out string name) ? name : null;
                }
            }
        }

        public string EnsurePersisted(string profileName)
        {
            string path = PathOf(profileName);
            if (path == null || !File.Exists(path)) return null;

            lock (ProfileXmlStore.XmlIoLock)
            {
                lock (_gate)
                {
                    EnsureIndex();

                    string name = Path.GetFileNameWithoutExtension(path);
                    if (_nameToId.TryGetValue(name, out string known)) return known;

                    // 索引にない: ファイルを読み（外から作られた直後など）、ID がなければ書く
                    string id = _file.ReadId(path);
                    if (id == null)
                    {
                        id = _file.NewId();
                        if (!_file.WriteId(path, id)) return null;
                    }

                    _idToName[id] = name;
                    _nameToId[name] = id;
                    return id;
                }
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

        // ---- 索引（呼び出し側が XmlIoLock と _gate を取る）----

        // 索引が有効か（作り直しが要らないか）。呼び出し側が _gate を取る
        private bool IsFresh()
        {
            if (!_built || !_trusted) return false;

            string folder = _pathService.ProfilesPath;
            return string.Equals(folder, _indexedFolder, StringComparison.Ordinal) && StampOf(folder) == _indexedStamp;
        }

        private void EnsureIndex()
        {
            if (IsFresh()) return;

            Rebuild(ReadEntries());
        }

        private void Rebuild(List<Entry> entries)
        {
            string folder = _pathService.ProfilesPath;
            DateTime stamp = StampOf(folder);

            _idToName.Clear();
            _nameToId.Clear();
            foreach (Entry entry in entries)
            {
                if (entry.Id == null) continue;

                // 重複した ID は、最初に見つかったものを残す（EnsureAllPersisted が付け替えるまでの暫定）
                if (!_idToName.ContainsKey(entry.Id)) _idToName[entry.Id] = entry.Name;
                _nameToId[entry.Name] = entry.Id;
            }

            _built = true;
            _indexedFolder = folder;
            _indexedStamp = stamp;

            // フォルダーの時刻が、いま作った時点から TrustAfter 以内に新しければ、直後の変更を時刻で見分けられないため、次の呼び出しでも作り直す
            _trusted = stamp != DateTime.MinValue && DateTime.UtcNow - stamp > TrustAfter;
        }

        private static DateTime StampOf(string folder)
        {
            try
            {
                return !string.IsNullOrEmpty(folder) && Directory.Exists(folder) ? Directory.GetLastWriteTimeUtc(folder) : DateTime.MinValue;
            }
            catch
            {
                return DateTime.MinValue;
            }
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
