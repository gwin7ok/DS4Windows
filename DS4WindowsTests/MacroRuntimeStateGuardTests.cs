using DS4Windows.Processing.Macro;
using System.Collections.Generic;
using System.Linq;
using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using DS4Windows.Processing;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step7 W3: マクロの実行時状態（macrodone／macroDispatchInFlight／macroDispatchLock／macroControl／macroCount／macroTaskQueue）を
    /// <c>MacroRuntimeState</c> へ切り出したことの回帰ガード。
    /// ソースが見つからない環境では、ソース検査を行わず合格とする。判断の根拠は Phase7-Step7-Plan.md（Q2・Q3）。
    /// </summary>
    public class MacroRuntimeStateGuardTests
    {
        private readonly ITestOutputHelper _output;

        public MacroRuntimeStateGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void NewInstance_HasTheSameShapeAsTheOldStaticFields()
        {
            var state = new MacroRuntimeState();

            // Phase8-Fix-MacroRepeatUnify（D5）: MacroDone／DispatchInFlight（control 単位の暫定対策。本番では到達しなかった）は削除した
            // Phase9 9-C（ユーザー判断 Q2）: MacroControl／MacroCount はデバイスごと（旧: 全コントローラーで 1 つを共有）
            Assert.Equal(DS4Windows.Core.Settings.ControllerLimits.MaxControllerCount, state.MacroControl.Length);
            Assert.All(state.MacroControl, flags => Assert.Equal(MacroRuntimeState.MacroControlLength, flags.Length));
            Assert.Equal(28, MacroRuntimeState.MacroControlLength);   // Phase9 9-C: 261〜288 のぶん（旧 26 は 287・288 で範囲外になっていた）
            Assert.Equal(DS4Windows.Core.Settings.ControllerLimits.MaxControllerCount, state.MacroCount.Length);
            Assert.Equal(DS4Windows.Core.Settings.ControllerLimits.MaxControllerCount, state.TaskQueue.Length);
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
        public void Registration_RegistersTheSharedState()
        {
            // Phase14d-Step3: 登録の文字列ではなく、コンテナが同じインスタンスを返すことを確かめる
            var first = TestHost.GetService<DS4Windows.Processing.Macro.MacroRuntimeState>();
            Assert.NotNull(first);
            Assert.Same(first, TestHost.GetService<DS4Windows.Processing.Macro.MacroRuntimeState>());
        }
    }
}
