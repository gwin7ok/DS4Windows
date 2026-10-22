using System.IO;
using System.Text.RegularExpressions;
using DS4Windows;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step7 W3: マクロの実行時状態（macrodone／macroDispatchInFlight／macroDispatchLock／macroControl／macroCount／macroTaskQueue）を
    /// <c>MacroRuntimeState</c> へ切り出したことの回帰ガード。
    /// ソースが見つからない環境では、ソース検査を行わず合格とする。判断の根拠は Phase7-Step7-Plan.md（Q2・Q3）。
    /// </summary>
    public class Phase7Step7MacroRuntimeStateGuardTests
    {
        private readonly ITestOutputHelper _output;

        public Phase7Step7MacroRuntimeStateGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private string Read(string relativePath)
        {
            string path = SourceFileLocator.Find(relativePath);
            if (path == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した: " + relativePath);
                return null;
            }

            string text = File.ReadAllText(path);
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            return Regex.Replace(text, @"//[^\r\n]*", string.Empty);
        }

        [Fact]
        public void NewInstance_HasTheSameShapeAsTheOldStaticFields()
        {
            var state = new MacroRuntimeState();

            // Phase8-Fix-MacroRepeatUnify（D5）: MacroDone／DispatchInFlight（control 単位の暫定対策。本番では到達しなかった）は削除した
            // Phase9 9-C（ユーザー判断 Q2）: MacroControl／MacroCount はデバイスごと（旧: 全コントローラーで 1 つを共有）
            Assert.Equal(DS4Windows.ControllerLimits.MaxControllerCount, state.MacroControl.Length);
            Assert.All(state.MacroControl, flags => Assert.Equal(MacroRuntimeState.MacroControlLength, flags.Length));
            Assert.Equal(28, MacroRuntimeState.MacroControlLength);   // Phase9 9-C: 261〜288 のぶん（旧 26 は 287・288 で範囲外になっていた）
            Assert.Equal(DS4Windows.ControllerLimits.MaxControllerCount, state.MacroCount.Length);
            Assert.Equal(DS4Windows.ControllerLimits.MaxControllerCount, state.TaskQueue.Length);
            Assert.All(state.TaskQueue, q => Assert.NotNull(q));

            // 参照は安定（再代入されない）
            var macroControl = state.MacroControl;
            var taskQueue = state.TaskQueue;
            Assert.Same(macroControl, state.MacroControl);
            Assert.Same(taskQueue, state.TaskQueue);
        }

        [Fact]
        public void TheRemovedControlIndexedState_IsGone()
        {
            Assert.Null(typeof(MacroRuntimeState).GetProperty("MacroDone"));
            Assert.Null(typeof(MacroRuntimeState).GetProperty("DispatchInFlight"));
            Assert.Null(typeof(MacroRuntimeState).GetMethod("ClearMacroDone"));
            Assert.Null(typeof(MacroRuntimeState).GetMethod("TryBeginDispatch"));
            Assert.Null(typeof(MacroRuntimeState).GetMethod("EndDispatch"));
        }

        [Fact]
        public void Mapping_HoldsNoMacroStateOfItsOwn_OnlyForwarders()
        {
            string mapping = Read("DS4Windows/DS4Control/Mapping.cs");
            if (mapping == null) return;

            // Phase7-Step8 W2: 転送プロパティは削除し、コンストラクターで受けた保持クラスの配列を readonly で複製して持つ
            Assert.Contains("private readonly MacroRuntimeState _macroRuntime;", mapping);
            // Phase8-Fix-MacroRepeatUnify（D5）: macrodone（と、その添字の表 ds4ControlMapping／DS4ControltoInt）、二重実行防止のフラグとロックは削除した
            Assert.DoesNotContain("macrodone", mapping);
            Assert.DoesNotContain("DS4ControltoInt", mapping);
            Assert.DoesNotContain("private readonly bool[] macroDispatchInFlight;", mapping);
            Assert.DoesNotContain("private readonly object macroDispatchLock;", mapping);
            // Phase9 9-C: macroControl／macroTaskQueue の複製は Mapping から無くなった（マクロの再生の部品と MacroRuntimeState.ApplyHeldControls へ移した）
            Assert.DoesNotContain("private readonly bool[] macroControl;", mapping);
            Assert.DoesNotContain("private readonly Dictionary<string, Task>[] macroTaskQueue;", mapping);
            Assert.DoesNotContain("macroTaskQueue = macroRuntime.TaskQueue;", mapping);
            Assert.DoesNotContain("MacroRuntimeState.Shared", mapping);
            Assert.DoesNotContain("DS4_CONTROL_MACRO_ARRAY_LEN", mapping);
        }

        [Fact]
        public void Registration_RegistersTheSharedState()
        {
            string registration = Read("DS4Windows/DI/ServiceRegistration.cs");
            if (registration == null) return;

            Assert.Contains("services.AddSingleton<MacroRuntimeState>();", registration); // Phase7-Step8 W4: Shared は削除した
        }
    }
}
