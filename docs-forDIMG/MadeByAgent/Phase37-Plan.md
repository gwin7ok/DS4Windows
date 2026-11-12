# Phase37 計画書: 設定の保持と永続化の、大きい型の分割

作成日: 2026-11-12
状態: **未着手。承認待ち**
出典: `docs-forDIMG/Code-Review-2026-11/07-External-Review-2026-11-12.md`（外部レビューのカテゴリ 4）、`05-Extensibility.md` の 5-5、`Structure-Review-2026-11.md` の A（Phase25 の続き）
前提: なし（Phase29〜36 と独立に進められる）。ただし、Phase31 の 31-A（プログラム起動の一本化）が `ProfileMiscLoadSection` と `BackingStore.ProcessInspector` に触れるため、37-3・37-4 はそのあとに行う。

## 目的
設定の保持（`BackingStore`）と、保存・読み込み（`SpecialActionsXmlSerializer` ほか）にある長い処理を、領域・種別ごとの部品へ分ける。**保存するファイルの形（`Profiles.xml`、プロファイルの XML、`Actions.xml`）は、1 文字も変えない。**

## 現状（調査。2026-11-12 の実測）
| ファイル | 行数 | 中身 |
|---|---|---|
| `Core/Persistence/SpecialActionsXmlSerializer.cs` | 689 | `LoadActions` 178 行、`SaveAction` 97 行、`SaveActionNew` 84 行、`SaveActions` 56 行。**保存の入口が 2 つ**ある（`SaveAction` と `SaveActionNew`）。どちらも、種別を数（`mode`。1〜11）で受け、`switch` で枝分かれする（`SaveActionNew` は同 `250`〜`284` 行）。呼び出し側も数を直接渡す（例: `CheckBatteryViewModel.cs:162` が `6`） |
| `Core/Settings/BackingStore.cs` | 1,179 | メソッド 39 個。`ResetProfile` が 140 行（各領域の値を既定へ戻す処理を、1 か所で書いている）。公開メンバーは約 215。Phase25 で、スロットごとの設定を領域のオブジェクト（`GyroSlotSettings` など 8 つ）にした。まだ配列のままの項目が残る |
| `Core/Persistence/Dto/AppSettingsDTO.cs` | 1,126 | `Profiles.xml` の形を保つための入れ物。コントローラー 8 台ぶんの同じ形の項目（`Controller1CurrentProfile`〜`8`、`SelectedProfile1`〜 など）と、項目ごとの「書き出すかどうか」（`ShouldSerialize…`）が並ぶ。`XmlSerializer` が読む形なので、項目の名前と並びが、そのままファイルの形になる |
| `Core/Persistence/ProfileSections/` | 15＋7 | プロファイルの読み込みは 15 の節、保存は 7 の節。同じ項目が、読み込みと保存で別の名前の節にある（5-5） |

- 守り: 保存の黄金テスト（`GoldenPersistenceTests`、`ProfileGoldenGapTests`、`DS4WindowsTests/Golden`、`TestData/Actions.xml`）がある。`SpecialActionsXmlSerializer` に触れるテストのファイルは 15、`BackingStore` は 114。
- 種別ごとの部品の前例: 特殊アクションの出力の組み立て（`ISpecialActionOutputBuilder` が 10 個。Phase28）、プロファイルの節（`IProfileLoadSection`／`IProfileSaveSection`。Phase8）。

## 方針（決定事項の案）
1. **保存の形は変えない**。各 Step の前後で、黄金テストが 1 バイトも変わらないことを確かめる。足りない黄金（`Actions.xml` の全種別）は、先に足す（37-0）。
2. **特殊アクションの保存・読み込みを、種別ごとの部品にする**。出力の組み立て（`OutputBuilders`）と同じ形で、種別 1 つにつき 1 つの部品が、その種別の読み方と書き方を持つ。種別を数で渡すのをやめ、`SpecialAction.ActionTypeId` で渡す（呼び出し側の数の直書きをなくす）。保存する値（XML の中の数や名前）は変えない。
3. **`ResetProfile` は、領域のオブジェクトが自分を既定へ戻す形にする**（Phase25 の続き）。`BackingStore` は、領域を順に呼ぶだけにする。戻す順序と値は変えない。
4. **`AppSettingsDTO` は、形を保つ入れ物として残す**のを基本とする。行数は多いが、長い処理はなく、`XmlSerializer` の決まりに従った並びである。まとめ直すと、ファイルの形に触れる危険がある。扱いは D3 で決める。
5. ログの文言は変えない（規約 2.3）。読み込みの失敗の扱い（握りつぶして既定値にする箇所）も変えない。

