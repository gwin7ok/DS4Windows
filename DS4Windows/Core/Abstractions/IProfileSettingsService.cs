using System.Collections.Generic;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;

namespace DS4Windows.Core.Abstractions
{

    public interface IProfileSettingsService
    {

        /// <summary>スロットの設定の入口。領域ごとの窓口を返す（利用者は、配列を直接使わず、こちらへ移していく）。</summary>
        ProfileSlotSettings ForSlot(int slot);

        bool[] TouchpadActiveArray { get; set; }
        // TODO(Phase8): 呼び出し元が 0 件のまま維持している。BackingStore の状態の窓口として作られたが、読み手が BackingStore を直接使っているため未接続。
        // BackingStore の分割（Phase8）とあわせて、読み手をこの契約へ切り替える。Phase7-Step10e W4 で経緯を確認し、削除せず維持と判断した。
        bool[] UseTempProfileArray { get; set; }
        bool[] TempProfileDistanceArray { get; set; }
        // Phase7-Step10d W5: 旧 Global.DistanceProfiles（BackingStore.distanceProfiles の配列そのもの）。ライトバー（DS4LightBar）が読む。
        bool[] DistanceProfiles { get; }
        bool[] UseDInputOnlyArray { get; set; }

        bool GetTouchpadActive(int deviceIndex);

        bool GetUseTempProfile(int deviceIndex);
        void SetUseTempProfile(int deviceIndex, bool value);

        string GetTempProfileName(int deviceIndex);
        void SetTempProfileName(int deviceIndex, string value);

        void SetTempProfileDistance(int deviceIndex, bool value);

        bool GetLinkedProfileCheck(int deviceIndex);
        void SetLinkedProfileCheck(int deviceIndex, bool value);

        X360Controls[] GetDefaultButtonMapping();

        // ---- Phase6-Step9-1: PresetMenuHelper／BindingWindowViewModel の Global.RefreshActionAlias 参照の置換先 ----
        /// <summary>
        /// 仮想キーの別名（actionAlias）を、現在の出力 KBM マッピング（<see cref="OutputKBMMapping"/>）で更新する
        /// （<c>Global.RefreshActionAlias</c> と同じ動作）。
        /// </summary>
        /// <param name="setting">編集中の <see cref="DS4ControlSettings"/></param>
        /// <param name="shift">シフト側のアクションを更新するか</param>
        void RefreshActionAlias(DS4ControlSettings setting);
        /// <summary>
        /// Xbox360 ボタン → DS4 ボタンの逆引き表そのもの（<c>Global.reverseX360ButtonMapping</c>）を、コピーせずに返す。
        /// 起動時に1回作られて以後変化しない定数表で、毎入力レポートの経路（<c>Mapping.ProcessControlSettingAction</c>）から引かれるため、
        /// 呼び出しごとに <c>Clone()</c> するコピー版は使わず、本プロパティを使う（ゼロアロケーション方針、Phase6-Step3-6a の決定 R1）。コピー版 <c>GetReverseX360ButtonMapping()</c> は、呼び出し元が 0 件のため Phase7-Step2 W1 で削除した。
        /// 返された配列は呼び出し側で書き換えないこと。書き換える必要がある場合は、呼び出し側で <c>Clone()</c> して使う。
        /// </summary>
        DS4Controls[] ReverseX360ButtonMapping { get; }

        // ---- Step10-2-A-1: スティック関連 (m_Config委譲) ----

        // ---- Step10-2-A-2: トリガー(L2/R2)関連 (m_Config委譲) ----
        /// <summary>スロットの出力の種類に関わる設定（保存される仮想コントローラーの種類、仮想のトリガーボタン、DS4 のトリガーの出力方式）の持ち主。</summary>
        OutputTypeSlotSettings OutputType(int slot);

        // ---- Step10-2-A-3: タッチパッド関連 (m_Config委譲) ----

        // ---- Step10-2-A-4: ジャイロ関連 (m_Config委譲) ----

        // ---- Step10-2-A-5: ライトバー・ランブル関連 (m_Config委譲) ----
        /// <summary>スロットの振動の設定（強さ、自動停止、モーターの入れ替え）の持ち主。</summary>
        RumbleSlotSettings RumbleSlot(int slot);
        /// <summary>スロットの DualSense 固有の設定（振動のエミュレーション方式、汎用の振動の再スケール、ハプティックの強さ）の持ち主。</summary>
        DualSenseSlotSettings DualSenseSlot(int slot);
        byte GetRumbleBoost(int deviceIndex);
        int GetRumbleAutostopTime(int deviceIndex);
        ref DS4Color GetMainColor(int deviceIndex);
        void SetRumbleAutostopTime(int index, int value);

        // ---- Step10-2-A-6: ボタン/マウス出力関連 (m_Config委譲) ----
        ButtonMouseInfo[] ButtonMouseInfos { get; }
        bool GetEnableTouchToggle(int deviceIndex);

        // ---- Step10-2-A-7: SA/デッドゾーン関連 (m_Config委譲) ----

        // ---- Step10-2-A-8: 残余設定・デバイスオプション (m_Config委譲) ----
        bool DS4Mapping { get; set; }
        /// <summary>スロットの基本的な設定（タッチパッドの切り替え、無操作での切断、出力データ、BT のポーリング、起動プログラム、DInput のみ）の持ち主。</summary>
        BasicSlotSettings BasicSlot(int slot);


        // ---- 課題④: ProfileActions インターフェースプロパティ ----
        List<string>[] ProfileActions { get; set; }

        int GetBTPollRate(int deviceIndex);
        bool GetDInputOnly(int deviceIndex);
        int GetIdleDisconnectTimeout(int deviceIndex);
        bool GetEnableOutputDataToDS4(int deviceIndex);
        DS4ControlSettings GetDS4CSetting(int deviceIndex, string control);
        DS4ControlSettings GetDS4CSetting(int deviceIndex, DS4Controls control);

        // ---- Step10-2-A-9: Mapping.cs専用 ----
        bool ProfileChangedNotification { get; set; }
        VirtualKBMMapping OutputKBMMapping { get; set; }

        // ---- Phase6-Step2-2 (PR-2): 接続時のエクストラボタン再構築（Global への薄い委譲）----
        void RefreshExtrasButtons(int deviceIndex, List<DS4Controls> buttons);

        // ---- Phase6-Step2-5 (PR-5): カスタムアクション／エクストラの有無（毎レポート評価。割り当てなし）----
        bool ContainsCustomAction(int deviceIndex);
        bool ContainsCustomExtras(int deviceIndex);

        // ---- Phase6-Step3-1: デバイスの入力→出力割り当てグループ（毎レポート評価。割り当てなし）----
        ControlSettingsGroup GetControlSettingsGroup(int deviceIndex);

        /// <summary>
        /// Phase8-Step7 7.8d: 廃止された「シフト」機能の設定（プロファイルの <c>&lt;ShiftControl&gt;</c>）を、解釈せずに保持した入れ物（スロットごと）。
        /// 編集画面が、プロファイルを読み込んだあと、警告（削除か残すか）に使う。
        /// </summary>
        RemovedShiftSettings GetRemovedShiftSettings(int deviceIndex);
    }
}