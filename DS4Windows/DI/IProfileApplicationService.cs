using DS4Windows;

namespace DS4Windows.DI
{
    public interface IProfileApplicationService
    {
        void ApplyFromAction(int deviceIndex, SpecialAction action);
        bool RestoreFromAction(int deviceIndex);

        /// <summary>
        /// 指定されたスロットに対してプロファイルを適用します（Halt保護を内包）。
        /// </summary>
        /// <param name="deviceIndex">デバイスインデックス（0〜スロット上限 − 1。最大 8 台のとき 0〜7）</param>
        /// <param name="profileName">適用するプロファイル名</param>
        /// <param name="isTemp">一時プロファイルフラグ（既定: false）</param>
        /// <param name="launchProgram">関連プログラムを起動するか（既定: false）</param>
        /// <param name="source">プロファイル切替要因（既定: Manual）</param>
        /// <param name="prolog">通知ログ等の前置テキスト（既定: null）</param>
        /// <returns>適用の成否（true: 成功, false: 失敗）</returns>
        bool ApplyProfile(int deviceIndex, string profileName, bool isTemp = false, bool launchProgram = false,
                    ProfileChangeSource source = ProfileChangeSource.Manual,
                    string prolog = null);

        /// <summary>
        /// 入力の読み取りを一時停止（Halt）せずにプロファイルを適用します（接続処理中など、呼び出し元がすでに停止している経路用。
        /// Phase7-Step3c W3 で追加。旧 <c>Global.ApplyProfileToSlot</c> と同じく例外は握りません）。
        /// </summary>
        bool ApplyProfileWithoutHalt(int deviceIndex, string profileName, bool isTemp = false, bool launchProgram = false,
                    ProfileChangeSource source = ProfileChangeSource.Manual,
                    string prolog = null);

        /// <summary>
        /// Phase8-Step5 W2（Phase8-Step5-Plan.md §3.2。Q6＝A）: そのデバイスの特殊アクションから束縛を組み立てて、登録簿へ差し替える。
        /// 適用の経路（<c>ApplyProfile</c> ほか）は後始末の中で呼ぶので、外から呼ぶ必要はない。適用の後始末を通らずにプロファイルを読み込む経路
        /// （名前付きパイプの「一時プロファイルの読込」）が、読込の成功後に呼ぶ。
        /// 範囲外のデバイス（編集スロットを含む）では何もしない。組み立てに失敗しても例外は投げない（そのデバイスの束縛は空になる）。
        /// </summary>
        void PublishBindings(int deviceIndex);
    }
}
