# Phase11 計画書: 安定化と不具合修正

作成日: 2026-11-02
状態: **承認済み（2026-11-02。決定事項 Q1〜Q6 はユーザーが回答。§3）。各 Wave は、着手前に内容を報告し、`Debug ALL` の成功を確認しながら進める**
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
- **結果: 完了（2026-11-02）。** 置換は 3 クラス。整形で落ちないことを実験で確認した（§8）。

### W2: Controller Readings の数値の見切れ

- 実機で、L2／R2／TX／TY などの 255 が見切れるかを再現する。再現できたら幅・余白を調整する（案: 幅 38、`Padding="1,1"`、右寄せのまま）。ダーク・ライトの両テーマ、日本語・英語で確認する。
- 見た目だけの変更。挙動・ログは変えない。
- **結果: 実装と自動検証は完了（2026-11-02）。** 実アプリでの目視は、W6 の台帳（P11-n）で行う（§9）。

### W3: Issue #4 の 2 — `Keepsize`

- 案 A（推奨）: `set` を足す（`Keepsize { get => keepsize; set => keepsize = value; }`）。`Click` ハンドラーは残し、バインディングと二重にならないことを確認する。
- 案 B: `Mode=TwoWay` を外す（`OneWay`）。
- 受入: チェックの ON／OFF で、保存・適用時のウィンドウサイズ保持（`ProfileEditor.xaml.cs` の `keepsize` の分岐）が従来どおり。特性化テストを先に書く。
- **結果: 完了（2026-11-02）。ただし、案 A・B のどちらでもなく、「死んでいるバインディングを外す」で閉じた**（調査で、案 A の前提が成り立たないと判明したため。§10）。

### W4: Issue #4 の 1 — `UseDs3PitchRollSim`（欠落した補正の復元）

- 先に **失われた挙動を特性化する**。旧コード（`9da60b85` の親）の動き: (a) 擬似が ON のとき、出力を DS4 にする（`TempControllerIndex = 1`）、(b) 擬似の設定が変わったとき、サービスを再起動する（`MainWindow.ChangeService`）。あわせて (c) `EnableOutputDataToDS4` が ON で出力が X360 のとき、DS4 にして GUI ログを出す（Phase5-Step14 (c)-2。旧コードの文言は `9da60b85` の親を参照）。
- 補正は `ExecuteSaveOrApply`（保存・適用の唯一の合流点）の前段に置く。単体テストできるよう、判定は `ProfileSettingsViewModel` の側（画面に依存しない）に置く案を第一候補にする。
- 画面の `OutConTypeCombo` の表示も、補正後の値に合わせる。
- 受入: 擬似を ON にして **保存** と **適用** のどちらでも、出力が DS4 になる。OFF のときは従来どおり。(c) も同様。ログは §2.3 に従い、旧文言があれば維持する。
- 実機: DS3 で擬似の効果を確認する（機材がなければ「不能」として記録し、ユニットテストで担保する）。
- **決定済み**（Q1: 補正を復元する、Q2: 再起動も復元する）。再起動は、旧コードの動き（`MainWindow.ChangeService` による非同期の再起動）を特性化してから、現在のアーキテクチャ（`IServiceLifecycle` など）に合わせて呼び出す。復元した再起動が実機（DS3 または DS4）で問題を起こさないことを、W6 で確認する。
- **結果: 実装と自動検証は完了（2026-11-02）。** 補正は保存・適用の両方、再起動は保存のみ（旧コードと同じ）。実機（DS4 での再起動の安全性）は W6 の台帳（P11-n）で行う（§11）。

### W5: Issue #4 の 3・4 — `CustomSteamFolder`、`CheckEvery`

- 3: まず検査・破棄の場所を特定し、現象を再現する。**決定済み（Q3）: 警告を出し、保存は続ける**（利用者の入力を黙って捨てない）。警告の文言は日本語・英語のリソースに足す（直書きにしない。4.2.3 の方針）。
- 4: 目視で、OFF のとき数値が操作できないことを確認する。**決定済み（Q4）: 仕様として閉じる**。コードの変更なし（コメントとテストで意図を残す）。操作できてしまう場合のみ、ユーザーに再確認する。
- **結果: 実装と自動検証は完了（2026-11-02）。** 3 は、警告を出して保存を続けるよう修正（`SettingsViewModel.CustomSteamFolder` の setter。JA/EN のリソースを追加）。4 は、挙動を変えず、仕様として閉じた（コメントとテストで意図を固定）。実アプリでの目視は W6 の台帳（P11-n）で行う（§12）。

### W6: 実機確認の消化

- §4 の順序表に沿って、**機能ごとにまとめて**行う。結果は台帳に記録する（○／△／×／不能）。
- 不具合が出たときは、その場で直さず台帳に記録する。**決定済み（Q5）: 重大（クラッシュ・データ破損・設定の消失）のみ Phase11 内で直し、それ以外は台帳に記録して Phase12 以降へ回す。**
- **状況: 準備完了、実機確認の実施中（2026-11-02〜）。** 台帳 §52 に S1〜S8 の確認項目を作り、機材が無い 20 行を `不能` にした。確認はユーザーが実機で行い、結果を受けてエージェントが台帳に記入する（§13）。

### W7: K7b-1 の締め

- 新しいダンプが出ていないかの確認（ユーザー）→ 「解消」として閉じる（`Release-Backlog.md` §5、`K7b1-Investigation.md`、`Phase9-Completion-Report.md` §5）。
- 閉じたら、9-H（Phase16）の前提が満たされる。

### W8: 文書とリリース

- 完了報告書 `Phase11-Completion-Report.md`、`Phase11-Status.md`、全体計画書の追記、`Release-Backlog.md`（R-7 の状態）、`Phase11-and-Beyond-Roadmap.md`、CHANGELOG。
- 版数: **決定済み（Q6）: Phase11 の完了で 4.2.4（パッチ）を出す**（W4 の復元ほか、利用者に見える修正を含むため）。

## 3. 決定事項（2026-11-02 にユーザーが回答）

| ID | 質問 | 回答 |
|---|---|---|
| Q1 | W4: DS3 の擬似を ON にしたとき、出力を DS4 にする補正を **復元してよいか**（機能の欠落の是正） | 回答: **復元する** |
| Q2 | W4: 擬似の設定が変わったときの **サービスの再起動**（旧: `ChangeService`）も復元するか。現在のアーキテクチャでは再起動の要否を、DS3 の実機で確認してから決める必要がある | 回答: **再起動も復元する** |
| Q3 | W5: `CustomSteamFolder` に存在しないパスを入れたときの挙動 | 回答: **警告を出し、保存は続ける** |
| Q4 | W5: `CheckEvery`: OFF のとき数値が操作できるなら、保存するか | 回答: **仕様として閉じる**（OFF のとき数値は操作できない） |
| Q5 | W6: 実機確認で見つかった不具合は、Phase11 内で直すか | 回答: **重大のみ直す** |
| Q6 | 版数: Phase11 の完了でパッチ版（4.2.4）を出すか。リリースを Phase12 の後にまとめるか | 回答: **4.2.4 を出す** |

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

## 7. W0 の結果（2026-11-02。ベースラインと棚卸し）

### 7.1 ベースライン（`Debug ALL` 相当。`Phase11` ブランチの `425d23d5` 時点、worktree でクリーンビルド）

| 項目 | 結果 |
|---|---|
| アプリ本体 `Build Debug`（`Remove-bin` → `Build-Debug.ps1`） | 成功。**警告 0・エラー 0**（36.7 秒） |
| `Build Test (Actions)`／`Build Test (Standalone)` | 成功（警告 0） |
| `Execute Test (Actions)` | **2,728 件すべて合格**（失敗 0・スキップ 0。48 秒） |
| `Execute Test (Standalone)` | **11 件すべて合格**（2 秒） |

以後の Wave は、この値（警告 0・エラー 0、2,728 件＋11 件）から減らさない。新しいテストを足した分だけ件数は増える。

