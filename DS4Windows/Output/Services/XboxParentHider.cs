using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DS4Windows.Core.Abstractions;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Output.Services
{
    /// <summary>
    /// <see cref="IXboxParentHider"/> の実装。HidHide の非表示の一覧（ブラックリスト）へ、Xbox 系コントローラーの USB の親を足す・外す。
    /// 利用者が入れた登録は触らない: 足したのは自分だけの分を、メモリと記録ファイルに持ち、戻すのもその分だけ。
    /// 記録ファイルは、異常終了で足したまま残ったときに、次のサービスの開始で戻すためのもの。
    /// 隠す機能の全体スイッチ（cloak）と、許可リストの反転は、利用者の設定なので切り替えない（使えないときは、何もせず理由を返す）。
    /// </summary>
    public sealed class XboxParentHider : IXboxParentHider
    {
        public const string RecordFileName = "HidHideAdded.txt";

        private readonly Func<bool> _hidHideInstalled;
        private readonly Func<string, IReadOnlyList<string>> _targetsOf;
        private readonly IPathService _pathService;
        private readonly IHidHideApiFactory _apiFactory;
        private readonly IHidHideService _hidHideService;
        private readonly object _sync = new object();
        private readonly HashSet<string> _added = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public XboxParentHider(IEnvironmentService environmentService, IPathService pathService, IHidHideApiFactory apiFactory, IHidHideService hidHideService)
            : this(() => environmentService.HidHideInstalled, path => TargetsOf(environmentService, path), pathService, apiFactory, hidHideService)
        {
            if (environmentService == null)
            {
                throw new ArgumentNullException(nameof(environmentService));
            }
        }

        /// <summary>導入済みかの確認と、パスから隠す対象のインスタンス ID の一覧への変換（OS に問い合わせる）を、差し替えられる形。テストで、OS に触れずに使う。</summary>
        public XboxParentHider(Func<bool> hidHideInstalled, Func<string, IReadOnlyList<string>> targetsOf, IPathService pathService, IHidHideApiFactory apiFactory, IHidHideService hidHideService)
        {
            _hidHideInstalled = hidHideInstalled ?? throw new ArgumentNullException(nameof(hidHideInstalled));
            _targetsOf = targetsOf ?? throw new ArgumentNullException(nameof(targetsOf));
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
            _apiFactory = apiFactory ?? throw new ArgumentNullException(nameof(apiFactory));
            _hidHideService = hidHideService ?? throw new ArgumentNullException(nameof(hidHideService));
        }

        // XInput を隠すには、USB の親（XUSB の持ち主）と、その下の HID の子の両方が要る（親だけでは、XInput が他のアプリに見える）
        private static IReadOnlyList<string> TargetsOf(IEnvironmentService environmentService, string devicePath)
        {
            string parent = environmentService.GetInstanceIdFromDevicePath(devicePath);
            if (string.IsNullOrEmpty(parent))
            {
                return Array.Empty<string>();
            }

            return new[] { parent }.Concat(DS4Windows.Core.Platform.DevicePathProbe.GetHidDescendantInstanceIds(parent)).ToList();
        }

        public XboxParentHideResult Apply(string devicePath, bool hide)
        {
            IReadOnlyList<string> targets = string.IsNullOrEmpty(devicePath) ? Array.Empty<string>() : _targetsOf(devicePath);
            if (targets.Count == 0)
            {
                return hide ? XboxParentHideResult.DeviceNotFound : XboxParentHideResult.NothingToRestore;
            }

            lock (_sync)
            {
                if (!_hidHideInstalled())
                {
                    return XboxParentHideResult.HidHideNotInstalled;
                }

                // 隠したあとも DS4Windows が XInput で読めるよう、先に許可リストへ入れる（すでにあれば何もしない）
                if (hide)
                {
                    _hidHideService.CheckPresence();
                }

                using (IHidHideApi api = _apiFactory.Open())
                {
                    if (!api.IsOpen())
                    {
                        return XboxParentHideResult.NoAccess;
                    }

                    if (hide)
                    {
                        if (api.GetWhiteListInverseState())
                        {
                            return XboxParentHideResult.InverseWhitelist;
                        }

                        if (!api.GetActiveState())
                        {
                            return XboxParentHideResult.CloakOff;
                        }

                        List<string> list = ReadList(api);
                        string[] missing = targets.Where(t => !list.Contains(t, StringComparer.OrdinalIgnoreCase)).ToArray();
                        if (missing.Length == 0)
                        {
                            return XboxParentHideResult.AlreadyHidden;
                        }

                        list.AddRange(missing);
                        if (!api.SetBlacklist(list))
                        {
                            return XboxParentHideResult.WriteFailed;
                        }

                        foreach (string id in missing)
                        {
                            _added.Add(id);
                        }

                        SaveRecord();
                        return XboxParentHideResult.Hidden;
                    }

                    string[] mine = targets.Where(t => _added.Contains(t)).ToArray();
                    if (mine.Length == 0)
                    {
                        return XboxParentHideResult.NothingToRestore;
                    }

                    return RemoveFromList(api, mine) ? Forget(mine) : XboxParentHideResult.WriteFailed;
                }
            }
        }

        public void RestoreAll()
        {
            lock (_sync)
            {
                if (_added.Count == 0)
                {
                    return;
                }

                using (IHidHideApi api = _apiFactory.Open())
                {
                    // 開けないときは、記録ファイルを残し、次の開始で戻す
                    if (!api.IsOpen())
                    {
                        return;
                    }

                    if (RemoveFromList(api, _added.ToArray()))
                    {
                        _added.Clear();
                        DeleteRecord();
                    }
                }
            }
        }

        public void RecoverStale()
        {
            lock (_sync)
            {
                string[] stale = ReadRecord();
                if (stale.Length == 0 || !_hidHideInstalled())
                {
                    return;
                }

                using (IHidHideApi api = _apiFactory.Open())
                {
                    if (!api.IsOpen())
                    {
                        return;
                    }

                    if (RemoveFromList(api, stale))
                    {
                        DeleteRecord();
                    }
                }
            }
        }

        private XboxParentHideResult Forget(IEnumerable<string> instanceIds)
        {
            foreach (string id in instanceIds)
            {
                _added.Remove(id);
            }

            if (_added.Count == 0)
            {
                DeleteRecord();
            }
            else
            {
                SaveRecord();
            }

            return XboxParentHideResult.Restored;
        }

        private static List<string> ReadList(IHidHideApi api)
        {
            return api.GetBlacklist().Where(x => !string.IsNullOrEmpty(x)).ToList();
        }

        // 指定の ID だけを外す（他の登録は、そのまま書き戻す）。外す対象が一覧になくても、成功とする
        private static bool RemoveFromList(IHidHideApi api, IEnumerable<string> ids)
        {
            List<string> list = ReadList(api);
            int removed = list.RemoveAll(x => ids.Contains(x, StringComparer.OrdinalIgnoreCase));
            return removed == 0 || api.SetBlacklist(list);
        }

        private string RecordPath => Path.Combine(_pathService.AppDataPath, RecordFileName);

        private void SaveRecord()
        {
            try
            {
                File.WriteAllLines(RecordPath, _added);
            }
            catch (Exception)
            {
                // 記録に失敗しても、隠すこと自体は続ける（異常終了のあとの復旧ができなくなるだけ）
            }
        }

        private string[] ReadRecord()
        {
            try
            {
                return File.Exists(RecordPath) ? File.ReadAllLines(RecordPath).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray() : Array.Empty<string>();
            }
            catch (Exception)
            {
                return Array.Empty<string>();
            }
        }

        private void DeleteRecord()
        {
            try
            {
                if (File.Exists(RecordPath))
                {
                    File.Delete(RecordPath);
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
