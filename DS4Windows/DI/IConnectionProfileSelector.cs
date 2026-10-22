namespace DS4Windows.DI
{
    /// <summary>
    /// Phase9 9-E E-6a（Phase9-E6-Plan.md）: コントローラーをスロットへつなぐときの、プロファイルの選択と適用。
    /// 以前は <c>ControlService.PrepareConnectedInputControllerSettingEvents</c> の前半だった。
    /// 初回接続は Linked プロファイルか OlderProfilePath、再接続はそのコントローラー（MAC）が直前に使っていたプロファイル。
    /// 一時プロファイル／AutoProfile を使う接続は、プロファイルを適用し直さず、束縛だけを差し替える。
    /// </summary>
    public interface IConnectionProfileSelector
    {
        /// <summary>
        /// プロファイルを選んで、スロットへ適用する。適用できたら true。
        /// <paramref name="isValidSerial"/> が false のとき（MAC が使えない）は、<paramref name="macAddress"/> は使わず、従来のスロット単位で判定する。
        /// </summary>
        bool ApplyOnConnect(int index, bool useAutoProfile, bool isValidSerial, string macAddress);
    }
}
