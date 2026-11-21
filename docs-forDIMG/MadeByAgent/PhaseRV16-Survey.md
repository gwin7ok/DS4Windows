# PhaseRV16 調査: 500 行以上の型の候補（責務の境の判定）

作成日: 2026-11-21

| 型（行数） | 読んだ結果 | 判定 |
|---|---|---|
| `AutoProfilesViewModel`（694） | ショートカットの解決（約 90 行。`StartupEntryService` に同じ処理の重複あり）、選択位置とプロファイル名の対応（約 90 行。8 回の繰り返し）に境がある | **実施**: `ShortcutResolver`（RV16-1）、`AutoProfileSelectionMapper`（RV16-3）。694→517 |
| `SpecialActionsListViewModel`（約 590） | 並べ替え（列・方向の状態と比較）が独立している | **実施**: `SpecialActionListSorter`（RV16-2）。→529 |
| `TouchpadMouse`（730） | すでに多くの部品（`TapClickDetector`・`TouchButtonDetector`・`TouchStickOutput`・`MouseCursor` ほか）へ分け済み。残りは、それらを結ぶ調整役で、境が曖昧 | 見送り（無理に分けると「何でも箱」になる） |
| `GyroSettingsViewModel`（592） | 約 370 行は、タブの Binding と同名のプロパティの宣言。ロジックは、トリガーの表と、方式の番号の検索だけ | 見送り（宣言的でよい。名前を変えられない） |
| `AppSettingsService`（642） | 設定項目ごとのプロパティ。RV10 で領域の型へ移した残り | 見送り（宣言的） |
| `SettingsViewModel`（731）・`RecordBoxViewModel`（572） | 今回は読んでいない | 未調査（RV17 以降の候補） |

結果: 500 行以上の型の件数は 30 のまま（`AutoProfilesViewModel` 517・`SpecialActionsListViewModel` 529 が、あと少しで 500 未満）。D3 の方針（件数でなく、単体で試験できる部品へ分けることが成果）に従い、500 未満にするためだけの分割はしていない。
