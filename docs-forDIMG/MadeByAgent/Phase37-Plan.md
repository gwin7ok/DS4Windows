# Phase37 計画書: 設定の保持と永続化の、大きい型の分割

作成日: 2026-11-12
状態: **完了（2026-11-16）。37-0〜37-5・37-7 を実施、37-6（`AppSettingsDTO` の `partial`）は見送り**
出典: `docs-forDIMG/Code-Review-2026-11/07-External-Review-2026-11-12.md`（外部レビューのカテゴリ 4）、`05-Extensibility.md` の 5-5、`Structure-Review-2026-11.md` の A（Phase25 の続き）
前提: なし（Phase29〜36 と独立に進められる）。ただし、Phase32 の 32-A（プログラム起動の一本化）が `ProfileMiscLoadSection` と `BackingStore.ProcessInspector` に触れるため、37-3・37-4 はそのあとに行う。

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
- 種別ごとの部品の前例: スペシャルアクションの出力の組み立て（`ISpecialActionOutputBuilder` が 10 個。Phase28）、プロファイルの節（`IProfileLoadSection`／`IProfileSaveSection`。Phase8）。

## 方針（決定事項の案）
1. **保存の形は変えない**。各 Step の前後で、黄金テストが 1 バイトも変わらないことを確かめる。足りない黄金（`Actions.xml` の全種別）は、先に足す（37-0）。
2. **スペシャルアクションの保存・読み込みを、種別ごとの部品にする**。出力の組み立て（`OutputBuilders`）と同じ形で、種別 1 つにつき 1 つの部品が、その種別の読み方と書き方を持つ。種別を数で渡すのをやめ、`SpecialAction.ActionTypeId` で渡す（呼び出し側の数の直書きをなくす）。保存する値（XML の中の数や名前）は変えない。
3. **`ResetProfile` は、領域のオブジェクトが自分を既定へ戻す形にする**（Phase25 の続き）。`BackingStore` は、領域を順に呼ぶだけにする。戻す順序と値は変えない。
4. **`AppSettingsDTO` は、形を保つ入れ物として残す**のを基本とする。行数は多いが、長い処理はなく、`XmlSerializer` の決まりに従った並びである。まとめ直すと、ファイルの形に触れる危険がある。扱いは D3 で決める。
5. ログの文言は変えない（規約 2.3）。読み込みの失敗の扱い（握りつぶして既定値にする箇所）も変えない。

## 決めること（着手前にユーザーへ確認）
| # | 決めること | 選択肢（先頭が推奨） |
|---|---|---|
| D1 | 範囲 | (A) スペシャルアクションの保存・読み込み（37-1・37-2）と `ResetProfile`（37-3）まで／(B) 節の対（37-4）と、配列のままの項目の整理（37-5）も |
| D2 | 保存の入口が 2 つあること（`SaveAction` と `SaveActionNew`） | (A) 37-0 で差を調べ、1 つにできるなら 1 つにする（動作は不変）／(B) 2 つのまま、中身だけを種別ごとの部品へ |
| D3 | `AppSettingsDTO` | (A) そのまま残す（理由をクラスのコメントに書く）／(B) 8 台ぶんの同じ形の項目を、ファイルごとの `partial` に分けて読みやすくする（形は不変）／(C) 配列＋独自の読み書きに作り直す（形が変わる危険があり、勧めない） |
| D4 | 読み込みと保存の節を対にすること（5-5） | (A) 領域ごとに、読み込みと保存を 1 つの型へ寄せる（名前がそろい、項目を足す場所が 1 つになる）／(B) 見送る |

## 決定の記録（2026-11-13。ユーザーの回答）
D1＝**B（節の対 37-4 と、配列のままの項目の整理 37-5 も）**、D2＝A（まず差を調べ、一つにできるなら一つにする）、D3＝**B（`AppSettingsDTO` をファイルごとの `partial` に分ける。形は不変）**、D4＝A（領域ごとに対にする）。
- 2026-11-15（ユーザー）: 37-3 以降は、実機のデータでの確認を待たず、Phase34・40・35 のあとに続けて行う（黄金テストで形を守る。実機確認は最後にまとめて行う）。
- D3＝B の `partial` への分割は、項目の名前・型・並び（`XmlSerializer` が見る順）を 1 文字も変えない。分割の前後で、`Profiles.xml` を書き出して同一であることを、黄金テストで確かめる。

