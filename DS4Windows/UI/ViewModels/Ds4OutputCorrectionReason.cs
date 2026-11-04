namespace DS4WinWPF.DS4Forms.ViewModels
{
    /// <summary>
    /// Phase11 W4（Issue #4 の 1）: <see cref="ProfileSettingsViewModel.CorrectOutputTypeToDs4"/> が、出力の種類を DS4 へ変えた理由。
    /// </summary>
    public enum Ds4OutputCorrectionReason
    {
        /// <summary>変えなかった（すでに DS4、または補正の対象となる設定が OFF）</summary>
        None,

        /// <summary>DS3 のピッチ／ロールの擬似（<c>UseDs3PitchRollSim</c>）が ON のため、DS4 へ変えた</summary>
        Ds3PitchRollSim,

        // Phase11 W6c（K-W6-12）: 以前ここにあった EnableOutputDataToDS4［DS4 固有データの出力が ON のため DS4 へ変えた］は、補正 (c) の廃止に伴い削除した
    }
}