注: Git Bash から `dotnet build/test ... /p:platform=x64` を実行すると、`/p:` がパスと解釈されて失敗する（MSB1008）。PowerShell から実行する（VSCode のタスクは問題ない）。

### 7.2 実機確認の棚卸し（`Phase6-Step11-RealDevice-Verification-Checklist.md`）

結果の欄が「未」「先送り」「未確認」で、まだ消化されていない行。

| 区分 | 項目 | 件数 | 必要な環境 |
|---|---|---|---|
| A. Phase10 の先送り | P10-79〜P10-98（§27）、P10-21 | 20＋1 | DS4（BT）のみ。**手元で実施できる** |
| B. 統合後の追加 | P10-99・100（§29）、P10-101〜105（§30） | 2＋5 | DS4。P10-101〜103 はアナログ出力（ゲームパッドのテスターなど） |
| C. Phase9 の未確認（機材は不要） | P9E6b-2・5、P9E6e-4・6、P9M2-2〜4、P9M3-3、P9F1-4、P9E6a-4、P9E5-5（任意）、P9E6f1-5（任意）、P9E6f2-6・7（任意） | 約 14 | DS4 のみ。P9F1-4 は設定のない環境が要る |
| D. 2 台目のコントローラーが要る | P9B-4、P9C-4、P9E-14、P9E6a-3、P9E6e-5 | 5 | 2 台目 |
| E. 外部ツールが要る | OSC（P9E-1〜3）、UDP 受信（P9E-6〜8）、振動の実機（P9E-11） | 7 | OSC ツール、UDP 受信ツール、振動の確認手段 |
| F. 他機種が要る | DualSense（P9E6b-4、P9E6c-3・4）、DualSense Edge（P9E6c-2）、Switch Pro（P9E6c-6）、Joy-Con（P9E-16、P9E6c-5、P9E6d-1〜4）、タッチパッド付きの実機（P9E6b-3、P9E6f1-3） | 約 13 | 各機種 |
| G. 古い台帳の整理 | §11 の U-1〜U-7（機材・環境が無く確認不能）、U-11〜U-22（「未」のまま）、T-5・7・10〜13（2026-10-18 に先送り）、T-6（△） | 約 25 | — |

- A・B・C の約 36 件は、DS4 だけで実施できる。W6 の本体はここ。
- D〜F は、機材・環境が無いと確認できない。**消化できない項目は、`不能` として §11 の様式（代替の根拠・影響範囲・確認の時期）で記録して閉じる**（台帳の記入方法のとおり）。どの機材が使えるかは、ユーザーの確認が要る。
- **G は、台帳の更新漏れの疑い。** U-11〜U-22 は「総合検証 T-n で ○ になったら記入する」と書かれているのに、T-1〜T-4・T-8・T-9・T-14 が ○ になった後も「未」のまま。T-n の結果から、U-n の結果を埋め直せる見込み（W6 の最初に整理する）。U-8（デバウンスの即時反映）は 2026-10-17 に「不合格」と記録されているが、原因の DebouncingMs の保存・読込は後に是正されている（`ProfileLegacyXmlRoundTripTests`）。最新の状態を確認し、台帳に結論を足す。

### 7.3 W6 への影響（要・ユーザー確認）

受入条件「『未』が残らない」（§6）は、D〜F の機材が無い項目を `不能` に変えることで満たす。W6 を始める前に、次を教えてもらう。

1. 実機: DS4（BT）以外に、使える機種・2 台目はあるか（DualSense、Switch Pro、Joy-Con、DS3 など）。W4 の DS3 の確認にも関わる。
2. OSC・UDP（Cemuhook）の受信ツールは使えるか。
3. 古い台帳（G）の整理は、T-n の結果からの転記で足りるか（ユーザーが再確認するのは、T-5・7・10〜13 と T-6 のみ）。

### 7.4 機材の回答（2026-11-02、ユーザー）と、W6 の確認の範囲

回答: DS4（BT）＋ **DS4 の 2 台目**。DualSense・DualSense Edge・DS3・Switch Pro・Joy-Con は無し。OSC・UDP の受信ツール、振動を確認する手段は無し。

| 区分 | 項目 | 扱い |
|---|---|---|
| A・B・C | P10-79〜105、P10-21、Phase9 の機材不要の項目（約 36 件） | **実施する**（DS4 1 台） |
| D | P9B-4、P9C-4、P9E-14、P9E6a-3、P9E6e-5（2 台目が要る 5 件） | **実施する**（DS4 を 2 台つなぐ） |
| E | OSC（P9E-1〜3）、UDP 受信（P9E-6〜8）、振動の実機（P9E-11）の 7 件 | **`不能`** として §11 に記録して閉じる。代替の根拠は、各領域の差分テスト（Phase9 E-1・E-3・E-4）と、実機で確認できた部分（P9E-4・5・9・10・12・13） |
| F | DualSense・Edge・Switch Pro・Joy-Con・タッチパッド付き他機種の約 13 件 | **`不能`** として §11 に記録して閉じる。代替の根拠は、デバイス種類ごとの差分テスト（Phase9 E-6c・E-6d） |
| W4 | DS3 の擬似（ピッチ／ロール）の実機確認 | **`不能`**（DS3 が無い）。ユニットテスト（旧コードの動きを特性化したもの）で担保する |

- W4 で復元するサービスの再起動（Q2）は、DS3 が無くても **DS4 で、再起動が安全に動くこと**（再接続・プロファイル再適用・仮想パッドの付け外し）を W6 で確認する。再起動の引き金（擬似の設定の変更）は DS3 専用ではなく、設定の値の変更で起こせる。
- 不能にした項目は、K7b-1 と同様に、後で機材が手に入ったときに再開できるよう、§11 に「確認の時期」を書く（例: 「該当機材を入手したとき」）。
- 古い台帳（G）の整理は、まだ未回答。W6 の最初に、T-n からの転記案を示して確認する。

### 7.5 台帳整理の結果（2026-11-02。W0）

**実施したこと**: `Phase6-Step11-RealDevice-Verification-Checklist.md` §11 の U-11〜U-22 に、2026-10-18 の総合検証（§12 の T-n、結果欄はユーザーが記入したもの）の結果を、対応表（T-n の「対応」列）どおりに転記した。新しい確認はしていない。

| U | 転記した結果 | 残り |
|---|---|---|
| U-11 | 一部 ○（T-3） | 出力スロット画面の Plug／Unplug（T-7） |
| U-12 | 一部 ○（T-1） | 設定値の保存→再起動、UDP の平滑化、Fake Exe 名（T-11） |
| U-13 | 一部 ○（(1)(2)(4)(5)） | (3) は △（T-6）、(6) 切替通知の ON／OFF は対応なし |
| U-14 | 一部 ○（(1)(2)(3)） | (4)〜(7)（T-13） |
| U-15 | 一部 ○（(1)(2)(3)(7)） | (4)(5)(6)(8)（T-7・T-10・T-12。(8) の DS3 は不能） |
| U-16 | 一部 ○（(3)(4)） | そのほか（T-7） |
| U-17 | 一部 ○（(1)(2)、(6)(7) は △） | (3)〜(5)（T-5）、(8) は対応なし |
| U-18 | 一部 ○（(2)(3)(9)、(5)〜(7) は △） | (4)（T-5）、(8)（T-7）、(1) は対応なし |
| U-19 | 一部 ○（(2)(5)、(3)(4) は △） | (1) は対応なし |
| U-20 | **○**（T-2 で全項目） | なし |
| U-21 | 一部 ○（(1)） | (2)〜(8)（T-10・T-11・T-12） |
| U-22 | 一部 ○（T-1・3・8・9） | T-7 |

**先送りのまま残る T-n（2026-10-18、ユーザー判断）と、その後の確認の対応**

