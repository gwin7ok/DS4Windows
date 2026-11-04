using System;
using DS4Windows.DI;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step3 W2: <see cref="ITriggerInput"/> の実装（デバイスごとに 1 つ。<c>Mapping</c> が保持して毎フレーム <see cref="Update"/> する。Step4 で配線）。
    /// <see cref="IsActive"/> の本体は、現行の <c>Mapping.getBoolSpecialActionMapping</c> をそのまま移したもの（意味は同じ。<c>TriggerInputTests</c> が全コントロールで比較する）。
    /// 割り当て 0: 判定は表の参照のみ。前フレームの表（<c>_previousFields</c>）は、コンストラクターで 1 回だけ作り、最初に <see cref="WasActive"/> が呼ばれたときだけ展開する（遅延）。
    /// </summary>
    public sealed class TriggerInput : IAnalogTriggerInput, IFixedMouseInput
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

        /// <summary>
        /// Phase8-Step7 7.8f-3: マウスの移動の生の量の計算の借り先（<c>Mapping</c> が、デバイスごとに 1 回だけ設定する）。引数は（コントロール、向き）。
        /// 計算が感度・加速・デッドゾーン・経過時間を使うため、<c>Mapping.getMouseMapping</c> を呼ぶ。
        /// </summary>
        public Func<DS4Controls, int, double> MouseSampler { get; set; }

        public double GetMouseMovement(DS4Controls control, int direction)
        {
            Func<DS4Controls, int, double> sampler = MouseSampler;
            return sampler == null ? 0.0 : sampler(control, direction);
        }

        /// <summary>
        /// Phase10-Step6 後の Step5: SA のマウス移動（強さの固定値）の量の計算の借り先（<c>Mapping</c> が、デバイスごとに 1 回だけ設定する）。
        /// 引数は、方向（0＝Up、1＝Down、2＝Left、3＝Right）と強さ（0〜1）。
        /// </summary>
        public Func<int, double, double> FixedMouseSampler { get; set; }

        public double GetFixedMouseMovement(int direction, double fraction)
        {
            Func<int, double, double> sampler = FixedMouseSampler;
            return sampler == null ? 0.0 : sampler(direction, fraction);
        }

        public bool IsActive(DS4Controls control)
        {
            if (_current == null || _fields == null) return false;
            return Evaluate(control, _current, _fields);
        }

        /// <summary>R-5: SA の割り当てのデジタルな押下の判定（閾値なし）。<see cref="ITriggerInput.IsDigitalActive"/>。</summary>
        public bool IsDigitalActive(DS4Controls control)
        {
            if (_current == null || _fields == null) return false;
            return Evaluate(control, _current, _fields, digital: true);
        }

        public bool WasDigitalActive(DS4Controls control)
        {
            if (_previous == null || _exposed == null) return false;

            if (!_previousPopulated)
            {
                _previousFields.PopulateFieldMapping(_previous, _exposed, _mouse, true);
                _previousPopulated = true;
            }

            return Evaluate(control, _previous, _previousFields, digital: true);
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

        /// <summary>
        /// Phase8-Step7 7.8b: 現行の <c>Mapping.GetBoolActionMapping</c>（Controls タブの割り当て用）と同じ判定。スティックは角度の扇形（<c>LSAngle</c>／<c>RSAngle</c>）、
        /// L2／R2 は 0 より大きい、ジャイロは 0 より大きい／小さい（<c>GyroOutputMode</c> が Controls のときだけ）。
        /// </summary>
        public bool IsControlActive(DS4Controls control)
        {
            if (_current == null || _fields == null) return false;

            int controlNum = (int)control;
            if (controlNum < 0 || controlNum >= DS4StateFieldMapping.mappedType.Length)
                return false;

            DS4State cState = _current;
            DS4StateFieldMapping fieldMap = _fields;
            DS4StateFieldMapping.ControlType controlType = DS4StateFieldMapping.mappedType[controlNum];
            bool result = false;
            if (controlType == DS4StateFieldMapping.ControlType.Button)
            {
                result = fieldMap.buttons[controlNum];
            }
            else if (controlType == DS4StateFieldMapping.ControlType.AxisDir)
            {
                switch (control)
                {
                    case DS4Controls.LXNeg: result = cState.LX < 128 && (cState.LSAngle >= 112.5 && cState.LSAngle <= 247.5); break;
                    case DS4Controls.LYNeg: result = cState.LY < 128 && (cState.LSAngle >= 22.5 && cState.LSAngle <= 157.5); break;
                    case DS4Controls.RXNeg: result = cState.RX < 128 && (cState.RSAngle >= 112.5 && cState.RSAngle <= 247.5); break;
                    case DS4Controls.RYNeg: result = cState.RY < 128 && (cState.RSAngle >= 22.5 && cState.RSAngle <= 157.5); break;
                    case DS4Controls.LXPos: result = cState.LX > 128 && (cState.LSAngle <= 67.5 || cState.LSAngle >= 292.5); break;
                    case DS4Controls.LYPos: result = cState.LY > 128 && (cState.LSAngle >= 202.5 && cState.LSAngle <= 337.5); break;
                    case DS4Controls.RXPos: result = cState.RX > 128 && (cState.RSAngle <= 67.5 || cState.RSAngle >= 292.5); break;
                    case DS4Controls.RYPos: result = cState.RY > 128 && (cState.RSAngle >= 202.5 && cState.RSAngle <= 337.5); break;
                    default: break;
                }
            }
            else if (controlType == DS4StateFieldMapping.ControlType.Trigger)
            {
                result = fieldMap.triggers[controlNum] > 0;
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
                    case DS4Controls.GyroXPos: safeTest = fieldMap.gryodirs[controlNum] > 0; break;
                    case DS4Controls.GyroXNeg: safeTest = fieldMap.gryodirs[controlNum] < 0; break;
                    case DS4Controls.GyroZPos: safeTest = fieldMap.gryodirs[controlNum] > 0; break;
                    case DS4Controls.GyroZNeg: safeTest = fieldMap.gryodirs[controlNum] < 0; break;
                    default: break;
                }

                result = gyroForControls ? safeTest : false;
            }

            return result;
        }

        /// <summary>
        /// Phase8-Step7 7.8f-1（Q4＝A）: 現行の <c>Mapping.GetXYAxisMapping</c> と同じ値。128 が中立。
        /// <paramref name="positive"/> は、出力先が正の向きの軸か（旧 <c>alt</c>）。割り当て 0。
        /// </summary>
        public byte GetAxisValue(DS4Controls control, bool positive)
        {
            const byte falseVal = 128;
            if (_fields == null) return 0;

            int controlNum = (int)control;
            if (controlNum < 0 || controlNum >= DS4StateFieldMapping.mappedType.Length) return 0;

            DS4StateFieldMapping fieldMap = _fields;
            bool alt = positive;
            byte trueVal = alt ? (byte)255 : (byte)0;
            byte result = 0;
            DS4StateFieldMapping.ControlType controlType = DS4StateFieldMapping.mappedType[controlNum];

            if (controlType == DS4StateFieldMapping.ControlType.Button)
            {
                result = fieldMap.buttons[controlNum] ? trueVal : falseVal;
            }
            else if (controlType == DS4StateFieldMapping.ControlType.AxisDir)
            {
                byte axisValue = fieldMap.axisdirs[controlNum];

                switch (control)
                {
                    case DS4Controls.LXNeg: if (!alt) result = axisValue < falseVal ? axisValue : falseVal; else result = axisValue < falseVal ? (byte)(255 - axisValue) : falseVal; break;
                    case DS4Controls.LYNeg: if (!alt) result = axisValue < falseVal ? axisValue : falseVal; else result = axisValue < falseVal ? (byte)(255 - axisValue) : falseVal; break;
                    case DS4Controls.RXNeg: if (!alt) result = axisValue < falseVal ? axisValue : falseVal; else result = axisValue < falseVal ? (byte)(255 - axisValue) : falseVal; break;
                    case DS4Controls.RYNeg: if (!alt) result = axisValue < falseVal ? axisValue : falseVal; else result = axisValue < falseVal ? (byte)(255 - axisValue) : falseVal; break;
                    default: if (!alt) result = axisValue > falseVal ? (byte)(255 - axisValue) : falseVal; else result = axisValue > falseVal ? axisValue : falseVal; break;
                }
            }
            else if (controlType == DS4StateFieldMapping.ControlType.Trigger)
            {
                if (alt)
                    result = (byte)(128.0f + fieldMap.triggers[controlNum] / 2.0078740157480315f);
                else
                    result = (byte)(128.0f - fieldMap.triggers[controlNum] / 2.0078740157480315f);
            }
            else if (controlType == DS4StateFieldMapping.ControlType.Touch)
            {
                result = fieldMap.buttons[controlNum] ? trueVal : falseVal;
            }
            else if (controlType == DS4StateFieldMapping.ControlType.SwipeDir)
            {
                if (alt)
                    result = (byte)(_mouse != null ? 128.0f + fieldMap.swipedirs[controlNum] / 2f : 0);
                else
                    result = (byte)(_mouse != null ? 128.0f - fieldMap.swipedirs[controlNum] / 2f : 0);
            }
            else if (controlType == DS4StateFieldMapping.ControlType.GyroDir)
            {
                bool saControls = _profileSettings.GyroOutputMode[_device] == GyroOutMode.Controls;
                int gyro = fieldMap.gryodirs[controlNum];

                switch (control)
                {
                    case DS4Controls.GyroXPos:
                    case DS4Controls.GyroZPos:
                        if (saControls && gyro > 0)
                        {
                            if (alt) result = (byte)Math.Min(255, 128 + gyro); else result = (byte)Math.Max(0, 128 - gyro);
                        }
                        else result = falseVal;
                        break;
                    case DS4Controls.GyroXNeg:
                    case DS4Controls.GyroZNeg:
                        if (saControls && gyro < 0)
                        {
                            if (alt) result = (byte)Math.Min(255, 128 + -gyro); else result = (byte)Math.Max(0, 128 - -gyro);
                        }
                        else result = falseVal;
                        break;
                    default: break;
                }
            }

            return result;
        }

        /// <summary>Phase8-Step7 7.8f-1（Q4＝A）: 現行の <c>Mapping.GetByteMapping</c> と同じ値（0〜255）。割り当て 0。</summary>
        public byte GetByteValue(DS4Controls control)
        {
            if (_fields == null) return 0;

            int controlNum = (int)control;
            if (controlNum < 0 || controlNum >= DS4StateFieldMapping.mappedType.Length) return 0;

            DS4StateFieldMapping fieldMap = _fields;
            byte result = 0;
            DS4StateFieldMapping.ControlType controlType = DS4StateFieldMapping.mappedType[controlNum];

            if (controlType == DS4StateFieldMapping.ControlType.Button)
            {
                result = (byte)(fieldMap.buttons[controlNum] ? 255 : 0);
            }
            else if (controlType == DS4StateFieldMapping.ControlType.AxisDir)
            {
                byte axisValue = fieldMap.axisdirs[controlNum];

                switch (control)
                {
                    case DS4Controls.LXNeg: result = (byte)(axisValue - 128.0f >= 0 ? 0 : -(axisValue - 128.0f) * 1.9921875f); break;
                    case DS4Controls.LYNeg: result = (byte)(axisValue - 128.0f >= 0 ? 0 : -(axisValue - 128.0f) * 1.9921875f); break;
                    case DS4Controls.RXNeg: result = (byte)(axisValue - 128.0f >= 0 ? 0 : -(axisValue - 128.0f) * 1.9921875f); break;
                    case DS4Controls.RYNeg: result = (byte)(axisValue - 128.0f >= 0 ? 0 : -(axisValue - 128.0f) * 1.9921875f); break;
                    default: result = (byte)(axisValue - 128.0f < 0 ? 0 : (axisValue - 128.0f) * 2.0078740157480315f); break;
                }
            }
            else if (controlType == DS4StateFieldMapping.ControlType.Trigger)
            {
                result = fieldMap.triggers[controlNum];
            }
            else if (controlType == DS4StateFieldMapping.ControlType.Touch)
            {
                result = (byte)(_mouse != null && fieldMap.buttons[controlNum] ? 255 : 0);
            }
            else if (controlType == DS4StateFieldMapping.ControlType.SwipeDir)
            {
                result = (byte)(_mouse != null ? fieldMap.swipedirs[controlNum] : 0);
            }
            else if (controlType == DS4StateFieldMapping.ControlType.GyroDir)
            {
                bool saControls = _profileSettings.GyroOutputMode[_device] == GyroOutMode.Controls;
                int gyro = fieldMap.gryodirs[controlNum];

                switch (control)
                {
                    case DS4Controls.GyroXPos:
                    case DS4Controls.GyroZPos:
                        result = (byte)(saControls ? Math.Min(255, gyro * 2) : 0);
                        break;
                    case DS4Controls.GyroXNeg:
                    case DS4Controls.GyroZNeg:
                        result = (byte)(saControls ? Math.Min(255, -gyro * 2) : 0);
                        break;
                    default: break;
                }
            }

            return result;
        }

        // 現行の Mapping.getBoolSpecialActionMapping（cState／fieldMap を引数に取る形）と同じ判定。
        // digital が true のとき（R-5。SA の割り当て）は、軸と L2・R2 の閾値（幅 55／100）をなくす（軸は 128 から少しでも傾けば、L2・R2 は 0 を超えれば成立）
        private bool Evaluate(DS4Controls control, DS4State cState, DS4StateFieldMapping fieldMap, bool digital = false)
        {
            int axisWidth = digital ? 0 : AxisDeadWidth;
            int triggerThreshold = digital ? 0 : TriggerThreshold;
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
                    case DS4Controls.LXNeg: result = cState.LX < 128 - axisWidth; break;
                    case DS4Controls.LYNeg: result = cState.LY < 128 - axisWidth; break;
                    case DS4Controls.RXNeg: result = cState.RX < 128 - axisWidth; break;
                    case DS4Controls.RYNeg: result = cState.RY < 128 - axisWidth; break;
                    default: result = axisValue > 128 + axisWidth; break;
                }
            }
            else if (controlType == DS4StateFieldMapping.ControlType.Trigger)
            {
                result = fieldMap.triggers[controlNum] > triggerThreshold;
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
