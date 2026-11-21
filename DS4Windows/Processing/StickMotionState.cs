using DS4Windows.Processing.Gyro;
using System.Collections.Generic;
using DS4Windows.Core.Settings;

namespace DS4Windows.Processing
{
    /// <summary>
    /// スティック・ジャイロ・トリガーの処理状態のうち、<c>Mapping</c> の外から触られるものの保持クラス
    /// （旧: <c>Mapping</c> の静的フィールド <c>gyroStickX</c>／<c>gyroStickY</c>／<c>mapStickActionData</c>／<c>flickMappingData</c>／
    /// <c>l2TwoStageMappingData</c>／<c>r2TwoStageMappingData</c>／<c>wheelFilters</c>／<c>deltaAccelProcessors</c>）。
    /// <c>Mapping</c>、<c>Mouse</c>、<c>ControlService</c> が使う。Phase9 9-A(2)-B で、<c>Mapping</c> の内部だけで使っていたスティックの前処理の状態も、ここへ移した（<c>IStickInputConditioner</c> へ引数で渡す）。
    /// 配列の参照は再代入しない（要素の読み書き、または要素の差し替えのみ）。デバイスの添字ごとに分かれており、ロックはない（従来どおり）。
    /// </summary>
    public class StickMotionState
    {
        /// <summary>デバイスごとの処理状態（添字はデバイスの番号。配列の参照は再代入しない）。</summary>
        public DeviceStickMotion[] Devices { get; }

        /// <summary>アンチスナップバックの履歴（[デバイス][0＝左, 1＝右]）。旧 <c>Mapping.stickValueHistory</c>。添字は <c>ControllerLimits.TestProfileItemCount</c>（テスト用プロファイルの枠を含む）。</summary>
        public Queue<DS4TimedStickAxisValue>[][] StickValueHistory { get; }

        /// <summary>ファズの前回の値（[デバイス][LX＝0, LY＝1, RX＝2, RY＝3]。128＝中立）。旧 <c>Mapping.lastStickAxisValues</c>。添字は <c>ControllerLimits.TestProfileItemCount</c>。</summary>
        public byte[][] LastStickAxisValues { get; }


        public StickMotionState()
        {
            int count = ControllerLimits.MaxControllerCount;
            Devices = new DeviceStickMotion[count];
            for (int i = 0; i < count; i++) Devices[i] = new DeviceStickMotion();

            int profileCount = ControllerLimits.TestProfileItemCount;
            StickValueHistory = new Queue<DS4TimedStickAxisValue>[profileCount][];
            LastStickAxisValues = new byte[profileCount][];
            for (int i = 0; i < profileCount; i++)
            {
                StickValueHistory[i] = new Queue<DS4TimedStickAxisValue>[2] { new Queue<DS4TimedStickAxisValue>(), new Queue<DS4TimedStickAxisValue>() };
                LastStickAxisValues[i] = new byte[4] { 128, 128, 128, 128 };
            }
        }
    }
}