| T | 内容 | その後に確認済みの範囲 | 残り（W6 で扱う案） |
|---|---|---|---|
| T-5 | ジャイロ・タッチパッド・フリック | §31（P9B-3 ジャイロ以外 OK）、§45（M1・M2）、§46（M3） | P9M3-3（右スティックのフリック）、ジャイロの記録画面側 |
| T-6（△） | マクロ・SA。「Unload on trigger release」で元に戻らない | 同日の U-23（復帰 ○）、§32、§47、§51 | P10-81（Load Profile の Unload）で再確認 |
| T-7 | 出力種別・出力スロット | §36（P9E-9・10・12・13 OK）、§38〜§44 | 出力スロット画面の Plug／Unplug（U-11）を W6 で 1 回確認 |
| T-10 | トレイ・通知・ライトバー | §39（P9E6b-1 ライトバー） | トレイ・通知の ON／OFF（U-13(6)、U-15(6)） |
| T-11 | 設定画面・言語・テーマ・更新確認 | §50（R-6 ホイール）。言語は 1-7 で ○ | 設定値の保存→再起動、テーマ、更新確認（Skip Version は U-7 で不能） |
| T-12 | 第 1 層の設定 | §40（P9E6c-1） | デバウンス（P9E6b-2）。DS3 の疑似は不能 |
| T-13 | 起動引数 | §48（P9F1-1〜3）、§49（P9F2-1〜5 の `-command`） | `-m`、`-driverinstall`、`-re-enabledevice`、初回起動（P9F1-4） |

→ 「残り」の列は、W6 の確認の順序（§4）の 7 番「残り」に、まとめて入れる。個別の T-n をそのままもう一度実施はしない（Phase7 の時点の手順は、その後 Phase8〜10 で挙動が変わったため、現行の挙動に合わせた回帰として行う）。

**決定（2026-11-02、ユーザー）**: 先送りの T-5・T-7・T-10・T-11・T-12・T-13 は、**Phase11 W6 の回帰確認に統合して閉じる**。台帳では、結果を「統合（Phase11 W6）」とした。上の表の「残り」の項目は、W6 の確認の順序（§4）の 7 番「残り」で実施し、結果は新しい台帳の項目（P11-n）に記録する。T-6（△）は、P10-81 で再確認する。

## 8. W1 の結果（2026-11-02。テスト整備）

### 8.1 実施したこと

- `XamlTestHelper`（`DS4WindowsTests/TestHelpers/XamlTestHelper.cs`）を導入した。XAML を `XDocument` として読み、`x:Name`・属性・子要素・リソースキーで検査する。属性の並び・空白・改行・`/>` の形には依存しない。
- `XamlTestHelperTests`（10 件）で、同じ内容の XAML を「詰めた書式」と「整形した書式」の両方で読み、同じ結果になることを確認する。実物の `ProfileEditor.xaml` が XML として読めることも確認する。
- 置換した XAML の照合（脆さが確認できた 3 クラスのみ）:
  - `Phase10Step3bW0ProfileEditorLayoutTests`: XAML を読む 7 件（左右ペインのタブの順序、移したタブのリソース、Axis Config・Controls タブの構成、入口の Click ハンドラー、先頭タブ）。C# ソースを読む照合は据え置き。
  - `Phase10Step3W4ControlEditorHostGuardTests`: ホスト（`controlEditorHost`）の位置と、初期の `Collapsed`。
  - `Phase10Step3W3ControlMacroTabGuardTests`: 3 つのチェックボックスのバインディング。

### 8.2 検証（受入条件の確認）

`DS4Windows/` 配下の XAML 全 31 件を、属性を **1 行に 1 つ、名前順（昇順・降順の 2 通り）** に並べ替えて整形し、全テストを実行した。実験のあとは `git checkout` で XAML を戻した（差分なし）。

| テスト | 昇順 | 降順 |
|---|---|---|
| 置換後 | 2,738 件すべて合格 | 2,738 件すべて合格 |
| 置換前（`HEAD`） | **8 件失敗**（Layout 7・EditorHost 1） | **9 件失敗**（上記＋ControlMacroTab 1） |

- 置換前のテストが整形で落ちること（脆さ）と、置換後は落ちないことを、両方確かめた。
- 上の失敗は、置換した 3 クラスだけだった。XAML を読むほかの 13 クラス（`Phase10Step2W2TriggerPanelGuardTests`、`StickDeadZoneBindingGuardTests`、`ThemeResourceKeyGuardTests` ほか）は、整形では落ちなかった。**脆さが確認できたものだけ置換する**（§2 W1）の方針どおり、これ以上は書き換えない。
- この実験が確かめるのは、属性の順序・改行・インデントへの耐性まで。要素の並べ替えやコメントの削除をする整形は、対象外。

### 8.3 `Debug ALL` 相当の結果

| 項目 | 結果 |
|---|---|
| `Build Debug`（テストプロジェクトと依存） | 成功。**警告 0・エラー 0**（35.6 秒） |
| `Execute Test (Actions)` | **2,738 件すべて合格**（W0 の 2,728 件＋`XamlTestHelperTests` の 10 件） |
| `Execute Test (Standalone)` | **11 件すべて合格** |

注（今後の実験の手順）: `--no-build` でテストを動かす前に、テストのソースを入れ替えたら、ファイルの更新時刻を新しくして再ビルドすること。入れ替えたファイルの更新時刻が出力の DLL より古いと、MSBuild が再コンパイルを省略し、古いテストが動き続ける（今回、実際に起きた）。

## 9. W2 の結果（2026-11-02。Controller Readings の見切れ）

### 9.1 再現（実機の代わりに、実物のコントロールを描画して測った）

方法: `ControllerReadingsControl` を幅 470（左ペインの幅。Controls タブの 470 と同じ）で配置し、最大桁の値を入れて、WPF の割り当て幅と描画幅を比べた。フォントは、この PC の既定（**HiraMaruAA 16**。WPF の既定の 12 より大きい）と、Segoe UI 12・Yu Gothic UI 12・Meiryo UI 12・Segoe UI 16。

**計画の前提の訂正**: L2／R2／TX／TY の数値ラベルは、すでに幅 38・`Padding="1,1"` だった（2026-10-24 の `b00682e2`）。FurtherImprovements 03 の A-3 の案は実施済みで、残っていた見切れの原因は別だった（ラベル単体の幅ではなく、**行全体が列の幅を超えていた**）。

| # | 場所 | 症状（修正前） |
|---|---|---|
| 1 | トリガー・タッチパッドの行 | 中身の幅 229 に対し、列が 196.7。**フォントに関係なく**（Segoe UI 12 でも）両端が約 16 ずつ切れる。左の `I:`／`O:` と、右の TX／TY（`1920` が `192`） |
| 2 | スティックの行（LX／LY／RX／RY） | この PC のフォントで、行が 163〜166 に対し列が 154。右端の `255` が `25`。Meiryo UI 12・Segoe UI 16 でも、はみ出す |
| 3 | SixAxis の数値 | `-512` に必要な幅が 36.0〜36.3 で、ラベル幅 36 の余裕が 0 |
| 4 | ジャイロの `Pitch`（付随） | この PC のフォントで「h」が欠ける（数値ではないが、原因は同じ） |

### 9.2 変更（`ControllerReadingsControl.xaml`。見た目のみ。`x:Name`・バインディング・コードビハインド・ログは変えていない）

| 対象 | 変更 |
|---|---|
| スティック・SixAxis の「(I) 値 (O) 値」の行（6 行） | (I)／(O) の組を `WrapPanel` に入れ、**列の幅に収まらないときだけ 2 行に折り返す**。通常のフォント（12）では 1 行のまま。数値ラベルは、スティックが幅 34、SixAxis が幅 40、`Padding="1,2"`。折り返したときに左端が揃うよう、(O) 側の左余白 6 を、(I) 側の右余白 4 と (O) 側の左余白 2 に分けた（1 行のときの間隔 6 は同じ） |
| L2／R2 のグループ | 幅 70・右余白 12 → 幅 58・右余白 4（中身は `I:` 18＋数値 38＝56） |
| TX／TY のグループ | 幅 65 → 70、`TX:`／`TY:` のラベル幅 24 → 28（コロンが欠けないように） |
| トリガー／ジャイロの列の比率 | `1.3*`／`1.7*` → `1.35*`／`1.65*`（幅 470 で、トリガー側が約 205。中身は 194） |
| ジャイロの `Yaw`／`Pitch`／`Roll` のラベル幅 | 40 → 46（付随。ジャイロの枠の中に余裕がある） |

