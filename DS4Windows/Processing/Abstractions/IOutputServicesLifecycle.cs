namespace DS4Windows.Processing.Abstractions
{
    /// <summary>
    /// サービスの開始・停止のうち、出力側のサービス（仮想パッドのドライバー、仮想キーボード・マウス、HidHide、OSC、UDP のモーション配信）の
    /// 開始・停止の手順をまとめる。入力デバイスの探索・接続・切断の手順は <see cref="IServiceLifecycle"/> が持ち、その途中の決まった位置でこの契約を呼ぶ。
    /// 順序（ドライバーの開始 → キーボード・マウスの初期化 → 2 秒待ち、停止時の出力キューの待ち → スロットの停止 → ドライバーの破棄 → キーボード・マウスの切断）は、
    /// 各メソッドの内側で保つ。
    /// </summary>
    public interface IOutputServicesLifecycle
    {
        /// <summary>
        /// 仮想パッドのドライバーを開始し、使える状態なら、キーボード・マウスの初期化とログを行って true を返す。
        /// ドライバーが使えないときは、理由（未導入、未対応の版、接続失敗）をログとトレイに出して false を返す。
        /// </summary>
        bool StartVirtualOutputs(bool showlog);

        /// <summary>HidHide の対象の属性を更新する。</summary>
        void UpdateHidHide();

        /// <summary>設定で有効な OSC の受信・送信と、UDP のモーション配信を開始する（UDP は状態の切り替えが終わるまで待つ）。</summary>
        void StartNetworkOutputs();

        /// <summary>UDP のモーション配信が作られていれば、待ち受けを始める。</summary>
        void StartUdpListening();

        /// <summary>OSC のデバイス状態をリセットする（切断時）。</summary>
        void ResetDeviceState(int index);

        /// <summary>動いている OSC の受信・送信と UDP のモーション配信を止める。</summary>
        void StopNetworkOutputs();

        /// <summary>
        /// 出力キューの完了を待って出力スロットを止め、仮想パッドのドライバーを破棄し、キーボード・マウスを切断する。
        /// <paramref name="anyUnplugged"/> が true のときは、取り外しの完了を待つ時間を置いてからドライバーを破棄する。
        /// </summary>
        void StopVirtualOutputs(bool anyUnplugged);
    }
}
