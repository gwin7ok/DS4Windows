using DS4Windows.Core.Model;
using System;
using System.Collections.Generic;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Settings;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Output.Services
{
    /// <summary>受信した OSC メッセージの解釈とコマンドの実行。</summary>
    public partial class OscService
    {
        private string[] MapMonitoringOscMessageToCommand(string[] command)
        {
            // Overwrite "monitor" with the controller Id
            command[2] = command[3];

            switch (command[4])
            {
                case "battery":
                    command[3] = "battery";
                    break;
                case "l2":
                case "r2":
                    command[3] = "trigger";
                    break;
                case "rx":
                case "ry":
                case "lx":
                case "ly":
                    command[3] = "stick";
                    break;
                default:
                    command[3] = "press";
                    break;
            }

            return command;
        }

        public void HandleMessage(string address, IList<object> arguments)
        {
            string[]? command = null;
            try
            {
                command = address.Split("/");
            }
            catch (Exception e)
            {
                AppLogger.Log(AppLogLevel.Info, "Error Receiving OSC Message: " + e.Message, LogOptions.Temporary);
            }

            if (command == null)
            {
                return;
            }

            // 不正なメッセージ（アドレスの要素不足、引数の数・型の不一致、NaN など）で、例外を外へ出さない。
            // 旧実装は、受信スレッドで例外が出ていた（スレッドが落ちて、アプリの異常終了になりうる）。ログに出して捨てる。
            try
            {
                HandleCommand(command, address, arguments);
            }
            catch (Exception ex)
            {
                AppLogger.LogUser(AppLogLevel.Warn, "Log_Osc_MalformedMessage", address, ex.GetType().Name, ex.Message);
            }
        }

        /// <summary>
        /// 値（0.0〜255.0 の想定）を 0〜255 の byte にする。範囲外は端に丸める（旧実装は <see cref="OverflowException"/>）。
        /// 範囲内の値は、旧実装（<c>Convert.ToByte</c>。偶数丸め）と同じ結果になる。NaN は丸められないため <see cref="OverflowException"/>（呼び出し側が捨てる）。
        /// </summary>
        private static byte ToByteClamped(float value)
        {
            if (float.IsNaN(value))
            {
                throw new OverflowException("NaN is not a valid OSC value");
            }

            if (value >= 255.0f) return 255;
            if (value <= 0.0f) return 0;
            return Convert.ToByte(value);
        }

        private void HandleCommand(string[] command, string address, IList<object> arguments)
        {
            if (command[1] != "ds4windows")
            {
                return;
            }

            if (command[2] == "monitor")
            {
                if (_appSettings.InterpretingOscMonitoring)
                {
                    command = MapMonitoringOscMessageToCommand(command);
                }
                else
                {
                    return;
                }
            }

            int stateInd = -1;
            if (!int.TryParse(command[2], out stateInd))
            {
                stateInd = -1;
            }

            // 範囲外のコントローラー番号（負、スロット数以上）も不正なアドレスとして捨てる（旧実装は IndexOutOfRangeException）
            if (stateInd < 0 || stateInd >= _oscState.Length)
            {
                AppLogger.LogUser(AppLogLevel.Warn, "Log_Osc_MalformedAddress", address);
                return;
            }

            if (command[3] == "battery")
            {
                if (!_appSettings.UseOscSender)
                {
                    AppLogger.LogUser(AppLogLevel.Warn, "Log_Osc_SenderInactive");
                }
                else
                {
                    SendOsc("/ds4windows/monitor/" + stateInd + "/battery", _oscState[stateInd].Battery);
                }
                return;
            }
            else if (command[3] == "press")
            {
                int messageValue = Convert.ToInt32(arguments[0]);
                bool buttonBool = messageValue == 1 ? true : false;

                switch (command[4])
                {
                    case "cross":
                        _oscState[stateInd].Cross = buttonBool;
                        break;
                    case "square":
                        _oscState[stateInd].Square = buttonBool;
                        break;
                    case "circle":
                        _oscState[stateInd].Circle = buttonBool;
                        break;
                    case "triangle":
                        _oscState[stateInd].Triangle = buttonBool;
                        break;
                    case "r1":
                        _oscState[stateInd].R1 = buttonBool;
                        break;
                    case "r2":
                        _oscState[stateInd].R2 = Convert.ToByte(buttonBool ? 255 : 0);
                        break;
                    case "r3":
                        _oscState[stateInd].R3 = buttonBool;
                        break;
                    case "l1":
                        _oscState[stateInd].L1 = buttonBool;
                        break;
                    case "l2":
                        _oscState[stateInd].L2 = Convert.ToByte(buttonBool ? 255 : 0);
                        break;
                    case "l3":
                        _oscState[stateInd].L3 = buttonBool;
                        break;
                    case "dpadup":
                    case "dup":
                        _oscState[stateInd].DpadUp = buttonBool;
                        break;
                    case "dpaddown":
                    case "ddown":
                        _oscState[stateInd].DpadDown = buttonBool;
                        break;
                    case "dpadleft":
                    case "dleft":
                        _oscState[stateInd].DpadLeft = buttonBool;
                        break;
                    case "dpadright":
                    case "dright":
                        _oscState[stateInd].DpadRight = buttonBool;
                        break;
                    case "options":
                        _oscState[stateInd].Options = buttonBool;
                        break;
                    case "share":
                        _oscState[stateInd].Share = buttonBool;
                        break;
                }
            }
            else if (command[3] == "stick" && arguments.Count == 1)
            {
                switch (command[4])
                {
                    case "lx":
                        _oscState[stateInd].LX = ToByteClamped(Convert.ToSingle(arguments[0]));
                        break;
                    case "ly":
                        _oscState[stateInd].LY = ToByteClamped(Convert.ToSingle(arguments[0]));
                        break;
                    case "rx":
                        _oscState[stateInd].RX = ToByteClamped(Convert.ToSingle(arguments[0]));
                        break;
                    case "ry":
                        _oscState[stateInd].RY = ToByteClamped(Convert.ToSingle(arguments[0]));
                        break;
                }
            }
            else if (command[3] == "stick" && arguments.Count == 2)
            {
                float xValue = Convert.ToSingle(arguments[0]);
                float yValue = Convert.ToSingle(arguments[1]);

                if (command[4] == "left")
                {
                    // 先に 2 つとも変換してから代入する（片方だけ更新されるのを避ける）
                    byte lx = ToByteClamped(xValue * 255);
                    byte ly = ToByteClamped(yValue * 255);
                    _oscState[stateInd].LX = lx;
                    _oscState[stateInd].LY = ly;
                }
                else if (command[4] == "right")
                {
                    byte rx = ToByteClamped(xValue * 255);
                    byte ry = ToByteClamped(yValue * 255);
                    _oscState[stateInd].RX = rx;
                    _oscState[stateInd].RY = ry;
                }
            }
            else if (command[3] == "trigger")
            {
                switch (command[4])
                {
                    case "r2":
                        _oscState[stateInd].R2 = ToByteClamped(Convert.ToSingle(arguments[0]));
                        break;
                    case "l2":
                        _oscState[stateInd].L2 = ToByteClamped(Convert.ToSingle(arguments[0]));
                        break;
                }
            }
        }
    }
}
