# Phase14b 完了報告書: 依存の向きの是正

作成日: 2026-11-05
ブランチ: `Phase14b`（master f53b1959 から作成、master へ fast-forward で取り込み）
計画書: `Phase14b-Plan.md`（決定 D1〜D9 承認済み）
根拠: `Repository-Review-2026-11-05.md`（R-01〜R-04）

## 1. 結果の要約

- 層の依存の向き（規約 §1）を、アセンブリの実際の型参照で検査するテスト `LayerDependencyRuleTests`（ArchUnitNET 0.13.4）を加え、違反を許可リスト `LayerDependencyBaseline` で凍結して実数との一致を検査するようにした。
- 違反を是正し、禁止の規則の違反を **108 件 → 18 件** に減らした（L3［第1層 → 上位］と L5［第2層 → UI］は 0 件）。
- 機能・設定の保存形式・利用者に見えるログは変えていない。実機確認で問題なし（2026-11-05）。

## 2. 主な変更

| 区分 | 内容 |
|---|---|
| 検査（R-01） | `LayerDependencyRuleTests`／`LayerMap`／`LayerDependencyScanner`／`LayerDependencyBaseline`。規則 L1〜L7（L2・L7 は記録のみ） |
| 未使用の `using`（R-04） | `using static DS4Windows.Processing.Mouse` を 54 ファイルから削除。`TouchButtonActivationMode` を `Core/Settings` のトップレベルへ |
| 通知（R-03） | `INotificationService`／`NotificationEventArgs` → `Core/Abstractions`。`AppLogger` の未設定時のフォールバックを `NullNotificationService` に（2 つ目の `BackingStore` を作らない） |
| 第1層 → UI | `DS4Devices` の `UI.Services.NotificationService.ShowToast` を `AppLogger.LogToTray` の直接呼び出しへ（挙動は同じ） |
| UI の契約 | `IEditorWindowFactory`／`IMainWindowFactory`／`IViewModelFactory` → 新設の `UI/Abstractions` |
| 共有の型・契約 | `SpecialAction`・`MacroExecuteMode(s)`・`ProfileActions`・`ControlActionData` → `Core/Settings`、`BezierCurve` → `Core/Utilities`、`WindowTargetMatcher` → `Core/Platform`、`SelectedProfileChangedEventArgs`・`Window*` 3 型・`IAppSettingsService`・`IProfileLoadTarget` → `Core/Abstractions`、`IDeviceLayerSettings` → 新設の `Input/Abstractions` |
| 契約の変更 | `IProfileLoadTarget.GetTouchPad` の戻り値を `Mouse` → `ITouchPadGyroToggles`（新設。`Mouse` が実装、同じインスタンス） |
| フォールバックの撤去（D8） | `BackingStore.ProfileSettings` の「未登録なら自分で作る」を撤去し、未登録のまま使われたら例外 |
| 規約 | §3.1 に層の依存の検査、§3.4 R1 に `Input`・`UI` の `Abstractions`、R2 に Core・第1層が使う契約と共有の値の型の置き場所 |

## 3. 判断した点

- 決定（ユーザー承認）: D1 Phase14b／ブランチ、D2 ArchUnitNET、D3 Core → 第1層・第3層は記録のみ、D4 通知の契約を Core へ、D5 `TouchButtonActivationMode` は `Core/Settings`、D6 R-02 の 4 項目すべて、D7 `UI/Abstractions` 新設、D8 フォールバックの撤去、D9 実機確認は最後に 1 回。
- エージェントの判断（計画書 §2.1）: `IDeviceLayerSettings` は使う側の `Input/Abstractions`。`ITouchPadGyroToggles` の新設。ArchUnitNET は xUnit 拡張を使わず本体のパッケージだけ（版は公開から 2 週間以上の 0.13.4）。
- 調査の結論: `BackingStore.ProfileSettings` のフォールバックは通常の起動では通らなかった（計画書 §8.3）。撤去後も、フォールバックに頼るテストはなかった。

## 4. 検証

- Debug ALL: ビルド 警告 0・エラー 0、`DS4Windows.Actions.Tests` 3,229 件合格、`StandaloneTests` 11 件合格。
- Release ビルド 2 か所（`bin\x64\Release`、`bin\x64\DS4Windows`。2026-11-05 08:44）→ 実機確認（計画書 §8.6 の 6 項目）: **問題なし**（2026-11-05、ユーザー確認）。

## 5. 件数

| 時点 | 禁止の規則 | 記録のみ |
|---|---|---|
| 開始（Step 14b-1） | 108（L1 81、L3 5、L4 1、L5 15、L6 6） | L2 70、L7 80 |
| 完了（Step 14b-9） | **18**（L1 11、L4 1、L6 6） | L2 74、L7 80 |

L2 が増えたのは、Core へ移した型が第1層・第3層の型（`DS4Controls`、`DS4Device` など）を参照するため（D3 により記録のみ。是正はレビュー R-17）。

## 6. 作業上の記録

- コミット 20 件（移動と名前空間の書き換えは別コミット。R7）。`IProfileLoadTarget` の 1 件は `index.lock` の競合で移動に失敗し、移動だけの追加コミットにした。
- `StandaloneTests` の 3 ファイルをコミットに含め忘れ、追加のコミット（cbfc09a6）で補った。直前のコミット 8da0f58a 単体では `StandaloneTests` がビルドできない。
- 書き換えの補助スクリプト（scratchpad の `nsmove.py`）が、使用中の `using` を 1 件誤って消した。テストのビルドで検出して戻し、削除の判定を安全側に直した。

## 7. 残り（次の候補）

- 残る違反 18 件（計画書 §8.5 の一覧）。多くは 1 型の移動か小さな契約の切り出しで済む。
- `SpecialActionRepository`・`SpecialActionsXmlSerializer`（Core/Persistence）が第2層の `IManagedActionManager`・`SyntheticInputState` を使う件は、読み込み後の実行時状態のリセットを第2層へ寄せる設計が要る。
- 記録のみの L2（Core → 第1層・第3層、74 件）と L7（UI → 第1層・第3層、80 件）は、レビュー R-17・R-09 で扱う。
- レビューの R-05 以降（静的な状態、カートリッジ化など）は未着手。
