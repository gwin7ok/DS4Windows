using System;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DS4Windows.Core.Settings;
using DS4Windows.Processing;

namespace DS4Windows.UI.ViewModels
{
    /// <summary>
    /// プロファイル編集画面の「その他」タブの一般的な項目（入力の表示、ボタンによるマウス移動、プログラムの同時起動、
    /// 無操作での切断、Bluetooth のポーリング間隔）の ViewModel。1 つのスロットの設定（<see cref="GeneralSlotSettings"/>）だけを扱う。
    /// プロパティ名は、画面の Binding と同じ名前のまま。
    /// </summary>
    public class GeneralSettingsViewModel : ObservableObject
    {
        private readonly GeneralSlotSettings general;

        private int outputMouseSpeed;
        private double mouseOffsetSpeed;

        public GeneralSettingsViewModel(GeneralSlotSettings general)
        {
            this.general = general ?? throw new ArgumentNullException(nameof(general));
            RefreshFromSettings();

            // 自分の値が変わったときの連動（派生する表示用プロパティの再計算）。値を設定した直後に同期して実行する
            PropertyChanged += OnOwnPropertyChanged;
        }

        /// <summary>
        /// 保存された設定から、マウスの速度の表示値を計算し直す。PropertyChanged は出さない。
        /// 呼び出し側が DataContext を null にして再代入する規約のもとで使う（読み込み直しの流れ）。
        /// </summary>
        public void RefreshFromSettings()
        {
            outputMouseSpeed = CalculateOutputMouseSpeed(ButtonMouseSensitivity);
            mouseOffsetSpeed = RawButtonMouseOffset * outputMouseSpeed;
        }

        public bool UseControllerReadout
        {
            get => general.UseControllerReadout;
            set => general.UseControllerReadout = value;
        }

        public int ButtonMouseSensitivity
        {
            get => general.ButtonMouse.buttonSensitivity;
            set
            {
                if (general.ButtonMouse.buttonSensitivity == value) return;
                general.ButtonMouse.ButtonSensitivity = value;
                OnPropertyChanged(nameof(ButtonMouseSensitivity));
            }
        }

        public int ButtonMouseVerticalScale
        {
            get => Convert.ToInt32(general.ButtonMouse.buttonVerticalScale * 100.0);
            set
            {
                double attemptValue = value * 0.01;
                if (general.ButtonMouse.buttonVerticalScale == attemptValue) return;
                general.ButtonMouse.buttonVerticalScale = attemptValue;
                OnPropertyChanged(nameof(ButtonMouseVerticalScale));
            }
        }

        private double RawButtonMouseOffset => general.ButtonMouse.mouseVelocityOffset;

        public double ButtonMouseOffset
        {
            get => general.ButtonMouse.mouseVelocityOffset * 100.0;
            set
            {
                if (general.ButtonMouse.mouseVelocityOffset * 100.0 == value) return;
                general.ButtonMouse.mouseVelocityOffset = value * 0.01;
                OnPropertyChanged(nameof(ButtonMouseOffset));
            }
        }

        public int OutputMouseSpeed
        {
            get => outputMouseSpeed;
            set
            {
                if (value == outputMouseSpeed) return;
                outputMouseSpeed = value;
                OnPropertyChanged(nameof(OutputMouseSpeed));
            }
        }

        public double MouseOffsetSpeed
        {
            get => mouseOffsetSpeed;
            set
            {
                if (mouseOffsetSpeed == value) return;
                mouseOffsetSpeed = value;
                OnPropertyChanged(nameof(MouseOffsetSpeed));
            }
        }

        public bool MouseAcceleration
        {
            get => general.ButtonMouse.mouseAccel;
            set => general.ButtonMouse.mouseAccel = value;
        }

        public bool EnableTouchpadToggle
        {
            get => general.EnableTouchToggle;
            set => general.EnableTouchToggle = value;
        }

        public bool EnableOutputDataToDS4
        {
            get => general.EnableOutputDataToDS4;
            set => general.EnableOutputDataToDS4 = value;
        }

        public bool LaunchProgramExists
        {
            get => !string.IsNullOrEmpty(general.LaunchProgram);
            set
            {
                if (!value) ResetLauchProgram();
            }
        }

        public string LaunchProgram => general.LaunchProgram;

        public string LaunchProgramName
        {
            get
            {
                string temp = general.LaunchProgram;
                if (!string.IsNullOrEmpty(temp))
                {
                    temp = Path.GetFileNameWithoutExtension(temp);
                }
                else
                {
                    temp = "Browse";
                }

                return temp;
            }
        }

        public ImageSource LaunchProgramIcon
        {
            get
            {
                ImageSource exeicon = null;
                string path = general.LaunchProgram;
                if (File.Exists(path) && Path.GetExtension(path).ToLower() == ".exe")
                {
                    using (System.Drawing.Icon ico = System.Drawing.Icon.ExtractAssociatedIcon(path))
                    {
                        exeicon = Imaging.CreateBitmapSourceFromHIcon(ico.Handle, Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());
                        exeicon.Freeze();
                    }
                }

                return exeicon;
            }
        }

        public void UpdateLaunchProgram(string path)
        {
            general.LaunchProgram = path;
            NotifyLaunchProgramChanged();
        }

        public void ResetLauchProgram()
        {
            general.LaunchProgram = string.Empty;
            NotifyLaunchProgramChanged();
        }

        private void NotifyLaunchProgramChanged()
        {
            OnPropertyChanged(nameof(LaunchProgramExists));
            OnPropertyChanged(nameof(LaunchProgram));
            OnPropertyChanged(nameof(LaunchProgramName));
            OnPropertyChanged(nameof(LaunchProgramIcon));
        }

        public bool IdleDisconnectExists
        {
            get => general.IdleDisconnectTimeoutSeconds != 0;
            set
            {
                // 有効にするときは既定の時間にし、無効にするときは 0（無効）にする
                general.IdleDisconnectTimeoutSeconds = value ?
                    BackingStore.DEFAULT_ENABLE_IDLE_DISCONN_MINS * 60 : 0;

                OnPropertyChanged(nameof(IdleDisconnect));
                OnPropertyChanged(nameof(IdleDisconnectExists));
            }
        }

        /// <summary>無操作で切断するまでの時間（分）。</summary>
        public int IdleDisconnect
        {
            get => general.IdleDisconnectTimeoutSeconds / 60;
            set
            {
                if (general.IdleDisconnectTimeoutSeconds / 60 == value) return;
                general.IdleDisconnectTimeoutSeconds = value * 60;
                OnPropertyChanged(nameof(IdleDisconnect));
                OnPropertyChanged(nameof(IdleDisconnectExists));
            }
        }

        public int TempBTPollRateIndex
        {
            get => general.BTPollRate;
            set
            {
                if (general.BTPollRate == value) return;
                general.BTPollRate = value;
            }
        }

        private static int CalculateOutputMouseSpeed(int mouseSpeed)
        {
            return mouseSpeed * Mapping.MOUSESPEEDFACTOR;
        }

        private void OnOwnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ButtonMouseSensitivity):
                    OutputMouseSpeed = CalculateOutputMouseSpeed(ButtonMouseSensitivity);
                    MouseOffsetSpeed = RawButtonMouseOffset * OutputMouseSpeed;
                    break;
                case nameof(ButtonMouseOffset):
                    MouseOffsetSpeed = RawButtonMouseOffset * OutputMouseSpeed;
                    break;
            }
        }
    }
}
