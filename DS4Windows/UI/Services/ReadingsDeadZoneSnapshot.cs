using DS4Windows.Core.Settings;
using DS4Windows.UI.ViewModels;

namespace DS4Windows.UI.Services
{
    /// <summary>
    /// 編集中のプロファイルから、入力の表示（Controller Readings）に渡すデッドゾーンとドリフトの値を集めたもの（画面に依存しない）。
    /// スティックのデッドゾーンの種類が「円形」なら 1 つの値を X・Y の両方に、「軸ごと」なら軸ごとの ViewModel の値を使う。
    /// どちらでもない種類のときは、その値を持たない（<c>null</c>。表示側の値は変えない）。
    /// </summary>
    public sealed class ReadingsDeadZoneSnapshot
    {
        public double? LsDeadX { get; private set; }
        public double? LsDeadY { get; private set; }
        public double? RsDeadX { get; private set; }
        public double? RsDeadY { get; private set; }
        public double L2Dead { get; private set; }
        public double R2Dead { get; private set; }
        public double SixAxisXDead { get; private set; }
        public double SixAxisZDead { get; private set; }
        public sbyte LsDriftX { get; private set; }
        public sbyte LsDriftY { get; private set; }
        public sbyte RsDriftX { get; private set; }
        public sbyte RsDriftY { get; private set; }

        /// <param name="settings">編集中のプロファイルの設定。</param>
        /// <param name="axialLeft">左スティックの「軸ごと」のデッドゾーンの ViewModel。</param>
        /// <param name="axialRight">右スティックの「軸ごと」のデッドゾーンの ViewModel。</param>
        public static ReadingsDeadZoneSnapshot Create(ProfileSettingsViewModel settings,
            AxialStickControlViewModel axialLeft, AxialStickControlViewModel axialRight)
        {
            var snap = new ReadingsDeadZoneSnapshot();

            switch (settings.Sticks.LSMod.deadzoneType)
            {
                case StickDeadZoneInfo.DeadZoneType.Radial:
                    snap.LsDeadX = snap.LsDeadY = settings.Sticks.LSDeadZone;
                    break;
                case StickDeadZoneInfo.DeadZoneType.Axial:
                    snap.LsDeadX = axialLeft.DeadZoneX;
                    snap.LsDeadY = axialLeft.DeadZoneY;
                    break;
            }

            switch (settings.Sticks.RSMod.deadzoneType)
            {
                case StickDeadZoneInfo.DeadZoneType.Radial:
                    snap.RsDeadX = snap.RsDeadY = settings.Sticks.RSDeadZone;
                    break;
                case StickDeadZoneInfo.DeadZoneType.Axial:
                    snap.RsDeadX = axialRight.DeadZoneX;
                    snap.RsDeadY = axialRight.DeadZoneY;
                    break;
            }

            snap.L2Dead = settings.Triggers.L2DeadZone;
            snap.R2Dead = settings.Triggers.R2DeadZone;
            snap.SixAxisXDead = settings.SixAxis.SXDeadZone;
            snap.SixAxisZDead = settings.SixAxis.SZDeadZone;
            snap.LsDriftX = settings.Sticks.LeftStickDriftXAxis;
            snap.LsDriftY = settings.Sticks.LeftStickDriftYAxis;
            snap.RsDriftX = settings.Sticks.RightStickDriftXAxis;
            snap.RsDriftY = settings.Sticks.RightStickDriftYAxis;
            return snap;
        }
    }
}
