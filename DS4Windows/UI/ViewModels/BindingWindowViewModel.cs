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

using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Processing.Abstractions;
using CommunityToolkit.Mvvm.Input;
using DS4Windows.UI.ViewModels.Util;

namespace DS4Windows.UI.ViewModels
{
    public class BindingWindowViewModel
    {
        private int deviceNum;
        private OutContType outputType;
        private DS4ControlSettings settings;
        private OutBinding currentOutBind;
        private bool rumbleActive;

        /// <summary>出力ピッカーで編集している割り当ての、出力の種類（図とボタン名の表記を決める）。</summary>
        public OutContType OutputType
        {
            get => outputType;
        }
        public int DeviceNum { get => deviceNum; }
        public OutBinding CurrentOutBind { get => currentOutBind; }
        public OutBinding? ActionBinding { get; set; }

        public bool RumbleActive { get => rumbleActive; set => rumbleActive = value; }
        public DS4ControlSettings Settings { get => settings; }

        // 設定の読み書き先（DeviceNum＝編集スロット）とは別に、ランブルテスト・ライトバーの
        // プレビューで実際に使うコントローラーのスロット番号を持つ。-1 は「実機なし」
        // （プロファイル一覧から開いた場合）
        // Global.RefreshActionAlias の置換先（必須引数）
        private readonly IProfileSettingsService profileSettingsService;
        // スロット数の上限は、静的な ControlService.CURRENT_DS4_CONTROLLER_LIMIT ではなく、注入された環境サービスから読む
        private readonly IEnvironmentService environmentService;
        private readonly int targetDevice;
        public int TargetDevice { get => targetDevice; }

        /// <summary>
        /// 実機（targetDevice）が指定され、かつ有効なコントローラースロットの範囲内であるか。
        /// 実機に触れる処理はこの判定を使う（編集スロット DeviceNum ではない）。
        /// </summary>
        public bool HasTargetDevice =>
            targetDevice >= 0 && targetDevice < environmentService.ControllerSlotLimit;

        // ライトバーの強制表示の状態
        private readonly DS4Windows.Core.Model.LightbarOverrides _lightbar;

        public BindingWindowViewModel(DS4Windows.Core.Model.LightbarOverrides lightbar, int deviceNum, DS4ControlSettings settings,
            IProfileSettingsService profileSettingsService, IOutputSlotService outputSlotService,
            IEnvironmentService environmentService, int targetDevice = -1)
        {
            _lightbar = lightbar ?? throw new System.ArgumentNullException(nameof(lightbar));
            ArgumentNullException.ThrowIfNull(environmentService);
            this.environmentService = environmentService;
            ArgumentNullException.ThrowIfNull(profileSettingsService);
            this.profileSettingsService = profileSettingsService;
            if (outputSlotService == null) throw new ArgumentNullException(nameof(outputSlotService));
            this.deviceNum = deviceNum;
            this.targetDevice = targetDevice;
            outputType = outputSlotService.OutDevTypeTemp[deviceNum];
            this.settings = settings;
            currentOutBind = new OutBinding();
            PopulateCurrentBinds();
        }

        /// <summary>編集中のボタン（DS4 の入力）。画面の見出しに使う。</summary>
        public DS4Windows.Core.Model.DS4Controls Control => settings.control;

        /// <summary>入力に対する、既定（割り当てなし）の出力のボタン。</summary>
        public DS4Windows.Core.Model.X360Controls DefaultMappingFor(DS4Windows.Core.Model.DS4Controls input)
            => profileSettingsService.GetDefaultButtonMapping()[(int)input];

        /// <summary>振動のテストで、2 つのモーターを左右反転するか（編集中の設定の値）。</summary>
        public bool InverseRumbleMotors => profileSettingsService.ForSlot(deviceNum).Output.InverseRumbleMotors;
        public void PopulateCurrentBinds()
        {
            DS4ControlSettings setting = settings;
            bool sc = setting.keyType.HasFlag(DS4KeyType.ScanCode);
            bool toggle = setting.keyType.HasFlag(DS4KeyType.Toggle);
            currentOutBind.input = setting.control;
            if (setting.actionType != DS4ControlSettings.ActionType.Default)
            {
                switch(setting.actionType)
                {
                    case DS4ControlSettings.ActionType.Button:
                        currentOutBind.outputType = OutBinding.OutType.Button;
                        currentOutBind.control = (X360Controls)setting.action.actionBtn;
                        break;
                    case DS4ControlSettings.ActionType.Default:
                        currentOutBind.outputType = OutBinding.OutType.Default;
                        break;
                    case DS4ControlSettings.ActionType.Key:
                        currentOutBind.outputType = OutBinding.OutType.Key;
                        currentOutBind.outkey = setting.action.actionKey;
                        currentOutBind.hasScanCode = sc;
                        currentOutBind.toggle = toggle;
                        break;
                    case DS4ControlSettings.ActionType.Macro:
                        currentOutBind.outputType = OutBinding.OutType.Macro;
                        currentOutBind.macro = (int[])setting.action.actionMacro;
                        currentOutBind.macroType = settings.keyType;
                        currentOutBind.hasScanCode = sc;
                        break;
                }
            }
            else
            {
                currentOutBind.outputType = OutBinding.OutType.Default;
            }

            if (!string.IsNullOrEmpty(setting.extras))
            {
                currentOutBind.ParseExtras(setting.extras);
            }

        }

