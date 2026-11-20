using DS4Windows.Core.Model;
using System;
using System.Collections.Generic;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Settings;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Output.Services
{
    /// <summary>
    /// <see cref="IOscService"/> の実装（DI の Singleton）。
    /// 旧 <c>ControlService</c> の OSC 系（受信の解釈 <c>CreateOSCCallback</c>、リスナー／送信側のライフサイクル、<c>SendOsc</c>、<c>OSCPreMappingStep</c>／<c>OSCPostMappingStep</c>、
    /// <c>CompareAndSendChangesToOSC</c>、状態 <c>oscState</c>）を、そのまま移したもの（SharpOSC の型は <see cref="IOscTransportFactory"/> の実装の中に閉じた。送信する値は旧実装と同じ型のまま渡す）。
    /// 値の特性化は 対応するテスト（旧実装との差分テスト）が守る。
    /// 現行の癖: リスナーが null のときの停止は <see cref="NullReferenceException"/>（計画書 E3。呼び出し側が <see cref="HasListener"/> を確かめてから呼ぶ）。
    /// 範囲外のコントローラー番号・値・不正なメッセージ（計画書 E1・E2）は、旧実装では例外だったが、ユーザー承認により是正した（範囲外の番号は捨てる／値は 0〜255 に丸める／不正なメッセージはログに出して捨てる）。
    /// </summary>
    public sealed partial class OscService : IOscService
    {
        private readonly IAppSettingsService _appSettings;
        private readonly IOscTransportFactory _transportFactory;
        private readonly ControllerState[] _oscState = new ControllerState[ControllerLimits.MaxControllerCount];

        private IOscListenerHandle? _listener;
        private IOscMessageSender? _sender;

        public OscService(IAppSettingsService appSettings, IOscTransportFactory transportFactory)
        {
            ArgumentNullException.ThrowIfNull(appSettings);
            _appSettings = appSettings;
            ArgumentNullException.ThrowIfNull(transportFactory);
            _transportFactory = transportFactory;
            for (int i = 0; i < _oscState.Length; i++)
            {
                _oscState[i] = new ControllerState();
            }
        }

        public bool HasListener => _listener != null;

        public bool HasSender => _sender != null;

        public void ResetDeviceState(int index)
        {
            _oscState[index] = new ControllerState();
        }

        public void ChangeListenerStatus(bool state)
        {
            if (state)
            {
                _listener = _transportFactory.CreateListener(_appSettings.OscServerPort, HandleMessage);

                AppLogger.LogUser(AppLogLevel.Info, "Log_Osc_ListenerStarted", _appSettings.OscServerPort);
            }
            else
            {
                _listener!.Close(); // Start 済みのときだけ呼ぶ（null なら従来どおり NullReferenceException）
                _listener = null;
                AppLogger.LogUser(AppLogLevel.Info, "Log_Osc_ListenerStopped");
            }
        }

        public void ChangeSenderStatus(bool state)
        {
            if (state)
            {
                AppLogger.LogUser(AppLogLevel.Info, "Log_Osc_SenderStarted", _appSettings.OscSenderAddress, _appSettings.OscSenderPort);
                _sender = _transportFactory.CreateSender(_appSettings.OscSenderAddress, _appSettings.OscSenderPort);
            }
            else
            {
                AppLogger.LogUser(AppLogLevel.Info, "Log_Osc_SenderStopped");
                // フィールドを先に null にしてから閉じる（入力スレッドが閉じたソケットへ送らないようにする）
                IOscMessageSender? sender = _sender;
                if (sender == null) { return; }
                _sender = null;
                sender.Close();
            }
        }

        public void SendPlug(int index, bool plugged)
        {
            SendOsc("/ds4windows/monitor/" + index + "/plug", plugged ? 1 : 0);
        }

        // UseOscSender が先に true になり、_sender の生成（ChangeSenderStatus）が
        // 終わる前に入力スレッドが送信すると NullReferenceException でアプリが終了していた（以前から存在した競合）。
        // 送信側が未生成・破棄済みの間は送信しない。フィールドをローカルに取り、取得後に null になっても安全にする。
        private void SendOsc(string address, object value)
        {
            IOscMessageSender? sender = _sender;
            sender?.Send(address, value);
        }

        public void PostMappingStep(int ind, ControllerState tempMapState)
        {
            ControllerState oscMapState = _oscState[ind];
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

        public void PreMappingStep(int ind, ControllerState cState, ControllerState tempMapState)
        {
            ControllerState oscMapState = _oscState[ind];
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

        private void CompareAndSendChanges(int index, ControllerState oldState, ControllerState newState)
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
