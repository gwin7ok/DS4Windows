using System.Collections.Generic;

namespace DS4Windows.UI.Controls
{
    /// <summary>
    /// コントローラーの図の上に置くボタンの配置表。座標は、各配置の基準の大きさ（<see cref="ControllerLayout.ReferenceWidth"/>×<see cref="ControllerLayout.ReferenceHeight"/>）
    /// での画素の値で、表示領域がそれと違うときは <see cref="ControllerVisualizerControl"/> が比率で拡縮する。
    /// 値は、旧 BindingWindow の XAML（Xbox 360）とコード（DualShock 4）の座標をそのまま写したもの。
    /// </summary>
    public static class ControllerLayouts
    {
        /// <summary>出力ピッカー（BindingWindow）の Xbox 360 の図。</summary>
        public static ControllerLayout Xbox360 { get; } = new ControllerLayout("Xbox360", 630, 247, new List<HitboxRegion>
        {
            new HitboxRegion("guideBtn", "Guide", 303, 87, 26, 20),
            new HitboxRegion("aBtn", "A", 433, 114, 24, 18),
            new HitboxRegion("bBtn", "B", 470, 85, 24, 18),
            new HitboxRegion("xBtn", "X", 400, 89, 24, 18),
            new HitboxRegion("yBtn", "Y", 437, 59, 24, 18),
            new HitboxRegion("lbBtn", "LB", 27, 59, 64, 18),
            new HitboxRegion("rbBtn", "RB", 536, 62, 64, 18),
            new HitboxRegion("ltBtn", "LT", 48, 17, 42, 36),
            new HitboxRegion("rtBtn", "RT", 544, 20, 42, 36),
            new HitboxRegion("backBtn", "Back", 257, 86, 20, 20),
            new HitboxRegion("startBtn", "Start", 353, 88, 20, 20),
            new HitboxRegion("lsbBtn", "Left Stick", 174, 102, 16, 16),
            new HitboxRegion("lsuBtn", "LS Y-Axis-", 159, 81, 47, 16),
            new HitboxRegion("lsrBtn", "LS X-Axis+", 197, 96, 16, 28),
            new HitboxRegion("lsdBtn", "LS Y-Axis+", 167, 122, 28, 16),
            new HitboxRegion("lslBtn", "LS X-Axis-", 148, 95, 16, 28),
            new HitboxRegion("rsbBtn", "Right Stick", 370, 169, 16, 16),
            new HitboxRegion("rsuBtn", "RS Y-Axis-", 365, 149, 26, 16),
            new HitboxRegion("rsrBtn", "RS X-Axis+", 392, 161, 16, 28),
            new HitboxRegion("rsdBtn", "RS Y-Axis+", 363, 189, 28, 16),
            new HitboxRegion("rslBtn", "RS X-Axis-", 347, 167, 16, 16),
            new HitboxRegion("dpadUBtn", "Dpad Up", 240, 136, 16, 12),
            new HitboxRegion("dpadRBtn", "Dpad Right", 268, 150, 16, 30),
            new HitboxRegion("dpadDBtn", "Dpad Down", 244, 187, 16, 12),
            new HitboxRegion("dpadLBtn", "Dpad Left", 214, 148, 16, 30),
            new HitboxRegion("touchpadClickBtn", "Touchpad Click", 256, 57, 126, 72, isVisible: false)
        });

        /// <summary>出力ピッカー（BindingWindow）の DualShock 4 の図。ボタンは Xbox 360 と同じ ID で、位置と一部の大きさ、タッチパッド クリックの表示が違う。</summary>
        public static ControllerLayout DualShock4 { get; } = new ControllerLayout("DualShock4", 630, 247, new List<HitboxRegion>
        {
            new HitboxRegion("guideBtn", "Guide", 303, 162, 26, 20),
            new HitboxRegion("aBtn", "A", 442, 148, 24, 18),
            new HitboxRegion("bBtn", "B", 474, 120, 24, 18),
            new HitboxRegion("xBtn", "X", 408, 116, 24, 18),
            new HitboxRegion("yBtn", "Y", 440, 90, 24, 18),
            new HitboxRegion("lbBtn", "LB", 154, 24, 46, 20),
            new HitboxRegion("rbBtn", "RB", 428, 24, 46, 20),
            new HitboxRegion("ltBtn", "LT", 162, 6, 46, 20),
            new HitboxRegion("rtBtn", "RT", 428, 6, 46, 20),
            new HitboxRegion("backBtn", "Back", 218, 76, 20, 20),
            new HitboxRegion("startBtn", "Start", 395, 76, 20, 20),
            new HitboxRegion("lsbBtn", "Left Stick", 238, 182, 16, 16),
            new HitboxRegion("lsuBtn", "LS Y-Axis-", 230, 160, 32, 16),
            new HitboxRegion("lsrBtn", "LS X-Axis+", 264, 176, 16, 28),
            new HitboxRegion("lsdBtn", "LS Y-Axis+", 232, 202, 32, 16),
            new HitboxRegion("lslBtn", "LS X-Axis-", 216, 176, 16, 28),
            new HitboxRegion("rsbBtn", "Right Stick", 377, 184, 16, 16),
            new HitboxRegion("rsuBtn", "RS Y-Axis-", 370, 160, 32, 16),
            new HitboxRegion("rsrBtn", "RS X-Axis+", 400, 176, 16, 28),
            new HitboxRegion("rsdBtn", "RS Y-Axis+", 370, 200, 32, 16),
            new HitboxRegion("rslBtn", "RS X-Axis-", 352, 176, 16, 28),
            new HitboxRegion("dpadUBtn", "Dpad Up", 170, 100, 16, 12),
            new HitboxRegion("dpadRBtn", "Dpad Right", 194, 112, 16, 30),
            new HitboxRegion("dpadDBtn", "Dpad Down", 170, 144, 16, 12),
            new HitboxRegion("dpadLBtn", "Dpad Left", 144, 112, 16, 30),
            new HitboxRegion("touchpadClickBtn", "Touchpad Click", 256, 57, 126, 72)
        });

