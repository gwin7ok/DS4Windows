# Phase11 計画書: 安定化と不具合修正

作成日: 2026-11-02
状態: **案（ユーザーの承認待ち）。§3 の決定事項 Q1〜Q6 の回答後に着手する（copilot-instructions §4）**
ブランチ: `Phase11`
上位: `Phase11-and-Beyond-Roadmap.md`（Phase11〜16 の構成）、`Release-Backlog.md`（R-7）
関連: GitHub [Issue #4](https://github.com/gwin7ok/DS4Windows/issues/4)、`Phase6-Step11-RealDevice-Verification-Checklist.md` §27〜§30、`K7b1-Investigation.md`、`Phase9-Deferred-Ledger.md`

---

## 0. 目的と範囲

利用者に見える不具合と未確認の項目を片づけ、後続の構造変更（Phase13〜16）の前に、足場（テスト）と挙動の基準を固める。

| 区分 | 内容 |
|---|---|
| 含む | Issue #4 の 4 件（R-7）、テスト整備（XAML のセマンティック検証）、Controller Readings の数値の見切れ、先送りの実機確認の消化、K7b-1 の締め、文書・リリース |
| 含まない | 新機能（R-3・R-4 は Phase12）、`ControlService` の整理（Phase13）、名前空間・フォルダの再編（Phase14）、UI の共通化（Phase15） |

守る規約: 機能を削らない（§2.2）、ログの文言・レベルを維持（§2.3）、1 ファイル 1 型（§3.3）、ラチェットの基準値は件数を減らしたら同時に更新（§3.1）。

## 1. 調査結果（2026-11-02。着手前の再照合）

### 1.1 Issue #4 の 4 件

| # | 項目 | Issue の主張 | 照合の結果 | 判定 |
|---|---|---|---|---|
| 1 | `UseDs3PitchRollSim` | 「適用」で DS4 への補正が漏れる。直し先は `SetLateProperties` | **主張の前提が古く、実態はより重い。** 保存・適用は現在 `ExecuteSaveOrApply` に統合され（`ProfileEditor.xaml.cs`）、`SetLateProperties` は存在しない。**DS3 のピッチ／ロールの擬似を ON にしたとき、出力を DS4 にする補正は、保存でも適用でも行われない**（`TempControllerIndex` を触るのは `OutConTypeCombo_SelectionChanged` のみ）。2024-08-23 の導入時（`d35abe58`）にあった補正と、設定の変更時のサービスの再起動も、2026-09-14 の保存処理の統合（`9da60b85`）で消えている。同じ統合の `EnableOutputDataToDS4`＋出力が X360 のときの DS4 への補正（Phase5-Step14 (c)-2）も、現行のコードにない | **機能の欠落（回帰）**。§2.2 により是正の対象 |
| 2 | `Keepsize` | XAML は `Mode=TwoWay`、C# は get-only | **事実**（`ProfileEditor.xaml.cs:215`、`ProfileEditor.xaml:125`）。実害は小さい（`KeepSizeCheckBox_Click` で値を保っている） | 整理（挙動は変えない） |
| 3 | `CustomSteamFolder` | 存在しないパスを入れると警告なしで破棄される | **未確認。** `AppSettingsService.CustomSteamFolder` は代入するだけで、検査は見つからない。検査・破棄の場所（画面側か、保存時か）を W5 の最初に特定する | 要・追加調査 |
| 4 | `CheckEvery` | 「更新確認」が OFF のとき、数値を変えても保存されない | **コードは主張どおり**（`SettingsViewModel.cs` の `CheckEvery` の setter が、OFF のとき何もせず戻る）。ただし `MainWindow.xaml:350–351` の数値入力を含む行（`checkEveryOptionsStack`）は `IsEnabled="{Binding CheckForUpdates}"` で、**OFF のときは数値を操作できない**（コードで確認）。意図した仕様（OFF なら数値は無効）で、実害はない | 目視確認のうえ「仕様」として閉じる見込み |

### 1.2 テスト・UI

- FurtherImprovements の Phase A-2 が名指しする `Phase10Step3bW0ProfileEditorLayoutTests`（XAML の書式に依存する照合）は、コミット `3b7ca385`（2026-10-23）で書式に依存しない判定にすでに直してある。残りの脆いテストの有無は、W1 の最初に棚卸しする。
- Controller Readings の数値のラベル（`ControllerReadingsControl.xaml`）は幅 32、`Padding="2,2"`。L2／R2 などが 255 のとき見切れるかは、W2 の最初に実機で再現して確認する（FurtherImprovements 03 の A-3 の案: 幅 38、`Padding="1,1"`）。

### 1.3 実機確認の未消化

台帳 `Phase6-Step11-RealDevice-Verification-Checklist.md` に、次が残る（件数は W0 で数え直す）。
- §27: P10-79〜P10-98（Controls の閾値、ログ、Launch Program、Load Profile、固定 ID ほか）。ただし P10-69〜73・P10-83〜84 の期待値は、R-5（2026-10-23、押下判定を閾値なしに変更）で変わった（`Phase10-Status.md` §7）。**期待値を読み替えて確認する**
- §28（統合 Step の回帰）、§29（Check Battery、Multi-action）、§30（SA のアナログ出力）
- P9M3-3、P9F1-4、P9M2-2〜4

## 2. 作業の構成（Wave）

各 Wave の終わりに `Debug ALL`（警告 0・エラー 0、全テスト成功）を確認する。コミットは Wave ごと。

### W0: ベースラインと棚卸し

- `Debug ALL` を実行し、警告・エラー・テスト件数の基準値を記録する。
- 台帳の「未」「先送り」を数え直し、本書 §4 の確認の順序表を確定する。
- 実機の準備: DS4（BT）、DS3（あれば。W4 の確認に必要）。

### W1: テスト整備

- `XamlTestHelper`（`DS4WindowsTests/TestHelpers/`）を導入する（`XDocument` でノードと属性を検査）。
- 書式（空白・改行・属性の順序）に依存する照合テストを棚卸しし、置換する。範囲は **脆さが確認できたものだけ**（全面の書き換えはしない）。
- 受入: 置換したテストが、XAML の整形（フォーマッター）を適用しても落ちない。

### W2: Controller Readings の数値の見切れ

- 実機で、L2／R2／TX／TY などの 255 が見切れるかを再現する。再現できたら幅・余白を調整する（案: 幅 38、`Padding="1,1"`、右寄せのまま）。ダーク・ライトの両テーマ、日本語・英語で確認する。
- 見た目だけの変更。挙動・ログは変えない。

### W3: Issue #4 の 2 — `Keepsize`

- 案 A（推奨）: `set` を足す（`Keepsize { get => keepsize; set => keepsize = value; }`）。`Click` ハンドラーは残し、バインディングと二重にならないことを確認する。
- 案 B: `Mode=TwoWay` を外す（`OneWay`）。
- 受入: チェックの ON／OFF で、保存・適用時のウィンドウサイズ保持（`ProfileEditor.xaml.cs` の `keepsize` の分岐）が従来どおり。特性化テストを先に書く。

### W4: Issue #4 の 1 — `UseDs3PitchRollSim`（欠落した補正の復元）

- 先に **失われた挙動を特性化する**。旧コード（`9da60b85` の親）の動き: (a) 擬似が ON のとき、出力を DS4 にする（`TempControllerIndex = 1`）、(b) 擬似の設定が変わったとき、サービスを再起動する（`MainWindow.ChangeService`）。あわせて (c) `EnableOutputDataToDS4` が ON で出力が X360 のとき、DS4 にして GUI ログを出す（Phase5-Step14 (c)-2。旧コードの文言は `9da60b85` の親を参照）。
- 補正は `ExecuteSaveOrApply`（保存・適用の唯一の合流点）の前段に置く。単体テストできるよう、判定は `ProfileSettingsViewModel` の側（画面に依存しない）に置く案を第一候補にする。
- 画面の `OutConTypeCombo` の表示も、補正後の値に合わせる。
- 受入: 擬似を ON にして **保存** と **適用** のどちらでも、出力が DS4 になる。OFF のときは従来どおり。(c) も同様。ログは §2.3 に従い、旧文言があれば維持する。
- 実機: DS3 で擬似の効果を確認する（機材がなければ「不能」として記録し、ユニットテストで担保する）。
- **仕様の決定が要る**（Q1、Q2）。

### W5: Issue #4 の 3・4 — `CustomSteamFolder`、`CheckEvery`

- 3: まず検査・破棄の場所を特定し、現象を再現する。直し方（警告を出して保存を止める／警告を出して保存は続ける／従来どおり）は **Q3 で決める**。
- 4: 目視で、OFF のとき数値が操作できないことを確認する。操作できれば仕様の食い違いなので、Q4 で決める。操作できなければ「仕様」として閉じる。コードの変更なし（コメントとテストで意図を残すことは可）。

### W6: 実機確認の消化

- §4 の順序表に沿って、**機能ごとにまとめて**行う。結果は台帳に記録する（○／△／×／不能）。
- 不具合が出たときは、その場で直さず台帳に記録し、重要度を見て Wave を追加するか、Phase12 以降へ回す（Q5）。

### W7: K7b-1 の締め

- 新しいダンプが出ていないかの確認（ユーザー）→ 「解消」として閉じる（`Release-Backlog.md` §5、`K7b1-Investigation.md`、`Phase9-Completion-Report.md` §5）。
- 閉じたら、9-H（Phase16）の前提が満たされる。

### W8: 文書とリリース

- 完了報告書 `Phase11-Completion-Report.md`、`Phase11-Status.md`、全体計画書の追記、`Release-Backlog.md`（R-7 の状態）、`Phase11-and-Beyond-Roadmap.md`、CHANGELOG。
- 版数: W4 の復元（利用者に見える修正）を含むため **4.2.4（パッチ）** を第一候補にする（Q6）。

## 3. 決定事項（ユーザーに確認）

| ID | 質問 | 推奨 |
|---|---|---|
| Q1 | W4: DS3 の擬似を ON にしたとき、出力を DS4 にする補正を **復元してよいか**（機能の欠落の是正） | 復元する |
| Q2 | W4: 擬似の設定が変わったときの **サービスの再起動**（旧: `ChangeService`）も復元するか。現在のアーキテクチャでは再起動の要否を、DS3 の実機で確認してから決める必要がある | 実機で必要性を確認し、不要なら再起動はしない |
| Q3 | W5: `CustomSteamFolder` に存在しないパスを入れたときの挙動 | 警告を出し、保存は続ける（利用者の入力を黙って捨てない） |
| Q4 | W5: `CheckEvery`: OFF のとき数値が操作できるなら、保存するか | 操作できない見込み。できるなら保存する |
| Q5 | W6: 実機確認で見つかった不具合は、Phase11 内で直すか | 重大（クラッシュ・データ破損）は直す。それ以外は台帳に記録して Phase12 以降 |
| Q6 | 版数: Phase11 の完了でパッチ版（4.2.4）を出すか。リリースを Phase12 の後にまとめるか | 4.2.4 を出す |

## 4. 実機確認の順序（W6 の案）

| 順 | まとまり | 台帳 | 備考 |
|---|---|---|---|
| 1 | Controls の押下判定とログ | P10-83〜86 | 期待値は R-5 後の仕様で読み替える |
| 2 | Launch Program・Load Profile | P10-80〜82 | |
| 3 | 固定 ID | P10-90〜97 | 名前変更の追従。プロファイルのバックアップを取ってから |
| 4 | SA（Extras、LS／RS Outer、Touch Started／Ended） | P10-87・88・98 | |
| 5 | 回帰（Controls 全種別、SA 全種別） | P10-89、§28 | |
| 6 | Check Battery・Multi-action・SA のアナログ出力 | §29、§30 | |
| 7 | 残り | P9M3-3、P9F1-4、P9M2-2〜4 | P9F1-4 は設定のない環境が要る |

## 5. リスク

- W4 は、挙動を変える唯一の Wave。旧コードの動きを先に特性化してから直す。DS3 の実機がない場合は、ユニットテストで担保し、実機は不能として記録する。
- W2・W3 は画面の変更が小さいため低リスク。XAML の整形は伴わせない（差分を最小に）。
- 台帳の項目が多い（§27〜§30 だけで、「先送り」または「未」を含む行が 33 行。2026-11-02 の機械的な数え）。1 回で終わらせず、まとまりごとに記録し、中断・再開できるようにする。

## 6. 受入条件

- `Debug ALL`: 警告 0・エラー 0、全テスト成功。
- Issue #4 の 4 件それぞれに、「直した」「仕様として閉じた」のいずれかの結論と根拠が、Issue と本書に記録されている。
- 台帳 §27〜§30 と P9M3-3 ほかが、すべて ○／△／×／不能 で埋まっている（「未」が残らない）。
- K7b-1 が閉じている（または、閉じられない理由が記録されている）。
- CHANGELOG と各 Status・報告書が更新されている。
