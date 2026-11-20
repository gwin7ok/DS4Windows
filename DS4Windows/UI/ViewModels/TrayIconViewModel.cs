/*
DS4Windows
Copyright (C) 2023  Travis Nickles

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

#nullable enable

using DS4Windows.UI.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Profiles;
using DS4Windows.Core.Utilities;
using DS4Windows.Processing.Abstractions;
using DS4Windows.UI.Abstractions;
using DS4Windows.Core.Model;

namespace DS4Windows.UI.ViewModels
{
    public class TrayIconViewModel
    {
        private string tooltipText = "DS4Windows";
        private string iconSource;
        public const string ballonTitle = "DS4Windows";
        // static の初期化子（Global.exeversion）から、コンストラクタで解決するインスタンスの値へ変更
        // （利用者は PopulateToolText のみ。静的初期化から Global を呼ばない）。
        private readonly string trayTitle;
        private readonly List<TrayMenuItemModel> menuItems = new List<TrayMenuItemModel>();
        private readonly TrayMenuItemModel changeServiceItem;
        private readonly TrayMenuItemModel openItem;
        private readonly TrayMenuItemModel minimizeItem;
        private readonly TrayMenuItemModel openProgramItem;
        private readonly TrayMenuItemModel closeItem;
        #pragma warning disable CS0414 // prevBattery assigned but not read; keep for future tooltip updates
        private int? prevBattery = null;
        #pragma warning restore CS0414


        public string TooltipText
        {
            get => tooltipText;
            set
            {
                string temp = value;
                if (value.Length > 63) temp = value.Substring(0, 63);
                if (tooltipText == temp) return;
                tooltipText = temp;
                //Trace.WriteLine(tooltipText);
                try
                {
                    TooltipTextChanged?.Invoke(this, EventArgs.Empty);
                }
                catch (InvalidOperationException) { /* 後始末・解放の失敗は無視してよい（解放済み・切断済みでも続ける） */ }
            }
        }
        public event EventHandler TooltipTextChanged;

        public string IconSource { get => iconSource;
            set
            {
                if (iconSource == value) return;
                iconSource = value;
                IconSourceChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>トレイのメニューの項目（View が WPF のメニューへ変換する）。変わったときは <see cref="MenuChanged"/> が起きる。</summary>
        public IReadOnlyList<TrayMenuItemModel> MenuItems => menuItems;

        /// <summary>メニューの項目か状態が変わった。UI のスレッドで起きる（呼び出しもとのスレッドによらず、View 側で受け直す）。</summary>
        public event EventHandler MenuChanged;

        public event EventHandler IconSourceChanged;
        public event EventHandler RequestShutdown;
        public event EventHandler RequestOpen;
        public event EventHandler RequestMinimize;
        public event EventHandler RequestServiceChange;

        private ReaderWriterLockSlim _colLocker = new ReaderWriterLockSlim();
        private List<ControllerHolder> controllerList = new List<ControllerHolder>();
        private ProfileList profileListHolder;
        private readonly IControllerSlotQuery slots;
        private readonly DS4Windows.Processing.Abstractions.IServiceLifecycle lifecycle;
        private readonly IAppearanceSettingsService appearanceSettings;
        private readonly IProfileRepository profileRepository;
        private readonly IPathService pathService;
        // UI スレッドへの受け渡し（コンストラクタの必須引数）
        private readonly IUiDispatcher uiDispatcher;

        public delegate void ProfileSelectedHandler(TrayIconViewModel sender,
            ControllerHolder item, string profile);
        public event ProfileSelectedHandler ProfileSelected;

        //public TrayIconViewModel(Tester tester)
        public TrayIconViewModel(IControllerSlotQuery slots, DS4Windows.Processing.Abstractions.IServiceLifecycle lifecycle, ProfileList profileListHolder,
            IAppearanceSettingsService appearanceSettings, IProfileRepository profileRepository,
            IEnvironmentService environmentService, IPathService pathService,
            IUiDispatcher dispatcher)
        {
            ArgumentNullException.ThrowIfNull(dispatcher);
            this.uiDispatcher = dispatcher;
            // 依存はすべて必須引数（Pure DI。決定1＝S9-A）
            ArgumentNullException.ThrowIfNull(appearanceSettings);
            this.appearanceSettings = appearanceSettings;
            ArgumentNullException.ThrowIfNull(profileRepository);
            this.profileRepository = profileRepository;
            ArgumentNullException.ThrowIfNull(pathService);
            this.pathService = pathService;
            if (environmentService == null) throw new ArgumentNullException(nameof(environmentService));

            this.profileListHolder = profileListHolder;
            this.slots = slots;
            ArgumentNullException.ThrowIfNull(lifecycle);
            this.lifecycle = lifecycle;   // 旧 ControlService.running の転送
            trayTitle = $"DS4Windows v{environmentService.ApplicationVersion}";
            iconSource = TrayIconResources.iconChoiceResources[appearanceSettings.UseIconChoice];
            appearanceSettings.BatteryChanged += UpdateTrayBattery;
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace("[DI] TrayIconViewModel: subscribed BatteryChanged via IAppearanceSettingsService");
            changeServiceItem = new TrayMenuItemModel() { Header = "Start", Click = ChangeControlServiceItem_Click, IsEnabled = false };
            openItem = new TrayMenuItemModel() { Header = "Open", IsBold = true, Click = OpenMenuItem_Click };
            minimizeItem = new TrayMenuItemModel() { Header = "Minimize", Click = MinimizeMenuItem_Click };
            openProgramItem = new TrayMenuItemModel() { Header = "Open Program Folder", Click = OpenProgramFolderItem_Click };
            closeItem = new TrayMenuItemModel() { Header = "Exit", Click = ExitMenuItem_Click };

            PopulateControllerList();
            PopulateToolText();
            PopulateContextMenu();
            SetupEvents();
            profileListHolder.ProfileListCol.CollectionChanged += ProfileListCol_CollectionChanged;
            profileRepository.SelectedProfileChanged += ProfileRepository_SelectedProfileChanged;

            lifecycle.ServiceStarted += BuildControllerList;
            lifecycle.ServiceStarted += HookEvents;
            lifecycle.ServiceStarted += StartPopulateText;
            lifecycle.PreServiceStop += ClearToolText;
            lifecycle.PreServiceStop += UnhookEvents;
            lifecycle.PreServiceStop += ClearControllerList;
            lifecycle.RunningChanged += Service_RunningChanged;
            slots.SlotChanged += Service_HotplugController;
            /*tester.StartControllers += HookBatteryUpdate;
            tester.StartControllers += StartPopulateText;
            tester.PreRemoveControllers += ClearToolText;
            tester.HotplugControllers += HookBatteryUpdate;
            tester.HotplugControllers += StartPopulateText;
            */
        }

        private void Service_RunningChanged(object sender, EventArgs e)
        {
            string temp = lifecycle.Running ? "Stop" : "Start";
            uiDispatcher.BeginInvoke((Action)(() =>
            {
                changeServiceItem.Header = temp;
                changeServiceItem.IsEnabled = true;
                MenuChanged?.Invoke(this, EventArgs.Empty);
            }));
        }

        private void ClearControllerList(object sender, EventArgs e)
        {
            _colLocker.EnterWriteLock();
            controllerList.Clear();
            _colLocker.ExitWriteLock();
        }

        private void UnhookEvents(object sender, EventArgs e)
        {
            _colLocker.EnterReadLock();
            foreach (ControllerHolder holder in controllerList)
            {
                IControllerSlotView currentDev = holder.Device;
                RemoveDeviceEvents(currentDev);
            }
            _colLocker.ExitReadLock();
        }

        private void Service_HotplugController(object sender, ControllerSlotChangedEventArgs args)
        {
            IControllerSlotView device = args.View;
            int index = args.SlotIndex;
            SetupDeviceEvents(device);
            _colLocker.EnterWriteLock();
            controllerList.Add(new ControllerHolder(device, index));
            _colLocker.ExitWriteLock();
        }

        // プロファイルを適用したとき（トレイ・一覧・ホットキー・自動プロファイルのどれから選んでも）、チェックを移すために組み直す。
        // 適用は別のスレッドから来るため、項目の組み直しは UI のスレッドで行う
        private void ProfileRepository_SelectedProfileChanged(object sender, SelectedProfileChangedEventArgs e)
        {
            uiDispatcher.BeginInvoke(PopulateContextMenu);
        }

        private void ProfileListCol_CollectionChanged(object sender,
            System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            PopulateContextMenu();
        }

        private void BuildControllerList(object sender, EventArgs e)
        {
            PopulateControllerList();
        }

        public void PopulateContextMenu()
        {
            menuItems.Clear();
            TrayMenuItemModel item;
            int idx = 0;

            using (ReadLocker locker = new ReadLocker(_colLocker))
            {
                foreach (ControllerHolder holder in controllerList)
                {
                    int slotIdx = idx;
                    item = new TrayMenuItemModel() { Header = $"Controller {idx + 1}" };
                    string currentProfile = profileRepository.ProfilePath[idx];
                    foreach (ProfileEntity entry in profileListHolder.ProfileListCol)
                    {
                        string profileName = entry.Name;
                        item.Children.Add(new TrayMenuItemModel()
                        {
                            Header = profileName,
                            IsChecked = entry.Name == currentProfile,
                            Click = () => ProfileItem_Click(slotIdx, profileName),
                        });
                    }

                    menuItems.Add(item);
                    idx++;
                }

                item = new TrayMenuItemModel() { Header = "Disconnect Menu" };
                idx = 0;
                foreach (ControllerHolder holder in controllerList)
                {
                    IControllerSlotView tempDev = holder.Device;
                    if (tempDev.Synced && !tempDev.Charging)
                    {
                        int slotIdx = idx;
                        item.Children.Add(new TrayMenuItemModel()
                        {
                            Header = $"Disconnect Controller {idx + 1}",
                            Click = () => DisconnectMenuItem_Click(slotIdx),
                        });
                    }

                    idx++;
                }

                if (idx == 0)
                {
                    item.IsEnabled = false;
                }
            }

            menuItems.Add(item);
            menuItems.Add(TrayMenuItemModel.Separator());
            PopulateStaticItems();
            MenuChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ChangeControlServiceItem_Click()
        {
            changeServiceItem.IsEnabled = false;
            MenuChanged?.Invoke(this, EventArgs.Empty);
            RequestServiceChange?.Invoke(this, EventArgs.Empty);
        }

        private void OpenProgramFolderItem_Click()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(pathService.ProgramDirectory);
            startInfo.UseShellExecute = true;
            using (Process temp = Process.Start(startInfo))
            {
            }
        }

        private void OpenMenuItem_Click()
        {
            RequestOpen?.Invoke(this, EventArgs.Empty);
        }

        private void MinimizeMenuItem_Click()
        {
            RequestMinimize?.Invoke(this, EventArgs.Empty);
        }

        private void ProfileItem_Click(int idx, string profileName)
        {
            ControllerHolder holder = controllerList[idx];
            ProfileSelected?.Invoke(this, holder, profileName);
        }

        private void DisconnectMenuItem_Click(int idx)
        {
            ControllerHolder holder = controllerList[idx];
            IControllerSlotView tempDev = holder?.Device;
            if (tempDev != null && tempDev.Synced && !tempDev.Charging)
            {
                if (tempDev.ConnectionType == ConnectionType.BT)
                {
                    //tempDev.StopUpdate();
                    tempDev.DisconnectBluetooth();
                }
                else if (tempDev.ConnectionType == ConnectionType.SONYWA)
                {
                    tempDev.DisconnectDongle();
                }
            }

            //controllerList[idx] = null;
        }

        private void PopulateControllerList()
        {
            //IEnumerable<InputDeviceCore> devices = InputDeviceRegistry.GetControllers();
            int idx = 0;
            _colLocker.EnterWriteLock();
            foreach (ControllerSlotEntry entry in slots.Snapshot())
            {
                controllerList.Add(new ControllerHolder(entry.View, idx));
                idx++;
            }
            _colLocker.ExitWriteLock();
        }

        private void StartPopulateText(object sender, EventArgs e)
        {
            uiDispatcher.BeginInvoke(() =>
            {
                PopulateToolText();
            });
            //PopulateContextMenu();
        }

        private void PopulateToolText()
        {
            List<string> items = new List<string>();
            items.Add(trayTitle);
            //IEnumerable<InputDeviceCore> devices = InputDeviceRegistry.GetControllers();
            int idx = 1;
            //foreach (InputDeviceCore currentDev in devices)
            _colLocker.EnterReadLock();
            foreach (ControllerHolder holder in controllerList)
            {
                IControllerSlotView currentDev = holder.Device;
                items.Add($"{idx}: {currentDev.ConnectionType} {currentDev.Battery}%{(currentDev.Charging ? "+" : "")}");
                idx++;
            }
            _colLocker.ExitReadLock();

            TooltipText = string.Join("\n", items);
        }

        private void SetupEvents()
        {
            //IEnumerable<InputDeviceCore> devices = InputDeviceRegistry.GetControllers();
            //foreach (InputDeviceCore currentDev in devices)
            _colLocker.EnterReadLock();
            foreach (ControllerHolder holder in controllerList)
            {
                IControllerSlotView currentDev = holder.Device;
                SetupDeviceEvents(currentDev);
            }
            _colLocker.ExitReadLock();
        }

        private void SetupDeviceEvents(IControllerSlotView device)
        {
            device.BatteryChanged += UpdateForBattery;
            device.ChargingChanged += UpdateForBattery;
            device.Removal += CurrentDev_Removal;
        }

        private void RemoveDeviceEvents(IControllerSlotView device)
        {
            device.BatteryChanged -= UpdateForBattery;
            device.ChargingChanged -= UpdateForBattery;
            device.Removal -= CurrentDev_Removal;
        }

        private void CurrentDev_Removal(object sender, EventArgs e)
        {
            IControllerSlotView currentDev = sender as IControllerSlotView;
            ControllerHolder item = null;
            int idx = 0;

            using (WriteLocker locker = new WriteLocker(_colLocker))
            {
                foreach (ControllerHolder holder in controllerList)
                {
                    if (currentDev == holder.Device)
                    {
                        item = holder;
                        break;
                    }

                    idx++;
                }

                if (item != null)
                {
                    controllerList.RemoveAt(idx);
                    RemoveDeviceEvents(currentDev);
                }
            }

            PopulateToolText();
        }

        private void HookEvents(object sender, EventArgs e)
        {
            SetupEvents();
        }

        private void UpdateForBattery(object sender, EventArgs e)
        {
            // Force invoke from GUI thread
            uiDispatcher.BeginInvoke(() =>
            {
                PopulateToolText();
            });
        }

        private void ClearToolText(object sender, EventArgs e)
        {
            uiDispatcher.BeginInvoke(() =>
            {
                TooltipText = "DS4Windows";
            });
            //contextMenu.Items.Clear();
        }

        private void PopulateStaticItems()
        {
            menuItems.Add(changeServiceItem);
            menuItems.Add(openItem);
            menuItems.Add(minimizeItem);
            menuItems.Add(openProgramItem);
            menuItems.Add(TrayMenuItemModel.Separator());
            menuItems.Add(closeItem);
        }

        public void ClearContextMenu()
        {
            menuItems.Clear();
            PopulateStaticItems();
            MenuChanged?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateTrayBattery(object sender, byte percentage)
        {
            IconSource = percentage switch
            {
                < 10 => $"{ResourcePaths.ResourcesPrefix}/0.ico",
                >= 10 and < 20 => $"{ResourcePaths.ResourcesPrefix}/10.ico",
                >= 20 and < 30 => $"{ResourcePaths.ResourcesPrefix}/20.ico",
                >= 30 and < 40 => $"{ResourcePaths.ResourcesPrefix}/30.ico",
                >= 40 and < 50 => $"{ResourcePaths.ResourcesPrefix}/40.ico",
                >= 50 and < 60 => $"{ResourcePaths.ResourcesPrefix}/50.ico",
                >= 60 and < 70 => $"{ResourcePaths.ResourcesPrefix}/60.ico",
                >= 70 and < 80 => $"{ResourcePaths.ResourcesPrefix}/70.ico",
                >= 80 and < 90 => $"{ResourcePaths.ResourcesPrefix}/80.ico",
                >= 90 and < 100 => $"{ResourcePaths.ResourcesPrefix}/90.ico",
                100 => $"{ResourcePaths.ResourcesPrefix}/100.ico",
                _ => $"{ResourcePaths.ResourcesPrefix}/DS4W.ico"
            };
        }

        private void ExitMenuItem_Click()
        {
            RequestShutdown?.Invoke(this, EventArgs.Empty);
        }
    }
}
