using System;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.UI.Services
{
    /// <summary>
    /// 出力ピッカー（割り当ての選択画面）の「振動のテスト」ボタンの動作（画面に依存しない）。
    /// 割り当てに設定した 2 つのモーターの強さで振らせ、もう一度押すと止める。
    /// モーターの左右反転の設定では、2 つの強さを入れ替えて渡す。
    /// </summary>
    public sealed class ActionRumbleTestController
    {
        private readonly IControllerSlotCommands slotCommands;

        public ActionRumbleTestController(IControllerSlotCommands slotCommands)
        {
            this.slotCommands = slotCommands ?? throw new ArgumentNullException(nameof(slotCommands));
        }

        /// <param name="slot">振動させる実機のスロット番号。</param>
        /// <param name="currentlyActive">いま振動のテスト中か。</param>
        /// <param name="heavy">重いモーターの強さ（255 を超える値は 255 にする）。</param>
        /// <param name="light">軽いモーターの強さ（255 を超える値は 255 にする）。</param>
        /// <param name="inverseMotors">モーターの左右反転の設定。</param>
        public RumbleTestOutcome Toggle(int slot, bool currentlyActive, int heavy, int light, bool inverseMotors)
        {
            if (!slotCommands.IsConnected(slot))
                return RumbleTestOutcome.None;

            if (currentlyActive)
            {
                slotCommands.SetRumble(slot, 0, 0);
                return RumbleTestOutcome.Stopped;
            }

            byte h = (byte)Math.Min(255, heavy);
            byte l = (byte)Math.Min(255, light);
            // SetRumble の引数の順は（軽い・速い側, 重い・遅い側）。反転のときは入れ替える
            if (inverseMotors)
                slotCommands.SetRumble(slot, h, l);
            else
                slotCommands.SetRumble(slot, l, h);
            return RumbleTestOutcome.Started;
        }
    }
}
