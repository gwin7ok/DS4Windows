#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Profiles;
using DS4Windows.Processing.Abstractions;
using DS4Windows.UI.ViewModels;

namespace DS4Windows.UI.Services
{
    /// <summary>
    /// IPC コマンドが画面に頼む操作（<see cref="IIpcCommandHost"/>）の実装。コントローラー一覧とプロファイル一覧の ViewModel を操作し、
    /// サービスの切り替えとアプリの終了は、メイン画面が関数で渡す（画面の部品には触れない）。
    /// </summary>
    public sealed class MainWindowIpcHost : IIpcCommandHost
    {
        private readonly Action _toggleService;
        private readonly Action _closeApplication;
        private readonly ControllerListViewModel _controllers;
        private readonly ProfileList _profiles;

        public MainWindowIpcHost(Action toggleService, Action closeApplication, ControllerListViewModel controllers, ProfileList profiles)
        {
            ArgumentNullException.ThrowIfNull(toggleService);
            _toggleService = toggleService;
            ArgumentNullException.ThrowIfNull(closeApplication);
            _closeApplication = closeApplication;
            ArgumentNullException.ThrowIfNull(controllers);
            _controllers = controllers;
            ArgumentNullException.ThrowIfNull(profiles);
            _profiles = profiles;
        }

        public void ToggleService() => _toggleService();

        public void CloseApplication() => _closeApplication();

        public void DisconnectAllControllers()
        {
            // Attempt to disconnect all wireless controllers
            // Opt to make copy of Dictionary before iterating over contents
            var dictCopy = new Dictionary<int, CompositeDeviceModel>(_controllers.ControllerDict);
            foreach (KeyValuePair<int, CompositeDeviceModel> pair in dictCopy)
            {
                pair.Value.RequestDisconnect();
            }
        }

        public void DisconnectController(int index)
        {
            if (_controllers.ControllerDict.TryGetValue(index, out CompositeDeviceModel model))
            {
                model.RequestDisconnect();
            }
        }

        // 番号は入力スロット（Disconnect などと同じ）。一覧の並び順では引かない（途中のスロットが空だと、別のコントローラーの色が変わる）
        public void SetLightColor(int index, byte red, byte green, byte blue)
        {
            if (_controllers.ControllerDict.TryGetValue(index, out CompositeDeviceModel model))
            {
                model.ForceCustomLightColor(new DS4Windows.Core.Model.DS4Color(red, green, blue));
            }
        }

        public int FindProfileIndex(string profileName)
            => _profiles.ProfileListCol.Select((item, index) => new { item, index }).
                Where(x => string.Equals(x.item.Name, profileName, StringComparison.OrdinalIgnoreCase)).Select(x => x.index).DefaultIfEmpty(-1).First();

        public string GetProfileName(int index)
            => index >= 0 && index < _profiles.ProfileListCol.Count ? _profiles.ProfileListCol[index].Name : null;

        // 番号は入力スロット（一覧の並び順ではない）。接続していなければ null
        public IControllerSlotView GetControllerDevice(int index)
            => _controllers.ControllerDict.TryGetValue(index, out CompositeDeviceModel model) ? model.Device : null;

        public void LogDebug(string message) => AppLogger.Log(AppLogLevel.Info, message, LogOptions.UserFacing);
    }
}
