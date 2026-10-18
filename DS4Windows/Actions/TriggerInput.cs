using System;
using DS4Windows.DI;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step3 W2: <see cref="ITriggerInput"/> の実装（デバイスごとに 1 つ。<c>Mapping</c> が保持して毎フレーム <see cref="Update"/> する。Step4 で配線）。
    /// <see cref="IsActive"/> の本体は、現行の <c>Mapping.getBoolSpecialActionMapping</c> をそのまま移したもの（意味は同じ。<c>TriggerInputTests</c> が全コントロールで比較する）。
    /// 割り当て 0: 判定は表の参照のみ。前フレームの表（<c>_previousFields</c>）は、コンストラクターで 1 回だけ作り、最初に <see cref="WasActive"/> が呼ばれたときだけ展開する（遅延）。
    /// </summary>
    public sealed class TriggerInput : ITriggerInput
    {
        // L2／R2 の「押された」とみなす値、軸の「倒された」とみなす幅、ジャイロの「傾いた」とみなす値（現行の getBoolSpecialActionMapping と同じ）
        private const byte TriggerThreshold = 100;
        private const int AxisDeadWidth = 55;
        private const int GyroThreshold = 67;

        private readonly int _device;
        private readonly IProfileSettingsService _profileSettings;
        private readonly Func<DateTime> _utcNow;
        private readonly Func<long> _tickCount64;
        private readonly DS4StateFieldMapping _previousFields = new DS4StateFieldMapping();

        private DS4State _current;
        private DS4State _previous;
        private DS4StateExposed _exposed;
        private Mouse _mouse;
        private DS4StateFieldMapping _fields;
        private bool _previousPopulated;

        /// <param name="utcNow">現在時刻の提供元（省略時は <c>DateTime.UtcNow</c>）。テストで固定できる。</param>
        /// <param name="tickCount64">起動からの経過ミリ秒の提供元（省略時は <c>Environment.TickCount64</c>）。</param>
        public TriggerInput(int device, IProfileSettingsService profileSettings, Func<DateTime> utcNow = null, Func<long> tickCount64 = null)
        {
            _device = device;
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
            _tickCount64 = tickCount64 ?? (() => Environment.TickCount64);
            Delay = new DelayClock();
        }

        public int Device => _device;

        public DelayClock Delay { get; }

        public DateTime UtcNow => _utcNow();

        public long TickCount64 => _tickCount64();

        /// <summary>
        /// 1 フレームの入力を設定する。<paramref name="fields"/> は、<c>Mapping</c> が展開済みの表（<c>PopulateFieldMapping</c> 済み）。
        /// <paramref name="previous"/>／<paramref name="exposed"/>／<paramref name="mouse"/> は、前フレームの表を遅延して作るときだけ使う（null でもよい）。
        /// </summary>
        public void Update(DS4State current, DS4State previous, DS4StateExposed exposed, Mouse mouse, DS4StateFieldMapping fields)
        {
            _current = current;
            _previous = previous;
            _exposed = exposed;
            _mouse = mouse;
            _fields = fields;
            _previousPopulated = false;
        }

        public bool IsActive(DS4Controls control)
        {
            if (_current == null || _fields == null) return false;
            return Evaluate(control, _current, _fields);
        }

        public bool WasActive(DS4Controls control)
        {
            if (_previous == null || _exposed == null) return false;

            if (!_previousPopulated)
            {
                _previousFields.PopulateFieldMapping(_previous, _exposed, _mouse, true);
                _previousPopulated = true;
            }

            return Evaluate(control, _previous, _previousFields);
        }

        // 現行の Mapping.getBoolSpecialActionMapping（cState／fieldMap を引数に取る形）と同じ判定
        private bool Evaluate(DS4Controls control, DS4State cState, DS4StateFieldMapping fieldMap)
        {
            int controlNum = (int)control;
            if (controlNum < 0 || controlNum >= DS4StateFieldMapping.mappedType.Length)
                return false;

            DS4StateFieldMapping.ControlType controlType = DS4StateFieldMapping.mappedType[controlNum];
            bool result = false;
            if (controlType == DS4StateFieldMapping.ControlType.Button)
            {
                result = fieldMap.buttons[controlNum];
            }
            else if (controlType == DS4StateFieldMapping.ControlType.AxisDir)
            {
                byte axisValue = fieldMap.axisdirs[controlNum];

                switch (control)
                {
                    case DS4Controls.LXNeg: result = cState.LX < 128 - AxisDeadWidth; break;
                    case DS4Controls.LYNeg: result = cState.LY < 128 - AxisDeadWidth; break;
                    case DS4Controls.RXNeg: result = cState.RX < 128 - AxisDeadWidth; break;
                    case DS4Controls.RYNeg: result = cState.RY < 128 - AxisDeadWidth; break;
                    default: result = axisValue > 128 + AxisDeadWidth; break;
                }
            }
            else if (controlType == DS4StateFieldMapping.ControlType.Trigger)
            {
                result = fieldMap.triggers[controlNum] > TriggerThreshold;
            }
            else if (controlType == DS4StateFieldMapping.ControlType.Touch)
            {
                result = fieldMap.buttons[controlNum];
            }
            else if (controlType == DS4StateFieldMapping.ControlType.SwipeDir)
            {
                result = fieldMap.swipedirbools[controlNum];
            }
            else if (controlType == DS4StateFieldMapping.ControlType.GyroDir)
            {
                bool gyroForControls = _profileSettings.GyroOutputMode[_device] == GyroOutMode.Controls;
                bool safeTest = false;

                switch (control)
                {
                    case DS4Controls.GyroXPos: safeTest = fieldMap.gryodirs[controlNum] > GyroThreshold; break;
                    case DS4Controls.GyroXNeg: safeTest = fieldMap.gryodirs[controlNum] < -GyroThreshold; break;
                    case DS4Controls.GyroZPos: safeTest = fieldMap.gryodirs[controlNum] > GyroThreshold; break;
                    case DS4Controls.GyroZNeg: safeTest = fieldMap.gryodirs[controlNum] < -GyroThreshold; break;
                    default: break;
                }

                result = gyroForControls ? safeTest : false;
            }

            return result;
        }
    }
}