        /// <summary>プロファイル編集画面（ProfileEditor）の Controls タブの DualShock 4 の図。基準の大きさは図の画像（440×220）。
        /// 大きさ 0 で隠れているボタン（Mute、Function、パドル）は、DualSense Edge などの追加の入力のために ID だけ持つ。</summary>
        public static ControllerLayout DualShock4Editor { get; } = new ControllerLayout("DualShock4Editor", 440, 220, new List<HitboxRegion>
        {
            new HitboxRegion("crossConBtn", null, 330, 125, 26, 26),
            new HitboxRegion("circleConBtn", null, 357, 99, 26, 26),
            new HitboxRegion("squareConBtn", null, 300, 98, 26, 26),
            new HitboxRegion("triangleConBtn", null, 330, 74, 26, 26),
            new HitboxRegion("l1ConBtn", null, 75, 22, 50, 18),
            new HitboxRegion("r1ConBtn", null, 317, 22, 50, 18),
            new HitboxRegion("l2ConBtn", null, 78, 0, 46, 20),
            new HitboxRegion("r2ConBtn", null, 316, 0, 46, 20),
            new HitboxRegion("shareConBtn", null, 134, 63, 16, 24),
            new HitboxRegion("optionsConBtn", null, 291, 63, 16, 24),
            new HitboxRegion("guideConBtn", null, 211, 144, 20, 20),
            new HitboxRegion("muteConBtn", null, 0, 0, 0, 0, isVisible: false),
            new HitboxRegion("fnlConBtn", null, 0, 0, 0, 0, isVisible: false),
            new HitboxRegion("fnrConBtn", null, 0, 0, 0, 0, isVisible: false),
            new HitboxRegion("blpConBtn", null, 0, 0, 0, 0, isVisible: false),
            new HitboxRegion("brpConBtn", null, 0, 0, 0, 0, isVisible: false),
            new HitboxRegion("leftTouchConBtn", null, 158, 69, 37, 51),
            new HitboxRegion("multiTouchConBtn", null, 199, 74, 41, 45),
            new HitboxRegion("rightTouchConBtn", null, 251, 70, 32, 53),
            new HitboxRegion("topTouchConBtn", null, 157, 46, 125, 23),
            new HitboxRegion("l3ConBtn", null, 138, 147, 42, 42),
            new HitboxRegion("lsuConBtn", null, 150, 140, 17, 15),
            new HitboxRegion("lsrConBtn", null, 172, 162, 17, 15),
            new HitboxRegion("lsdConBtn", null, 150, 180, 17, 15),
            new HitboxRegion("lslConBtn", null, 126, 162, 17, 15),
            new HitboxRegion("r3ConBtn", null, 262, 149, 42, 42),
            new HitboxRegion("rsuConBtn", null, 275, 142, 17, 15),
            new HitboxRegion("rsrConBtn", null, 298, 162, 17, 15),
            new HitboxRegion("rsdConBtn", null, 275, 180, 17, 15),
            new HitboxRegion("rslConBtn", null, 248, 162, 17, 15),
            new HitboxRegion("upConBtn", null, 88, 74, 20, 35),
            new HitboxRegion("rightConBtn", null, 103, 103, 35, 20),
            new HitboxRegion("downConBtn", null, 88, 114, 20, 35),
            new HitboxRegion("leftConBtn", null, 56, 103, 35, 20)
        });
    }
}
