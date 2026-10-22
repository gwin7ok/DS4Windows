using System;
using System.Collections.Generic;
using DS4Windows.DI;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase9 9-E E-1（Phase9-E-Plan.md）: <see cref="IOscService"/> の実装（DI の Singleton）。
    /// 旧 <c>ControlService</c> の OSC 系（受信の解釈 <c>CreateOSCCallback</c>、リスナー／送信側のライフサイクル、<c>SendOsc</c>、<c>OSCPreMappingStep</c>／<c>OSCPostMappingStep</c>、
    /// <c>CompareAndSendChangesToOSC</c>、状態 <c>oscState</c>）を、そのまま移したもの（SharpOSC の型は <see cref="IOscTransportFactory"/> の実装の中に閉じた。送信する値は旧実装と同じ型のまま渡す）。
    /// 値の特性化は <c>Phase9EOscServiceDifferentialTests</c>（旧実装との差分テスト）が守る。
    /// 現行の癖: リスナーが null のときの停止は <see cref="NullReferenceException"/>（計画書 E3。呼び出し側が <see cref="HasListener"/> を確かめてから呼ぶ）。
    /// 範囲外のコントローラー番号・値・不正なメッセージ（計画書 E1・E2）は、旧実装では例外だったが、ユーザー承認により是正した（範囲外の番号は捨てる／値は 0〜255 に丸める／不正なメッセージはログに出して捨てる）。
    /// </summary>
    public sealed class OscService : IOscService
    {
        private readonly IAppSettingsService _appSettings;
        private readonly IOscTransportFactory _transportFactory;
        private readonly DS4State[] _oscState = new DS4State[ControllerLimits.MaxControllerCount];

        private IOscListenerHandle _listener;
        private IOscMessageSender _sender;

        public OscService(IAppSettingsService appSettings, IOscTransportFactory transportFactory)
        {
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _transportFactory = transportFactory ?? throw new ArgumentNullException(nameof(transportFactory));
            for (int i = 0; i < _oscState.Length; i++)
            {
                _oscState[i] = new DS4State();
            }
        }

        public bool HasListener => _listener != null;

        public bool HasSender => _sender != null;

        public void ResetDeviceState(int index)
        {
            _oscState[index] = new DS4State();
        }

        public void ChangeListenerStatus(bool state)
        {
            if (state)
            {
                _listener = _transportFactory.CreateListener(_appSettings.OscServerPort, HandleMessage);

                AppLogger.LogToGui("OSC LISTENER STARTED AT PORT: " + _appSettings.OscServerPort, false);
            }
            else
            {
                _listener.Close();
                _listener = null;
                AppLogger.LogToGui("OSC LISTENER STOPPED", false);
            }
        }

        public void ChangeSenderStatus(bool state)
        {
            if (state)
            {
                AppLogger.LogToGui("OSC SENDER STARTED AT IP: " + _appSettings.OscSenderAddress + " PORT: " + _appSettings.OscSenderPort, false);
                _sender = _transportFactory.CreateSender(_appSettings.OscSenderAddress, _appSettings.OscSenderPort);
            }
            else
            {
                AppLogger.LogToGui("OSC SENDER STOPPED", false);
                // Phase6-Step9-3c: フィールドを先に null にしてから閉じる（入力スレッドが閉じたソケットへ送らないようにする）
                IOscMessageSender sender = _sender;
                if (sender == null) { return; }
                _sender = null;
                sender.Close();
            }
        }

        public void SendPlug(int index, bool plugged)
        {
            SendOsc("/ds4windows/monitor/" + index + "/plug", plugged ? 1 : 0);
        }

        // Phase6-Step9-3c（K9-1）: UseOscSender が先に true になり、_sender の生成（ChangeSenderStatus）が
        // 終わる前に入力スレッドが送信すると NullReferenceException でアプリが終了していた（Step2 より前から存在した競合）。
        // 送信側が未生成・破棄済みの間は送信しない。フィールドをローカルに取り、取得後に null になっても安全にする。
        private void SendOsc(string address, object value)
        {
            IOscMessageSender sender = _sender;
            sender?.Send(address, value);
        }

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
            string[] command = null;
            try
            {
                command = address.Split("/");
            }
            catch (Exception e)
            {
                AppLogger.LogToGui("Error Receiving OSC Message: " + e.Message, false, true);
            }

            if (command == null)
            {
                return;
            }

            // Phase9 9-E E-1 の是正（ユーザー承認）: 不正なメッセージ（アドレスの要素不足、引数の数・型の不一致、NaN など）で、例外を外へ出さない。
            // 旧実装は、受信スレッドで例外が出ていた（スレッドが落ちて、アプリの異常終了になりうる）。ログに出して捨てる。
            try
            {
                HandleCommand(command, address, arguments);
            }
            catch (Exception ex)
            {
                AppLogger.LogToGui("Received malformed OSC message: " + address + " (" + ex.GetType().Name + ": " + ex.Message + ")", false);
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

            // Phase9 9-E E-1 の是正: 範囲外のコントローラー番号（負、スロット数以上）も不正なアドレスとして捨てる（旧実装は IndexOutOfRangeException）
            if (stateInd < 0 || stateInd >= _oscState.Length)
            {
                AppLogger.LogToGui("Received malformed OSC address: " + address, false);
                return;
            }

            if (command[3] == "battery")
            {
                if (!_appSettings.UseOscSender)
                {
                    AppLogger.LogToGui("Battery level requested, but the OSC Sender isn't active. Turn it on in Settings.", false);
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

        public void PostMappingStep(int ind, DS4State tempMapState)
        {
            DS4State oscMapState = _oscState[ind];
            tempMapState.Cross |= oscMapState.Cross;
            tempMapState.Square |= oscMapState.Square;
            tempMapState.Circle |= oscMapState.Circle;
            tempMapState.Triangle |= oscMapState.Triangle;
            tempMapState.R1 |= oscMapState.R1;
            tempMapState.R3 |= oscMapState.R3;
            tempMapState.L1 |= oscMapState.L1;
            tempMapState.L3 |= oscMapState.L3;
            tempMapState.DpadUp |= oscMapState.DpadUp;
            tempMapState.DpadLeft |= oscMapState.DpadLeft;
            tempMapState.DpadRight |= oscMapState.DpadRight;
            tempMapState.DpadDown |= oscMapState.DpadDown;
            tempMapState.Options |= oscMapState.Options;
            tempMapState.Share |= oscMapState.Share;

            tempMapState.LX = oscMapState.LX != 128 ? oscMapState.LX : tempMapState.LX;
            tempMapState.LY = oscMapState.LY != 128 ? oscMapState.LY : tempMapState.LY;
            tempMapState.L2 = oscMapState.L2 != 0 ? oscMapState.L2 : tempMapState.L2;
            tempMapState.RX = oscMapState.RX != 128 ? oscMapState.RX : tempMapState.RX;
            tempMapState.RY = oscMapState.RY != 128 ? oscMapState.RY : tempMapState.RY;
            tempMapState.R2 = oscMapState.R2 != 0 ? oscMapState.R2 : tempMapState.R2;
        }

        public void PreMappingStep(int ind, DS4State cState, DS4State tempMapState)
        {
            DS4State oscMapState = _oscState[ind];
            if (cState.Battery != oscMapState.Battery)
            {
                SendOsc("/ds4windows/monitor/" + ind + "/battery", Convert.ToInt32(cState.Battery));
                oscMapState.Battery = cState.Battery;
            }
            cState.Cross |= oscMapState.Cross;
            cState.Square |= oscMapState.Square;
            cState.Circle |= oscMapState.Circle;
            cState.Triangle |= oscMapState.Triangle;
            cState.R1 |= oscMapState.R1;
            cState.R3 |= oscMapState.R3;
            cState.L1 |= oscMapState.L1;
            cState.L3 |= oscMapState.L3;
            cState.DpadUp |= oscMapState.DpadUp;
            cState.DpadLeft |= oscMapState.DpadLeft;
            cState.DpadRight |= oscMapState.DpadRight;
            cState.DpadDown |= oscMapState.DpadDown;
            cState.Options |= oscMapState.Options;
            cState.Share |= oscMapState.Share;

            cState.LX = oscMapState.LX != 128 ? oscMapState.LX : cState.LX;
            cState.LY = oscMapState.LY != 128 ? oscMapState.LY : cState.LY;
            cState.L2 = oscMapState.L2 != 0 ? oscMapState.L2 : cState.L2;
            cState.RX = oscMapState.RX != 128 ? oscMapState.RX : cState.RX;
            cState.RY = oscMapState.RY != 128 ? oscMapState.RY : cState.RY;
            cState.R2 = oscMapState.R2 != 0 ? oscMapState.R2 : cState.R2;

            CompareAndSendChanges(ind, tempMapState, cState);
        }

        private void CompareAndSendChanges(int index, DS4State oldState, DS4State newState)
        {
            // Buttons
            if (oldState.Square != newState.Square)
            {
                SendOsc("/ds4windows/monitor/" + index + "/square", newState.Square == true ? 1 : 0);
            }

            if (oldState.Triangle != newState.Triangle)
            {
                SendOsc("/ds4windows/monitor/" + index + "/triangle", newState.Triangle == true ? 1 : 0);
            }

            if (oldState.Circle != newState.Circle)
            {
                SendOsc("/ds4windows/monitor/" + index + "/circle", newState.Circle == true ? 1 : 0);
            }

            if (oldState.Cross != newState.Cross)
            {
                SendOsc("/ds4windows/monitor/" + index + "/cross", newState.Cross == true ? 1 : 0);
            }

            if (oldState.DpadUp != newState.DpadUp)
            {
                SendOsc("/ds4windows/monitor/" + index + "/dpadup", newState.DpadUp == true ? 1 : 0);
            }

            if (oldState.DpadDown != newState.DpadDown)
            {
                SendOsc("/ds4windows/monitor/" + index + "/dpaddown", newState.DpadDown == true ? 1 : 0);
            }

            if (oldState.DpadLeft != newState.DpadLeft)
            {
                SendOsc("/ds4windows/monitor/" + index + "/dpadleft", newState.DpadLeft == true ? 1 : 0);
            }

            if (oldState.DpadRight != newState.DpadRight)
            {
                SendOsc("/ds4windows/monitor/" + index + "/dpadright", newState.DpadRight == true ? 1 : 0);
            }

            if (oldState.L1 != newState.L1)
            {
                SendOsc("/ds4windows/monitor/" + index + "/l1", newState.L1 == true ? 1 : 0);
            }

            if (oldState.L3 != newState.L3)
            {
                SendOsc("/ds4windows/monitor/" + index + "/l3", newState.L3 == true ? 1 : 0);
            }

            if (oldState.R1 != newState.R1)
            {
                SendOsc("/ds4windows/monitor/" + index + "/r1", newState.R1 == true ? 1 : 0);
            }

            if (oldState.R3 != newState.R3)
            {
                SendOsc("/ds4windows/monitor/" + index + "/r3", newState.R3 == true ? 1 : 0);
            }

            if (oldState.Options != newState.Options)
            {
                SendOsc("/ds4windows/monitor/" + index + "/options", newState.Options == true ? 1 : 0);
            }

            if (oldState.Share != newState.Share)
            {
                SendOsc("/ds4windows/monitor/" + index + "/share", newState.Share == true ? 1 : 0);
            }

            if (oldState.PS != newState.PS)
            {
                SendOsc("/ds4windows/monitor/" + index + "/ps", newState.PS == true ? 1 : 0);
            }

            // Sticks
            if (oldState.LX != newState.LX)
            {
                SendOsc("/ds4windows/monitor/" + index + "/lx", Convert.ToInt32(newState.LX));
            }

            if (oldState.LY != newState.LY)
            {
                SendOsc("/ds4windows/monitor/" + index + "/ly", Convert.ToInt32(newState.LY));
            }

            if (oldState.RX != newState.RX)
            {
                SendOsc("/ds4windows/monitor/" + index + "/rx", Convert.ToInt32(newState.RX));
            }

            if (oldState.RY != newState.RY)
            {
                SendOsc("/ds4windows/monitor/" + index + "/ry", Convert.ToInt32(newState.RY));
            }

            // Triggers
            if (oldState.L2 != newState.L2)
            {
                SendOsc("/ds4windows/monitor/" + index + "/l2", Convert.ToInt32(newState.L2));
            }

            if (oldState.R2 != newState.R2)
            {
                SendOsc("/ds4windows/monitor/" + index + "/r2", Convert.ToInt32(newState.R2));
            }

            // if (oldState.Battery != newState.Battery)
            // {
            //     AppLogger.LogToGui("BATTERY " + oldState.Battery + " : " + newState.Battery, false);
            //     SendOsc("/ds4windows/monitor/" + index + "/battery", Convert.ToInt32(newState.Battery));
            // }
        }
    }
}
