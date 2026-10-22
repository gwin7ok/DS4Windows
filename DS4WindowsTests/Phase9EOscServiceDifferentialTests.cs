using System;
using System.Collections.Generic;
using System.Linq;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase9 9-E E-1（Phase9-E-Plan.md）: <see cref="OscService"/> を、旧実装（<c>ControlService</c> の OSC 系を逐語的に写したもの。
    /// 送信は <c>SendOsc(アドレス, 値)</c> に直し、設定の 2 つのフラグはフィールドにした）と<b>突き合わせる差分テスト</b>。
    /// 受信の解釈（ボタン、スティック、トリガー、バッテリー、監視形式、不正なアドレス、範囲外の例外）、マッピング前後の重ね合わせ、変化の送信（全フィールド）、
    /// リスナー／送信側のライフサイクルを調べる。旧実装の写しは、Phase9 の後のテスト整理まで残す。
    /// </summary>
    public class Phase9EOscServiceDifferentialTests
    {
        /// <summary>旧 <c>ControlService</c> の OSC 系の逐語的な写し。</summary>
        private sealed class OldOsc
        {
            public readonly DS4State[] oscState = new DS4State[ControllerLimits.MaxControllerCount];
            public readonly List<(string Address, object Value)> Sent = new List<(string, object)>();
            public bool InterpretingOscMonitoring;
            public bool UseOscSender;

            public OldOsc()
            {
                for (int i = 0; i < oscState.Length; i++) oscState[i] = new DS4State();
            }

            private void SendOsc(string address, object value)
            {
                Sent.Add((address, value));
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

            public void Callback(string address, IList<object> arguments)
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

                if (command[1] != "ds4windows")
                {
                    return;
                }

                if (command[2] == "monitor")
                {
                    if (InterpretingOscMonitoring)
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

                if (stateInd == -1)
                {
                    AppLogger.LogToGui("Received malformed OSC address: " + address, false);
                    return;
                }

                if (command[3] == "battery")
                {
                    if (!UseOscSender)
                    {
                        AppLogger.LogToGui("Battery level requested, but the OSC Sender isn't active. Turn it on in Settings.", false);
                    }
                    else
                    {
                        SendOsc("/ds4windows/monitor/" + stateInd + "/battery", oscState[stateInd].Battery);
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
                            oscState[stateInd].Cross = buttonBool;
                            break;
                        case "square":
                            oscState[stateInd].Square = buttonBool;
                            break;
                        case "circle":
                            oscState[stateInd].Circle = buttonBool;
                            break;
                        case "triangle":
                            oscState[stateInd].Triangle = buttonBool;
                            break;
                        case "r1":
                            oscState[stateInd].R1 = buttonBool;
                            break;
                        case "r2":
                            oscState[stateInd].R2 = Convert.ToByte(buttonBool ? 255 : 0);
                            break;
                        case "r3":
                            oscState[stateInd].R3 = buttonBool;
                            break;
                        case "l1":
                            oscState[stateInd].L1 = buttonBool;
                            break;
                        case "l2":
                            oscState[stateInd].L2 = Convert.ToByte(buttonBool ? 255 : 0);
                            break;
                        case "l3":
                            oscState[stateInd].L3 = buttonBool;
                            break;
                        case "dpadup":
                        case "dup":
                            oscState[stateInd].DpadUp = buttonBool;
                            break;
                        case "dpaddown":
                        case "ddown":
                            oscState[stateInd].DpadDown = buttonBool;
                            break;
                        case "dpadleft":
                        case "dleft":
                            oscState[stateInd].DpadLeft = buttonBool;
                            break;
                        case "dpadright":
                        case "dright":
                            oscState[stateInd].DpadRight = buttonBool;
                            break;
                        case "options":
                            oscState[stateInd].Options = buttonBool;
                            break;
                        case "share":
                            oscState[stateInd].Share = buttonBool;
                            break;
                    }
                }
                else if (command[3] == "stick" && arguments.Count == 1)
                {
                    switch (command[4])
                    {
                        case "lx":
                            oscState[stateInd].LX = Convert.ToByte(Convert.ToSingle(arguments[0]));
                            break;
                        case "ly":
                            oscState[stateInd].LY = Convert.ToByte(Convert.ToSingle(arguments[0]));
                            break;
                        case "rx":
                            oscState[stateInd].RX = Convert.ToByte(Convert.ToSingle(arguments[0]));
                            break;
                        case "ry":
                            oscState[stateInd].RY = Convert.ToByte(Convert.ToSingle(arguments[0]));
                            break;
                    }
                }
                else if (command[3] == "stick" && arguments.Count == 2)
                {
                    float xValue = Convert.ToSingle(arguments[0]);
                    float yValue = Convert.ToSingle(arguments[1]);

                    if (command[4] == "left")
                    {
                        oscState[stateInd].LX = Convert.ToByte(xValue * 255);
                        oscState[stateInd].LY = Convert.ToByte(yValue * 255);
                    }
                    else if (command[4] == "right")
                    {
                        oscState[stateInd].RX = Convert.ToByte(xValue * 255);
                        oscState[stateInd].RY = Convert.ToByte(yValue * 255);
                    }
                }
                else if (command[3] == "trigger")
                {
                    switch (command[4])
                    {
                        case "r2":
                            oscState[stateInd].R2 = Convert.ToByte(Convert.ToSingle(arguments[0]));
                            break;
                        case "l2":
                            oscState[stateInd].L2 = Convert.ToByte(Convert.ToSingle(arguments[0]));
                            break;
                    }
                }
            }

            public void OSCPostMappingStep(DS4State tempMapState, DS4State oscMapState)
            {
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

            public void OSCPreMappingStep(int ind, DS4State cState, DS4State tempMapState,
                DS4State oscMapState)
            {
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

                CompareAndSendChangesToOSC(ind, tempMapState, cState);
            }

            private void CompareAndSendChangesToOSC(int index, DS4State oldState, DS4State newState)
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

        private sealed class Fixture : IDisposable
        {
            public readonly IAppSettingsService Settings = TestHost.GetService<IAppSettingsService>();
            public readonly RecordingOscTransportFactory Factory = new RecordingOscTransportFactory();
            public readonly OscService New;
            public readonly OldOsc Old = new OldOsc();
            private readonly bool _savedInterpreting;
            private readonly bool _savedSender;
            private readonly int _savedServerPort;
            private readonly string _savedAddress;
            private readonly int _savedSenderPort;

            public Fixture(bool interpreting, bool useSender)
            {
                _savedInterpreting = Settings.InterpretingOscMonitoring;
                _savedSender = Settings.UseOscSender;
                _savedServerPort = Settings.OscServerPort;
                _savedAddress = Settings.OscSenderAddress;
                _savedSenderPort = Settings.OscSenderPort;
                Settings.InterpretingOscMonitoring = interpreting;
                Settings.UseOscSender = useSender;
                Old.InterpretingOscMonitoring = interpreting;
                Old.UseOscSender = useSender;
                New = new OscService(Settings, Factory);
                New.ChangeSenderStatus(true);   // 新しい実装の送信は、記録用の送信側を通す
            }

            public void Dispose()
            {
                Settings.InterpretingOscMonitoring = _savedInterpreting;
                Settings.UseOscSender = _savedSender;
                Settings.OscServerPort = _savedServerPort;
                Settings.OscSenderAddress = _savedAddress;
                Settings.OscSenderPort = _savedSenderPort;
            }
        }

        private static string Describe(DS4State s)
        {
            return string.Join(",", s.Cross, s.Square, s.Circle, s.Triangle, s.L1, s.L2, s.L3, s.R1, s.R2, s.R3,
                s.DpadUp, s.DpadDown, s.DpadLeft, s.DpadRight, s.Options, s.Share, s.PS, s.LX, s.LY, s.RX, s.RY, s.Battery);
        }

        private static void AssertSame(Fixture f, string where)
        {
            for (int i = 0; i < ControllerLimits.MaxControllerCount; i++)
            {
                Assert.True(Describe(f.Old.oscState[i]) == Describe(NewState(f.New, i)), $"{where}: oscState[{i}] が違う");
            }

            Assert.True(f.Old.Sent.Select(m => m.Address + "=" + m.Value + ":" + m.Value?.GetType().Name)
                .SequenceEqual(f.Factory.Sent.Select(m => m.Address + "=" + m.Value + ":" + m.Value?.GetType().Name)),
                $"{where}: 送信の内容（アドレス、値、型）が違う");
        }

        /// <summary>新しい実装の OSC 側の入力状態を、読み取り専用で取り出す（テスト用。メッセージを重ねて調べる）。</summary>
        private static DS4State NewState(OscService service, int index)
        {
            // PostMappingStep は oscState を tempMapState に重ねる。空の状態に重ねた結果が、OSC 側の状態（フラグは OR、軸は 128／0 以外のとき）になる
            // （バッテリーは PostMappingStep では重ならず、受信のメッセージでも変わらない［PreMappingStep だけが更新する］ため、0 のまま比べる）
            var probe = new DS4State();
            service.PostMappingStep(index, probe);
            return probe;
        }

        private static readonly string[] PressNames =
        {
            "cross", "square", "circle", "triangle", "r1", "r2", "r3", "l1", "l2", "l3",
            "dpadup", "dup", "dpaddown", "ddown", "dpadleft", "dleft", "dpadright", "dright", "options", "share", "unknown",
        };

        private static IEnumerable<(string Address, object[] Args)> Messages()
        {
            for (int index = 0; index < 3; index++)
            {
                foreach (string name in PressNames)
                {
                    yield return ($"/ds4windows/{index}/press/{name}", new object[] { 1 });
                    yield return ($"/ds4windows/{index}/press/{name}", new object[] { 0 });
                }

                foreach (string name in new[] { "lx", "ly", "rx", "ry" })
                {
                    yield return ($"/ds4windows/{index}/stick/{name}", new object[] { 200f });
                    yield return ($"/ds4windows/{index}/stick/{name}", new object[] { 0f });
                }

                foreach (string name in new[] { "left", "right" })
                {
                    yield return ($"/ds4windows/{index}/stick/{name}", new object[] { 1.0f, 0.5f });
                    yield return ($"/ds4windows/{index}/stick/{name}", new object[] { 0f, 0f });
                }

                foreach (string name in new[] { "l2", "r2" })
                {
                    yield return ($"/ds4windows/{index}/trigger/{name}", new object[] { 255f });
                    yield return ($"/ds4windows/{index}/trigger/{name}", new object[] { 0f });
                }

                yield return ($"/ds4windows/{index}/battery/x", new object[] { 0 });
                // 監視形式
                yield return ($"/ds4windows/monitor/{index}/battery", new object[] { 0 });
                yield return ($"/ds4windows/monitor/{index}/l2", new object[] { 100f });
                yield return ($"/ds4windows/monitor/{index}/lx", new object[] { 50f });
                yield return ($"/ds4windows/monitor/{index}/cross", new object[] { 1 });
            }

            // 不正なもの
            yield return ("/ds4windows/abc/press/cross", new object[] { 1 });     // 番号が数値でない
            yield return ("/ds4windows/-1/press/cross", new object[] { 1 });      // 番号が -1
            yield return ("/other/0/press/cross", new object[] { 1 });            // 名前が違う
            yield return ("/ds4windows/0/stick/lx", new object[] { 1f, 2f, 3f }); // 引数の数が合わない
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void HandleMessage_MatchesTheOldImplementation(bool interpreting, bool useSender)
        {
            using var f = new Fixture(interpreting, useSender);

            foreach ((string address, object[] args) in Messages())
            {
                f.Old.Callback(address, args);
                f.New.HandleMessage(address, args);
                AssertSame(f, $"{address} [{string.Join(",", args)}]");
            }
        }

        [Fact]
        public void HandleMessage_ThrowsTheSameExceptionsAsTheOldImplementation_ForOutOfRangeInput()
        {
            // 現行の癖（計画書 E1・E2。ユーザー判断 Q3 によりそのまま移した）
            using var f = new Fixture(false, false);
            var cases = new List<(string, object[])>
            {
                ("/ds4windows/99/press/cross", new object[] { 1 }),            // 番号が範囲外
                ("/ds4windows/0/stick/lx", new object[] { 300f }),            // 値が範囲外（OverflowException）
                ("/ds4windows/0/trigger/l2", new object[] { -5f }),
                ("/ds4windows/0/press/cross", new object[0]),                   // 引数がない
            };

            foreach ((string address, object[] args) in cases)
            {
                Exception expected = Record.Exception(() => f.Old.Callback(address, args));
                Exception actual = Record.Exception(() => f.New.HandleMessage(address, args));
                Assert.NotNull(expected);
                Assert.True(actual != null && expected.GetType() == actual.GetType(), $"{address}: 旧={expected?.GetType().Name} 新={actual?.GetType().Name}");
            }
        }

        [Fact]
        public void HandleMessage_NullAddress_IsIgnoredLikeTheOldImplementation()
        {
            using var f = new Fixture(false, false);
            Exception expected = Record.Exception(() => f.Old.Callback(null, new object[0]));
            Exception actual = Record.Exception(() => f.New.HandleMessage(null, new object[0]));
            Assert.Null(expected);
            Assert.Null(actual);
        }

        private static DS4State RandomInput(Random rnd)
        {
            byte Axis() => rnd.Next(4) == 0 ? (byte)128 : (byte)rnd.Next(256);
            bool B() => rnd.Next(2) == 0;
            return new DS4State
            {
                Cross = B(), Square = B(), Circle = B(), Triangle = B(), L1 = B(), L3 = B(), R1 = B(), R3 = B(),
                DpadUp = B(), DpadDown = B(), DpadLeft = B(), DpadRight = B(), Options = B(), Share = B(), PS = B(),
                LX = Axis(), LY = Axis(), RX = Axis(), RY = Axis(), L2 = (byte)(rnd.Next(3) == 0 ? 0 : rnd.Next(256)),
                R2 = (byte)(rnd.Next(3) == 0 ? 0 : rnd.Next(256)), Battery = (byte)rnd.Next(0, 101),
            };
        }

        [Fact]
        public void PreAndPostMappingSteps_MatchTheOldImplementation()
        {
            using var f = new Fixture(true, true);
            var rnd = new Random(20261022);
            const int Index = 1;

            for (int round = 0; round < 200; round++)
            {
                // OSC 側の状態を、同じメッセージで作る
                foreach ((string address, object[] args) in Messages().Where(m => rnd.Next(6) == 0))
                {
                    f.Old.Callback(address, args);
                    f.New.HandleMessage(address, args);
                }

                DS4State inputOld = RandomInput(rnd);
                DS4State inputNew = new DS4State(inputOld);
                DS4State tempOld = RandomInput(rnd);
                DS4State tempNew = new DS4State(tempOld);

                f.Old.OSCPreMappingStep(Index, inputOld, tempOld, f.Old.oscState[Index]);
                f.New.PreMappingStep(Index, inputNew, tempNew);
                Assert.True(Describe(inputOld) == Describe(inputNew), $"round={round}: Pre の入力の重ね合わせが違う");
                Assert.True(Describe(tempOld) == Describe(tempNew), $"round={round}: Pre の後の状態が違う");

                DS4State outOld = RandomInput(rnd);
                DS4State outNew = new DS4State(outOld);
                f.Old.OSCPostMappingStep(outOld, f.Old.oscState[Index]);
                f.New.PostMappingStep(Index, outNew);
                Assert.True(Describe(outOld) == Describe(outNew), $"round={round}: Post の重ね合わせが違う");

                Assert.True(f.Old.Sent.Select(m => m.Address + "=" + m.Value + ":" + m.Value?.GetType().Name)
                    .SequenceEqual(f.Factory.Sent.Select(m => m.Address + "=" + m.Value + ":" + m.Value?.GetType().Name)),
                    $"round={round}: 送信の内容が違う");
            }
        }

        [Fact]
        public void SendPlug_SendsOneOrZeroAsAnInt()
        {
            using var f = new Fixture(false, true);
            f.New.SendPlug(3, true);
            f.New.SendPlug(3, false);

            Assert.Equal(2, f.Factory.Sent.Count);
            Assert.Equal("/ds4windows/monitor/3/plug", f.Factory.Sent[0].Address);
            Assert.Equal(1, f.Factory.Sent[0].Value);
            Assert.Equal(0, f.Factory.Sent[1].Value);
            Assert.IsType<int>(f.Factory.Sent[0].Value);
        }

        [Fact]
        public void ResetDeviceState_ClearsTheOscInputOfThatDevice()
        {
            using var f = new Fixture(false, false);
            f.New.HandleMessage("/ds4windows/2/press/cross", new object[] { 1 });
            var before = new DS4State();
            f.New.PostMappingStep(2, before);
            Assert.True(before.Cross);

            f.New.ResetDeviceState(2);
            var after = new DS4State();
            f.New.PostMappingStep(2, after);
            Assert.False(after.Cross);
        }

        [Fact]
        public void ListenerAndSender_LifecycleUsesTheTransportFactory()
        {
            using var f = new Fixture(false, false);
            f.Settings.OscServerPort = 9123;
            f.Settings.OscSenderAddress = "127.0.0.1";
            f.Settings.OscSenderPort = 9124;

            Assert.False(f.New.HasListener);
            f.New.ChangeListenerStatus(true);
            Assert.True(f.New.HasListener);
            Assert.Equal(new[] { 9123 }, f.Factory.ListenerPorts);
            Assert.NotNull(f.Factory.LastOnMessage);

            f.New.ChangeListenerStatus(false);
            Assert.False(f.New.HasListener);
            Assert.Equal(1, f.Factory.ListenersClosed);

            // 送信側は、Fixture が 1 つ作っている（HasSender）。停止すると閉じて、HasSender が false になる
            Assert.True(f.New.HasSender);
            f.New.ChangeSenderStatus(false);
            Assert.False(f.New.HasSender);
            Assert.Equal(1, f.Factory.SendersClosed);

            f.New.ChangeSenderStatus(false);   // 既に無いとき: 例外にならず、閉じる回数も増えない
            Assert.Equal(1, f.Factory.SendersClosed);

            f.New.ChangeSenderStatus(true);
            Assert.Equal(("127.0.0.1", 9124), f.Factory.SenderTargets.Last());
        }

        [Fact]
        public void StoppingAListenerThatIsNotRunning_ThrowsNullReference_LikeTheOldImplementation()
        {
            // 現行の癖（計画書 E3）: 呼び出し側（ControlService）が HasListener を確かめてから呼ぶ前提
            using var f = new Fixture(false, false);
            Assert.Throws<NullReferenceException>(() => f.New.ChangeListenerStatus(false));
        }
    }
}
