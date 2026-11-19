using DS4Windows.Core.Model;

namespace DS4Windows.UI
{

    public class ControlIndexCheck
    {
        public int TiltUp { get => (int)DS4Controls.GyroZNeg; }
        public int TiltDown { get => (int)DS4Controls.GyroZPos; }
        public int TiltLeft { get => (int)DS4Controls.GyroXPos; }
        public int TiltRight { get => (int)DS4Controls.GyroXNeg; }

        public int SwipeUp { get => (int)DS4Controls.SwipeUp; }
        public int SwipeDown { get => (int)DS4Controls.SwipeDown; }
        public int SwipeLeft { get => (int)DS4Controls.SwipeLeft; }
        public int SwipeRight { get => (int)DS4Controls.SwipeRight; }
        public int L2FullPull { get => (int)DS4Controls.L2FullPull; }
        public int R2FullPull { get => (int)DS4Controls.R2FullPull; }

        public int LSOuterBind { get => (int)DS4Controls.LSOuter; }
        public int RSOuterBind { get => (int)DS4Controls.RSOuter; }
    }
}