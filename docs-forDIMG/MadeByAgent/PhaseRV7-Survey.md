# PhaseRV7-0: 調査と、承認をお願いする点

作成日: 2026-11-20

## 1. 実施済み（承認不要の機械的な変更）
- RV7-4b: 文の形の `?? throw new ArgumentNullException(nameof(x))` 653 件を `ArgumentNullException.ThrowIfNull(x)` へ。残る 19 件は、式の中（`var a = b ?? throw`、`(x ?? throw …).Prop` など）で、文の形でないため触らない。
- RV7-1・2: 独自の `ObservableObject` を削除し、`CommunityToolkit.Mvvm` の `ObservableObject` へ。継承 12 型、直接実装 8 型（`Core` の `ProfileEntity`・`ProfileSubSettingBase` を含む）、`NotifyDataErrorBase` と、それを継ぐ 3 つの ViewModel。`INotifyPropertyChanged` の直接実装は 0（`ProfileEditor` は、すでに実装していなかった）。
- RV7-3（改名のみ）: 他の識別子と衝突しない小文字始まりのメソッド 49 件を PascalCase へ（名前だけ。ログの文言・文字列は不変）。
- RV7-5: `StandaloneTests`（MSTest）を xUnit へ移して削除。`debug-all.yml` の手順 2 つを削除。テスト 3,687→3,698（空の `StressDispatchRaceTests` は移さず）。
- RV7-6: `UnifiedCodingPracticeRuleTests`（`ThrowIfNull`、`INotifyPropertyChanged` の直接実装 0）。

## 2. 残る小文字始まりのメソッド（承認をお願いする点 A）
衝突（同名の PascalCase が別の型にある）、または、名前が文字列・テスト（「撤去済みであること」を検査する文字列）に出るため、一括の改名から外した。

| 種類 | 名前 | 判断案 |
|---|---|---|
| 同名の PascalCase が別の型・インターフェースにある | `InputDeviceCore` の `isHidExclusive`・`isExclusive`・`getConnectionType`・`getBattery`・`isCharging`・`getBTPollRate`・`setRumble`・`queueEvent`・`updateSerial`、`InputDeviceRegistry` の `findControllers`・`stopControllers`・`reEnableDevice`、`DS4Touchpad`／`MouseCursor`／`MouseWheel`／`TouchpadMouse` の `touchesBegan`・`touchesMoved`・`touchesEnded`・`touchUnchanged`・`touchButtonUp`・`touchButtonDown`・`sixaxisMoved`、`writeOutput`、`SixAxis`・`Touch` の `populate`・`copy` | インターフェース側の PascalCase と同じ実体の可能性が高い。型ごとに、衝突を 1 件ずつ確かめて改名する（`InputDeviceCore` が最大。別の続きの作業にする） |
| 撤去済みの確認の文字列に名前が出る | `getDS4ControlsByName`・`getX360ControlsByName`（`DS4ControlNames`・`X360ControlTables` に静的メソッドが残る）、`createControllerConfigs`・`createLinkedProfiles`、`getTransitionedColor`、`sendOutputReport` | 文字列を持つガードテスト（`NameFunctionsTests`・`DeadShimRemovalGuardTests`・`SerializersGuardTests`）も同時に直す必要がある |
| 例外 | `flush_Queue`（`HidDevice`）、`getX360ControlString` | 改名のみ |

プロパティ化（D2＝(B)）の対象は、この残りが改名されたあとに、引数なしの `getXxx()`／`isXxx()`（`getBattery`・`isCharging`・`isExclusive`・`isHidExclusive`・`getConnectionType`・`getBTPollRate`・`getIdleTimeout`・`getMacAddress`・`getSerial`・`isSynced`・`getWarnInterval` など約 15 件）と、`getIdleTimeout`／`setIdleTimeout` の対に絞る。引数あり、副作用あり（`updateSerial`・`queueEvent`）、`ref` を返す（`GetCurrentStateRef`・`GetPreviousStateRef`）ものは、メソッドのまま。`InputDeviceCore` の呼び出し元が全層に広いため、改名のあと、別の Phase として扱うことを提案する。

## 3. タイマー（承認をお願いする点 B。D3＝(B)）
| 場所 | 種類 | 用途（周期） | UI に触るか |
|---|---|---|---|
| `App.xaml.cs`（2 か所） | `System.Threading.Timer` | GC の起動（30 秒） | いいえ |
| `RepeatHelper` | `TimeProvider.CreateTimer` | キーの連打 | いいえ |
| `BatteryStatusLogger` | `System.Threading.Timer` | バッテリーのログ | いいえ |
| `CompositeDeviceModel` | `System.Threading.Timer` | バッテリー表示の更新 | はい（通知を介して） |
| `ControllerReadingsControl`・`MainWindow`・`ProfileEditor`・`RecordBox`・`WelcomeDialog`（3 か所） | `System.Timers.Timer`（別名 `NonFormTimer`） | 画面の周期処理 | はい |

統一先の案: バックグラウンド（UI に触らない）は `TimeProvider.CreateTimer`（時間を差し替えてテストできる）、UI に触るものは `System.Timers.Timer` の別名を外した直接の記述のまま、UI への戻しは `IUiDispatcher`＋`GuardedTimerTick`。「全部を 1 種類」にすると、UI のタイマーが別スレッドで走る危険があるため、**2 種類（UI／非 UI）に揃える案**を推奨する。実機確認（バッテリー、トレイ、自動プロファイル、連打）が必要。
- 別名 `using NonFormTimer = …`（5 か所）の廃止だけを先に行うことも可能（動きは変わらない）。

## 4. 承認をお願いする点
1. A: 残る小文字のメソッドの改名を、続きの作業（型ごと）として行うか。プロパティ化は、その後に別の Phase として扱うか。
2. B: タイマーを「UI／非 UI の 2 種類」へ揃える案でよいか（D3＝(B) の「1 種類」から変更の提案）。別名の廃止だけを先に行ってよいか。

## 5. 承認の結果（2026-11-20）
タイマーは「UI／非 UI の 2 種類」で承認。実施: 別名 `NonFormTimer`（5 ファイル）を `System.Timers.Timer` の直接の記述へ。`System.Threading.Timer`（`App` の GC、`BatteryStatusLogger`、`CompositeDeviceModel`）を `TimeProvider.System.CreateTimer` へ。検査は `UnifiedCodingPracticeRuleTests`。規約への追記（§3.1 に 1〜2 行）の文面は、承認を得てから足す。実機確認は `Device-Check-Checklist.md` §9g。
