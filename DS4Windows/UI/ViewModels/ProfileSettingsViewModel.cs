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
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Core.Utilities;
using DS4Windows.Input;
using DS4Windows.Input.Devices;
using DS4Windows.Processing;
using DS4Windows.Processing.Abstractions;
using DS4Windows.UI.ViewModels.Util;

namespace DS4Windows.UI.ViewModels
{
    public class ProfileSettingsViewModel : ObservableObject
    {
        private int device;
        public int Device { get => device; }

        /// <summary>「ジャイロ」タブの ViewModel。</summary>
        public GyroSettingsViewModel Gyro { get; }

        /// <summary>「タッチパッド」タブの ViewModel。</summary>
        public TouchpadSettingsViewModel Touchpad { get; }

        /// <summary>「L2 &amp; R2」（トリガー）の ViewModel。</summary>
        public TriggerSettingsViewModel Triggers { get; }

        /// <summary>「LS」「RS」（スティック）の ViewModel。</summary>
        public StickSettingsViewModel Sticks { get; }

        /// <summary>「ライトバー」タブの ViewModel。</summary>
        public LightbarSettingsViewModel Lightbar { get; }

        /// <summary>「その他」タブの仮想コントローラー・振動の設定。</summary>
        public OutputSettingsViewModel Output { get; }

        /// <summary>「その他」タブの一般項目（入力の表示、マウス移動、プログラムの同時起動、無操作での切断、ポーリング間隔）。</summary>
        public GeneralSettingsViewModel General { get; }
        private readonly IProfileSettingsService profileSettings;
        private readonly IProfileLoadTarget loadTarget;   // Phase13-Step6 6-e: 旧 ControlService controlService（ジャイロ系 setter と空プロファイルの読込に渡すだけだった）
        private readonly IEnvironmentService environmentService;
        private readonly IPathService pathService;
        private readonly IOutputSlotService outputSlotService;
        private readonly IProfileRepository profileRepo;
        private readonly IProfilePresetLoader presetLoader;

        private int funcDevNum;
        public int FuncDevNum { get => funcDevNum; }

        // Phase6-Step7b: 設定の読み書き先（Device＝編集スロット）とは別に、ランブルテスト・ライトバーの
        // プレビュー・校正などで実際に使うコントローラーのスロット番号を持つ。-1 は「実機なし」
        // （プロファイル一覧から開いた場合）。詳細は docs-forDIMG/MadeByAgent/Phase6-Step7b-Plan.md §2.1
        private readonly int targetDevice;
        public int TargetDevice { get => targetDevice; }

        // Phase11 K-W6-10: DebouncingMs（デバウンス時間）のプロパティとイベントは機能廃止で削除した

        // DS3 の傾きの擬似はジャイロのタブにある。保存時の補正（DS4 への補正）と、画面の再起動の確認が使うため、窓口をここにも残す
        public bool HasUseDs3PitchRollSimChanged => Gyro.HasUseDs3PitchRollSimChanged;

        public bool UseDs3PitchRollSim
        {
            get => Gyro.UseDs3PitchRollSim;
            set => Gyro.UseDs3PitchRollSim = value;
        }

        /// <summary>サービスの再起動を求めたあとに、<see cref="HasUseDs3PitchRollSimChanged"/> の印を戻す。同じ変更のために、再起動をくり返し求めないようにするため。</summary>
        public void AcknowledgeUseDs3PitchRollSimChange() => Gyro.AcknowledgeUseDs3PitchRollSimChange();