### 9.3 検証

- 新しいテスト `ControllerReadingsNoClippingTests`（2 件）: (1) 各数値ラベルの宣言の幅が、最大桁の値（255・1920・942・-512）に足りること。(2) 実物のコントロールを幅 470 で配置し、数値ラベルの文字と、それを入れる親の要素が、割り当てられた幅を超えて切られないこと。フォントは、この PC の既定・Segoe UI 12・Yu Gothic UI 12・Meiryo UI 12・Segoe UI 16。
- **赤→緑を確認**: 元の XAML（`HEAD`）では 2 件とも失敗し（上の 1・2・3 を検出）、修正後は合格する。
- `Debug ALL` 相当: Build 警告 0・エラー 0。Actions **2,740 件**（W1 の 2,738 件＋2 件）・Standalone 11 件がすべて合格。
- 修正の前後の画像（幅 470・この PC の既定フォント）で、スティック・トリガー・SixAxis・ジャイロの数値と `Pitch` が欠けず、Segoe UI 12 の見た目が変わらないことを確かめた。

### 9.4 未確認と注意

- **実アプリでの目視は未了。** 画像は、実物のコントロールを `RenderTargetBitmap` で描いたもので、実アプリの左ペインの実際の幅は測っていない（470 以上と見込む）。W6 の台帳に項目を足して確認する（ダーク・ライトの両テーマ、日本語・英語、L2／R2 全開、タッチパッドの右端）。
- 左ペインが 470 より狭くなる使い方は、想定していない（Controls タブが 470 の固定幅のため）。
- ジャイロの枠のほかの非数値のラベルは、確認の範囲外（`Pitch` だけ、同じ原因として直した）。

## 10. W3 の結果（2026-11-02。`Keepsize`）

### 10.1 調査で分かったこと（計画の前提の訂正）

§1.1 の 2 は「XAML は `Mode=TwoWay`、C# は get-only」という**事実**どおりだが、原因の見立てが違った。

- `ProfileEditor` は `DataContext` を自分に設定しておらず（`ProfileEditor.xaml.cs` の `DataContext` 代入は、個々の要素へのみ）、メインウィンドウの `DataContext = mainWinVM`（`MainWindowsViewModel`）を継承する。
- よって `IsChecked="{Binding Keepsize, Mode=TwoWay}"` は **`MainWindowsViewModel` の `Keepsize`** を探す。そこには `Keepsize` が無い（`Keepsize` を持つのは `ProfileEditor` だけ）。**バインディングは最初から解決していなかった**（`PathError`。実測で確認）。
- 状態を持っていたのは、Click ハンドラー `KeepSizeCheckBox_Click`（2025-11-06 の `d9cede6f` で、画像のボタンからチェックボックスへ変えたときに加わった）だけ。
- したがって、案 A（`set` を足す）は、バインディングの宛先が違うため、足しても何も変わらない。案 B（`OneWay` にする）も、宛先は同じく違う。

### 10.2 変更（挙動は変わらない）

| 対象 | 変更 |
|---|---|
| `ProfileEditor.xaml` | 解決しないバインディング `IsChecked="{Binding Keepsize, Mode=TwoWay}"` を外す。理由のコメントを足す。`Click="KeepSizeCheckBox_Click"` は残す |
| `ProfileEditor.xaml.cs` | `Keepsize` は get-only のまま。書き込みの経路が Click ハンドラーだけであることのコメントを足す |

`Keepsize` を読む側（`MainWindow` の SizeChanged・LocationChanged・`ProfileEditor_Closed`、`ProfileEditor` の `Close`・`ProfileEditor_Closed`）は変えていない。外したバインディングは、解決しないために、何も読み書きしていなかった（実行時に、出力のバインディングエラーが 1 件減る）。

### 10.3 検証

- 新しいテスト `Phase11W3KeepsizeTests`（4 件）:
  1. **証拠**: 本物の `MainWindowsViewModel` を DataContext にした `CheckBox` で、`Keepsize` のバインディングが `PathError` になり、チェックの操作で例外にならないこと（変更の前後とも合格）
  2. XAML: `keepSizeCheckBox` が Click ハンドラーだけを持ち、`IsChecked` のバインディング（`Keepsize`）がないこと（**変更前は失敗、変更後は合格**）
  3. Click ハンドラーがチェックの状態をフィールドへ入れること、`Keepsize` が get-only であること（特性化。変更の前後とも合格）
  4. 保存・復元の分岐が従来どおりであること（`ProfileEditor` の `if (keepsize)` の 2 か所、`MainWindow` の `editor.Keepsize` の 3 か所。特性化。変更の前後とも合格）
- `Debug ALL` 相当: Build 警告 0・エラー 0。Actions **2,744 件**（W2 の 2,740 件＋4 件）・Standalone 11 件がすべて合格。

### 10.4 未確認

- 実アプリでの目視は未了。W6 の台帳（P11-n）に、「画面サイズを保持」の確認を足す: ON のまま編集画面を閉じる → ウィンドウのサイズ・位置が設定に保存され、次回も同じ。OFF のまま閉じる → 編集画面を開く前のサイズへ戻る。

## 11. W4 の結果（2026-11-02。DS4 への出力補正とサービス再起動の復元）

### 11.1 失われていた挙動（旧コード＝`9da60b85` の親の特性化）

2026-09-14 の保存・適用の統合（`9da60b85`）で、旧 `SaveBtn_Click`／`SetLateProperties` にあった次の動きが落ちていた。

| 記号 | 旧コード | 保存 | 適用 | ログ |
|---|---|---|---|---|
| (a) | `UseDs3PitchRollSim` が ON なら `TempControllerIndex = 1`（出力を DS4 へ。出力が今何かは見ない） | ○ | なし | なし |
| (b) | `HasUseDs3PitchRollSimChanged` なら、`MainWindow.ChangeService`（停止／開始の**切り替え**）を 2 回続けて呼ぶ | ○ | なし | なし |
| (c)-2 | `EnableOutputDataToDS4` が ON で出力が DS4 でないなら `TempControllerIndex = 1` ((a) のあとなので、擬似が ON のときは動かない) | ○ | ○ | `LogToGui`（エラーではない） |

### 11.2 変更

| 対象 | 変更 |
|---|---|
| `Ds4OutputCorrectionReason.cs`（新規） | 補正の理由（`None`／`Ds3PitchRollSim`／`EnableOutputDataToDS4`）。1 ファイル 1 型 |
| `ProfileSettingsViewModel.cs` | `CorrectOutputTypeToDs4()`: 保存される値（`ControllerTypeIndex`）と画面の選択（`TempControllerIndex`）のどちらかが DS4 でなく、かつ擬似または `EnableOutputDataToDS4` が ON なら、両方を DS4 にして理由を返す。OFF のときは何もしない（DS4 から X360 へは戻さない）。`AcknowledgeUseDs3PitchRollSimChange()`: 再起動を求めたあとに変更の印を戻す |
| `ProfileEditor.xaml.cs` | `ExecuteSaveOrApply`（保存・適用の合流点）で、名前の検査のあと・保存の前に補正を呼ぶ。補正したら `outConTypeCombo.SelectedIndex = 1`（`SelectionChanged` が、設定・マッピング一覧の出力の種類も更新する）。(c)-2 の GUI ログは旧文言のまま。イベント `ServiceRestartRequested` を足し、保存が成功したときだけ `RequestServiceRestartIfNeeded()` で出す |
| `MainWindow.xaml.cs` | `ServiceRestartRequested` を購読し、`RestartService()` を呼ぶ。サービスが動いているときだけ、1 つの `Task.Run` の中で `Stop(immediateUnplug: true)` → `Start()` を順に行う |