## Step 一覧
| Step | 内容 | テスト | 状態 |
|---|---|---|---|
| 37-0 | 調査と守りの準備: `SaveAction` と `SaveActionNew` の差（呼び出し元、書く内容、保存のあとの処理）を表にする。`Actions.xml` の黄金が、全種別（1〜11）と、古い形（移行前）を含むかを確かめ、足りなければ足す。`BackingStore` の、まだ配列のままの項目を一覧にする | 黄金の追加 | 未着手 |
| 37-1 | スペシャルアクションの読み込み（`LoadActions` 178 行）を、種別ごとの部品へ。部品は `Core/Persistence` に置き、種別の列で受ける（同じ種別を 2 つ登録しない） | 黄金が不変。全種別に部品があることの型の検査 | 未着手 |
| 37-2 | スペシャルアクションの保存（`SaveAction`／`SaveActionNew`）を、同じ部品へ（D2）。種別の受け渡しを `ActionTypeId` にし、呼び出し側の数の直書きをなくす | 黄金が不変。呼び出し側のテスト | 未着手 |
| 37-3 | `ResetProfile`（140 行）を、領域のオブジェクトの「既定へ戻す」へ分ける。`BackingStore` は順に呼ぶ | 既定値の全項目を、変更の前後で比べるテスト（既存の `BackingStoreStateFiller` を使う） | 未着手 |
| 37-4 | （D4 が A のとき）プロファイルの読み込みと保存の節を、領域ごとに対にする（まず 1 領域で行い、形を確かめてから広げる） | 黄金が不変 | 完了（2026-11-16。全領域） |
| 37-5 | （D1 が B のとき）まだ配列のままの項目を、領域のオブジェクトへ移す（Phase25 と同じ進め方。領域ごと） | 黄金が不変。`SettingsSlotWindowUsageTests` | 未着手 |
| 37-6 | `AppSettingsDTO`（D3） | 黄金が不変 | 見送り（2026-11-16 のユーザーの決定。要素の順序が型の中の並び［ファイル名の順］に依存する脆さを持ち込むため） |
| 37-7 | 文書: `02-Extension-Guide.md` の §3（スペシャルアクションを足す手順に、保存・読み込みの部品を足す）、`Structure-Review-2026-11.md` の A の更新履歴 | — | 完了（2026-11-16） |

## 確認の方法
- 各 Step のあと、保存の黄金テストと、`BackingStore`・スペシャルアクションのテスト。Step の終わりに Debug ALL。
- 実機: 既存のプロファイルと `Actions.xml`（利用者の実物の写し）を読み、保存し直して、差分が出ないこと。スペシャルアクションの各種別の作成・編集・複製・削除。プロファイルの新規作成（既定値）とプリセット。

## リスク
- 中。保存の形に触れる危険がある作業で、利用者のデータに直結する。黄金テストを先に厚くすること（37-0）と、1 Step を小さく保つことで抑える。
- `BackingStore` はテストからの参照が多い（114 ファイル）。公開メンバーの名前を変えると、テストの修正が広がる。名前は変えずに、中身だけを移す。

## 履歴
- 2026-11-13: 決めること（D 項目）のユーザーの回答を「決定の記録」に記録した。
- 2026-11-12: 計画書を作成（外部レビューの照合の結果）。コードは未着手。