        /// <summary>
        /// Phase11 W4（Issue #4 の 1）: DS4 の出力でしか成り立たない設定が ON なのに、出力の種類（Emulated Controller）が DS4 でないとき、
        /// DS4 へ補正する。保存・適用の両方が通る <c>ProfileEditor.ExecuteSaveOrApply</c> から、保存する前に呼ぶ。
        /// 2026-09-14 の保存・適用の統合（<c>9da60b85</c>）で失われた、旧 <c>ProfileEditor</c> の補正の復元（旧コードの動きに合わせる）:
        /// <list type="bullet">
        /// <item>(a) <see cref="UseDs3PitchRollSim"/> が ON なら DS4（旧 <c>SaveBtn_Click</c>。GUI ログなし）</item>
        /// <item>(c) <see cref="GeneralSettingsViewModel.EnableOutputDataToDS4"/> が ON で、(a) のあとも DS4 でなければ DS4（旧 <c>SetLateProperties</c> の
        /// Phase5-Step14 (c)-2。呼び出し側が GUI ログを出す）</item>
        /// </list>
        /// OFF のときは何もしない（DS4 から X360 へ戻すことはしない）。
        /// 「DS4 でない」の判定は、保存される値（<see cref="OutputSettingsViewModel.ControllerTypeIndex"/>）と画面の選択（<see cref="OutputSettingsViewModel.TempControllerIndex"/>）の
        /// どちらかが DS4 でなければ、とする（ずれていても、保存される値を DS4 にそろえるため）。
        /// </summary>
        /// <returns>DS4 へ変えた理由。変えなかった場合は <see cref="Ds4OutputCorrectionReason.None"/></returns>
        public Ds4OutputCorrectionReason CorrectOutputTypeToDs4()
        {
            bool notDs4 = Output.ControllerTypeIndex != 1 || Output.TempControllerIndex != 1;
            if (!notDs4)
            {
                return Ds4OutputCorrectionReason.None;
            }

            Ds4OutputCorrectionReason reason;
            if (UseDs3PitchRollSim)
            {
                reason = Ds4OutputCorrectionReason.Ds3PitchRollSim;
            }
            // Phase11 W6c（K-W6-12。ユーザー指示 2026-11-03）: (c)［EnableOutputDataToDS4 が ON なら DS4 へ強制］は廃止した。
            // この設定は「物理コントローラーへ出力データを送るか」であり、仮想パッドの種類（Emulated Controller）とは無関係
            else
            {
                return Ds4OutputCorrectionReason.None;
            }

            Output.ControllerTypeIndex = 1;   // 保存される値（profileSettings.OutContType）
            Output.TempControllerIndex = 1;   // 画面の選択
            return reason;
        }

        // ---- Phase6-Step8-4: ProfileEditor のバックエンド操作（保存・ブランク読み込み・キャッシュ更新）----
        // 編集スロット（device）に対する操作だけを行い、コントローラーのスロットへの再適用はしない
        // （再適用は ProfileSaved を受けた MainWindow.SyncProfileListAndControllers の責務。Step7b 決定8）。

        /// <summary>編集スロットの内容を、プロファイル <paramref name="profileName"/> として保存する。保存の成否を返す。</summary>
        public bool SaveProfile(string profileName)
        {
            return profileRepo.SaveProfile(device, profileName);
        }

        /// <summary>
        /// 編集スロットへ空のプロファイルを読み込む（新規作成でプリセット選択をキャンセルしたとき）。
        /// Phase13-Step6 6-e: 読込先の窓口（<see cref="IProfileLoadTarget"/>）は、呼び出し元から渡さず、この ViewModel が持つものを使う。
        /// </summary>
        public void LoadBlankProfile()
        {
            presetLoader.LoadBlank(device, false, loadTarget, false);
        }

        /// <summary>編集スロットのカスタムフラグのキャッシュを更新する（ボタン設定画面を閉じた後など）。</summary>
        public void RefreshCustomsFlags()
        {
            profileRepo.CacheProfileCustomsFlags(device);
        }

        /// <summary>編集スロットの拡張プロファイル情報（スペシャルアクション参照など）のキャッシュを更新する。</summary>
        public void RefreshExtraProfileInfo()
        {
            profileRepo.CacheExtraProfileInfo(device);
        }

        public OutContType ContType
        {
            // Issue7是正（Phase5-Step14-Issue7-Fix-Plan.md タスク2）: 参照先を
            // outputSlotService.GetOutputDeviceType(device) から
            // profileSettings.OutContType（Global.OutContTypeと同一実体）へ修正。
            // （GetOutputDeviceType は Phase6-Step5-2 で削除済み）
            get => profileSettings.OutContType[device];
        }

