using DS4Windows.Processing.Macro;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Settings;
using DS4Windows.UI.Abstractions;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.UI.ViewModels
{
    /// <summary>マクロの手順の追加・削除・移動。</summary>
    public partial class RecordBoxViewModel
    {
        public void AddMacroStep(MacroStep step, bool ignoreDelay = false)
        {
            if (recordDelays && macroSteps.Count > 0 && !ignoreDelay)
            {
                int elapsed = (int)sw.ElapsedMilliseconds + 300;
                MacroStep waitstep = new MacroStep(elapsed, $"Wait {elapsed - 300}ms",
                    MacroStep.StepType.Wait, MacroStep.StepOutput.None);
                MacroStepItem waititem = new MacroStepItem(waitstep);
                if (appendIndex == -1)
                {
                    macroSteps.Add(waititem);
                }
                else
                {
                    macroSteps.Insert(appendIndex, waititem);
                    appendIndex++;
                }
            }

            sw.Restart();
            MacroStepItem item = new MacroStepItem(step);
            if (appendIndex == -1)
            {
                macroSteps.Add(item);
            }
            else
            {
                macroSteps.Insert(appendIndex, item);
                appendIndex++;
            }

            // If recording and not using delays already, check action type
            // and insert small wait period on Key ActDown. Needed
            // for some apps so events are not skipped in an event queue
            if (recording && !recordDelays &&
                step.ActType == MacroStep.StepType.ActDown &&
                step.OutputType == MacroStep.StepOutput.Key)
            {
                // 300 + WaitTime (ms)
                MacroStep waitstep = new MacroStep(MacroStep.WAIT_OFFSET + 50, $"Wait 50ms",
                    MacroStep.StepType.Wait, MacroStep.StepOutput.None);
                item = new MacroStepItem(waitstep);
                if (appendIndex == -1)
                {
                    macroSteps.Add(item);
                }
                else
                {
                    macroSteps.Insert(appendIndex, item);
                    appendIndex++;
                }
            }
        }

        /// <summary>
        /// マウスポインターの移動ステップ（<paramref name="mode"/> の (0, 0)）を、選択した位置の前へ入れる。選択がなければ末尾へ足す。入れた位置を返す。
        /// モードは、記録の基準の一覧（<see cref="PointerRecordModeIndex"/>）の選択に合わせる。座標は、リストのダブルクリックで直す。
        /// </summary>
        public int AddPointerMoveStep(int index, MacroPointerMoveMode mode = MacroPointerMoveMode.Absolute)
        {
            var move = new MacroPointerMove(mode, 0, 0);
            var step = new MacroStep(move.ToMacroValue(), move.DisplayName, MacroStep.StepType.ActDown, MacroStep.StepOutput.PointerMove);
            if (index < 0 || index > macroSteps.Count) index = macroSteps.Count;
            InsertMacroStep(index, step);
            return index;
        }

        /// <summary>
        /// 選択された行のうち、いちばん上の行の位置。選択がなければ -1。
        /// Record／Insert の挿入位置になる（複数選択のときは、いちばん上の選択行の前。選択がなければ、いちばん下に足す）。
        /// </summary>
        public static int FirstIndexOf(IEnumerable<int> selectedIndexes)
        {
            int first = -1;
            foreach (int index in selectedIndexes)
            {
                if (index >= 0 && (first < 0 || index < first)) first = index;
            }

            return first;
        }

        /// <summary>指定した位置の行をすべて消す（範囲外・重複は無視）。消した行数を返す。</summary>
        public int RemoveSteps(IEnumerable<int> indexes)
        {
            List<int> targets = indexes.Where(i => i >= 0 && i < macroSteps.Count).Distinct().OrderByDescending(i => i).ToList();
            foreach (int index in targets)
            {
                macroSteps.RemoveAt(index);
            }

            return targets.Count;
        }

        /// <summary>
        /// 指定した位置の行を、元の並びのまま 1 かたまりにして、<paramref name="targetIndex"/>（移す前の並びでの、挿入先の行の位置。末尾は行数）の前へ移す。
        /// 移した行の、新しい位置を返す。並びが変わらないとき（かたまりの中・すぐ前後へ落とす）は、何も変えず null。
        /// </summary>
        public int[]? MoveSteps(IEnumerable<int> sourceIndexes, int targetIndex)
        {
            List<int> sources = sourceIndexes.Where(i => i >= 0 && i < macroSteps.Count).Distinct().OrderBy(i => i).ToList();
            if (sources.Count == 0) return null;

            targetIndex = Math.Min(Math.Max(targetIndex, 0), macroSteps.Count);
            int insertAt = targetIndex - sources.Count(i => i < targetIndex);
            int[] newIndexes = Enumerable.Range(insertAt, sources.Count).ToArray();
            if (sources.SequenceEqual(newIndexes)) return null;

            List<MacroStepItem> moving = sources.Select(i => macroSteps[i]).ToList();
            foreach (int index in sources.OrderByDescending(i => i))
            {
                macroSteps.RemoveAt(index);
            }

            for (int i = 0; i < moving.Count; i++)
            {
                macroSteps.Insert(insertAt + i, moving[i]);
            }

            return newIndexes;
        }
    }
}