## 決めること（着手前にユーザーへ確認）
| # | 決めること | 選択肢（先頭が推奨） |
|---|---|---|
| D1 | 範囲 | (A) 特殊アクションの保存・読み込み（37-1・37-2）と `ResetProfile`（37-3）まで／(B) 節の対（37-4）と、配列のままの項目の整理（37-5）も |
| D2 | 保存の入口が 2 つあること（`SaveAction` と `SaveActionNew`） | (A) 37-0 で差を調べ、1 つにできるなら 1 つにする（動作は不変）／(B) 2 つのまま、中身だけを種別ごとの部品へ |
| D3 | `AppSettingsDTO` | (A) そのまま残す（理由をクラスのコメントに書く）／(B) 8 台ぶんの同じ形の項目を、ファイルごとの `partial` に分けて読みやすくする（形は不変）／(C) 配列＋独自の読み書きに作り直す（形が変わる危険があり、勧めない） |
| D4 | 読み込みと保存の節を対にすること（5-5） | (A) 領域ごとに、読み込みと保存を 1 つの型へ寄せる（名前がそろい、項目を足す場所が 1 つになる）／(B) 見送る |

## Step 一覧
| Step | 内容 | テスト | 状態 |
|---|---|---|---|
| 37-0 | 調査と守りの準備: `SaveAction` と `SaveActionNew` の差（呼び出し元、書く内容、保存のあとの処理）を表にする。`Actions.xml` の黄金が、全種別（1〜11）と、古い形（移行前）を含むかを確かめ、足りなければ足す。`BackingStore` の、まだ配列のままの項目を一覧にする | 黄金の追加 | 未着手 |
| 37-1 | 特殊アクションの読み込み（`LoadActions` 178 行）を、種別ごとの部品へ。部品は `Core/Persistence` に置き、種別の列で受ける（同じ種別を 2 つ登録しない） | 黄金が不変。全種別に部品があることの型の検査 | 未着手 |
| 37-2 | 特殊アクションの保存（`SaveAction`／`SaveActionNew`）を、同じ部品へ（D2）。種別の受け渡しを `ActionTypeId` にし、呼び出し側の数の直書きをなくす | 黄金が不変。呼び出し側のテスト | 未着手 |
| 37-3 | `ResetProfile`（140 行）を、領域のオブジェクトの「既定へ戻す」へ分ける。`BackingStore` は順に呼ぶ | 既定値の全項目を、変更の前後で比べるテスト（既存の `BackingStoreStateFiller` を使う） | 未着手 |
| 37-4 | （D4 が A のとき）プロファイルの読み込みと保存の節を、領域ごとに対にする（まず 1 領域で行い、形を確かめてから広げる） | 黄金が不変 | 未着手 |
| 37-5 | （D1 が B のとき）まだ配列のままの項目を、領域のオブジェクトへ移す（Phase25 と同じ進め方。領域ごと） | 黄金が不変。`SettingsSlotWindowUsageTests` | 未着手 |
| 37-6 | `AppSettingsDTO`（D3） | 黄金が不変 | 未着手 |
| 37-7 | 文書: `02-Extension-Guide.md` の §3（特殊アクションを足す手順に、保存・読み込みの部品を足す）、`Structure-Review-2026-11.md` の A の更新履歴 | — | 未着手 |

## 確認の方法
- 各 Step のあと、保存の黄金テストと、`BackingStore`・特殊アクションのテスト。Step の終わりに Debug ALL。
- 実機: 既存のプロファイルと `Actions.xml`（利用者の実物の写し）を読み、保存し直して、差分が出ないこと。特殊アクションの各種別の作成・編集・複製・削除。プロファイルの新規作成（既定値）とプリセット。

## リスク
- 中。保存の形に触れる危険がある作業で、利用者のデータに直結する。黄金テストを先に厚くすること（37-0）と、1 Step を小さく保つことで抑える。
- `BackingStore` はテストからの参照が多い（114 ファイル）。公開メンバーの名前を変えると、テストの修正が広がる。名前は変えずに、中身だけを移す。

## 履歴
- 2026-11-12: 計画書を作成（外部レビューの照合の結果）。コードは未着手。
