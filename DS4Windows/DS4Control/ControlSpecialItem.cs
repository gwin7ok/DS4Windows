using System.Collections.Generic;

namespace DS4Windows
{
    /// <summary>
    /// Phase10-Step4 W1（Phase10-Step4-Plan.md §3.1）: Controls のボタン 1 つの割り当てに持たせる、SA 専用の種別と項目の、<b>型のある</b>定義。
    /// SA（<see cref="SpecialAction"/>）は、種別ごとの意味を、<c>details</c>／<c>extra</c> の文字列に詰めている（例: Program はパスを details、引数と <c>$hidden</c> を extra）。
    /// これは、その文字列に詰めず、項目ごとに型を持つ。実行のときは、この項目から、合成の <see cref="SpecialAction"/> を作って、SA の出力アクションへ渡す（W2 以降）。
    /// <para>
    /// 使い方: <see cref="DS4ControlSettings.actionType"/> が <c>Special</c> のとき、<see cref="Type"/> が SA 専用の種別を表す
    /// （Program、Profile、DisconnectBT、BatteryCheck、MultiAction、較正 2 種）。<c>Macro</c> のとき、Macro の SA 専用の 3 項目
    /// （<see cref="RunOnRelease"/>、<see cref="Synchronized"/>、<see cref="KeepKeyState"/>）の入れ物になる（マクロの手順は、従来どおり <c>action.actionMacro</c>）。
    /// 種別に関係のない項目は、既定の値のまま（保存しない）。
    /// </para>
    /// </summary>
    public sealed class ControlSpecialItem
    {
        /// <summary>種別。Controls で使えるのは、Macro（3 項目の入れ物）、Program、Profile、DisconnectBT、BatteryCheck、MultiAction、較正 2 種。</summary>
        public SpecialAction.ActionTypeId Type { get; set; } = SpecialAction.ActionTypeId.None;

        /// <summary>押し続けてから成立するまでの秒数（SA の <c>delayTime</c>）。0 以上。</summary>
        public double HoldSeconds { get; set; }

        // ---- Macro の SA 専用の項目 ----

        /// <summary>トリガーを離したときに実行する（SA の「Run on trigger release」）。</summary>
        public bool RunOnRelease { get; set; }

        /// <summary>同じトリガーの、押したときと離したときのマクロを、順に実行する（SA の「Synchronized Run」）。</summary>
        public bool Synchronized { get; set; }

        /// <summary>マクロの終了後に、押しっぱなしのキーを自動で離さない（SA の「Keep key state」）。</summary>
        public bool KeepKeyState { get; set; }

        // ---- Program ----

        /// <summary>起動するプログラムのパス（SA の <c>details</c>）。</summary>
        public string ProgramPath { get; set; } = string.Empty;

        /// <summary>引数（SA の <c>extra</c> から、<c>$hidden</c> を除いたもの）。</summary>
        public string ProgramArguments { get; set; } = string.Empty;

        /// <summary>ウィンドウを表示せずに起動する（SA の <c>extra</c> の <c>$hidden</c>）。</summary>
        public bool HideWindow { get; set; }

        // ---- Profile ----

        /// <summary>切り替え先のプロファイル名（SA の <c>details</c>）。</summary>
        public string ProfileName { get; set; } = string.Empty;

        /// <summary>
        /// Phase10-Step4d W3: 切り替え先のプロファイルの固定 ID（プロファイル XML の <c>&lt;ProfileId&gt;</c>）。保存のとき、名前から引いて書く。
        /// 読み込みでは、ID が見つかれば、現在の名前（<see cref="ProfileName"/>）に置き換える（名前変更への追従）。ない・見つからないときは、名前のまま。
        /// </summary>
        public string ProfileId { get; set; } = string.Empty;

        /// <summary>離したときに、元のプロファイルへ自動で戻る（SA の <c>AutomaticUntrigger</c>）。</summary>
        public bool AutomaticUntrigger { get; set; }

        // ---- BatteryCheck ----

        /// <summary>電池が空のときの色（SA の <c>details</c> の「r,g,b|r,g,b」の前半）。</summary>
        public DS4Color EmptyColor { get; set; }

        /// <summary>電池が満タンのときの色（同、後半）。</summary>
        public DS4Color FullColor { get; set; }

        // TODO(Phase10-Step6 I2 → W7 相当): 作られた経緯＝SA の BatteryCheck の通知・ライトバーの 2 項目（details の 2 番目・3 番目）を、型のある項目として受けるため（変換部品 SpecialActionItemMapper が使う）。
        // 未接続である理由＝Check Battery はまだ Controls に来ていない（ControlsSpecialTypeTable に入らない）ため、プロファイル XML（ControlSpecialItemXml）には書かない。
        // 接続予定＝Check Battery を Controls へ移す作業（W7 相当）で、保存・読み込みと UI を足す。

        /// <summary>電池の残量を通知する（SA の BatteryCheck の通知）。</summary>
        public bool BatteryNotification { get; set; } = true;

        /// <summary>ライトバーで残量を示す（SA の BatteryCheck のライトバー）。</summary>
        public bool BatteryLightbar { get; set; } = true;

        // ---- MultiAction ----

        /// <summary>タップのマクロ（SA の <c>details</c> の「,」区切りの 1 つ目）。</summary>
        public List<int> TapMacro { get; set; } = new List<int>();

        /// <summary>長押しのマクロ（同、2 つ目）。</summary>
        public List<int> HoldMacro { get; set; } = new List<int>();

        /// <summary>ダブルタップのマクロ（同、3 つ目）。</summary>
        public List<int> DoubleTapMacro { get; set; } = new List<int>();

        /// <summary>Macro の SA 専用の項目が、1 つでも設定されているか（保存の要否の判定）。</summary>
        public bool HasMacroOptions => RunOnRelease || Synchronized || KeepKeyState;

        /// <summary>独立した複製（編集用のコピーのため。リストも複製する）。</summary>
        public ControlSpecialItem Clone()
        {
            return new ControlSpecialItem
            {
                Type = Type,
                HoldSeconds = HoldSeconds,
                RunOnRelease = RunOnRelease,
                Synchronized = Synchronized,
                KeepKeyState = KeepKeyState,
                ProgramPath = ProgramPath,
                ProgramArguments = ProgramArguments,
                HideWindow = HideWindow,
                ProfileName = ProfileName,
                ProfileId = ProfileId,
                AutomaticUntrigger = AutomaticUntrigger,
                EmptyColor = EmptyColor,
                FullColor = FullColor,
                BatteryNotification = BatteryNotification,
                BatteryLightbar = BatteryLightbar,
                TapMacro = new List<int>(TapMacro),
                HoldMacro = new List<int>(HoldMacro),
                DoubleTapMacro = new List<int>(DoubleTapMacro),
            };
        }
    }
}