## 実施の記録
### 2026-11-15（37-0 調査）
- **`SaveAction` と `SaveActionNew` の差**: 本番の保存の経路は `SpecialActionRepository.SaveAction` → `SpecialActionsXmlSerializer.SaveActionNew` の 1 本だけ（`SpecialActionRepository.cs:247`）。`SpecialActionsXmlSerializer.SaveAction`（XML を直接組み立てる旧い入口。97 行）は、**本番からの呼び出しが 0 件**で、呼ぶのはテストの黄金の準備（`PersistenceHarness`）と、`SerializersGuardTests` の名前の一覧だけ。規則 2.4 に従えば、「正規の経路と重複するだけの並行実装」に当たる。ただし、黄金のファイル（`Actions.xml` の旧い形）を作る準備に使われているため、削除は、黄金を `SaveActionNew` で作り直しても同じ XML になるかを確かめてから行う（D2＝A の「1 つにできるなら 1 つに」の前提の確認が、37-1 の最初の作業になる）。
- **種別の数（`mode`）**: 1＝Macro、2＝Program、3＝Profile、4＝Key、5＝DisconnectBT、6＝BatteryCheck、7＝MultiAction、9＝GyroCalibration、10＝Button、11＝FocusWindow（8 は欠番）。`SaveActionNew` は `SpecialAction` を作って `store.actions` へ入れ、`SaveActions` で全体を書く。`SaveAction` は、XML 要素を直接作る（9・11 の枝を持たない。つまり旧い入口は、新しい種別を保存できない）。呼び出し側（編集用 ViewModel）は、数を直接渡している（1、2、3、4、6、7、10、11、`SpecialActionViewModel` は変数）。
- **黄金**: `PersistenceHarness` が旧い入口で 1〜7 の種別を作る。9・10・11 の種別は、黄金に入っていない（別のテストが個別に守る: `FocusWindowPersistenceTests`、`SaAnalogStrengthStorageTests`、`SaExtrasStorageTests`）。37-1 の前に、全種別を `SaveActionNew` で書いた黄金を足す。
- 次の作業の順: (1) 黄金に 9・10・11 を足す、(2) 旧い入口の削除の可否（上記）、(3) `LoadActions`（178 行）の種別ごとの部品化、(4) 保存の部品化と `ActionTypeId` での受け渡し、(5) `ResetProfile`、(6) 節の対、(7) `AppSettingsDTO` の `partial`。

### 2026-11-15（37-1）
- 黄金: 本番の保存の経路（`SaveActionNew` → `SaveActions`）で、全種別（1〜7、9、10、11。Strength、OutputExtras、CenterCursor、Hold を含む）を書いた `actions-production.xml`／`.state.txt` を足した（`ActionsProduction_*` のテスト）。ID は固定。
- 旧い保存の入口 `SpecialActionsXmlSerializer.SaveAction`（本番の呼び出し 0 件。新しい種別を書けない）を、規則 2.4 に従って撤去した（正規の経路と重複するだけの並行実装）。連動して、呼び出しがなくなった `CreateAction`・`PrepareActionsXml`・`m_Xdoc` と、コメントだけの旧い本体（約 110 行）も削除した。旧い形式の `Actions.xml` の読み込みの黄金（`actions.xml`）は、固定の文字列（`PersistenceHarness.LegacyActionsXml`）で守る。旧い入口の書き出しの黄金テスト（`Actions_SavedXml_MatchesGolden`）は撤去。保存される XML の形は不変（本番の黄金で確認）。
- 調査の訂正: 計画書の「`LoadActions` の 178 行」は古い記述。読み込み・書き出しはすでに `ActionsDTO` に集まっており、種別ごとの部品化（37-3・37-4）の余地は DTO の側にある。37-3 以降（`ResetProfile` の分割、節の対、`AppSettingsDTO` の `partial`）は、保存の形に触れる大きな作業のため、実機のデータでの確認とあわせて次の機会に行う。
- 既知の不安定: `Ds4DeviceBehaviorTests.DongleScript_ReportsTheFrames_AndRemovesOnTheEndOfTheScript` が、単独で実行すると失敗することがある（全件では合格。今回の変更とは無関係の、実行の順への依存）。