### 11.3 設計上の選択（旧コードとの違い。要・ユーザー確認）

1. **再起動は保存のときだけ**（旧コードと同じ）。適用では再起動しない。適用のあとに保存すると、変更の印は再起動を求めたときだけ戻すので、保存で再起動を求める。
2. **旧コードの「切り替えを 2 回呼ぶ」は戻さない。** `ChangeService` は停止／開始の切り替えで、旧コードは 1 回目の完了を待たずに 2 回目を呼んでいた。復元では、停止→開始を 1 つの処理で順に行う。
3. **止まっているサービスは再起動しない**（ユーザーが止めたものを勝手に動かさない。設定は、次の開始で効く）。
4. **(a) の補正は GUI ログを出さない**（旧コードと同じ）。デバッグログ（`LogDebug`）だけ足した。(c)-2 は旧文言の `LogToGui` を維持した。
5. **Pure DI**: 編集画面は `Application.Current.MainWindow` を取り出さず、イベントで MainWindow に依頼する（旧コードは取り出して `ChangeService` を直接呼んでいた）。
6. 補正は、擬似が ON のとき**無条件に DS4 を保存する**ため、`OutContType` が X360 の既存プロファイルで擬似を ON にして保存すると、出力が DS4 に変わる（旧コードと同じ。Q1 の決定）。

### 11.4 検証

- 新しいテスト（15 件）:
  - `Phase11W4Ds4OutputCorrectionTests`（11 件）: 補正の組み合わせ 7 通り（X360／DS4 × 擬似 × `EnableOutputDataToDS4`）の期待値（理由・保存される値・画面の選択）、保存される値が基準であること（画面の選択だけが DS4 でも補正する）、くり返しても変わらないこと、変更の印の設定・解除、同じ値は変更ではないこと
  - `Phase11W4SaveApplyWiringGuardTests`（4 件。ソース走査）: 補正が名前の検査のあと・保存の前にあること、(c)-2 のログ文言が旧文言のままであること、再起動が保存だけでイベント経由であること（`ChangeService` を戻していないこと）、MainWindow が動いているときだけ停止→開始を 1 処理で行うこと
- **赤→緑**: 配線のガード 4 件は、変更前のソース（`HEAD`）では 4 件とも失敗、変更後は合格。ViewModel のテストは、変更前は新しい API が無くコンパイルできないため赤は取れない（特性化の期待値を、旧コードの動きから書いた）。
- `Debug ALL` 相当: Build 警告 0・エラー 0。Actions **2,759 件**（W3 の 2,744 件＋15 件）・Standalone 11 件がすべて合格。ラチェット（`Global`／`AppHost`／`Program.rootHub`）は増やしていない。

### 11.5 未確認

- **DS3 の実機での擬似の効果は `不能`**（DS3 が無い。§7.4）。ユニットテスト（旧コードの動きの特性化）で担保する。
- **DS4 での再起動の安全性は未了。** W6 の台帳（P11-n）に足す: プロファイル編集で `UseDs3PitchRollSim` を変えて**保存** → サービスが停止→開始し、DS4 が再接続され、プロファイルが再適用され、仮想パッドが付け外しされること。適用だけでは再起動しないこと。サービスを止めた状態で保存しても、開始されないこと。再起動中に [Start/Stop] ボタンが一時的に無効になり、終われば「停止」表示で有効に戻ること（停止の通知で一度有効になる瞬間がある点も観察する）。
- (c)-2 の GUI ログは、`EnableOutputDataToDS4` を ON・出力 X360 で保存または適用したときに 1 行出る（実アプリでの目視は W6）。

## 12. W5 の結果（2026-11-02。`CustomSteamFolder`、`CheckEvery`）

### 12.1 `CustomSteamFolder`（Issue #4 の 3）— 修正

**調査**: §1.1 の「`AppSettingsService` は代入するだけ」は正しいが、破棄の場所はそこではなかった。**画面の ViewModel `SettingsViewModel.CustomSteamFolder` の setter** が、`Directory.Exists(value) || value == string.Empty` のときだけ保存し、それ以外は何も言わずに捨てていた（この検査が入ったのは `68844a3a`「Fixed saving of custom steam folder setting」）。`TextBox`（`UpdateSourceTrigger=LostFocus`）には入力が残るため、保存されたように見える。Issue の主張どおり。

**決定（Q3）に沿った変更**:

| 対象 | 変更 |
|---|---|
| `SettingsViewModel.cs` | setter: 値は常に保存し（`null` は空文字として扱う）、空でなく存在しないフォルダーのときは `AppLogger.LogToGui(..., warning: true)` で警告を出す。同じ値の再入力では、くり返さない |
| `Strings.resx`／`Strings.ja.resx`／`Strings.Designer.cs` | 新しいリソース `CustomSteamFolderNotFound`（英語は既定、日本語は `ja`。`{0}` に入力したパス）。直書きにしない（4.2.3 の方針） |

- 警告の出し方は、既存の方式に合わせた GUI ログの warning。メインウィンドウの下端（`lastMsgLb`）に、どのタブでも警告の色で出て、ログの一覧とログファイルにも残る。モーダルのダイアログは、`LostFocus` の直後に割り込むため避けた。
- **保存しても害がないこと**: 値を使う側は `AutoProfiles` だけで、`UseCustomSteamFolder && Directory.Exists(CustomSteamFolder)` で確かめ、無ければ既定の Steam フォルダー（x86／通常）へ戻る。この確認が残ることは、テストで固定した。
- 文言（英）: `Custom Steam folder not found: {0}. The path was saved, but it is not used until the folder exists (the default Steam folders are used instead).` （日）`カスタム Steam フォルダーが見つかりません: {0}。入力したパスは保存しましたが、フォルダーが存在するまでは使われません（代わりに既定の Steam フォルダーを使います）。`

### 12.2 `CheckEvery`（Issue #4 の 4）— 仕様として閉じる

- **コードの動き**: `SettingsViewModel.CheckEvery` の setter は、「更新確認」（`CheckUpdateStartupEnabled`）が OFF のとき何もしない。ON のときは、値を保存して `Save()` する。
- **目視の代わりの検証**: 数値の入力欄を含む行 `checkEveryOptionsStack` は、XAML で `IsEnabled="{Binding CheckForUpdates}"`（入力欄自身は `IsEnabled` を上書きしない）。実物の `SettingsViewModel` に、同じ形のバインディング（チェックボックスと行の `IsEnabled`）をつなぐと、チェックを外した瞬間に行と中の入力欄が無効になり、戻すと有効に戻る。VM は `INotifyPropertyChanged` を実装しないが、書き戻しの経路で他のバインディングが更新されることを実測で確認した。**OFF のとき数値は操作できない**ので、Q4 の「操作できてしまう場合のみ再確認」には当たらない。
- **変更**: コードの動きは変えていない。意図を残すコメントを、setter（`SettingsViewModel.cs`）と `checkEveryOptionsStack`（`MainWindow.xaml`）に足した。setter が何もしないのは、無効な入力欄のバインディングが値を書き戻して更新確認を ON に戻すのを防ぐ安全弁。

### 12.3 検証

- 新しいテスト（13 件）:
  - `Phase11W5CustomSteamFolderTests`（9 件）: 存在しないフォルダーは**保存され、警告（warning）が 1 件出る**、存在するフォルダーは警告なしで保存、空文字は警告なしでクリア、`null` は空文字として保存、同じ値では警告をくり返さない、存在しない→存在するへの変更、警告文の英語・日本語のリソース（`{0}` あり、日本語は英語のコピーでない）、Designer の入口、使う側の `Directory.Exists` の確認が残ること（ソース走査）
  - `Phase11W5CheckEveryTests`（4 件）: OFF のとき数値の変更は無視される（保存もしない）、ON のとき保存される、`checkEveryOptionsStack` が行ごと `IsEnabled` に束縛されること（XAML）、実物の VM でチェックの外し／戻しに行が追随すること（STA）
