using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using DS4Windows.Core.Logging;
using DS4Windows.Input;
using DS4Windows.UI.ViewModels;

namespace DS4Windows.UI
{
    /// <summary>
    /// Phase9 9-F F-2b（Phase9-F-Plan.md §6）: MainWindow の IPC コマンド（WM_COPYDATA）の処理。旧 <c>WndProc</c> の巨大な if の連なりを、コマンドごとのメソッドへ分けた。
    /// コマンドの受け付け順（if／else if の順序）、条件、例外の握りつぶし、query の結果の書き込みは、従来のまま。UI の部品に触れるため、MainWindow の partial に置く。
    /// </summary>
    public partial class MainWindow : DS4Windows.Processing.Abstractions.IIpcCommandHost
    {

        /// <summary>
        /// WM_COPYDATA: DS4Win のコマンドが、文字列として lpData のバッファに入っている。例外は、すべて握りつぶす（致命的ではないため）。
        /// </summary>
        private void HandleCopyData(IntPtr lParam)
        {
            try
            {
                DS4Windows.Core.Platform.COPYDATASTRUCT cds = (DS4Windows.Core.Platform.COPYDATASTRUCT)Marshal.PtrToStructure(lParam, typeof(DS4Windows.Core.Platform.COPYDATASTRUCT));   // Phase14b-Step16: 旧 App.COPYDATASTRUCT
                if (IpcCommandParser.IsValidSize(cds.cbData))
                {
                    byte[] buffer = new byte[cds.cbData];
                    Marshal.Copy(cds.lpData, buffer, 0, cds.cbData);
                    string[] strData = IpcCommandParser.Parse(buffer);

                    // Phase13-Step11 11-c: コマンドの実行は IpcCommandProcessor へ移した（受け付けの順序・条件は従来のまま）
                    ipcProcessor.Execute(this, strData);
                }
            }
            catch
            {
                // Eat all exceptions in WM_COPYDATA because exceptions here are not fatal for DS4Windows background app
            }
        }


        // Phase13-Step11 11-c: IIpcCommandHost（IpcCommandProcessor が UI に頼む操作）。旧 Ipc* の UI に触れる部分を、そのまま移したもの
        void DS4Windows.Processing.Abstractions.IIpcCommandHost.ToggleService() => ChangeService();

        void DS4Windows.Processing.Abstractions.IIpcCommandHost.CloseApplication()
        {
            // Call closing method and let it to close editor wnd (if it is open) before proceeding to the actual "app closed" handler
            MainDS4Window_Closing(null, new System.ComponentModel.CancelEventArgs());
            MainDS4Window_Closed(this, new System.EventArgs());
        }

        void DS4Windows.Processing.Abstractions.IIpcCommandHost.DisconnectAllControllers()
        {
            // Attempt to disconnect all wireless controllers
            // Opt to make copy of Dictionary before iterating over contents
            var dictCopy = new Dictionary<int, CompositeDeviceModel>(conLvViewModel.ControllerDict);
            foreach (KeyValuePair<int, CompositeDeviceModel> pair in dictCopy)
            {
                pair.Value.RequestDisconnect();
            }
        }

        void DS4Windows.Processing.Abstractions.IIpcCommandHost.DisconnectController(int index)
        {
            if (conLvViewModel.ControllerDict.TryGetValue(index, out CompositeDeviceModel model))
            {
                model.RequestDisconnect();
            }
        }

        void DS4Windows.Processing.Abstractions.IIpcCommandHost.SetLightColor(int index, byte red, byte green, byte blue)
            => conLvViewModel.ControllerCol[index].UpdateCustomLightColor(Color.FromRgb(red, green, blue));

        int DS4Windows.Processing.Abstractions.IIpcCommandHost.FindProfileIndex(string profileName)
            => profileListHolder.ProfileListCol.Select((item, index) => new { item, index }).
                Where(x => string.Equals(x.item.Name, profileName, StringComparison.OrdinalIgnoreCase)).Select(x => x.index).DefaultIfEmpty(-1).First();

        string DS4Windows.Processing.Abstractions.IIpcCommandHost.GetProfileName(int index)
            => index >= 0 && index < profileListHolder.ProfileListCol.Count ? profileListHolder.ProfileListCol[index].Name : null;

        int DS4Windows.Processing.Abstractions.IIpcCommandHost.ControllerCount => conLvViewModel.ControllerCol.Count;

        DS4Device DS4Windows.Processing.Abstractions.IIpcCommandHost.GetControllerDevice(int index)
            => conLvViewModel.ControllerCol[index]?.Device;

        void DS4Windows.Processing.Abstractions.IIpcCommandHost.LogDebug(string message) => AppLogger.Log(AppLogLevel.Info, message, LogOptions.UserFacing);

        void DS4Windows.Processing.Abstractions.IIpcCommandHost.WriteQueryResult(string value)
            => ipcResultWriter.WriteQueryResult(value);   // Phase14b-Step16: 旧 ((Application.Current) as App).WriteIPCResultDataMMF
    }
}