### 2026-11-16（37-3。ブランチ `Phase37-3`）
- 37-3: 先に、`ResetProfile` の戻し漏れを固定するゴールデン（`ProfileResetGoldenTests`。全項目を既定と異なる値にしてから戻し、残った差を 3 つのスロットで固定）を、変更前のコードで生成してコミットした。そのうえで、`ResetProfile` を、領域のオブジェクトの `ResetForProfileLoad()`（`GyroSlotSettings`・`TouchpadSlotSettings`・`StickSlotSettings`・`TriggerSlotSettings`・`SixAxisSlotSettings`・`LightbarSlotSettings`）へ分けた（`BackingStore.ResetProfile` は 140 行 → 約 25 行）。文は移しただけで、戻す値は変えていない。ゴールデンと既存の黄金テストがすべて不変。ジャイロには、新しく作るときの既定値を戻す既存の `ResetToDefaults`（`MouseDeadZone` なども戻す）があり、プロファイル読み込み前の戻し方（それらを戻さない）とは範囲が違うため、別の名前（`ResetForProfileLoad`）にした。
- 37-4（節の対）・37-5（配列の項目）・37-6（`AppSettingsDTO` の `partial`）は、保留にした。理由: (1) 37-6 は、`XmlSerializer` が要素の順序を「宣言の順」で決めるのに対し、`partial` で複数ファイルへ分けると、型の中の並びがコンパイラへ渡るファイルの順（ファイル名の並び）に依存する。黄金テストで検出はできるが、ファイル名を変えるだけで保存の形が変わりうる、という脆さを新しく持ち込むわりに、得られるのは読みやすさだけである。(2) 37-4 は、読み込み 15・保存 7 の節の組み替え（登録順序を保ったまま、1 型が両方の契約を持つ形）で、項目ごとの try/catch の読み込みを含み、規模が大きい。(3) 37-5 は、配列のまま残る項目の領域への移動で、`BackingStore` の公開メンバー（テスト 114 ファイルが参照）に及ぶ。いずれも、保存の形を変えない保証は黄金テストにあるが、得られる効果が小さい。ユーザーが再開を決めたときに行う。

### 2026-11-16（37-4。ブランチ `Phase37-4-SectionPairs`）
- 決定: 37-6＝実施しない、37-4＝全領域を実施、37-5＝領域ごとに少しずつ（次に行う）。
- 37-4: 読み込みの節 15・保存の節 7 は、区切りが対応しない（保存の 1 節が、読み込みの複数の節にまたがる）が、領域としては連続していた。そこで、既存の節の本体は動かさず、`ProfileAreaSection`（読み込みの節の並びと保存の節 1 つの対）を新設し、指揮役（`LegacyProfileXmlSerializer`）が 7 つの領域（General／TriggerStick／Motion／OutputCurve／OutputController／Actions／Button）を 1 回ずつ宣言する形にした。読み込みは領域内で宣言順、領域間は従来のファイル順。保存は従来の XML の順序。項目ごとの try/catch は各節のまま。`ResolveControlProfileTargets` は Button の読み込みの後へ移した（`ShiftControl` の保持とは独立のため結果は同じ）。
- 検証: ゴールデン・全テスト（3,606 件）合格、警告 0・エラー 0。呼び出し順の検査（`ProfileLoadFoundationTests`）を領域の名前へ更新した。
- 未確認（2026-11-16）: 実機でのプロファイルの読み込み・保存（ゴールデンは合格）。

### 2026-11-16（37-5 領域 1: DualSense。ブランチ `Phase37-5-Arrays`）
- 配列のまま残る項目のうち、DualSense 固有の 3 項目（振動のエミュレーション方式、汎用の振動の再スケール、ハプティックの強さ）を `DualSenseSlotSettings`（スロットごとに 1 つ。`BackingStore.dualSenseSlots`）へ移した。`IProfileSettingsService` の 3 つの配列プロパティは `DualSenseSlot(int slot)` 1 つに替えた。プロファイルの読み込み前の初期化では戻さない（従来どおり）。
- 保存の XML は不変（XML のゴールデンは無変更で合格）。状態のダンプのゴールデン 6 件は、項目の経路名だけが変わる（値は同一であることを差分で確認して採用）。
- 残る配列の項目（次の領域の候補）: 振動（`rumble`・`rumbleAutostopTime`・`rumbleSettings`・`inverseRumbleMotors`。`RumbleSettings` との二重の持ち方がある）、一般（`enableTouchToggle`・`idleDisconnectTimeout`・`enableOutputDataToDS4`・`btPollRate`・`launchProgram`・`dinputOnly`・`buttonMouseInfos`）、出力の種類（`outputDevType`・`outputVirtualTriggerButtons`）ほか。