        public double SXDeadZone
        {
            get => profileSettings.SXDeadzone[device];
            set
            {
                double temp = profileSettings.SXDeadzone[device];
                if (temp == value) return;
                profileSettings.SXDeadzone[device] = value;
                OnPropertyChanged(nameof(SXDeadZone));
            }
        }
        public double SZDeadZone
        {
            get => profileSettings.SZDeadzone[device];
            set
            {
                double temp = profileSettings.SZDeadzone[device];
                if (temp == value) return;
                profileSettings.SZDeadzone[device] = value;
                OnPropertyChanged(nameof(SZDeadZone));
            }
        }
        public double SXMaxZone
        {
            get => profileSettings.SXMaxzone[device];
            set => profileSettings.SXMaxzone[device] = value;
        }

        public double SZMaxZone
        {
            get => profileSettings.SZMaxzone[device];
            set => profileSettings.SZMaxzone[device] = value;
        }

        public double SXAntiDeadZone
        {
            get => profileSettings.SXAntiDeadzone[device];
            set => profileSettings.SXAntiDeadzone[device] = value;
        }

        public double SZAntiDeadZone
        {
            get => profileSettings.SZAntiDeadzone[device];
            set => profileSettings.SZAntiDeadzone[device] = value;
        }

        public double SXSens
        {
            get => profileSettings.SXSens[device];
            set => profileSettings.SXSens[device] = value;
        }

        public double SZSens
        {
            get => profileSettings.SZSens[device];
            set => profileSettings.SZSens[device] = value;
        }

        public int SXOutputCurveIndex
        {
            get => profileSettings.GetSxOutCurveMode(device);
            set
            {
                profileSettings.SetSxOutCurveMode(device, value);
                OnPropertyChanged(nameof(SXCustomCurveSelected));
            }
        }

        public int SZOutputCurveIndex
        {
            get => profileSettings.GetSzOutCurveMode(device);
            set
            {
                profileSettings.SetSzOutCurveMode(device, value);
                OnPropertyChanged(nameof(SZCustomCurveSelected));
            }
        }

        public bool SXCustomCurveSelected
        {
            get => profileSettings.GetSxOutCurveMode(device) == 6;
        }
        public bool SZCustomCurveSelected
        {
            get => profileSettings.GetSzOutCurveMode(device) == 6;
        }
        public string SXCustomCurve
        {
            get => profileSettings.SxOutBezierCurveObj[device].CustomDefinition;
            set => profileSettings.SxOutBezierCurveObj[device].InitBezierCurve(value, BezierCurve.AxisType.SA, true);
        }

        public string SZCustomCurve
        {
            get => profileSettings.SzOutBezierCurveObj[device].CustomDefinition;
            set => profileSettings.SzOutBezierCurveObj[device].InitBezierCurve(value, BezierCurve.AxisType.SA, true);
        }

        private PresetMenuHelper presetMenuUtil;
        public PresetMenuHelper PresetMenuUtil
        {
            get => presetMenuUtil;
        }




        public bool UsingMinViGEm173333
        {
            get => environmentService.IsUsingMinViGEm117333();
        }



