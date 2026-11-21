﻿/*
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
using DS4Windows.Core.Logging;
using DS4Windows.Input;
using DS4Windows.Processing.Synthetic;

namespace DS4Windows.Processing.Touchpad
{
    /// <summary>
    /// タッチパッドのタップ（軽く触れて離す）と、ダブルタップ・タップ＆ホールドの判定。
    /// 成立したタップは、左クリックを一定時間（<see cref="TapClickHoldMs"/>）保持して出す。状態の持ち主は、スロットごとの <see cref="TouchpadMouse"/>。
    /// </summary>
    public sealed class TapClickDetector
    {
        // タップ判定で許容する始点と終点のずれ（タッチパッド座標。上流は 10。実機で厳しすぎたため 100 に拡大した）
        private const int TapMaxMoveHw = 100;

        // タップによる左クリックを押したままにする時間（ms）。
        // 上流は 1 フレームだけの瞬間的なクリックで、フォーカスの切り替えが効かない環境があったため保持する
        private const double TapClickHoldMs = 50.0;

        private readonly int deviceNum;
        private readonly TimeProvider _time;
        private DateTime pastTime, firstTap, timeofEnd;
        private bool tappedOnce;
        private DateTime tapClickHoldUntil = DateTime.MinValue;

        public TapClickDetector(int deviceNum, TimeProvider timeProvider)
        {
            this.deviceNum = deviceNum;
            ArgumentNullException.ThrowIfNull(timeProvider);
            _time = timeProvider;
        }

        /// <summary>ダブルタップの 2 回目の触れ始め（タップ＆ホールドを含む）。真の間は、ドラッグとしてクリックを出し続ける。</summary>
        public bool SecondTouchBegan { get; private set; }

        /// <summary>ダブルタップの 1 回目が成立し、クリックを保留している間は真。</summary>
        public bool TappedOnce => tappedOnce;

        /// <summary>タップのクリックを保持している間、または保留している間は真。指を離したあとも、クリックの合成を続ける目安。</summary>
        public bool IsPendingOrHolding => tappedOnce || _time.GetUtcNow().UtcDateTime < tapClickHoldUntil;

        private void StartTapClickHold()
        {
            tapClickHoldUntil = _time.GetUtcNow().UtcDateTime + TimeSpan.FromMilliseconds(TapClickHoldMs);
        }

        /// <summary>触れ始め。タップの時間の基準を取り、ダブルタップの 2 回目かを調べる（<paramref name="checkDoubleTap"/>：マウスで、かつダブルタップが有効）。</summary>
        public void TouchBegan(DateTime timeStamp, bool buttonPressed, bool checkDoubleTap, byte tapSensitivity)
        {
            pastTime = timeStamp;
            if (checkDoubleTap)
            {
                if (timeStamp <= (firstTap + TimeSpan.FromMilliseconds((double)tapSensitivity * 1.5)) && !buttonPressed)
                    SecondTouchBegan = true;
            }
        }

        /// <summary>指を離したとき。時間と動きがタップの範囲なら、クリックを始める（ダブルタップが有効なら保留する）。</summary>
        public void TouchEnded(TouchpadEventArgs arg, Touch firstTouch, byte tapSensitivity, bool doubleTap)
        {
            if (SecondTouchBegan)
            {
                tappedOnce = false;
                SecondTouchBegan = false;
            }

            DateTime test = arg.TimeStamp;
            // 調査用 Trace（動作は変えない）: タップ判定の入力値と成否
            if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"[TAP] device={deviceNum} ended sens={tapSensitivity} doubleTap={doubleTap} " +
                $"elapsedMs={(test - pastTime).TotalMilliseconds:F0} limitMs={tapSensitivity * 2} buttonPressed={arg.TouchButtonPressed} tappedOnce={tappedOnce} " +
                $"dx={Math.Abs(firstTouch.HwX - arg.Touches![0].HwX)} dy={Math.Abs(firstTouch.HwY - arg.Touches[0].HwY)}"); // タッチパッドのイベントは、接触の配列を持って来る（null なら従来どおり NullReferenceException）
            if (test <= (pastTime + TimeSpan.FromMilliseconds((double)tapSensitivity * 2)) && !arg.TouchButtonPressed && !tappedOnce)
            {
                if (Math.Abs(firstTouch.HwX - arg.Touches![0].HwX) < TapMaxMoveHw && Math.Abs(firstTouch.HwY - arg.Touches[0].HwY) < TapMaxMoveHw) // タッチパッドのイベントは、接触の配列を持って来る（null なら従来どおり NullReferenceException）
                {
                    if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"[TAP] device={deviceNum} tap accepted (doubleTap={doubleTap})");
                    if (doubleTap)
                    {
                        tappedOnce = true;
                        firstTap = arg.TimeStamp;
                        timeofEnd = _time.GetUtcNow().UtcDateTime; //since arg can't be used in synthesizeMouseButtons
                    }
                    else
                        StartTapClickHold(); //this way no delay if disabled (クリックは保持時間の間出し続ける)
                }
            }
        }

        /// <summary>
        /// 毎フレーム、保留していたダブルタップの 1 回目を確定し（時間が経ったとき）、保持中のクリックを出す。
        /// タップ＆ホールド（<see cref="SecondTouchBegan"/>）のクリックは、呼び出し側が出す。
        /// </summary>
        public void CountPendingClicks(SyntheticInputState syntheticInput, byte tapSensitivity)
        {
            if (tappedOnce)
            {
                DateTime tester = _time.GetUtcNow().UtcDateTime;
                if (tester > (timeofEnd + TimeSpan.FromMilliseconds((double)tapSensitivity * 1.5)))
                {
                    StartTapClickHold();
                    tappedOnce = false;
                }
                //if it fails the method resets, and tries again with a new tester value (gives tap a delay so tap and hold can work)
            }

            // タップのクリックを保持時間の間、押したままにする（毎フレーム。state は毎フレーム消去されるため）
            if (_time.GetUtcNow().UtcDateTime < tapClickHoldUntil)
            {
                syntheticInput.CountClick(deviceNum, Click.Left);
            }
        }
    }
}
