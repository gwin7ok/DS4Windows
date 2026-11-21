# PhaseRV15 調査: 構造の重複 11.6% の上位（記録のみ。整理は行っていない）

作成日: 2026-11-21
方法: `.cs` ごとに、空行・コメント・`{`・`}` を除いた 14 行の窓が、同じ形で複数回現れる数を数えた（`Measure-CodeReview.py` と同じ考え方の簡易集計）。

| # | 場所 | 重複の形 | 分類 | 方針 |
|---|---|---|---|---|
| 1 | `UI/MainWindow.xaml.cs`（窓の内部） | イベントハンドラーごとの同じ確認・更新の並び | 共通化できる（RV16 の分割と一緒に） | RV16 |
| 2 | `UI/ViewModels/AutoProfilesViewModel.cs` | 項目ごとの同じプロパティ通知・コマンドの並び | 宣言的でよい（プロパティの定型） | 触らない |
| 3 | `UI/ViewModels/SpecialActions/CheckBatteryViewModel`・`MultiActButtonViewModel` ほか | 種類ごとの ViewModel の読み込み・保存の並び | 共通化できる（基底または表） | RV16 以降 |
| 4 | `Core/Settings/DS4ControllerOptions`・`DualSenseControllerOptions`・`SwitchProControllerOptions` | 機種ごとの設定の定型 | `ControllerSetting` の定義から作る方向へ（RV17 で実施済みの流れの残り） | 別 Phase |
| 5 | `Core/Settings/AppSettingsService.cs` | 設定の項目ごとの代入（45%） | 宣言的でよいが、表にできる | 別 Phase |

分けるべきでないもの: 機種ごとの入力の読み取り（`ProcessFrame`）。形は似るが、バイト配置が違い、共通化は誤りを生む。
注意: 窓の数え方は簡易なので、順位は目安。整理の前に、各場所を読んで確かめる。