        // Phase6-Step9-4: 依存はコンストラクタで必須引数として受け取る（Pure DI。決定1＝S9-A）。
        // 以前は ?? AppHost.GetService／Global.*Instance／Program.rootHub のフォールバックを持っていた。
        // loadTarget は、ジャイロ系 setter が IProfileSettingsService へ渡す引数と、空プロファイルの読込にだけ使われ、従来も
        // Program.rootHub が null の環境（テスト等）では null だったため、null を許容する（Phase13-Step6 6-e: 旧 ControlService controlService。
        // 生成側の ViewModelFactory が IProfileLoadTarget を直接渡す）。
        // targetDevice は、編集スロット（device）と実機を分けて渡す引数（Phase6-Step7b）。変更しない。
        public ProfileSettingsViewModel(DS4Windows.Core.Model.LightbarOverrides lightbar, int device, IProfileSettingsService profileSettings,
            IProfileLoadTarget loadTarget,
            IOutputSlotService outputSlotService,
            IProfileRepository profileRepo,
            IProfilePresetLoader presetLoader,
            IEnvironmentService environmentService,
            IPathService pathService,
            int targetDevice = -1)
        {
            if (lightbar == null) throw new System.ArgumentNullException(nameof(lightbar));
            this.device = device;
            this.targetDevice = targetDevice;
            this.profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            this.loadTarget = loadTarget;
            this.outputSlotService = outputSlotService ?? throw new ArgumentNullException(nameof(outputSlotService));
            this.profileRepo = profileRepo ?? throw new ArgumentNullException(nameof(profileRepo));
            this.presetLoader = presetLoader ?? throw new ArgumentNullException(nameof(presetLoader));
            this.environmentService = environmentService ?? throw new ArgumentNullException(nameof(environmentService));
            this.pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
            // Phase6-Step7b: 実機を使う機能（ランブルテスト、校正、入力ハイライト等）の対象。
            // 実機なし（-1）の場合は、プロファイル一覧から開いたときの従来動作（コントローラー0）を維持する
            funcDevNum = HasTargetDevice ? targetDevice : 0;

            presetMenuUtil = new PresetMenuHelper(device, this.profileSettings);
            General = new GeneralSettingsViewModel(this.profileSettings.ForSlot(device).General);
            Output = new OutputSettingsViewModel(this.profileSettings.ForSlot(device).Output, this.outputSlotService, device);
            Lightbar = new LightbarSettingsViewModel(this.profileSettings.ForSlot(device).Lightbar, lightbar, environmentService, targetDevice);
            Sticks = new StickSettingsViewModel(this.profileSettings.ForSlot(device).Sticks);
            // スティックの出力モードを変えたら、保存用のプロファイルの印を更新する
            Sticks.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(StickSettingsViewModel.LSOutputIndex) || e.PropertyName == nameof(StickSettingsViewModel.RSOutputIndex)) CalcProfileFlags(s, EventArgs.Empty);
            };
            Triggers = new TriggerSettingsViewModel(this.profileSettings.ForSlot(device).Triggers);
            Touchpad = new TouchpadSettingsViewModel(this.profileSettings.ForSlot(device).Touchpad, environmentService);
            Gyro = new GyroSettingsViewModel(this.profileSettings.ForSlot(device).Gyro, loadTarget, environmentService);
            // ジャイロの出力モードを変えたら、保存用のプロファイルの印を更新する
            Gyro.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(GyroSettingsViewModel.GyroOutModeIndex)) CalcProfileFlags(s, EventArgs.Empty);
            };

            SetupEvents();
        }

        private void CalcProfileFlags(object sender, EventArgs e)
        {
            profileRepo.CacheProfileCustomsFlags(device);
        }

        private void SetupEvents()
        {
            // Phase6-Step7b（決定7＝H1）: 編集操作のたびに実機へ即時に効かせていた 6 件のフックを削除した
            // （GyroOutModeIndexChanging／TouchpadOutputIndexChanging の touchPad.Reset()、TouchpadOutputIndexChanged の
            // PostSetup()、TouchMouseStickTrackballFrictionChanged の ResetTouchStickAccel、LS／RSDeltaAccelEnabledChanged の
            // deltaAccelProcessors のリセット）。プロファイル編集画面の編集スロットが常に作業スロット（8）になり、
            // device < CURRENT_DS4_CONTROLLER_LIMIT の判定を通らなくなったため。同じ処理は、適用・保存時の
            // ApplyProfile → ControlService.PreLoadReset／CheckProfileOptions で行われる（Phase6-Step7b-Plan.md §1A.3）
        }



        /// <summary>
        /// 実機（targetDevice）が指定され、かつ有効なコントローラースロットの範囲内であるか。
        /// Phase6-Step7b: ライトバーのプレビュー等、実機に触れる処理はこの判定を使う（編集スロット Device ではない）。
        /// </summary>
        private bool HasTargetDevice =>
            targetDevice >= 0 && targetDevice < environmentService.ControllerSlotLimit;




        public void LaunchCurveEditor(string customDefinition)
        {
            // Custom curve editor web link clicked. Open the bezier curve editor web app usign the default browser app and pass on current custom definition as a query string parameter.
            // The Process.Start command using HTML page doesn't support query parameters, so if there is a custom curve definition then lookup the default browser executable name from a sysreg.
            string defaultBrowserCmd = String.Empty;
            try
            {
                if (!String.IsNullOrEmpty(customDefinition))
                {
                    string progId = String.Empty;
                    using (RegistryKey userChoiceKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\Shell\\Associations\\UrlAssociations\\http\\UserChoice"))
                    {
                        progId = userChoiceKey?.GetValue("Progid")?.ToString();
                    }

                    if (!String.IsNullOrEmpty(progId))
                    {
                        using (RegistryKey browserPathCmdKey = Registry.ClassesRoot.OpenSubKey($"{progId}\\shell\\open\\command"))
                        {
                            defaultBrowserCmd = browserPathCmdKey?.GetValue(null).ToString().ToLower();
                        }

                        if (!String.IsNullOrEmpty(defaultBrowserCmd))
                        {
                            int iStartPos = (defaultBrowserCmd[0] == '"' ? 1 : 0);
                            defaultBrowserCmd = defaultBrowserCmd.Substring(iStartPos, defaultBrowserCmd.LastIndexOf(".exe") + 4 - iStartPos);
                            if (Path.GetFileName(defaultBrowserCmd) == "launchwinapp.exe")
                                defaultBrowserCmd = String.Empty;
                        }

                        // Fallback to IE executable if the default browser HTML shell association is for some reason missing or is not set
                        if (String.IsNullOrEmpty(defaultBrowserCmd))
                            defaultBrowserCmd = "C:\\Program Files\\Internet Explorer\\iexplore.exe";

                        if (!File.Exists(defaultBrowserCmd))
                            defaultBrowserCmd = String.Empty;
                    }
                }

                // Launch custom bezier editor webapp using a default browser executable command or via a default shell command. The default shell exeution doesn't support query parameters.
                if (!String.IsNullOrEmpty(defaultBrowserCmd))
                    System.Diagnostics.Process.Start(defaultBrowserCmd, $"\"file:///{pathService.ProgramDirectory}\\BezierCurveEditor\\index.html?curve={customDefinition.Replace(" ", "")}\"");
                else
                {
                    System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo($"{pathService.ProgramDirectory}\\BezierCurveEditor\\index.html");
                    startInfo.UseShellExecute = true;
                    using (System.Diagnostics.Process temp = System.Diagnostics.Process.Start(startInfo))
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogUser(AppLogLevel.Error, "Log_Ui_BezierEditorOpenFailed", pathService.ProgramDirectory, ex.Message);
            }
        }

        // TODO(技術的負債・Phase5-Step14 タスク(c)-1関連): 本メソッドはフィールドを直接書き換え、
        // PropertyChanged を出さない。本メソッドが更新する各フィールド（tempControllerIndex等）がUIへ正しく反映される
        // のは、呼び出し元（ProfileEditor.xaml.cs）が必ずDataContextをnullにしてから再代入する
        // （Reload()、およびStopEditorBindings()+RefreshEditorBindings()のペア）という規約を
        // 守っているためである。将来、本メソッドを新たな呼び出し元から呼ぶ場合、このDataContext
        // 再設定の規約を守らないと、値は正しく更新されてもUI上には反映されない不具合が再発する。
        // 詳細: docs-forDIMG/MadeByAgent/Phase5-Step14-FormSettings-Unification-Status.md タスク(c)-1
        public void UpdateLateProperties()
        {
            // 読み取り側は保存された種類（OutContType）、書き込み先の一時の種類（OutDevTypeTemp）は outputSlotService 側
            Output.RefreshFromSettings();
            Output.SyncOutDevTypeTempFromSettings();
            General.RefreshFromSettings();
            Gyro.RefreshSmoothMethodIndexes();
        }
    }
}