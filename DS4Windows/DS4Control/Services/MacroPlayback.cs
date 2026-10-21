using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DS4Windows.DI;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase9 9-C（Phase9-C-Plan.md）: <see cref="IMacroPlayback"/> の実装（DI の Singleton）。
    /// 旧 <c>Mapping.PlayMacro</c>／<c>PlayMacroTask</c>／<c>RunMacroOnce</c>（予約、同期キュー、押している間の繰り返し、1 回分の再生、後始末）を、そのまま移したもの。
    /// 1 つのマクロ値の実行は <see cref="IMacroStepExecutor"/> へ、同期キューの Task の連鎖は <c>MacroRuntimeState.TaskQueue</c> に持つ。
    /// TODO(技術的負債・Phase9 9-E 付近): <c>ActionManager.GetStateFor</c>／<c>ActionManager.IsBeingTriggered</c>（Actions サブシステムの静的な窓口）は、旧実装どおり直接呼ぶ。
    /// <c>IManagedActionManager</c> に <c>IsBeingTriggered</c> がまだなく、Actions サブシステムの DI 化（規約 R3）の範囲のため、今回は変えない。
    /// ライトバーの強制色の復帰（<c>DS4LightBar.forcedFlash</c>／<c>forcelight</c>）も、静的な配列へ直接書く（ライトバーのサービス化で置き換える）。
    /// 実際の挙動は、入口の <c>PlayMacroDirect</c>／<c>PlayMacroString</c> を通る既存の挙動テスト（<c>Phase8Step78cW2ControlsMacroTests</c> ほか）が守る。
    /// </summary>
    public sealed class MacroPlayback : IMacroPlayback
    {
        private readonly IMacroStepExecutor _stepExecutor;
        private readonly MacroRuntimeState _macroRuntime;

        public MacroPlayback(IMacroStepExecutor stepExecutor, MacroRuntimeState macroRuntime)
        {
            _stepExecutor = stepExecutor ?? throw new ArgumentNullException(nameof(stepExecutor));
            _macroRuntime = macroRuntime ?? throw new ArgumentNullException(nameof(macroRuntime));
        }

        /// <summary>切断時の後始末: そのデバイスの同期キュー（特殊アクションごとの Task の連鎖）を空にする。</summary>
        public void ClearDevice(int device)
        {
            // 旧 Mapping.HandleDeviceDisconnect の「Clear macro task queue entries for device」と同じ処理
            try
            {
                var queues = _macroRuntime.TaskQueue;
                if (queues != null && device >= 0 && device < queues.Length)
                {
                    var dict = queues[device];
                    if (dict != null)
                    {
                        try { dict.Clear(); } catch { }
                    }
                }
            }
            catch { }
        }

        // Play macro as a background task. Optionally the new macro play waits for completion of a previous macro execution (synchronized macro special action).
        // Macro steps are defined either as macrostr string value, macroLst list<int> object or as macroArr integer array. Only one of these should have a valid macro definition when this method is called.
        // If the macro definition is a macroStr string value then it will be converted as integer array on the fl. If steps are already defined as list or array of integers then there is no need to do type cast conversion.
        // Phase8-Fix-MacroRepeatUnify（D5）: 呼び出しは常に control == DS4Controls.None（SA・Controls タブ・MultiAction のすべて）だったため、control 単位の経路
        // （macrodone、macroDispatchInFlight／TryBeginDispatch）と、未使用の引数 control／macrocontrol を削除した。二重起動の防止は ActionInstanceState.IsMacroRunning が担う。
        public void Play(int device, string macroStr, List<int> macroLst, int[] macroArr, DS4KeyType keyType, SpecialAction action = null, ActionInstanceState actionDoneState = null)
        {
            // If caller didn't provide per-action state, obtain it so we can check for running macro.
            ActionInstanceState st = actionDoneState;
            if (st == null && action != null)
            {
                try { st = ActionManager.GetStateFor(action, device); } catch { st = null; }
            }

            // Startup guard: if a macro iteration is already running for this action/device,
            // skip starting another macro and log the decision (DECISION=RUN/NORUN).
            try
            {
                bool actionRunning = st != null && st.IsMacroRunning;
                string decision = actionRunning ? "NORUN" : "RUN";
                AppLogger.LogTrace($"PlayMacro START GUARD: action={action?.name} device={device} IsMacroRunning={actionRunning} DECISION={decision}");
                if (actionRunning) return;
            }
            catch { }

            // Note: do not use a task-wide "macro running" guard here. Per-iteration
            // running state is represented by `IsMacroRunning` on the ActionInstanceState
            // and `RepeatMacro` semantics rely on `BeingTriggered`. Allow PlayMacro to
            // start; synchronization for repeat macros is handled via `MacroRuntimeState.TaskQueue`.
            // For "Repeat while held" macros we must avoid overlapping executions.
            // Treat repeat-macros as synchronized so a new run waits for previous to finish.
            bool shouldSync = action != null && (action.synchronized || keyType.HasFlag(DS4KeyType.RepeatMacro));
            if (shouldSync)
            {
                // Run special action macros in synchronized order (ie. FirstIn-First-Out). The trigger control name string is the execution queue identifier (ie. each unique trigger combination has an own synchronization queue).
                if (!_macroRuntime.TaskQueue[device].TryGetValue(action.controls, out Task prevTask))
                    _macroRuntime.TaskQueue[device].Add(action.controls, (Task.Factory.StartNew(() => PlayMacroTask(device, macroStr, macroLst, macroArr, keyType, action, actionDoneState))));
                else
                    _macroRuntime.TaskQueue[device][action.controls] = prevTask.ContinueWith((x) => PlayMacroTask(device, macroStr, macroLst, macroArr, keyType, action, actionDoneState));
            }
            else
                // Run macro as "fire and forget" background task. No need to wait for completion of any of the other macros.
                // If the same trigger macro is re-launched while previous macro is still running then the order of parallel macros is not guaranteed.
                Task.Factory.StartNew(() => PlayMacroTask(device, macroStr, macroLst, macroArr, keyType, action, actionDoneState));
        }

        /// <summary>Phase10-Step3: 手順が空のマクロを「押している間繰り返す」ときの、1 回ごとの間隔（ミリ秒）。手順のあるマクロの 1 回の再生時間（約 50ms）に合わせた。</summary>
        private const int EmptyMacroRepeatIntervalMs = 50;

        // Play through a macro. The macro steps are defined either as string, List or Array object (always only one of those parameters is set to a valid value)
        private void PlayMacroTask(int device, string macroStr, List<int> macroLst, int[] macroArr, DS4KeyType keyType, SpecialAction action, ActionInstanceState actionDoneState)
        {
            // Ensure we have the per-action state from ActionManager if not provided by caller
            if (actionDoneState == null && action != null)
            {
                try { actionDoneState = ActionManager.GetStateFor(action, device); } catch { }
            }
            if (!String.IsNullOrEmpty(macroStr))
            {
                string[] skeys;

                skeys = macroStr.Split('/');
                macroArr = new int[skeys.Length];
                for (int i = 0; i < macroArr.Length; i++)
                    macroArr[i] = int.Parse(skeys[i]);
            }

            // Do not mark a task-wide "macro running" flag here. `IsMacroRunning` will
            // be set/cleared per single macro iteration below.

            try
            {
                try { AppLogger.LogTrace($"PlayMacroTask START: action={action?.name} device={device} IsMacroRunning={(actionDoneState != null ? actionDoneState.IsMacroRunning : false)} IsBeingTriggered={(action != null ? ActionManager.IsBeingTriggered(action, device) : false)} keyType={keyType}"); } catch { }

                // Phase8-Step7-3a（機能廃止。ユーザー承認済み）: Alt-Tab の並び（Alt 押す・Tab 押す・Tab 離す・Alt 離す）で始まるマクロの特別扱いを廃止した。
                // この並びのマクロも、他のマクロと同じく、手順どおりに再生する（Phase8-Step7-3a-AltTab-Removal-Plan.md）。
                // 最初の 1 回は、IsMacroRunning を立てずに再生する（従来どおり）。
                RunMacroOnce(device, macroLst, macroArr, keyType, action, actionDoneState, markRunning: false);

                // Repeat-while-held handling: instead of re-dispatching a released edge (which may enqueue
                // additional macro runs), perform repeat-in-place. After a macro run completes, check the
                // per-action BeingTriggered flag; if it remains true (trigger still held), run the macro again.
                // This avoids accumulating a queue of runs that would execute after physical release.
                // Phase8-Fix-MacroRepeatUnify（D1）: 「押している間繰り返す」は RepeatMacro に統合した（SA と Controls タブで共通。旧 HoldMacro は読込時に RepeatMacro になる）。
                if (action != null && keyType.HasFlag(DS4KeyType.RepeatMacro) && actionDoneState != null)
                {
                    bool keepRunning = true;
                    while (keepRunning)
                    {
                        bool curBeing = false;
                        try { curBeing = ActionManager.IsBeingTriggered(action, device); keepRunning = curBeing; } catch { keepRunning = false; curBeing = false; }

                        // Log the recheck result and the decision whether we'll rerun this macro
                        try
                        {
                            string decision = keepRunning ? "RERUN" : "NORUN";
                            AppLogger.LogTrace($"PlayMacroTask ITERATION CHECK: action={action?.name} device={device} IsMacroRunning={actionDoneState?.IsMacroRunning ?? false} IsBeingTriggered={curBeing} DECISION={decision}");
                        }
                        catch { }

                        if (!keepRunning) break;

                        // Phase10-Step3: 手順が空のマクロも、押している間は繰り返す。ただし、1 回の再生に時間がかからず、待ちなしの空回りになるため、
                        // 繰り返しの間に、短い間隔（EmptyMacroRepeatIntervalMs）を置く（手順のあるマクロは、手順そのものの時間が間隔になる）
                        if ((macroLst == null || macroLst.Count == 0) && (macroArr == null || macroArr.Length == 0))
                            Task.Delay(EmptyMacroRepeatIntervalMs).Wait();

                        // Execute the macro sequence again inline (the iteration is marked as running)
                        RunMacroOnce(device, macroLst, macroArr, keyType, action, actionDoneState, markRunning: true);

                        // loop will check BeingTriggered again
                    }
                }
            }
            finally
            {
                if (actionDoneState != null)
                {
                    try { lock (actionDoneState) { actionDoneState.IsMacroRunning = false; } } catch { }
                }
            }
            try { AppLogger.LogTrace($"PlayMacroTask END: action={action?.name} device={device} IsMacroRunning={(actionDoneState != null ? actionDoneState.IsMacroRunning : false)} IsBeingTriggered={(action != null ? ActionManager.IsBeingTriggered(action, device) : false)}"); } catch { }
        }

        // Phase8-Fix-MacroRepeatUnify（D5）: 旧 PlayMacroTask の「最初の 1 回」と「繰り返しの 1 回」は、ほぼ同じコードが 2 つあったため、1 つにまとめた（挙動は同じ）。
        // markRunning: 繰り返しの 1 回は、実行中に IsMacroRunning を立てる（最初の 1 回は、従来どおり立てない）。
        private void RunMacroOnce(int device, List<int> macroLst, int[] macroArr, DS4KeyType keyType, SpecialAction action, ActionInstanceState actionDoneState, bool markRunning)
        {
            IList<int> steps = macroLst != null ? (IList<int>)macroLst : macroArr;

            // Mark iteration running for this inline repeat
            if (markRunning && actionDoneState != null)
            {
                try { lock (actionDoneState) { actionDoneState.IsMacroRunning = true; } } catch { }
            }

            bool[] keydown = new bool[512];

            // Play macro codes and simulate key down/up events (note! The same key may go through several up and down events during the same macro).
            // If the return value is TRUE then this method should do a asynchronized delay (the usual Thread.Sleep doesnt work here because it would block the main gamepad reading thread).
            if (steps != null)
            {
                for (int i = 0; i < steps.Count; i++)
                {
                    int macroCodeValue = steps[i];
                    if (_stepExecutor.Execute(device, keyType, macroCodeValue, keydown))
                        Task.Delay(macroCodeValue - 300).Wait();
                }
            }

            // The macro is finished. If any of the keys is still in down state then release a key state (ie. simulate key up event) unless special action specified to keep the last state as it is left in a macro
            if (action == null || !action.keepKeyState)
            {
                for (int i = 0, arlength = keydown.Length; i < arlength; i++)
                {
                    if (keydown[i])
                        _stepExecutor.Execute(device, keyType, i, keydown);
                }

                // Reset lightbar back to a default value (if the macro modified the color) because keepKeyState macro option was not set
                DS4LightBar.forcedFlash[device] = 0;
                DS4LightBar.forcelight[device] = false;
            }

            // Commented out rumble reset. No need to zero out rumble after a macro because it may conflict with a game generated rumble events (ie. macro would stop a game generated rumble effect).
            // If macro generates rumble effects then the macro can stop the rumble as a last step or wait for rumble watchdog timer to do it after few seconds.
            //Program.rootHub.DS4Controllers[device].setRumble(0, 0);

            // Clear iteration-running so the log shows the iteration completed
            if (actionDoneState != null)
            {
                try { lock (actionDoneState) { actionDoneState.IsMacroRunning = false; } } catch { }
            }
            try { AppLogger.LogTrace($"PlayMacroTask MACRO EXECUTE END: action={action?.name} device={device} IsMacroRunning={(actionDoneState != null ? actionDoneState.IsMacroRunning : false)} IsBeingTriggered={(action != null ? ActionManager.IsBeingTriggered(action, device) : false)}"); } catch { }
        }
    }
}
