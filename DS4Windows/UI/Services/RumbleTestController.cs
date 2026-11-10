using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Processing.Abstractions;
using DS4Windows.UI.ViewModels;

namespace DS4Windows.UI.Services
{
    /// <summary>
    /// 出力の設定画面にある「振動テスト」ボタンの動作（画面に依存しない）。
    /// 押したボタン（重い／軽い）とモーターの左右反転の設定から、実際に振らせるモーターを決め、
    /// 振動の開始・停止と、画面の状態（<see cref="OutputSettingsViewModel.HeavyRumbleActive"/> など）を更新する。
    /// ボタンの文字は、結果（<see cref="RumbleTestOutcome"/>）を見て画面が変える。
    /// </summary>
    public sealed class RumbleTestController
    {
        private readonly IControllerSlotCommands slotCommands;
        private readonly IEnvironmentService environmentService;

        public RumbleTestController(IControllerSlotCommands slotCommands, IEnvironmentService environmentService)
        {
            this.slotCommands = slotCommands ?? throw new ArgumentNullException(nameof(slotCommands));
            this.environmentService = environmentService ?? throw new ArgumentNullException(nameof(environmentService));
        }

        /// <summary>
        /// 振動テストのボタンを押したときの処理。
        /// </summary>
        /// <param name="output">編集中の出力の設定（モーターの反転、強さ、DualSense の方式、動作中の印）。</param>
        /// <param name="rumbleDevice">振動させる実機のスロット番号。</param>
        /// <param name="heavyButtonPressed">押したのが「重い」のボタンか。</param>
        public RumbleTestOutcome Toggle(OutputSettingsViewModel output, int rumbleDevice, bool heavyButtonPressed)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));
            if (rumbleDevice >= environmentService.ControllerSlotLimit || !slotCommands.IsConnected(rumbleDevice))
                return RumbleTestOutcome.None;

            // モーターの左右反転のときは、ボタンと実際に振るモーターが入れ替わる
            bool heavyMotor = output.InverseRumbleMotors ? !heavyButtonPressed : heavyButtonPressed;
            bool rumbleActive = heavyMotor ? output.HeavyRumbleActive : output.LightRumbleActive;

            if (!rumbleActive)
            {
                int rumbleBoost = output.RumbleBoost;

                // DualSense のときは、方式と強さを機器へ反映する。一般的な強さの補正を使わない設定なら 100% にする
                if (slotCommands.ApplyDualSenseRumbleOptions(rumbleDevice,
                    output.DualSenseRumbleEmulationPerIndex,
                    (byte)output.DualSenseHapticPowerLevelPerIndex))
                {
                    if (!output.EnableGenericRumbleStrRescaleForDualSenseDevices)
                        rumbleBoost = 100;
                }

                byte strength = (byte)Math.Min(255, 255 * rumbleBoost / 100);
                if (heavyMotor)
                {
                    output.HeavyRumbleActive = true;
                    slotCommands.SetHeavyRumble(rumbleDevice, strength);
                }
                else
                {
                    output.LightRumbleActive = true;
                    slotCommands.SetLightRumble(rumbleDevice, strength);
                }

                return RumbleTestOutcome.Started;
            }

            if (heavyMotor)
            {
                output.HeavyRumbleActive = false;
                slotCommands.SetHeavyRumble(rumbleDevice, 0);
            }
            else
            {
                output.LightRumbleActive = false;
                slotCommands.SetLightRumble(rumbleDevice, 0);
            }

            return RumbleTestOutcome.Stopped;
        }
    }
}
