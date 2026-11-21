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
using DS4Windows.Core.Settings;
using DS4Windows.Input;

namespace DS4Windows.Processing.Touchpad
{
    /// <summary>
    /// タッチパッドのボタン（押し込み）が、左・右・上部・複数の指のどれにあたるかを決め、その押下の状態を持つ。
    /// 判定の方式（Click／Touch／Release）は設定の <see cref="TouchButtonActivationMode"/>。タッチのイベントのたびに <see cref="Check"/> が呼ばれる。
    /// 状態の持ち主は、スロットごとの <see cref="TouchpadMouse"/>。
    /// </summary>
    public sealed class TouchButtonDetector
    {
        private enum Candidate : ushort
        {
            None,
            Left,
            Right,
            Multi,
            Upper,
        }

        private Candidate candidate;

        // Release 方式の保持時間の基準になる時計。テストが手動の時計を渡して、実時間を待たずに確かめる
        private readonly TimeProvider _time;

        public TouchButtonDetector(TimeProvider? timeProvider = null)
        {
            _time = timeProvider ?? TimeProvider.System;
        }

        /// <summary>Release 方式の押下を保持する時間（ミリ秒）。</summary>
        public const double ReleaseHoldMs = 100.0;

        /// <summary>
        /// Release 方式の押下が成立してから <see cref="ReleaseHoldMs"/> 経ったら、押下の状態を下ろして真を返す（呼び出し側が、その 1 フレームの出力を続ける）。
        /// 離した直後のフレームで下ろすと、割り当てへ届かない（上流は比較が逆だった）。経過するまでは何もしない。
        /// </summary>
        public bool TryExpireRelease()
        {
            if (!ReleaseButtonActive) return false;
            if (_time.GetUtcNow().UtcDateTime < OnReleaseTime + TimeSpan.FromMilliseconds(ReleaseHoldMs)) return false;

            ReleaseButtonActive = false;
            ClearDownFlags();
            return true;
        }

        /// <summary>指を離した直後かどうか（Release 方式が、離したときに押下を成立させるために使う）。</summary>
        public bool WasTouched { get; set; }

        /// <summary>タッチボタンが直前に押され、離されたか（Release 方式の、上部の判定に使う）。</summary>
        public bool WasTouchButtonClicked { get; set; }

        /// <summary>Release 方式の押下が成立していて、一定時間保持している間は真。</summary>
        public bool ReleaseButtonActive { get; set; }

        /// <summary>Release 方式の押下が成立した時刻。</summary>
        public DateTime OnReleaseTime { get; set; }

        public bool LeftDown { get; private set; }
        public bool RightDown { get; private set; }
        public bool UpperDown { get; private set; }
        public bool MultiDown { get; private set; }

        /// <summary>押下の状態と候補を消す。</summary>
        public void ClearDownFlags()
        {
            UpperDown = LeftDown = RightDown = MultiDown = false;
            candidate = Candidate.None;
        }

        /// <summary>押下の状態だけを消す（候補は残す）。</summary>
        public void ClearDownFlagsKeepCandidate()
        {
            UpperDown = LeftDown = RightDown = MultiDown = false;
        }

        /// <summary>候補を消す。</summary>
        public void ClearCandidate()
        {
            candidate = Candidate.None;
        }

        /// <summary>新しい触れ始めのための初期化。</summary>
        public void ResetForNewTouch()
        {
            WasTouched = false;
            WasTouchButtonClicked = false;
            ReleaseButtonActive = false;
            ClearDownFlags();
        }

        private static bool IsLeft(Touch t)
        {
            return t.HwX < 1920 * 2 / 5;
        }

        private static bool IsRight(Touch t)
        {
            return t.HwX >= 1920 * 2 / 5;
        }

        /// <summary>タッチのイベントの内容から、候補を決め、押下を成立させる。</summary>
        public void Check(TouchpadEventArgs arg, TouchButtonActivationMode touchButtonMode)
        {
            bool activateTouchButton = false;
            if (touchButtonMode == TouchButtonActivationMode.Click &&
                arg.TouchButtonPressed)
            {
                if (arg.Touches == null)
                {
                    candidate = Candidate.Upper;
                }
                else if (arg.Touches.Length > 1)
                {
                    candidate = Candidate.Multi;
                }
                else
                {
                    if (IsLeft(arg.Touches[0]))
                    {
                        candidate = Candidate.Left;
                    }
                    else if (IsRight(arg.Touches[0]))
                    {
                        candidate = Candidate.Right;
                    }
                }

                activateTouchButton = true;
            }
            else if (touchButtonMode == TouchButtonActivationMode.Touch)
            {
                if (arg.TouchActive || arg.TouchButtonPressed)
                {
                    if (arg.Touches == null)
                    {
                        candidate = Candidate.Upper;
                    }
                    else if (arg.Touches.Length > 1)
                    {
                        candidate = Candidate.Multi;
                    }
                    else
                    {
                        if (IsLeft(arg.Touches[0]))
                        {
                            candidate = Candidate.Left;
                        }
                        else if (IsRight(arg.Touches[0]))
                        {
                            candidate = Candidate.Right;
                        }
                    }

                    activateTouchButton = true;
                }
                else
                {
                    UpperDown = LeftDown = RightDown = MultiDown = false;
                }
            }
            else if (touchButtonMode == TouchButtonActivationMode.Release)
            {
                if (candidate == Candidate.None)
                {
                    // Top region of Touchpad was clicked. Out of range of Touchpad sensors
                    if (!arg.TouchActive && !arg.TouchButtonPressed && WasTouchButtonClicked)
                    {
                        candidate = Candidate.Upper;
                    }
                    else if (arg.TouchActive)
                    {
                        if (arg.Touches!.Length > 1) // タッチパッドのイベントは、接触の配列を持って来る（null なら従来どおり NullReferenceException）
                        {
                            candidate = Candidate.Multi;
                        }
                    }
                    else if (!arg.TouchActive && arg.Touches != null)
                    {
                        if (IsLeft(arg.Touches[0]))
                        {
                            candidate = Candidate.Left;
                        }
                        else if (IsRight(arg.Touches[0]))
                        {
                            candidate = Candidate.Right;
                        }
                    }
                }

                if (candidate != Candidate.None)
                {
                    if (candidate == Candidate.Multi)
                    {
                        // Check that no finger is touching Touchpad
                        if (WasTouched && !arg.TouchActive)
                        {
                            activateTouchButton = true;
                            ReleaseButtonActive = true;
                            OnReleaseTime = _time.GetUtcNow().UtcDateTime;
                        }
                    }
                    else if ((WasTouched && !arg.TouchActive) ||
                        (WasTouchButtonClicked && !arg.TouchButtonPressed && !arg.TouchActive))
                    {
                        activateTouchButton = true;
                        ReleaseButtonActive = true;
                        OnReleaseTime = _time.GetUtcNow().UtcDateTime;
                    }
                }
            }

            if (activateTouchButton)
            {
                switch (candidate)
                {
                    case Candidate.Left:
                        LeftDown = true;
                        break;
                    case Candidate.Right:
                        RightDown = true;
                        break;
                    case Candidate.Multi:
                        MultiDown = true;
                        break;
                    case Candidate.Upper:
                        UpperDown = true;
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
