using System.Collections.Generic;
using DS4Windows;

namespace DS4WinWPF.DS4Forms.ViewModels
{
    /// <summary>
    /// Phase9 UI 変更（ユーザー承認 2026-10-22）: 入力ボタンの一覧の、<b>表示順と表示名の唯一の定義</b>。
    /// Controls タブの一覧（<c>MappingListViewModel</c> の表示順と各ボタンの名前）と、SA タブのトリガーの一覧（<see cref="TriggerCatalog"/>）が、この表から作られる。
    /// 並びは、SA のトリガーの一覧の従来の並びを基準にし、LS／RS Outer だけ、LS／RS の 4 方向の下に置く（L2／R2 Full Pull は、L2／R2 の下）。
    /// 表示名は、スティックは「LS Up」、DualSense Edge は「Function L」「Paddle L」。保存する名前（<see cref="InputControlEntry.Name"/>）は、従来のまま変えない。
    /// 状態を持たない（不変）。Controls の内部の添字（<c>MappingListViewModel.Mappings</c>）は、この並びとは関係ない（入力による選択、ホバーの表が使うため）。
    /// </summary>
    public static class InputControlCatalog
    {
        private static readonly InputControlEntry[] entries =
        {
            new InputControlEntry(DS4Controls.Cross, "Cross", "Cross"),
            new InputControlEntry(DS4Controls.Circle, "Circle", "Circle"),
            new InputControlEntry(DS4Controls.Square, "Square", "Square"),
            new InputControlEntry(DS4Controls.Triangle, "Triangle", "Triangle"),
            new InputControlEntry(DS4Controls.Options, "Options", "Options"),
            new InputControlEntry(DS4Controls.Share, "Share", "Share"),
            new InputControlEntry(DS4Controls.DpadUp, "Up", "Up"),
            new InputControlEntry(DS4Controls.DpadDown, "Down", "Down"),
            new InputControlEntry(DS4Controls.DpadLeft, "Left", "Left"),
            new InputControlEntry(DS4Controls.DpadRight, "Right", "Right"),
            new InputControlEntry(DS4Controls.PS, "PS", "PS"),
            new InputControlEntry(DS4Controls.Mute, "Mute", "Mute"),
            new InputControlEntry(DS4Controls.Capture, "Capture", "Capture"),
            new InputControlEntry(DS4Controls.SideL, "SideL", "Side L"),
            new InputControlEntry(DS4Controls.SideR, "SideR", "Side R"),
            new InputControlEntry(DS4Controls.L1, "L1", "L1"),
            new InputControlEntry(DS4Controls.R1, "R1", "R1"),
            new InputControlEntry(DS4Controls.L2, "L2", "L2"),
            new InputControlEntry(DS4Controls.L2FullPull, "L2 Full Pull", "L2 Full Pull"),
            new InputControlEntry(DS4Controls.R2, "R2", "R2"),
            new InputControlEntry(DS4Controls.R2FullPull, "R2 Full Pull", "R2 Full Pull"),
            new InputControlEntry(DS4Controls.L3, "L3", "L3"),
            new InputControlEntry(DS4Controls.R3, "R3", "R3"),
            // DualSense Edge の追加ボタン
            new InputControlEntry(DS4Controls.FnL, "Function Left", "Function L"),
            new InputControlEntry(DS4Controls.FnR, "Function Right", "Function R"),
            new InputControlEntry(DS4Controls.BLP, "Bottom Left Paddle", "Paddle L"),
            new InputControlEntry(DS4Controls.BRP, "Bottom Right Paddle", "Paddle R"),
            new InputControlEntry(DS4Controls.TouchLeft, "Left Touch", "Left Touch"),
            new InputControlEntry(DS4Controls.TouchUpper, "Upper Touch", "Upper Touch"),
            new InputControlEntry(DS4Controls.TouchMulti, "Multitouch", "Multitouch"),
            new InputControlEntry(DS4Controls.TouchRight, "Right Touch", "Right Touch"),
            new InputControlEntry(DS4Controls.LYNeg, "Left Stick Up", "LS Up"),
            new InputControlEntry(DS4Controls.LYPos, "Left Stick Down", "LS Down"),
            new InputControlEntry(DS4Controls.LXNeg, "Left Stick Left", "LS Left"),
            new InputControlEntry(DS4Controls.LXPos, "Left Stick Right", "LS Right"),
            new InputControlEntry(DS4Controls.LSOuter, "LS Outer", "LS Outer"),
            new InputControlEntry(DS4Controls.RYNeg, "Right Stick Up", "RS Up"),
            new InputControlEntry(DS4Controls.RYPos, "Right Stick Down", "RS Down"),
            new InputControlEntry(DS4Controls.RXNeg, "Right Stick Left", "RS Left"),
            new InputControlEntry(DS4Controls.RXPos, "Right Stick Right", "RS Right"),
            new InputControlEntry(DS4Controls.RSOuter, "RS Outer", "RS Outer"),
            new InputControlEntry(DS4Controls.SwipeUp, "Swipe Up", "Swipe Up"),
            new InputControlEntry(DS4Controls.SwipeDown, "Swipe Down", "Swipe Down"),
            new InputControlEntry(DS4Controls.SwipeLeft, "Swipe Left", "Swipe Left"),
            new InputControlEntry(DS4Controls.SwipeRight, "Swipe Right", "Swipe Right"),
            new InputControlEntry(DS4Controls.GyroZNeg, "Tilt Up", "Tilt Up"),
            new InputControlEntry(DS4Controls.GyroZPos, "Tilt Down", "Tilt Down"),
            new InputControlEntry(DS4Controls.GyroXPos, "Tilt Left", "Tilt Left"),
            new InputControlEntry(DS4Controls.GyroXNeg, "Tilt Right", "Tilt Right"),
            new InputControlEntry(DS4Controls.TouchStarted, "Touch Started", "Touch Started"),
            new InputControlEntry(DS4Controls.TouchEnded, "Touch Ended", "Touch Ended"),
        };

        /// <summary>全項目（画面に出す順）。</summary>
        public static IReadOnlyList<InputControlEntry> Entries => entries;

        /// <summary>入力の表示名。カタログにないときは、列挙子の名前。</summary>
        public static string LabelOf(DS4Controls control)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].Control == control) return entries[i].Label;
            }

            return control.ToString();
        }
    }
}
