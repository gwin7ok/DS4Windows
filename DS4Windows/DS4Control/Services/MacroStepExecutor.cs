using System;
using DS4Windows.DI;
using DS4Windows.DS4Control;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase9 9-C（Phase9-C-Plan.md）: <see cref="IMacroStepExecutor"/> の実装（DI の Singleton）。
    /// 旧 <c>Mapping.PlayMacroCodeValue</c> を、そのまま移したもの（キー・マウスボタンの送出は <c>IVirtualKBM</c>、設定は <c>IProfileSettingsService</c>、
    /// ゲームパッドのボタンの出力フラグは <c>MacroRuntimeState</c> へ。フラグと個数は、ユーザー判断（Q2）によりデバイスごとに持つ）。
    /// 値の特性化は <c>Phase9CMacroStepExecutorDifferentialTests</c>（旧実装との差分テスト）が守る。
    /// TODO(技術的負債・Phase9 9-E 付近): ライトバーの強制色（<c>DS4LightBar.forcelight</c>／<c>forcedColor</c>／<c>forcedFlash</c>）は、旧実装どおり静的な配列へ直接書く。
    /// ライトバーのサービス化（<c>ILightbarService</c>）のときに、窓口経由へ置き換える。
    /// </summary>
    public sealed class MacroStepExecutor : IMacroStepExecutor
    {
        private readonly IVirtualKBM _virtualKbm;
        private readonly IProfileSettingsService _profileSettings;
        private readonly MacroRuntimeState _macroRuntime;
        // ControlService が IDeviceStateAccessor の実装者のため、循環を避けて遅延で取得する（マクロの振動イベントで使う）
        private readonly Func<IDeviceStateAccessor> _deviceStateAccessor;

        public MacroStepExecutor(IVirtualKBM virtualKbm, IProfileSettingsService profileSettings,
            MacroRuntimeState macroRuntime, Func<IDeviceStateAccessor> deviceStateAccessor)
        {
            _virtualKbm = virtualKbm ?? throw new ArgumentNullException(nameof(virtualKbm));
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            _macroRuntime = macroRuntime ?? throw new ArgumentNullException(nameof(macroRuntime));
            _deviceStateAccessor = deviceStateAccessor ?? throw new ArgumentNullException(nameof(deviceStateAccessor));
        }

        public bool Execute(int device, DS4KeyType keyType, int macroCodeValue, bool[] keydown)
        {
            // Phase6-Step3-6c（決定P2）: 非同期マクロ経路（別スレッド、ctrl を持たない連鎖）のため、Mapping が保持する profileSettings から読む。
            // Phase7-Step8 W2 で instance 化（コンストラクター注入）済み。この経路も instance の profileSettings を読む（Phase7c W3 でコメントを訂正）。
            bool doDelayOnCaller = false;
            if (macroCodeValue >= 261 && macroCodeValue <= DS4ControlSettings.MAX_MACRO_VALUE)
            {
                // Gamepad button up or down macro event. macroCodeValue index value is the button identifier (codeValue-261 = idx in 0..24 range)
                if (!keydown[macroCodeValue])
                {
                    _macroRuntime.MacroControl[device][macroCodeValue - 261] = keydown[macroCodeValue] = true;
                    _macroRuntime.MacroCount[device]++;
                }
                else
                {
                    _macroRuntime.MacroControl[device][macroCodeValue - 261] = keydown[macroCodeValue] = false;
                    if (_macroRuntime.MacroCount[device] > 0) _macroRuntime.MacroCount[device]--;
                }
            }
            else if (macroCodeValue < 300)
            {
                // Keyboard key or mouse button macro event
                // Phase9 9-C（Q4）: 旧・押下と離上の 2 つの switch（マウスボタン 5 種、キー、スキャンコード）は、SendKeyOrMouseButton にまとめた。
                // 送出するイベント、その順序、ログの文言は変えていない（旧実装との一致は Phase9CMacroStepExecutorDifferentialTests が守る）。
                if (!keydown[macroCodeValue])
                {
                    SendKeyOrMouseButton(device, keyType, macroCodeValue, down: true);
                    keydown[macroCodeValue] = true;
                }
                else
                {
                    SendKeyOrMouseButton(device, keyType, macroCodeValue, down: false);
                    keydown[macroCodeValue] = false;
                }
            }
            else if (macroCodeValue >= 1000000000)
            {
                // Lightbar color event
                if (macroCodeValue > 1000000000)
                {
                    string lb = macroCodeValue.ToString().Substring(1);
                    byte r = (byte)(int.Parse(lb[0].ToString()) * 100 + int.Parse(lb[1].ToString()) * 10 + int.Parse(lb[2].ToString()));
                    byte g = (byte)(int.Parse(lb[3].ToString()) * 100 + int.Parse(lb[4].ToString()) * 10 + int.Parse(lb[5].ToString()));
                    byte b = (byte)(int.Parse(lb[6].ToString()) * 100 + int.Parse(lb[7].ToString()) * 10 + int.Parse(lb[8].ToString()));
                    DS4LightBar.forcelight[device] = true;
                    DS4LightBar.forcedFlash[device] = 0;
                    DS4LightBar.forcedColor[device] = new DS4Color(r, g, b);
                }
                else
                {
                    DS4LightBar.forcedFlash[device] = 0;
                    DS4LightBar.forcelight[device] = false;
                }
            }
            else if (macroCodeValue >= 1000000)
            {
                // Rumble event
                DS4Device d = null;
                try
                {
                    var accessor = _deviceStateAccessor();
                    if (accessor != null) d = accessor.GetController(device);
                }
                catch { }
                // Phase7-Step5 W2: IDeviceStateAccessor は ControlService 自身の登録のため、旧 Program.rootHub のフォールバックは冗長だった
                string r = macroCodeValue.ToString().Substring(1);
                byte heavy = (byte)(int.Parse(r[0].ToString()) * 100 + int.Parse(r[1].ToString()) * 10 + int.Parse(r[2].ToString()));
                byte light = (byte)(int.Parse(r[3].ToString()) * 100 + int.Parse(r[4].ToString()) * 10 + int.Parse(r[5].ToString()));
                if (_profileSettings.InverseRumbleMotors[device])
                    d.setRumble(heavy, light);
                else
                    d.setRumble(light, heavy);
            }
            else
            {
                // Delay specification. Indicate to caller that it should do a delay of macroCodeValue-300 msecs
                doDelayOnCaller = true;
            }

            return doDelayOnCaller;
        }

        /// <summary>キー（0〜255）またはマウスボタン（256〜260）の押下／離上を送出する。旧 <c>PlayMacroCodeValue</c> の、押下と離上の 2 つの switch の共通部分。</summary>
        private void SendKeyOrMouseButton(int device, DS4KeyType keyType, int macroCodeValue, bool down)
        {
            VirtualKBMMapping kbm = _profileSettings.OutputKBMMapping;
            switch (macroCodeValue)
            {
                //anything above 255 is not a keyvalue
                case 256:
                    SendMouseButton(device, macroCodeValue, down ? "MouseLeftDown" : "MouseLeftUp",
                        down ? kbm.MOUSEEVENTF_LEFTDOWN : kbm.MOUSEEVENTF_LEFTUP);
                    break;
                case 257:
                    SendMouseButton(device, macroCodeValue, down ? "MouseRightDown" : "MouseRightUp",
                        down ? kbm.MOUSEEVENTF_RIGHTDOWN : kbm.MOUSEEVENTF_RIGHTUP);
                    break;
                case 258:
                    SendMouseButton(device, macroCodeValue, down ? "MouseMiddleDown" : "MouseMiddleUp",
                        down ? kbm.MOUSEEVENTF_MIDDLEDOWN : kbm.MOUSEEVENTF_MIDDLEUP);
                    break;
                case 259:
                    SendMouseXButton(device, macroCodeValue, down ? "MouseXButtonDown btn=1" : "MouseXButtonUp btn=1",
                        down ? kbm.MOUSEEVENTF_XBUTTONDOWN : kbm.MOUSEEVENTF_XBUTTONUP, 1);
                    break;
                case 260:
                    SendMouseXButton(device, macroCodeValue, down ? "MouseXButtonDown btn=2" : "MouseXButtonUp btn=2",
                        down ? kbm.MOUSEEVENTF_XBUTTONDOWN : kbm.MOUSEEVENTF_XBUTTONUP, 2);
                    break;

                default:
                    uint eventMacroCode = !kbm.macroKeyTranslate ? (uint)macroCodeValue :
                        kbm.GetRealEventKey((uint)macroCodeValue);
                    bool scanCode = keyType.HasFlag(DS4KeyType.ScanCode);

                    if (down)
                    {
                        AppLogger.LogDebug($"EVENT SENT [INLINE] device={device} macroCode={macroCodeValue} event={(scanCode ? "KeyPressAlt" : "KeyPress")} native={eventMacroCode}");
                        if (scanCode) _virtualKbm.PerformKeyPressAlt(eventMacroCode);
                        else _virtualKbm.PerformKeyPress(eventMacroCode);
                    }
                    else
                    {
                        long _deltaSendDbg = 0; // inline macro release: lastSyntheticSend not tracked here
                        AppLogger.LogTrace($"INLINE RELEASE TRACE device={device} macroCode={macroCodeValue} native={eventMacroCode} lastSendDeltaMs={_deltaSendDbg}");
                        AppLogger.LogDebug($"EVENT SENT [INLINE] device={device} macroCode={macroCodeValue} event={(scanCode ? "KeyReleaseAlt" : "KeyRelease")} native={eventMacroCode}");
                        if (scanCode) _virtualKbm.PerformKeyReleaseAlt(eventMacroCode);
                        else _virtualKbm.PerformKeyRelease(eventMacroCode);
                    }

                    break;
            }
        }

        private void SendMouseButton(int device, int macroCodeValue, string eventName, uint mouseButton)
        {
            AppLogger.LogDebug($"EVENT SENT [INLINE] device={device} macroCode={macroCodeValue} event={eventName}");
            _virtualKbm.PerformMouseButtonEvent(mouseButton);
        }

        private void SendMouseXButton(int device, int macroCodeValue, string eventName, uint mouseButton, int xButton)
        {
            AppLogger.LogDebug($"EVENT SENT [INLINE] device={device} macroCode={macroCodeValue} event={eventName}");
            _virtualKbm.PerformMouseButtonEventAlt(mouseButton, xButton);
        }
    }
}
