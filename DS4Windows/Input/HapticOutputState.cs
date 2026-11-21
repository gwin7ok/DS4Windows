#nullable enable

using System.Diagnostics;
using DS4Windows.Core.Model;

namespace DS4Windows.Input
{
    /// <summary>
    /// 1 台ぶんの触覚の出力（振動・ライトバー）の状態の持ち主。機種の出力が毎フレーム読む「いまの出力」（<see cref="Current"/>）と、
    /// 外から設定された振動（テスト用・仮想パッドからの振動）を合成する（<see cref="Merge"/>）。振動の自動停止の時計もここで持つ。
    /// 構造体のコピーを避けるため、<see cref="Current"/> は ref で返す（毎フレームの経路で割り当てない）。
    /// </summary>
    public sealed class HapticOutputState
    {
        private DS4HapticState _current = new DS4HapticState();
        private DS4HapticState _requested = new DS4HapticState();

        /// <summary>機種の出力が読む、いまの触覚の状態。</summary>
        public ref DS4HapticState Current => ref _current;

        /// <summary>振動が止まらなくなったときに止める時計（仮想パッドからの停止の通知が失われた場合の保険）。</summary>
        public Stopwatch AutostopTimer { get; } = new Stopwatch();

        /// <summary>自動停止までのミリ秒。0 以下なら無効。</summary>
        public int AutostopTime { get; private set; }

        /// <summary>自動停止の時間を設定する。無効（0 以下）なら時計を止め、有効なら再始動する。</summary>
        public void SetAutostopTime(int milliseconds)
        {
            AutostopTime = milliseconds;
            if (milliseconds <= 0)
                AutostopTimer.Reset();
            else
                AutostopTimer.Restart();
        }

        /// <summary>仮想パッドなどからの振動の要求を受ける。次の <see cref="Merge"/> で、いまの出力へ反映される。</summary>
        public void SetRumble(byte rightLightFastMotor, byte leftHeavySlowMotor)
        {
            _requested.rumbleState.RumbleMotorStrengthRightLightFast = rightLightFastMotor;
            _requested.rumbleState.RumbleMotorStrengthLeftHeavySlow = leftHeavySlowMotor;
            _requested.rumbleState.RumbleMotorsExplicitlyOff = rightLightFastMotor == 0 && leftHeavySlowMotor == 0;
            _requested.dirty = true;

            // 自動停止が有効なら、振動を変えるたびに時計を再始動する（0 にしたときは止める）
            if (AutostopTime > 0)
            {
                if (_requested.rumbleState.RumbleMotorsExplicitlyOff)
                    AutostopTimer.Reset();   // ViGEm から 0 の通知が正しく届いたので、時計を止める
                else if (_current.rumbleState.RumbleMotorStrengthLeftHeavySlow != leftHeavySlowMotor || _current.rumbleState.RumbleMotorStrengthRightLightFast != rightLightFastMotor)
                    AutostopTimer.Restart(); // 停止の通知が失われても止まるように、時計を始める
            }
        }

        /// <summary>要求された振動を、いまの出力へ合成する（出力の組み立ての先頭で呼ぶ）。</summary>
        public void Merge()
        {
            if (_requested.IsRumbleSet())
            {
                if (_requested.rumbleState.RumbleMotorsExplicitlyOff)
                    _requested.rumbleState.RumbleMotorsExplicitlyOff = false;

                _current.rumbleState = _requested.rumbleState;
            }

            _current.dirty = _requested.dirty;
            _requested.dirty = false;
        }

        /// <summary>ライトバーの状態を設定する。変わったときだけ、出力を要求する（dirty）。</summary>
        public void SetLightbar(ref DS4LightbarState lightState)
        {
            if (!_current.lightbarState.Equals(lightState))
            {
                _current.dirty = true;
            }

            _current.lightbarState = lightState;
        }

        /// <summary>触覚の状態を丸ごと置き換え、出力を要求する。</summary>
        public void SetHaptic(ref DS4HapticState hs)
        {
            _current = hs;
            _current.dirty = true;
        }
    }
}