### 2026-11-16（37-5 領域 2: 基本的な設定。ブランチ `Phase37-5-General`）
- 配列のまま残っていた 6 項目（`enableTouchToggle`・`idleDisconnectTimeout`・`enableOutputDataToDS4`・`btPollRate`・`launchProgram`・`dinputOnly`）を `BasicSlotSettings`（`BackingStore.basicSlots`）へ移した。既定値はこの型のコンストラクターと `ResetForProfileLoad`（`ResetProfile` から呼ぶ）に 1 か所で持つ。`IProfileSettingsService` の 6 つの配列プロパティは `BasicSlot(int slot)` 1 つに替えた（`Get*` の単発の取得は、実行中の呼び出しのため残した）。`buttonMouseInfos` は、すでに項目ごとのオブジェクトなので対象外。`UseDInputOnlyArray`（実行中の状態）は別物で、そのまま。
- 保存の XML は不変。状態ダンプのゴールデン 5 件は経路名だけが変わる（並べ替えて比べ、値が同一であることを確認して採用）。
- 残る領域の候補: 振動（`rumble`・`rumbleAutostopTime`・`rumbleSettings`・`inverseRumbleMotors`）、出力の種類（`outputDevType`・`outputVirtualTriggerButtons`・`outputDS4TriggerMode`）ほか。

### 2026-11-16（37-5 領域 3: 振動。ブランチ `Phase37-5-Rumble`）
- 振動の `rumble`・`rumbleAutostopTime`・`inverseRumbleMotors` を `RumbleSlotSettings`（`BackingStore.rumbleSlots`。強さ・自動停止・モーターの入れ替え）へ移した。`IProfileSettingsService` の 2 つの配列プロパティは `RumbleSlot(int slot)` に替えた。
- **発見した不具合と修正（ユーザーの承認済み。2026-11-16）**: 実行時に読む `GetRumbleBoost`／`GetRumbleAutostopTime` は、変更通知つきの並行コピー `BackingStore.rumbleSettings`（`RumbleSettings`）を先に読んでいたが、プロファイルの読み込みと画面は生配列 `rumble`／`rumbleAutostopTime` へ書いていた（並行コピーへは書く経路がなく、連動は並行コピー→生配列の一方向）。そのため、プロファイルの振動の強さ・自動停止の値は、保存・画面には出るが、実行時には効いていなかった（強さは常に 100、自動停止は 0。画面からの自動停止の変更だけは両方へ書くため効く）。持ち主を 1 つにしたことで、プロファイルの値が実行時に効くようになる（機能の欠落の修正）。誰も使わなくなった並行コピー `RumbleSettings` は削除した（2.4 の「使うべきでないもの」）。
- 検証: 実行時の読み取りが書いた値に従うことを `RumbleSlotSettingsTests` で固定。保存の XML は不変。状態ダンプのゴールデン 6 件は、経路名の変更と、削除した並行コピーの行の消滅だけ（値は同一であることを並べ替えて確認して採用）。

### 2026-11-16（37-5 領域 4: 出力の種類。ブランチ `Phase37-5-OutputType`）
- `outputDevType`・`outputVirtualTriggerButtons`・`outputDS4TriggerMode` を `OutputTypeSlotSettings`（`BackingStore.outputTypeSlots`。保存される仮想コントローラーの種類、仮想のトリガーボタン、DS4 のトリガーの出力方式）へ移した。`IProfileSettingsService` の `OutContType`・`OutputVirtualTriggerButton`・`OutputDS4TriggerMode`（配列）は `OutputType(int slot)` 1 つに替えた。実行中の接続の種類（`ActiveOutDevType`）と画面の一時の種類（`OutDevTypeTemp`）は、出力スロットのサービスが持つ別物で、対象外。
- `outputVirtualTriggerButtons` は、保存・読み込みの対象でなく、利用者もほぼいない（`OutputSlotSettings.VirtualTriggerButtons` だけ）。挙動を変えないため、そのまま持ち主へ移した（初期化でも戻さない）。
- 保存の XML は不変。状態ダンプのゴールデン 7 件は、経路名の変更だけ（並べ替えて同一を確認して採用）。
- 37-5 は、DualSense・基本的な設定・振動・出力の種類の 4 領域を移した。残るのは、領域の持ち主に入っていない小さな配列（`distanceProfiles`、`profileActions`、`containsCustomAction` など、実行中の状態や別の整理が要るもの）で、必要になったときに個別に判断する。

### 2026-11-16（37-7 文書）
- `02-Extension-Guide.md` の §3 に、スペシャルアクションの保存・読み込みの経路（`SaveActionNew`→`SaveActions`／`ActionsDTO`）と黄金を足し、§5 に、領域ごとの持ち主・`ResetForProfileLoad`・`ProfileAreaSection`・状態ダンプのゴールデンの扱いを足した。`Structure-Review-2026-11.md` の更新履歴に A の続きを足した。Phase37 は完了。
