# PhaseRV16 計画書: 500 行以上の型の整理（部品化できる型から）

作成日: 2026-11-21
状態: **計画。決定事項 D1〜D4 の選択待ち（コードは未着手）**
出典: `Code-Review-2026-11-19/02-Scorecard.md`（RV15 完了時点）、2026-11-21 の型ごとの行数の集計（`partial` の合算）。
動き: **挙動・ログ・保存の形は変えない**。

## 目的
「500 行以上の型 30」（点 0.71。重み 1）を減らす。ただし、RV14 の教訓（D6）のとおり、**件数だけを目的にしない**。目的は「単体で試験できる部品へ分ける」こと。

## 読んで分かった実態
- 画面の型（`ProfileEditor` 2,025、`MainWindow` 1,828、`RecordBox` 995、`App` 983）は、すでに `partial` で 10 ファイル前後に分けてある（最大のファイルでも 480 行）。**ファイルは小さいが、型としては 1 つ**で、フィールドを共有している。さらに分けるには、`partial` の追加ではなく、**別の型（協力クラス、`UserControl`、ViewModel）へ状態ごと移す**必要がある。
- 画面の型は、見た目の確認が要る（実機確認 §9d〜§9l が後回しのまま積まれている）。
- 画面以外にも大きい型がある: `AppSettingsDTO` 1,183（保存の形そのもの。分割しにくい）、`BackingStore` 980、`SettingsViewModel` 731、`TouchpadMouse` 730、`AutoProfilesViewModel` 694、`ControllerReadingsControl` 648、`AppSettingsService` 642、`ProfilePresets` 602、`Mapping` 597、`GyroSettingsViewModel` 592、`SpecialActionsListViewModel` 588、`RecordBoxViewModel` 572 ほか。これらは、**画面の見た目に触れずに**（単体テストで）分けられる見込みが高い。
- 入力機器（`InputDeviceCore` 1,055、`DualSenseDevice` 1,016、`DualShock4Device` 808、`SwitchProDevice` 543）は RV14 で部品化済み。残りは入力の読み取り本体で、これ以上は見送り（RV14 の記録）。

## 決定してほしい事項

### D1 範囲
- **(A) 画面の外の型（ViewModel・サービス・設定）から（推奨）**: `SettingsViewModel`、`AutoProfilesViewModel`、`TouchpadMouse`、`GyroSettingsViewModel`、`SpecialActionsListViewModel`、`RecordBoxViewModel`、`AppSettingsService` のうち、責務の境が読み取れるものを選んで部品へ。単体テストで守れ、実機確認がほとんど要らない。
- (B) 画面の型（`MainWindow`・`ProfileEditor`・`RecordBox`・`App`）: 効果は大きいが、実機確認が多く、後回しの確認が積まれた状態では危険。実機確認（§9d〜）を先に行ってから。
- (C) 両方（長い Phase になる。推奨しない）。

### D2 型を選ぶ基準
- **(A) 先に Step 0 で各型を読み、「責務の境が 2 つ以上ある」「状態の持ち主が分けられる」ものだけを選ぶ（推奨）**。境が曖昧な型は見送りとして記録する（RV12 の「呼び出しの広さで見送り」と同じ）。
- (B) 行数の多い順に機械的に進める（境が曖昧な型で無理な分割になる）。

### D3 成果の基準
- **(A) 「単体で試験できる部品へ分けた」ことを成果とする（推奨）**。500 行未満になった型の数は副次の指標。見込み: 30→27〜28。
- (B) 500 行以上の件数の削減を目標にする（点は上がるが、部品の質が下がる恐れ）。

### D4 実機確認の扱い
- **(A) 画面に触れない範囲なので、通常の Debug ALL のみ。後回しの §9d〜§9l は、RV16 の前か後に、まとめて 1 回で行うことを推奨（推奨）**。
- (B) RV16 の前に、先に §9d〜§9l を行う（安全。ただし利用者の手間が先に来る）。

## Step（推奨の場合）
| Step | 内容 | 検査 |
|---|---|---|
| RV16-0 | 候補の型（上の 7 型）を読み、責務の境・状態の持ち主・呼び出しの広さを表にする（`PhaseRV16-Survey.md`）。分ける型を確定し、承認を得る | 記録のみ |
| RV16-1〜n | 型ごとに 1 Step: 状態を持たない計算は具象の部品へ、状態は持ち主の型へ。新しい部品に単体テスト（特性化テストを先に） | Debug ALL（`Run-DebugAll.ps1`）、`ConstructorArityBaseline` と各ラチェットの一致 |
| RV16-last | 再採点（`RV16.json`）、`02-Scorecard.md`、計画書の完了記録、`ModelDiagramGuardTests` | 記録 |

ブランチは `PhaseRV16`。コミットは Step ごと、完了で `--no-ff` マージ（push なし）。

## 予想（03-Rubric.md §5）
- 500 行以上の型 30→27〜28（点 0.71→約 0.75）。
- 等級の変化: 共有部品化 A−（84.6%）・オブジェクト指向 A−（87.8%）が約 +0.5 点動く見込み。**等級の変化はおそらくなし**。
- 画面の型は変わらない（RV17 以降、または実機確認のあと）。

## リスク
- ViewModel の分割は、バインディング名（プロパティ名）が変わると XAML が壊れる。**公開プロパティの名前は変えない**（`ViewCommandBindingRatchetTests`・XAML のキーの検査が見張る）。
- 状態の持ち主を動かすと、保存・読み込み（`AppSettingsRoundTripTests`、黄金テスト）に影響する。保存の形は変えない。
- 境が曖昧な型を無理に分けると、「何でも箱」になる。D2 の基準で見送る。

## 実機確認
基本は不要（画面の見た目に触れない）。ViewModel の分割で表示が変わりうる場合だけ、`Device-Check-Checklist.md` に追記する。