        public void PrepareSaveMacro(OutBinding bind)
        {
            DS4ControlSettings setting = settings;

            bind.outputType = OutBinding.OutType.Macro;
            bind.macro = (int[])setting.action.actionMacro;
            bind.macroType = settings.keyType;
            // 記録画面（RecordBox）で設定した Scan Code を、WriteBind が書く hasScanCode へ渡す。
            // 渡さないと、記録画面でチェックしても、保存時に落ちていた（WriteBind の Macro は、hasScanCode［選択ダイアログのチェックボックス］から書く）
            bind.hasScanCode = settings.keyType.HasFlag(DS4KeyType.ScanCode);
        }

        public void WriteBinds()
        {
            currentOutBind.WriteBind(settings, profileSettingsService);
        }

        // ライトバー強制色のプレビューは、編集スロット（DeviceNum）ではなく実機（targetDevice）に対して行う
        public void StartForcedColor(DS4Color color)
        {
            if (HasTargetDevice)
            {
                _lightbar.forcedColor[targetDevice] = color;
                _lightbar.forcedFlash[targetDevice] = 0;
                _lightbar.forcelight[targetDevice] = true;
            }
        }

        public void EndForcedColor()
        {
            if (HasTargetDevice)
            {
                _lightbar.forcedColor[targetDevice] = new DS4Color(0, 0, 0);
                _lightbar.forcedFlash[targetDevice] = 0;
                _lightbar.forcelight[targetDevice] = false;
            }
        }

        public void UpdateForcedColor(DS4Color color)
        {
            if (HasTargetDevice)
            {
                _lightbar.forcedColor[targetDevice] = color;
                _lightbar.forcedFlash[targetDevice] = 0;
                _lightbar.forcelight[targetDevice] = true;
            }
        }

        // ---- 出力ピッカーの選択（ボタンの Command）。選んだら、ピッカーを閉じる ----

        /// <summary>選択が終わり、ピッカーを閉じてよいときに発生する。</summary>
        public event EventHandler? CloseRequested;

        /// <summary>
        /// ユーザーが「Default」を選んだ（Default ボタン、または既定の割り当てと同じ Pad のボタン）か。
        /// × で何も選ばずに閉じたときは false。割り当てエディターが、種別の一覧を Default へ切り替えるかの判断に使う。
        /// </summary>
        public bool DefaultPicked { get; private set; }

        private RelayCommand<string>? _selectKeyCommand;
        private RelayCommand<string>? _selectButtonCommand;
        private RelayCommand? _pickDefaultCommand;
        private RelayCommand? _pickUnboundCommand;

        /// <summary>キーのボタン。<c>CommandParameter</c> は仮想キーコード（<c>0x44</c> の形）。</summary>
        public RelayCommand<string> SelectKeyCommand => _selectKeyCommand ??= CommandGuard.CreateSync<string>(SelectKey, name: nameof(SelectKeyCommand));

        /// <summary>仮想パッド（マウスを含む）のボタン。<c>CommandParameter</c> は <c>X360Controls</c> の名前。</summary>
        public RelayCommand<string> SelectButtonCommand => _selectButtonCommand ??= CommandGuard.CreateSync<string>(SelectButton, name: nameof(SelectButtonCommand));

        public RelayCommand PickDefaultCommand => _pickDefaultCommand ??= CommandGuard.CreateSync(PickDefault, name: nameof(PickDefaultCommand));

        public RelayCommand PickUnboundCommand => _pickUnboundCommand ??= CommandGuard.CreateSync(PickUnbound, name: nameof(PickUnboundCommand));

        /// <summary>仮想キーコードの文字列（<c>0x44</c>）を、整数にする。</summary>
        public static int ParseKeyCode(string text)
            => Convert.ToInt32(text.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? text.Substring(2) : text, 16);

        private void SelectKey(string? parameter)
        {
            OutBinding binding = ActionBinding!;   // 出力ピッカーを開く前に、必ず設定される
            binding.outputType = OutBinding.OutType.Key;
            if (!string.IsNullOrEmpty(parameter))
            {
                binding.outkey = ParseKeyCode(parameter);
            }

            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private void SelectButton(string? parameter)
        {
            OutBinding binding = ActionBinding!;   // 出力ピッカーを開く前に、必ず設定される
            if (!string.IsNullOrEmpty(parameter))
            {
                X360Controls selected = (X360Controls)Enum.Parse(typeof(X360Controls), parameter);
                if (DefaultMappingFor(binding.input) == selected)
                {
                    DefaultPicked = true;
                    binding.outputType = OutBinding.OutType.Default;
                }
                else
                {
                    binding.outputType = OutBinding.OutType.Button;
                    binding.control = selected;
                }
            }

            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private void PickDefault()
        {
            OutBinding binding = ActionBinding!;   // 出力ピッカーを開く前に、必ず設定される
            DefaultPicked = true;
            binding.outputType = OutBinding.OutType.Default;
            binding.control = DefaultMappingFor(binding.input);
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private void PickUnbound()
        {
            OutBinding binding = ActionBinding!;   // 出力ピッカーを開く前に、必ず設定される
            binding.outputType = OutBinding.OutType.Button;
            binding.control = X360Controls.Unbound;
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
