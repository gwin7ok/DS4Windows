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

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Model;
using DS4Windows.Core.Profiles;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.UI.ViewModels
{

    public class CompositeDeviceModel : ObservableObject
    {
        private IControllerSlotView device;
        private string selectedProfile;
        private ProfileList profileListHolder;
        private ProfileEntity? selectedEntity;
        private int selectedIndex = -1;
        internal bool suppressSelectedIndexChanged = false; // ★統合抑制フラグ（Global_SelectedProfileChangedからアクセス可能）
        private int devIndex;
        private readonly IProfileSettingsService profileSettingsService;
        // 領域の窓口は呼ぶたびに作られるため、ライトバーの窓口だけ一度作って使い回す（色の取得は画面の更新で頻繁に呼ばれる）
        private DS4Windows.Core.Settings.LightbarSlotSettings? lightbarWindow;
        private DS4Windows.Core.Settings.LightbarSlotSettings LightbarWindow =>
            lightbarWindow ??= profileSettingsService.ForSlot(devIndex).Lightbar;
        private readonly IProfileRepository profileRepo;


        public IControllerSlotView Device { get => device; set => device = value; }

        /// <summary>機器の同期の変化（USB／Bluetooth の接続の切り替わり）を購読する。画面が機器の型を直接触らないための入口。</summary>
        public void SubscribeSyncChange(EventHandler handler) => device.SyncChange += handler;
        public string SelectedProfile { get => selectedProfile; set => selectedProfile = value; }
        public ProfileList ProfileEntities { get => profileListHolder; set => profileListHolder = value; }
        public ObservableCollection<ProfileEntity> ProfileListCol => profileListHolder.ProfileListCol;

        public string LightColor
        {
            get
            {
                DS4Color color;
                if (LightbarWindow.Info.ds4winSettings.useCustomLed)
                {
                    color = LightbarWindow.Info.ds4winSettings.m_CustomLed;
                }
                else
                {
                    color = LightbarWindow.Info.ds4winSettings.m_Led;
                }
                return $"#FF{color.red.ToString("X2")}{color.green.ToString("X2")}{color.blue.ToString("X2")}";
            }
        }

        public event EventHandler? LightColorChanged;

        public DS4Color CustomLightColor => LightbarWindow.Info.ds4winSettings.m_CustomLed;

        public string BatteryState
        {
            get
            {
                string temp = $"{device.Battery}%{(device.Charging ? "+" : "")}";
                return temp;
            }
        }
        public event EventHandler? BatteryStateChanged;

        /// <summary>電池表示を読み直す間隔（ミリ秒。ユーザー指定: 60 秒）。</summary>
        public const int BatteryRefreshIntervalMs = 60000;
        private ITimer? batteryRefreshTimer;
        private string lastBatteryState;

        private void RaiseBatteryStateChanged(EventArgs e)
        {
            lastBatteryState = BatteryState;
            BatteryStateChanged?.Invoke(this, e);
            // このクラスは INotifyPropertyChanged を実装しているため、WPF のバインディングは「<プロパティ名>Changed」イベントを使わず、
            // PropertyChanged だけを見る。以前は PropertyChanged を出していなかったため、一覧の電池表示が、行を作った時点の値のまま更新されなかった
            // （S9-5 の確認で判明。60 秒ごとの読み直しも、これがないと画面に届かない）
            OnPropertyChanged(nameof(BatteryState));
        }

        /// <summary>電池表示を読み直し、前回の通知から文字列が変わっていたときだけ <see cref="BatteryStateChanged"/> を出す。</summary>
        public void RefreshBatteryState()
        {
            if (device == null) return;

            string current = BatteryState;
            if (current == lastBatteryState) return;

            RaiseBatteryStateChanged(EventArgs.Empty);
        }

        private void StopBatteryRefresh()
        {
            ITimer? timer = Interlocked.Exchange(ref batteryRefreshTimer, null);
            timer?.Dispose();
        }

        public int SelectedIndex
        {
            get => selectedIndex;
            set
            {
                if (selectedIndex == value) return;
                selectedIndex = value;

                OnPropertyChanged(nameof(SelectedIndex));

                if (!suppressSelectedIndexChanged)
                {
                    SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        public event EventHandler? SelectedIndexChanged;

        public string StatusSource
        {
            get
            {
                string imgName = ImageName(device.ConnectionType == ConnectionType.USB ? "UsbImg" : "BtImg");
                string source = $"{ResourcePaths.ResourcesPrefix}/{imgName}";
                return source;
            }
        }

        public string ExclusiveSource
        {
            get
            {
                string imgName = ImageName("CancelImg");
                string source = $"{ResourcePaths.ResourcesPrefix}/{imgName}";
                switch (device.ExclusiveStatus)
                {
                    case DeviceExclusiveStatus.Exclusive:
                        imgName = ImageName("CheckedImg");
                        source = $"{ResourcePaths.ResourcesPrefix}/{imgName}";
                        break;
                    case DeviceExclusiveStatus.HidHideAffected:
                    case DeviceExclusiveStatus.HidGuardAffected:
                        imgName = ImageName("KeyImageImg");
                        source = $"{ResourcePaths.ResourcesPrefix}/{imgName}";
                        break;
                    default:
                        break;
                }

                return source;
            }
        }

        public bool LinkedProfile
        {
            get
            {
                return profileSettingsService.GetLinkedProfileCheck(devIndex);
            }
            set
            {
                bool temp = profileSettingsService.GetLinkedProfileCheck(devIndex);
                if (temp == value) return;
                profileSettingsService.SetLinkedProfileCheck(devIndex, value);
                SaveLinked(value);
            }
        }
        public event EventHandler? LinkedProfileChanged;

        public void RaiseLinkedProfileChanged()
        {
            LinkedProfileChanged?.Invoke(this, EventArgs.Empty);
        }

        public string SelectedProfileName
        {
            get => profileRepo.SelectedProfile[devIndex];
            set
            {
                if (profileRepo.SelectedProfile[devIndex] == value) return;
                profileRepo.SelectedProfile[devIndex] = value;
                profileRepo.ProfilePath[devIndex] = value;
                profileRepo.OlderProfilePath[devIndex] = value;
                SelectedProfileChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler? SelectedProfileChanged;

        public ProfileEntity? LinkedProfileName
        {
            get
            {
                string linkedProfileUI = profileRepo.LinkedProfileUI[devIndex];
                if (!string.IsNullOrEmpty(linkedProfileUI))
                {
                    return profileListHolder.ProfileListCol.SingleOrDefault(x => x.Name == linkedProfileUI);
                }
                return null;
            }
            set
            {
                string newValue = value?.Name ?? string.Empty;
                if (profileRepo.LinkedProfileUI[devIndex] == newValue) return;

                profileRepo.LinkedProfileUI[devIndex] = newValue;

                if (profileSettingsService.GetLinkedProfileCheck(devIndex) && device?.HasValidSerial == true)
                {
                    if (!string.IsNullOrEmpty(newValue))
                    {
                        profileRepo.ChangeLinkedProfile(device.MacAddress, newValue);
                        profileRepo.SaveLinkedProfiles();
                    }
                }

                LinkedProfileNameChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler? LinkedProfileNameChanged;

        public bool LinkEnabled
        {
            get => profileSettingsService.GetLinkedProfileCheck(devIndex);
            set
            {
                bool temp = profileSettingsService.GetLinkedProfileCheck(devIndex);
                if (temp == value) return;

                AppLogger.LogDebug($"LinkEnabled setter: device={devIndex}, oldValue={temp}, newValue={value}");
                profileSettingsService.SetLinkedProfileCheck(devIndex, value);

                if (value)
                {
                    string linkedValue = profileRepo.LinkedProfileUI[devIndex];
                    if (string.IsNullOrEmpty(linkedValue))
                    {
                        linkedValue = profileRepo.SelectedProfile[devIndex];
                        AppLogger.LogDebug($"LinkEnabled ON: LinkedProfileUI is empty, using SelectedProfile '{linkedValue}' for device {devIndex}");
                        profileRepo.LinkedProfileUI[devIndex] = linkedValue;
                    }

                    AppLogger.LogDebug($"LinkEnabled ON: Registering profile '{linkedValue}' for device {devIndex}");

                    if (!string.IsNullOrEmpty(linkedValue) && device?.HasValidSerial == true)
                    {
                        profileRepo.ChangeLinkedProfile(device.MacAddress, linkedValue);
                        profileRepo.SaveLinkedProfiles();
                        AppLogger.LogDebug($"LinkEnabled ON: Saved to LinkedProfiles.xml for MAC={device.MacAddress}");
                    }
                    else
                    {
                        AppLogger.LogWarn($"LinkEnabled ON: Could not save - linkedValue='{linkedValue}', validSerial={device?.HasValidSerial}");
                    }

                    LinkedProfileNameChanged?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    AppLogger.LogDebug($"LinkEnabled OFF: Removing from LinkedProfiles.xml for device {devIndex}");
                    if (device?.HasValidSerial == true)
                    {
                        profileRepo.RemoveLinkedProfile(device.MacAddress);
                        profileRepo.SaveLinkedProfiles();
                        AppLogger.LogDebug($"LinkEnabled OFF: Removed from LinkedProfiles.xml for MAC={device.MacAddress}");
                    }

                    AppLogger.LogDebug($"LinkEnabled OFF: Keeping LinkedProfileUI value '{profileRepo.LinkedProfileUI[devIndex]}' for device {devIndex}");
                }

                LinkedProfileChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public int DevIndex { get => devIndex; }
        public int DisplayDevIndex { get => devIndex + 1; }

        public string TooltipIDText
        {
            get
            {
                string temp = string.Format(Properties.Resources.InputDelay, device.Latency);
                return temp;
            }
        }

        public event EventHandler? TooltipIDTextChanged;

        private bool useCustomColor;
        public bool UseCustomColor { get => useCustomColor; set => useCustomColor = value; }


        public string IdText
        {
            get => $"{device.DisplayName} ({device.MacAddress})";
        }
        public event EventHandler? IdTextChanged;

        public string IsExclusiveText
        {
            get
            {
                if (device.ExclusiveStatus == DeviceExclusiveStatus.Exclusive)
                {
                    return Translations.Strings.ExclusiveAccess;
                }
                else if (device.ExclusiveStatus == DeviceExclusiveStatus.HidHideAffected)
                {
                    return Translations.Strings.HidHideAccess;
                }
                else if (device.ExclusiveStatus == DeviceExclusiveStatus.HidGuardAffected)
                {
                    return Translations.Strings.HidGuardianAccess;
                }

                return Translations.Strings.SharedAccess;
            }
        }

        public delegate void CustomColorHandler(CompositeDeviceModel sender);
        public event CustomColorHandler? RequestColorPicker;

        // 画像の資源名の引き当て（テーマで差し替わる。null のときは名前を空として扱う）
        private readonly DS4Windows.UI.Abstractions.IImageResourceLookup? imageResources;

        private string ImageName(string key) => imageResources?.Find(key) ?? string.Empty;

        public CompositeDeviceModel(IControllerSlotView device, int devIndex, string profile,
            ProfileList collection, IProfileSettingsService profileSettingsService,
            IProfileRepository profileRepo, DS4Windows.UI.Abstractions.IImageResourceLookup? imageResources = null)
        {
            this.imageResources = imageResources;
            // 必須引数（null は拒否する）
            ArgumentNullException.ThrowIfNull(profileSettingsService);
            this.profileSettingsService = profileSettingsService;
            ArgumentNullException.ThrowIfNull(profileRepo);
            this.profileRepo = profileRepo;
            this.device = device;
            device.BatteryChanged += (sender, e) => RaiseBatteryStateChanged(e);
            device.ChargingChanged += (sender, e) => RaiseBatteryStateChanged(e);
            // S9-5 の確認で判明: 変化通知だけでは、表示が古い値のまま止まることがあった（実機: 表示 50%、実際 37%）。
            // 通知に頼らず、一定間隔で読み直す（文字列が変わったときだけ画面へ通知する）。コントローラーが外れたら止める
            lastBatteryState = BatteryState;
            batteryRefreshTimer = TimeProvider.System.CreateTimer(_ => RefreshBatteryState(), null, TimeSpan.FromMilliseconds(BatteryRefreshIntervalMs), TimeSpan.FromMilliseconds(BatteryRefreshIntervalMs));
            device.Removal += (sender, e) => StopBatteryRefresh();
            device.MacAddressChanged += (sender, e) => IdTextChanged?.Invoke(this, e);
            this.devIndex = devIndex;
            this.selectedProfile = profile;
            profileListHolder = collection;
            if (!string.IsNullOrEmpty(selectedProfile))
            {
                this.selectedEntity = profileListHolder.ProfileListCol.SingleOrDefault(x => x.Name == selectedProfile);
            }

            if (this.selectedEntity != null)
            {
                selectedIndex = profileListHolder.ProfileListCol.IndexOf(this.selectedEntity);
                HookEvents(true);
            }

            useCustomColor = LightbarWindow.Info.ds4winSettings.useCustomLed;
        }

        public void HookEvents(bool state)
        {
            if (selectedEntity == null) return;
            if (state)
            {
                selectedEntity.ProfileDeleted += SelectedEntity_ProfileDeleted;
            }
            else
            {
                selectedEntity.ProfileDeleted -= SelectedEntity_ProfileDeleted;
            }
        }

        private void SelectedEntity_ProfileDeleted(object? sender, EventArgs e)
        {
            HookEvents(false);
            ProfileEntity? entity = profileListHolder.ProfileListCol.FirstOrDefault();
            if (entity != null)
            {
                SelectedIndex = profileListHolder.ProfileListCol.IndexOf(entity);
            }
        }

        public void RequestUpdatedTooltipID()
        {
            TooltipIDTextChanged?.Invoke(this, EventArgs.Empty);
        }

        private void SaveLinked(bool status)
        {
            if (device != null && device.Synced)
            {
                if (status)
                {
                    if (device.HasValidSerial)
                    {
                        profileRepo.ChangeLinkedProfile(device.MacAddress, profileRepo.ProfilePath[devIndex]);
                    }
                }
                else
                {
                    profileRepo.RemoveLinkedProfile(device.MacAddress);
                    profileRepo.ProfilePath[devIndex] = profileRepo.OlderProfilePath[devIndex];
                }

                profileRepo.SaveLinkedProfiles();
            }
        }

        /// <summary>ライトバーの色を、プロファイルの色にする（メニューの「Use Profile Color」）。</summary>
        public void UseProfileLightColor()
        {
            useCustomColor = false;
            LightbarWindow.Info.ds4winSettings.useCustomLed = false;
            RaiseLightColorChanged();
        }

        /// <summary>ライトバーの色を、カスタム色にし、色の選択を求める（メニューの「Use Custom Color」）。</summary>
        public void UseCustomLightColor()
        {
            useCustomColor = true;
            LightbarWindow.Info.ds4winSettings.useCustomLed = true;
            RaiseLightColorChanged();
            RequestColorPicker?.Invoke(this);
        }
        public void UpdateCustomLightColor(DS4Color color)
        {
            LightbarWindow.Info.ds4winSettings.m_CustomLed = color;
            RaiseLightColorChanged();
        }

        /// <summary>
        /// CLI の ChangeLedColor 用。カスタム色を書き換えるだけでなく、ライトバーのモードをカスタム色にして、すぐ見た目に出す
        /// （モードがカスタム色でないと、書き換えた色が使われない）。メモリ上の設定だけを変え、プロファイルのファイルには書かない。
        /// </summary>
        public void ForceCustomLightColor(DS4Color color)
        {
            var settings = LightbarWindow.Info.ds4winSettings;
            settings.useCustomLed = true;
            settings.ledAsBattery = false;
            settings.rainbow = 0;
            UpdateCustomLightColor(color);
        }

        /// <summary>色の列（<see cref="LightColor"/> にバインド）も更新させるため、イベントと変更通知の両方を出す。</summary>
        public void RaiseLightColorChanged()
        {
            LightColorChanged?.Invoke(this, EventArgs.Empty);
            OnPropertyChanged(nameof(LightColor));
        }

        public void RequestDisconnect()
        {
            if (device.Synced && !device.Charging)
            {
                if (device.ConnectionType == ConnectionType.BT)
                {
                    device.QueueEvent(() =>
                    {
                        device.DisconnectBluetooth();
                    });
                }
                else if (device.ConnectionType == ConnectionType.SONYWA)
                {
                    device.DisconnectDongle();
                }
            }
        }
    }
}