- **赤→緑**: 変更前の setter では、`ANonExistentFolder_IsKept_AndWarned`・`TheSameNonExistentFolder_IsNotWarnedAgain`・`ANullValue_IsStoredAsEmpty` の 3 件が失敗（入力が保存されない）、リソースを足して setter を直したあと 13 件すべて合格。`CheckEvery` の 4 件と残りは特性化のため、変更前後とも合格。
- `Debug ALL` 相当: Build 警告 0・エラー 0。Actions **2,772 件**（W4 の 2,759 件＋13 件）・Standalone 11 件がすべて合格。

### 12.4 未確認

実アプリでの目視は未了。W6 の台帳（P11-n）に足す:
- 設定タブの「Use custom Steam Folder」を ON にし、存在しないパスを入れてフォーカスを外す → メインウィンドウの下端に警告が出る。アプリを再起動しても入力したパスが残る。存在するフォルダーを入れると、警告が出ず、自動プロファイルの Steam メニューがそのフォルダーを使う。
- 設定タブの「更新確認」を OFF にする → 間隔の数値と単位が灰色になり、操作できない。ON に戻すと操作できる。数値を変えると保存される。

## 13. W6 の準備と進行（2026-11-02。実機確認）

### 13.1 実施したこと（エージェント側）

- **台帳 `Phase6-Step11-RealDevice-Verification-Checklist.md` の §52 を新設**。確認項目を、§4 の順序に沿って 8 つのまとまり（S1〜S8）に整理した。各行は、結果欄が「未」。
- **機材が無い 20 行を `不能` にした**（§7.4 のユーザーの回答どおり）。各行の結果欄に `不能（U-50／U-51。…）。元の記録: …` と書き、§11 に U-50（外部ツール 7 行）、U-51（他機種 13 行）、U-52（W4 の DS3 実機）を足した。確認の時期は「機材・ツールを入手したとき」。
- **§28.1 の合格を、台帳へ転記**: P10-102（確認 7）、P10-105（確認 11）は項目の全体を覆うため ○。ほか（P10-80〜82、P10-98〜101・103・104）は、§28.1 で合格した部分を除いた「残り」だけを S3・S5・S6 に置いた。
- **実機確認用のビルド**: `13370cad`（W5 まで）の Release x64（版は 4.2.3 のまま。4.2.4 は W8）を `DS4Windows\bin\x64\DS4Windows\` に作った（`Remove-bin` → `publish-win-x64.ps1` → `post-build.ps1`）。管理者権限が要る最後の「Program Files への複写」（`scripts\CopyDS4WindowsToJK.bat`）はユーザーが行う。

### 13.2 確認項目の件数

| まとまり | 内容 | 件数 | 機材 |
|---|---|---|---|
| S1 | Phase11 で変えた箇所（P11-1〜8） | 8 | DS4 1 台（P11-1 は接続中のコントローラー） |
| S2 | Controls の押下判定とログ（P10-83〜86。R-5 後の期待値） | 4 | DS4 1 台 |
| S3 | Launch Program・Load Profile・保存の可否（P10-79〜82） | 4 | DS4 1 台 |
| S4 | 固定 ID（P10-90〜97。要バックアップ） | 8 | DS4 1 台 |
| S5 | SA（P10-87・88・98）、Controls の Extras（P10-21） | 4 | DS4 1 台 |
| S6 | 回帰（P10-89 と §29・§30 の残り） | 6 | DS4 1 台（アナログ出力はゲームコントローラーのテスター） |
| S7 | 残り（P9M2-2〜4、P9M3-3、P9E6b-2・5、P9E6e-4・6、P9E6a-4、P9F1-4。任意 4 件を除く） | 10 | DS4 1 台。P9F1-4 は設定のない環境 |
| S8 | 2 台目の DS4（P9B-4、P9C-4、P9E-14、P9E6a-3、P9E6e-5） | 5 | DS4 2 台 |

合計 49 行（任意の P9E5-5、P9E6f1-5、P9E6f2-6・7 を除く）。1 回で終わらせず、まとまりごとに結果を受けて記録する。**S1 は、Phase11 で変えたコードの確認のため、最初に行う。**

### 13.3 結果

（実機確認の結果を、まとまりごとに記録する。）

- **S1（P11-1〜8）: 2026-11-02 完了。8 件とも ○**（詳細は台帳 §52）。確認中に、W4・W5 の変更とは無関係の既存の課題を 3 件見つけた（K-W6-1〜3、§13.4。修正は全部の実機テストが終わってから）。
- **S2（P10-83〜86）: 2026-11-02 完了。4 件とも ○**（台帳 §52）。確認中に K-W6-4〜6 を見つけた（§13.4。K-W6-4 は方針案［承認待ち］、K-W6-5・6 は調査待ち）。
- **S3（P10-79〜82）: 2026-11-02 完了。4 件とも ○**（台帳 §52）。P10-82 の確認中に K-W6-7 を見つけた（§13.4。テスト後に調査）。
- **S4（P10-90〜97）: 2026-11-02 完了。7 件 ○、P10-96 は △**（台帳 §52）。リンクが指すプロファイルの削除でクラッシュする重大な不具合 K-W6-9 と、複製の直後の ID の不具合 K-W6-8 を見つけた（§13.4）。
- **S5（P10-87・88・98・21）: 2026-11-02 完了。4 件とも ○**（台帳 §52）。P10-98 の振動は不能［U-53］。
- **S6（P10-89 と残り 5 行）: 2026-11-02 完了。6 件とも ○**（台帳 §52）。P10-89 の Macro の Scan Code・Synchronized Run・Keep key state は見送り［ユニットテストで担保。W8 の完了報告に明記］。
- **S7（途中）: 2026-11-02。P9M2-2・3・4 は 2026-10-22 の P9EX-1・P9M3-1 の確認を根拠に ○、P9E6e-4 は P10-96 ① を根拠に ○**（台帳 §52。再実施なし）。残りの 6 行（P9M3-3、P9E6b-2・5、P9E6e-6、P9E6a-4、P9F1-4）は未。

### 13.4 実機確認で見つかった課題と修正方針（修正は、全部の実機テストが終わってから行う。ユーザー決定 2026-11-02）

**K-W6-1: Settings タブで、最下部の 1 行メッセージ欄（`lastMsgLb`）が隠れる**（P11-7 で発見）
- 原因: `MainWindow.xaml.cs` の `MainTabCon_SelectionChanged` が、タブの番号（`SelectedIndex == 4`）で欄を隠す。本来は Log タブを隠す意図と思われるが、Output Slots タブが途中に入って番号がずれ、実際は **Settings** で隠れる（取り込んだ初期コミット `906946a7` から同じ書き方。今回の変更とは無関係）。Settings で出る警告（P11-7 の Steam フォルダーなど）が、他のタブへ移るまで見えない。
- 方針（決定）: **すべてのタブで表示する**。`MainTabCon_SelectionChanged` と、`MainWindow.xaml` の `mainTabCon` の `SelectionChanged` 属性を削除し、欄を隠す処理を持たない。
- テスト（作成済み。実装時に戻す）: `Phase11W5StatusMessageVisibilityTests`（欄がタブの外にあり `Visibility` を持たない、`SelectionChanged` と `lastMsgLb.Visibility` がコードにない）。変更前は 1 件失敗することを確認済み。
- 実機確認: 修正後に、P11-7 の手順 A を Settings タブのまま再確認する。
- それまでは、P11-7 の警告は、Settings 以外のタブへ移って確認する。

**K-W6-2: サービス停止中に、プロファイルを Save／Apply すると、接続していないスロットへ適用され、ログと独自ウィンドウ通知が出る**（P11-6 の手順 C で発見）
- 症状: サービスを Stop した状態で、直前まで使っていたプロファイル（例: verifyA）を Edit から Save または Apply すると、Gyro 設定に関係なく、ログに `Controller 1 is now using Profile "verifyA" (Battery: N/A%)` が出て、独自ウィンドウの通知も出る。サービスは起動しない（P11-6 の期待どおり）。
- 原因（調査済み・コードの読みのみ）: `ProfileEditor` の Save／Apply → `MainWindow.Editor_ProfileSaved` → `SyncProfileListAndControllers`（`MainWindow.xaml.cs` の ④ 再適用）が、`profileRepo.ProfilePath[i]` が保存したプロファイルと一致する**全スロット**へ `profileSlotApplier.ApplyToSlot(i, …, Manual)` を呼ぶ。停止中でもスロット 0 の `ProfilePath` は前回の名前のまま残るため一致する。`ProfileSlotApplier.ApplyToSlot` は範囲と名前の検査だけで、そのスロットに**デバイスが接続されているか、サービスが動いているか**を見ない。`ApplyProfileWithoutHalt` → `ApplyCore` → `CompleteProfileApplication` が、`ds4Device == null` のとき電池 `N/A`、`LogToGui` と `LogProfileChanged`（→ `MainWindow.OnProfileChanged` → 通知）を出す。
- 影響: 動作中でも、接続していないスロット（`ProfilePath` が同じプロファイルを指す）に対して、同じ無意味な適用・ログ・通知が出る可能性がある（未確認。S2 以降で観察する）。XML の読み込みは行われるため、停止中の副作用は通知とログと `SelectedProfile`・`OlderProfilePath` の更新。
- 方針（ユーザー承認済み 2026-11-02。修正は全部の実機テストが終わってから）: `SyncProfileListAndControllers` の再適用ループで、**そのスロットにデバイスが接続されている**（`ControlService.DS4Controllers[i] != null`）ときだけ `ApplyToSlot` を呼ぶ。停止中は何も適用しない（プロファイルの XML はディスクに保存済みで、次の接続時に読まれる）。`ProfilePath` の更新（③）は従来どおり。
- 注意: 機能の削減にならないか（接続前のスロットの `SelectedProfile` が Save で更新されなくなる影響）を、実装前に確認する。通知・ログ文言は変えない。

**K-W6-3: コントローラーの接続直後のログ・通知で、電池残量が 0% と表示される**（P11-6 手順 D の確認中に発見）
- 症状: 接続時のログ `Controller 1 is now using Profile "verifyA" (Battery: 0%)` と、通知 `Battery level is 0%` が 0% になる。同じプロファイルを接続後に Save すると、`Battery: 100%` と正しく出る。
- 原因（調査済み・コードの読みのみ）: 電池残量 `DS4Device.battery` は、入力レポートの読み取りループ（`DS4Device.cs` 1137〜1156 行付近）だけが更新する。`DeviceConnector.PrepareConnected` は、`ApplyOnConnect`（プロファイルの適用。ここで `CompleteProfileApplication` が `device.Battery` を読んでログ・通知を作る）を呼んだあとの最後（169 行）で `device.StartUpdate()` を呼ぶ。したがって、ログを作る時点では、最初の入力レポートがまだ読まれておらず、`battery` は初期値の 0。
- 方針（案 A をユーザー承認済み 2026-11-02。修正は全部の実機テストが終わってから）:
  - 案 A（推奨）: `StartUpdate` の前に、**最初の入力レポートを 1 回だけ同期で読み、電池残量だけを取り込む**（`DS4Device` に `PrimeBatteryReading` のような仮想メソッドを足し、DS4 の USB／BT を実装。レポートの種類が違う DualSense・Switch Pro・Joy-Con・DS3 は、従来どおり 0 → 後で更新のままにするか、各クラスの電池の取り出しを共通の小メソッドへ切り出す）。プロファイル適用の順序、ログ文言は変えない。
  - 案 B: 電池がまだ読まれていない間は、数値ではなく `N/A` を出す（`DS4Device` に `HasBatteryReading` を足す）。0% とは出なくなるが、「最初のログから正しい値」にはならない。
  - 案 C: 接続時のログ・通知を、最初の入力レポートが届いてから出す。ただしプロファイル適用はレポートの処理前に終える必要があり（適用前のレポートが既定の割り当てで処理されるため）、ログだけ遅らせると順序が変わる。リスクが大きく非推奨。
- 注意: 今回の変更（Phase11）が原因ではなく、接続処理の順序が元からこの形である可能性が高い（上流のコードの流れ。履歴の確認は未実施）。機種ごとの電池の取り出しが違うため、案 A は DS4 だけ実機で確認できる（他機種は 不能、U-51）。

**K-W6-4: 編集画面で Emulated Controller を DS4 ↔ Xbox 360 に変えても、そのまま開いた出力ピッカー（BindingWindow）の Pad タブが前の種類のまま**（S2 の P10-84 準備中に発見）
- 症状: プロファイルの編集中に Emulated Controller を DS4 から Xbox 360 へ変え、保存せずに出力ピッカーを開くと、Pad の表示が DS4 のまま。いったん保存し、割り当て編集画面を開き直してから出力ピッカーを開くと、Xbox 360 の表示になる。
- 原因（調査済み・コードの読みのみ）: `BindingWindowViewModel` のコンストラクター（77 行）は、`outputSlotService.OutDevTypeTemp[deviceNum]` から Xbox 360 モードかどうかを決める。一方、編集画面のコンボを変えたときに更新される `ProfileSettingsViewModel.TempControllerIndex`（814〜823 行）は、`profileSettings.OutContType[device]` だけを書き換え、`OutDevTypeTemp[device]` は更新しない。`OutDevTypeTemp` は、編集画面を開いたとき（2861・3469 行付近）にしか書かれないため、画面を開き直すまで古いまま。
- 方針（ユーザー承認済み 2026-11-02。修正は全部の実機テストが終わってから）: `TempControllerIndex` の setter で、`outputSlotService.OutDevTypeTemp[device]` も同じ種類へ更新する（`TempConType` 経由も同じ setter を通る）。保存する値（`OutContType`）の扱いと、W4 の補正（`CorrectOutputTypeToDs4`）は変えない。
- テスト案: `TempControllerIndex` を 0 ↔ 1 に変えたあとの `OutDevTypeTemp[device]` を検査する（変更前は失敗する）。

**K-W6-5: 出力を Xbox 360 にすると、ライトバーが正しく表示されないことがある**（S2 の P10-84 の確認中にユーザーが報告）
- 症状: Emulated Controller を Xbox 360 にしたとき、ライトバーが消える、または別の色になることがある（再現条件は未特定）。
- 方針（ユーザー決定 2026-11-02）: 全部の実機テストが終わってから調査する。原因・修正案は未調査。調査では、Xbox 360 のときのライトバーの色の決まり方（プロファイルの色、バッテリー色、Xbox の LED 通知の取り込みの有無）と、プロファイル適用・接続時の順序を見る。

**K-W6-6: 出力が Default のボタンで、出力ピッカーを何も選ばずに × で閉じると、行のプルダウンが Default 以外の種別から Default に切り替わって見える**（S2 の P10-85 準備中にユーザーが報告）
- 症状: 出力が Default になっているボタンの行で、出力ピッカー（BindingWindow）を開き、何も押さずに右上の × で閉じると、行のプルダウンの表示が「Output Key／Mouse／Button」ではなく Default に切り替わる（詳細な前後の状態は未確認）。
- 方針（ユーザー決定 2026-11-02）: 全部の実機テストが終わってから原因を調べる。原因・修正案は未調査。調べる場所は、`BindingWindow` を閉じたときに行へ結果を書き戻す処理（キャンセルと決定の区別）と、行のプルダウンの選択の更新。

**K-W6-7: Load Profile の切り替え先が (none)（削除で未選択）のとき、ボタンを押すと仮想コントローラーが切断され、離すと元のプロファイルが再適用されて再接続する**（S3 の P10-82 で発見）
- 症状: Controls のボタンに「Load Profile」を割り当て、切り替え先のプロファイルを削除すると、割り当て編集画面の切り替え先は (none) 表示になる。この状態でボタンを押すと、仮想コントローラーが切断され（Windows の切断音）、離すと現在のプロファイル（verifyA）が適用され直して独自の通知ウィンドウが出て、仮想コントローラーが再接続される（接続音）。
- ユーザー要望（2026-11-02）: (none) のときは「何もしない」形に変えられるなら、テスト終了後に調査して修正する。現状の動きのままでも許容できる。
- 方針: 全部の実機テストが終わってから調査する。調べる場所は、Load Profile（`ProfileApplicationService.ApplyFromAction`／`ResolveTarget`）で切り替え先が空のとき。空の名前のまま `ApplyCore` へ進んでいないか。空なら先頭で何もせずに戻す案（ログ文言と機能は変えない）。実装前にユーザーの承認を得る。

**K-W6-8: アプリの「複製（Duplicate）」で作ったプロファイルが、元と同じ ProfileId のまま（再起動するまで）**（S4 の P10-90 で発見）
- 症状: Profiles タブの複製で verifyC3 を作ると、verifyC3.xml の `<ProfileId>` が元の verifyC2 と同じ。アプリを再起動すると、コピー側だけに新しい ID が振られる（起動時の補正）。
- 原因（調査済み・コードの読みのみ）: `DupBox.xaml.cs` の `SaveBtn_Click` が `File.Copy` で XML をそのままコピーするだけで、新しい ID を振らない（63〜72 行）。P10-90 の仕様は「ファイルをコピーして起動し直すと新 ID」だが、アプリ内の複製は、起動し直す前から別のプロファイルなので、複製の直後に新しい ID を振るのが自然。同じ ID のまま使うと、ID で引く参照（スロット・リンク・自動プロファイル・Load Profile）が、どちらを指すか曖昧になる期間が生じる。
- 方針の案（ユーザー承認待ち。修正は全部の実機テストが終わってから）: `DupBox` の複製の直後（`Save?.Invoke` の前）に、コピー側へ新しい ID を振る（`IProfileIdentityService` に、既存の「別名で保存」「新規作成」が新 ID を得ている経路と同じ手段があれば、それを使う。実装前に確認）。起動時の補正は、そのまま残す。

**K-W6-9【重大・クラッシュ】: 自動プロファイル（またはリンク）が指しているプロファイルを削除すると、アプリが落ちる（`TargetException: Object does not match target type`）**（S4 の P10-96 手順 3 で発見。2026-11-02 12:55:21）
- 症状: リンクと自動プロファイルのルールから指されているプロファイル（verifyD1）を Profiles タブで削除すると、`[ERROR] Thread Crashed with message Object does not match target type.`（`System.Reflection.TargetException`）でアプリが異常終了した（WER のダンプも出力された）。直前に Controllers タブの選択を `aimlabs` に変えていた。
- スタック（要点）: `Selector.OnItemsChanged → RemoveFromSelection → ComboBox.OnSelectionChanged → ComboBox.UpdateSelectionBoxItem → ContentPresenter.OnContentChanged →（継承プロパティの変更で）BindingExpression.OnDataContextChanged → PropertyPathWorker.ReplaceItem → ValueChangedEventManager.AddHandler → ReflectEventDescriptor.AddEventHandler → RuntimeMethodInfo.Invoke`。つまり、ComboBox の項目が削除されて選択が外れ、選択欄の中身（ContentPresenter）の型が変わったときに、WPF のバインディングが、前の型のプロパティ記述子で新しい型のオブジェクトにイベントを付けようとして失敗している。
- 有力な原因（コードの読みのみ。未確認）: `AutoProfiles.xaml` の 4 つのコンボ（`cont1AutoProf` ほか）は、項目が `CompositeCollection`（先頭の `ComboBoxItem`「(none)」＋ `ProfileEntity` の一覧）で、`DataTemplate` を型別に 2 つ持つ（`{Binding Content}` と `{Binding Name}`）。選択中の項目（ProfileEntity）が消えて (none) の `ComboBoxItem` へ選択が移るとき、選択欄の `ContentPresenter` の型が入れ替わり、上のスタックの経路で落ちる可能性が高い。リンクの Linked Profile コンボ（`MainWindow.xaml` 138 行、`ItemsSource="{Binding ProfileListCol}"`・`SelectedItem="{Binding LinkedProfileName}"`）も同じ形で、どちらが原因か、両方かは未確認。
- 位置づけ: Q5 の「重大な不具合は直す」に当たる（アプリが落ちる）。ただし、ユーザー決定（2026-11-02）に従い、修正は全部の実機テストが終わってから行う。P10-82 の verifyB の削除（Controls の Load Profile だけが指していた）では落ちなかったため、「自動プロファイル／リンクが指している」ことが条件とみられる。再現の確認・既存の問題かどうか（Phase10 以前から）は未確認。
- 方針の案（承認待ち）: まず再現手順を確定し（自動プロファイルだけ／リンクだけ／両方）、落ちるコンボを特定する。修正の候補は、(a) 項目を削除する前に、そのプロファイルを指す選択を (none) へ明示的に外す（コンボの `SelectedIndex` を先に 0 にする）、(b) `CompositeCollection` をやめて、(none) を含む 1 種類の項目型にそろえる。どちらも、画面の動作は変えない。単体テストは STA でコンボを作り、選択中の項目を削除する形で再現する。
- **絞り込みの結果（2026-11-02、再起動後に再現確認）**: 自動プロファイルのルールだけが指すプロファイル（verifyE）の削除は**落ちない**。**リンク（Linked Profile）だけが指すプロファイル（verifyF）の削除は落ちる**。スタックの末尾は `MainWindow.DeleteProfBtn_Click`（1581 行 `profileListHolder.ProfileListCol.RemoveAt(idx)`）→ `ObservableCollection.OnCollectionChanged` → コンボの `Selector.RemoveFromSelection` → `UpdateSelectionBoxItem`。よって原因のコンボは、**Controllers タブの Linked Profile コンボ（`linkedProfCombo`、`MainWindow.xaml` 137〜145 行。`SelectedItem="{Binding LinkedProfileName, Mode=TwoWay}"`）**。Auto Profiles のコンボは原因ではない（上の「有力な原因」は誤り。訂正する）。
- 追加の手がかり: `ProfileEntity`（`DS4Control/Profiles/ProfileEntity.cs`）は `INotifyPropertyChanged` ではなく、旧式の `NameChanged` イベント（30〜42 行）で名前の変更を通知する。WPF はこの形のオブジェクトへのバインディング（`{Binding Name}`）に `ValueChangedEventManager`＋リフレクション（スタックの `ReflectEventDescriptor.AddEventHandler`）を使う。選択欄の中身が入れ替わるとき、前の型のプロパティ記述子で別の型のオブジェクトにイベントを付けようとして落ちている可能性が高い（選択が外れた瞬間に、選択欄の `Content` が `ProfileEntity` から別の型［null のプレースホルダーなど］へ変わる経路）。
- 修正方針の候補（承認待ち。全部の実機テストが終わってから）: (a) **`ProfileEntity` に `INotifyPropertyChanged` を実装**し、WPF の旧式イベント経路（`ValueChangedEventManager`）を使わせない（`NameChanged` の既存の購読先は維持）。(b) 削除の前に、そのプロファイルを `LinkedProfileName` に持つ行のコンボの選択を外す（削除対象の `ProfileEntity` を指す行の `LinkedProfileName` を null/(none) へ）。(a) は構造的な対策、(b) は症状の回避。まず単体テスト（STA で `linkedProfCombo` 相当のコンボを作り、`SelectedItem` の項目を `RemoveAt`）で再現して、(a)／(b) のどちらで直るかを確かめる。
