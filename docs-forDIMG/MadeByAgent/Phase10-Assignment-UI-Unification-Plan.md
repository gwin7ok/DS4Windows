# Phase10 計画書（案）: 割り当て編集画面の統合（Controls タブと SpecialActions タブ）

作成日: 2026-10-20  
改訂日: 2026-10-21（範囲を「マクロだけ」から「共通の編集画面への統合」へ拡大。`Phase8-Fix-MacroRepeatUnify` の影響を反映）  
対象ブランチ: `For-DI-migration-work`  
上位: `Phase8-Unified-Trigger-Dispatch-Plan.md`（D2＝B1: 違いはトリガーのボタン数だけ）、`Phase8-Step2b-ControlsTab-Design.md`、`Phase8-Step7-8c-Plan.md`（Controls の Macro を SA の仕様に合わせる）、`Phase8-Fix-MacroRepeatUnify-Plan.md`（マクロの繰り返しフラグの統合）、`Phase9-Responsibility-Split-Plan.md`  
状態: **方針承認済み（2026-10-21、§6）。Step1 完了（2026-10-21）。Step2 以降は、個別計画書を作り、承認を得てから着手する。進捗は `Phase10-Status.md`**

---

## 1. 目的と構想（ユーザーの指示、2026-10-21）

Controls タブ（トリガー＝単体のボタン）と SpecialActions（SA）タブ（トリガー＝複数のボタンの組み合わせ）は、トリガーの選び方が違うだけで、割り当てる処理はほぼ同じである。UI を次のように統合する。

- タブ自体は今のまま 2 つに分ける。
- Controls タブ: 設定対象のボタンを選ぶ → **共通の編集画面**を開く。
- SA タブ: 設定対象の SA を編集 → **共通の編集画面**を開く。
- 共通の編集画面: 現在の SA 編集画面をベースにする。Controls から開いたときは、左ペイン（トリガー）に対象のボタンだけを表示し、Name 欄は編集不可にして、対象のボタン名を表示する。
- 右ペイン最上部の種別のリストに、Controls 側・SA 側で設定できる処理を統合して表示する。
- 実行時の処理も、どちらのタブで設定したものも共通の部品で処理する。

## 2. 現状（2026-10-21 の調査）

### 2.1 画面とデータ

| 項目 | Controls タブ | SA タブ |
|---|---|---|
| 編集画面 | `BindingWindow`（コードビハインド 958 行、XAML 579 行）。Xbox 画像、キーボード、マウスをクリックして選ぶ | `SpecialActionEditor`（708 行、XAML 547 行）。トリガーはチェックボックス約 50 個（+ 解除トリガー約 50 個） |
| 保存先 | プロファイルの `DS4ControlSettings`。種別は `Default／Key／Button／Macro` の 4 つ、`extras`（9 項目）、`keyType` のフラグ | `Actions.xml` の `SpecialAction`。種別は 10 個（None, Key, Program, Profile, Macro, DisconnectBT, BatteryCheck, MultiAction, 較正 2 つ, Button）。プロファイルごとに有効・無効 |
| 編集の ViewModel | `BindingWindowViewModel`、`OutBinding` | `SpecialActEditorViewModel`（保存・検証・リスト操作と密に結合）、`SpecialActions/*` |
| 実行 | `ControlBindingBuilder` → `TriggerDispatchEngine` | `ActionBindingBuilder` → `TriggerDispatchEngine`（同じエンジン） |

### 2.2 Macro の違い（`Phase8-Fix-MacroRepeatUnify` 後）

| 項目 | Controls | SA |
|---|---|---|
| 繰り返しのフラグ | `RepeatMacro`（**統合済み**。旧 `HoldMacro` は廃止。読み込みでは旧値を受け付けて置き換える） | `RepeatMacro` |
| 再生の種別の表現 | ドロップダウン（Play Once／Repeat While Held） | チェックボックス（Repeat While Held） |
| Scan Code | あり | あり |
| Run on release、Sync、Keep key state | なし（保存する欄がない） | あり |

## 3. 位置づけ

- **新しい Phase（Phase10）。Phase9 の中には入れない。** Phase9 は機能追加と保存形式の変更を範囲外にしている。この統合は、新しい画面、Controls への項目追加、保存形式の拡張を含む。
- **実行時の共通化は Phase8 でほぼ完了している**（同じエンジン、Key・Macro・Button は共通の出力アクション）。Phase10 の主な作業は、画面の統合と保存形式である。
- **Phase9 との順序**: 9-A → 9-B → 9-C → 9-E（`Mapping`／`ControlService` の内部整理。画面と重ならない）→ **Phase10** → 9-F → 9-G → 9-I。
  - 理由: 9-F は `ProfileEditor`／`MainWindow` のコードビハインドの整理。9-G は `ProfileSettingsViewModel` の分割。`SpecialActionEditor.xaml.cs`（708 行）と `BindingWindow.xaml.cs`（958 行）は、Phase10 で作り直すため、先に整理すると二重の作業になる。

## 4. 想定される問題と対応

| # | 問題 | 対応の案 |
|---|---|---|
| P1 | Controls の保存形式（`DS4ControlSettings`）に、SA 専用の種別（プログラム起動、プロファイル切替、DisconnectBT、バッテリー確認、MultiAction、較正）と、SA 専用の項目（Run on release、Sync、Keep key state、Hold 秒数、解除トリガー）の保存先がない | Q1（保存形式の方針）。推奨は案 A（`DS4ControlSettings`／`Profile.xml` を拡張） |
| P2 | Controls 専用の出力（マウス移動、ホイール、タッチパッドクリック、Extras、アナログ軸）は、入力のアナログ値で強さが決まる。SA の組み合わせトリガーはオン／オフのみで、意味が定義できない | Q2。SA 側で選べる出力の範囲を決める |
| P3 | SA は `Actions.xml`（全プロファイルで共有、プロファイルごとに有効化）、Controls はプロファイルに属する | 共通画面の保存先を、アダプターで切り替える（トリガーの供給元と保存先を抽象化） |
| P4 | `SpecialActEditorViewModel` が SA の保存・検証・リスト操作と密に結合している | 画面の状態（種別、各項目）、トリガーの供給元、保存先、検証に分離する |
| P5 | `BindingWindow` の視覚的な選択 UI（キーボード、Xbox 画像、マウス）を、共通画面に移す必要がある。SA 側には、キー取得ダイアログしかない | Q3。UserControl として埋め込む。**最大の作業量**で、実機（画面）の確認が必須 |
| P6 | Controls 固有の状態: Default／Unbound。Toggle の意味の違い（持ち越し C-5、マウスボタンの Toggle の制限） | Default／Unbound は Controls モードのみに表示。C-5 は、統合の前に調査して、仕様か不具合かを確定する |
| P7 | 機能を落とさない検証 | 既存のプロファイル、`Actions.xml`、`ProfileActions`、Extras 文字列、`keyType` を、**往復の変換テスト**で固定する（読込 → 保存 → 読込の一致） |
| P8 | 言語リソース（4 言語）、ホバーの表（`ProfileEditor.HoverTables`）、`ProfileEditor` から `BindingWindow` を開く経路 | Step ごとに更新 |
| P9 | 保存形式の下方互換 | `Phase8-Fix-MacroRepeatUnify` の D4（新しいファイルを旧バージョンで読めなくてもよい）が前例。ただし Q1 で改めて承認を得る |

## 5. Step の構成（案）

- **Step1（完了 2026-10-21）: マクロの再生オプションの共通部品**（Controls と SA。個別計画書: `Phase10-Step1-Plan.md`）
  - 範囲（2026-10-21 改訂）: SA の Macro タブに Execute Mode（Play Once／Repeat While Held のドロップダウン）を追加し、SA から開く記録画面（`RecordBox`）の繰り返し・Scan Code を消す。Press Key の Scan Code を選択ダイアログからタブへ移し、ダイアログの Toggle と Scan Code を（SA から開く場合に）消す。部品は `MacroPlaybackOptions` と `MacroPlaybackOptionsPanel`。**Controls 側の画面は、共通の編集画面（Step2）ができるまで変えない**（Controls は、選択ダイアログの「Record A Macro」「Toggle」「Scan Code」が唯一の設定場所のため）。Run on release、Sync、Keep key state を Controls に出すことは、保存形式の拡張が要るため **Step3 に回す**（承認済み）。
  - 繰り返しのフラグは、`Phase8-Fix-MacroRepeatUnify` で `RepeatMacro` に統合済みのため、保存・読み込み・実行側の統一は不要。
- **Step2（完了 2026-10-21）: 共通画面の骨格**。ViewModel の分離（P4）と、Controls／SA の 2 モード化（トリガーの供給元、Name 欄、保存先）。個別計画書: `Phase10-Step2-Plan.md`。
- **Step3（繰り上げ。2026-10-21 の順序の改訂）: Controls タブを共通画面へ接続する**（旧 Step4）。個別計画書: `Phase10-Step3-Plan.md`。
  - **順序を変えた理由**（ユーザーの確認、2026-10-21）: 「2 つのタブから共通の割り当て設定画面を開く」（UI の統一）は、保存形式の拡張を**必要としない**。Step2 で、Controls のモードは、「今 Controls が保存できる種別［Key・Button・Macro］だけ選べて、ほかはグレーアウト」になっている。保存形式を広げずに、Controls タブを共通画面へ接続すれば、**機能は減らず**、UI が統一される。Controls で SA 専用の種別を選べるようにする作業（保存形式の拡張）は、後から足せる（グレーアウトの項目が、選べるようになるだけ）。
  - 範囲: Controls の割り当ての編集（6 か所の呼び出し）を、従来の選択ダイアログ `BindingWindow` から、割り当てエディター（左ペイン全体に重ねて表示）へ切り替える。選択ダイアログ `BindingWindow` は、**出力ピッカー**として、エディターの「Select a Key」から開く（Record A Macro、Toggle、Scan Code を削除。Default／Unbound と Extras は、Controls のときだけ出す。Extras は、エディターへは移さず、出力ピッカーに残す）。C-5（Button の Toggle）は、共通画面の Switch Mode で解消する（Step2 W4 で書き出しは対応済み）。
- **Step3b（追加。2026-10-21、ユーザーの方針）: Touchpad タブとジャイロタブを左ペインへ移す**。2 つは、入力から出力への割り当て（Swipe、Tilt）が主な機能のため。Axis Config、Lightbar、Other は右ペインに残す。左ペインの幅［470］に全メニューが収まることは、ユーザーが画面で確認済み。レイアウトの変更のみで、ロジックは変えない。Step3 の割り当てエディターの表示場所（左ペイン全体）は、タブの位置に依存しない。**あわせて、Axis Config（右ペインに残る）の 4 つの入口（LS／RS の Outer Bind、L2／R2 の Full Pull）を、左ペインの Controls タブに、ボタンとして追加する**（ユーザーの指示、2026-10-21。一覧への項目追加ではなく、ボタン）。これで、割り当てエディターを開ける入口が、すべて左ペインのタブのボタンになり、編集中は左ペイン全体が割り当てエディターで覆われるため、二重に開けない（構造による防止）。コントローラーの入力で開く経路（タイマー）の「編集中は開かない」判定は、安全装置として残す。個別計画書は、Step3 の完了後に作る。
- **Step4: 保存形式の拡張と、Controls での SA 専用の種別の実行**（旧 Step3＋旧 Step5）。`DS4ControlSettings`／`Profile.xml` に、SA 専用の種別・項目（プログラム起動、プロファイル切替、DisconnectBT、バッテリー確認、MultiAction、較正、Run on release、Sync、Keep key state、Hold 秒数）と、SA 側の Extras の保存先を足す。`ControlBindingBuilder` を拡張して、Controls のボタンから、これらを実行する。往復変換と読込のテストを先に作る。**Step3 のあとで、実施するか・後回しにするかを、ユーザーが判断する**（Controls で SA 専用の種別が必要になったとき）。
- **Step5: SA 側のアナログ出力**（旧 Step4b。マウス移動・ホイール・アナログ軸を、固定値の強さで出す）。
- **Step6: SA の文字列の受け渡しを、型のある独立した項目へ**（§6 Q7）、**旧 `BindingWindow`／旧エディタ（Full のモード、Extras のコード）の撤去**、`SpecialActionEditor` の `AssignmentEditor` への改名、モデル図・文書の更新、完了報告書。
- **順序の改訂（2026-10-21、ユーザー決定）: Step4 は W6 で締め、仕様変更と統合を先に行う**。W7（Check Battery）・W8（Multi-action）は統合後へ繰り延べる。新しい順序（Step4d を 2026-10-21 に追加）: Step4（W6 まで）→ **Step4c（仕様変更: 押下判定の閾値、Fresh-press ガード、元の出力の扱い、ログ、プロファイルの固定 ID、Hold の共有、検証の揃え。項目 1・2・4・7 を採用。`Phase10-Step4c-Plan.md`）**→ **Step4d（固定 ID: プロファイルと SA の参照を、名前から ID へ）**→ **統合の Step（旧 Step6 を前倒し: SA の型のある項目、builder の統合、暫定の構造の整理、改名、旧コードの撤去。最初に、Controls にも複数ボタンの組み合わせを許すかを確認）**→ W7・W8 相当 → Step5（SA 側のアナログ出力）。理由: 共通度を高めてから統合すると、以後の種別の追加が 1 か所で済み、仕様変更と構造の統合を同じ Step に混ぜないで済むため。詳細は `Phase10-Step4-Plan.md` §10「Step4 の締め」と `Phase10-Status.md`。
- **最終形の方針（追記。2026-10-21、ユーザーとの議論）: 束縛を作る builder の統合**。現在、`ActionBindingBuilder`（入力＝`SpecialAction`）と `ControlBindingBuilder`（入力＝`DS4ControlSettings`）が、同じ種類の処理（キー、マクロ、パッド、マウスなど）の解釈を、入力の形が違うために重複して持つ。出力は、どちらもエンジンの共通の束縛（`IActionBinding`）で、分ける本質的な理由は、保存形式の違いだけ。最終形は、(1) **共通の割り当て定義**（種別、型のある項目、トリガー、Extras。Step6 の Q7 が土台）、(2) 読み込みの部品を 2 つ（SA の保存形式 → 定義、Controls の保存形式 → 定義）、(3) 束縛を作る builder は **1 つ**。Controls 固有の方針（割り当てのないボタンの素通し、アナログ入力の強さで出すマウス移動）は、別の部品として分ける。これにより、Step4 の「Controls で SA 専用の種別を使う」は、builder への変換の追加が要らず、保存場所だけで済む形に近づく。進め方: Step6 で定義を作り、そのあとに builder を統合する（実行の中心部の変更のため、Phase8 の優先順位・抑制の特性化テストを守りにする）。統合の専用 Step を設けるかは、Step4 の計画時に判断する。

各 Step の前に、個別計画書を作り、承認を得る。実機（画面）の確認項目は、`Phase6-Step11-RealDevice-Verification-Checklist.md` §11 の台帳に追加する。

## 6. 決定事項（2026-10-21、ユーザー承認）

- **Q1＝全種別を両方で選べる**: 「Controls だけ」「SA だけ」の種別は作らない。リストの全項目を、どちらのタブからも選べる。これを実現するため、**`DS4ControlSettings`／`Profile.xml` を拡張**し、SA 専用の種別・項目（プログラム起動、プロファイル切替、DisconnectBT、バッテリー確認、MultiAction、較正、Run on release、Sync、Keep key state、Hold 秒数、解除トリガー）の保存先を Controls にも作る（旧 案 A）。裏で SA を作る案（B）と、無効にする案（C）は採らない。
- **Q2＝すべて出す**: SA 側にも、マウス移動、ホイール、Extras、アナログ軸を出す。
  - **強さの規則（承認）**: SA の組み合わせトリガーは、オン／オフのため、**編集画面で「強さ（0〜100%）」を指定**し、成立中はその固定値で出力する。Controls 側は、従来どおり入力のアナログ値で決まる。
  - 逆に、Controls の単体トリガーに、SA 専用の項目（Hold 秒数、解除トリガーなど）を出す。
- **Q3＝別ダイアログで開く**: `BindingWindow` の視覚的な選択 UI（キーボード、Xbox 画像、マウス）は、共通画面の「キー／ボタンを選ぶ」から開く選択ダイアログとして残す（埋め込まない）。
- **Q4＝統合の前に調べる**: 持ち越し C-5（マウスボタンへの割り当てで Toggle を設定できない制限）を、Phase10 の Step2 の前に調べ、仕様か不具合かを確定する。
  - **調査結果（2026-10-21）**: 原因は編集画面の書き込み漏れ。`OutBinding.WriteBind`（`OutBinding.cs` 398〜455 行付近）が `DS4KeyType.Toggle` を書くのは `OutType.Key` の分岐だけで、`OutType.Button`（マウスボタン `LeftMouse` ほか、パッドのボタンを含む）の分岐は書かない。一方、プロファイルの読み込み・保存（`ProfileButtonControlLoadSection`／`ProfileButtonSaveSection`）と実行側（`ControlBindingBuilder`、`ControlToggleOutputAction`。Phase8-Step7 7.8f-2 でマウス・パッドの Toggle を明示的にサポート）は、Button の Toggle に対応している。したがって、**仕様ではなく編集画面の欠落**（XML を直接編集した Toggle つきの Button は動く）。
  - 注意: Step9 の B-11 の記録には「パッドのボタン（A）では Toggle が設定される」とあるが、コードの読み取りでは、Button の分岐は Toggle を書かない。この食い違いは未解決（A の割り当てが、既定値の `Default` に戻る経路や、既存の設定の再保存で残っていた可能性）。Step2 で、共通画面の Toggle を「Key と Button の両方で書く」ことを、往復変換テストで固定する際に再現して確定する。
  - 対応: 別の是正は行わず、Phase10 の共通画面（Step2〜4）で、Key と Button の両方に Toggle を書く。
- **Q5＝許容する**: 新しいファイルを旧バージョンで読めなくなることを許容する。旧 → 新の読み込みは完全に保つ（読込テストを先に作る）。
- **Q6＝Extras は両方で出す、Default／Unbound は Controls から開いたときだけ出す**（ユーザー決定、2026-10-21）: 統一した選択ダイアログに、Extras パネル（振動・ライトバー・マウス感度）を常に出す。Default／Unbound ボタンは、Controls のタブから開いたときだけ出す（SA から開いたときは隠す）。注意: SA 側で Extras を保存するには、`SpecialAction` に Extras の保存先が要る（Step3 の保存形式の拡張に含める。Q2＝「すべて出す」と整合）。

- **Q7＝SA の文字列の受け渡しを、型のある独立した項目に置き換える（案 A。実装は Step3）**（ユーザー決定、2026-10-21）: 現在、SA の保存・読み込みは、文字列の組み立てで値を運んでいる。
  - Key: キーの値と Scan Code を「65 Scan Code」の 1 つの文字列（`details`）で運び、スペースで分けて読む。Switch Mode と解除トリガーは、別の「`Press
R1`」（改行区切り。`extras`）で運ぶ。
  - Macro: `extras` に「Scan Code/RunOnRelease/Sync/KeepKeyState/Repeat」を `/` 区切りで詰め、`Contains` で調べる。
  - これらを、`SpecialAction`、Actions.xml（`SpecialActionSerializer`、`ActionsDTO`）、`ISpecialActionRepository.SaveAction` の引数、各 ViewModel（`PressKeyViewModel`、`MacroViewModel` ほか）の水準で、**独立した項目**（Scan Code、Switch Mode、解除トリガー、Run on release、Sync、Keep key state、繰り返しなど）に置き換える。
  - Step3 の保存形式の拡張（Controls への SA 専用項目の追加）と、同じ場面で行う。型のある項目にしておけば、Controls と SA で、同じ項目の定義を共有できる。
  - 互換: 古い書式（「65 Scan Code」、`extras` の文字列）の**読み込みは、引き続き受け付ける**。新しい書式で保存したファイルは、古いバージョンでは読めない（Q5 で許容済み）。読込テストを、先に作る。
  - Step1 W4 の応急の是正（`ActionsDTO` が Key の Scan Code を Details に「 Scan Code」と書く）は、Step3 でこの置き換えをするまでの暫定。

### 決定による Step への影響

- Step3（保存形式の拡張）は必須になり、**範囲が広い**（`DS4ControlSettings` に全種別と項目を持たせる）。Step3 の個別計画書で、拡張する欄と XML の書式を決め、承認を得る。
- Step5（`ControlBindingBuilder` の拡張）で、Controls の単体トリガーから全種別を実行する。Step5 の前に、SA 側のアナログ出力（固定値の強さ）を、`ActionBindingBuilder` に追加する Step を足す（Step4b）。
- 抑制の挙動と優先順位は、§7 に明文化した（承認済み）。Step2 の設計で、§7 を満たすことを特性化テストで確認する。

## 7. 優先順位と抑制の仕様（2026-10-21、ユーザー承認。現行の挙動を仕様として明文化）

### 7.1 基本条件（ユーザー指定）

- **条件 1**: Controls と SA で、同じボタンに割り当てがあるときは、**SA が優先**する。
- **条件 2**: SA のトリガーが複数のボタンのとき、**トリガーが成立する手前までに押されたボタンは、条件 1 に従って割り当てられた処理を実行する**。**トリガーが成立したときに押された最後のボタンは、そのボタン単体に（Controls および SA で）割り当てられた処理を実行しない**。

### 7.2 補足の規則（すべて推奨どおり承認）

| # | 場面 | 規則 |
|---|---|---|
| R1 | 成立に時間がかかるトリガー（遅延つき、MultiAction のタップ・長押し、Run on release） | 成立前は、Controls の処理が出る。成立後は、抑制する（現行どおり）。成立前の保留はしない。Run on release は、成立が離した時点のため、押した時点の Controls の処理は済んでいる |
| R2 | ほぼ同時に押したとき | 猶予（遅延）を置かない。別々のフレームで押された場合、先のボタンの Controls は出る。同じフレームで同時に押された場合は「最後」がなく、トリガーの全ボタンが抑制される |
| R3 | 成立前に出た出力の後始末 | 取り消さない。キー、パッドのボタン、Toggle の反転、繰り返しマクロは、そのボタンを離すまで続く。ただし Profile 切替と DisconnectBT は、現行どおり、トリガーのボタンのキーを解放する（`TriggerButtonKeyReleaser`） |
| R4 | SA どうしの重なり（L1+R1 と L1+R1+Cross など） | 現行どおり、成立した SA はすべて実行する。ボタン数が多いものだけを実行する、という規則は設けない |
| R5 | 「割り当てられた処理」に Default（パススルー）を含めるか | 含める。先に押したボタンは素通しで出力され、最後のボタンは抑制される |
| R6 | アナログ入力（L2／R2 の軸、スティックの方向） | 同じ規則。閾値に達する前の Controls の出力は出る。成立後は抑制され、出力は 0 に戻る |
| R7 | 先に押したボタンを離して、再び押したとき | トリガーが崩れ、再押しで SA が再成立する。そのボタンが「最後」として抑制される |
| R8 | プロファイル切替の直後 | 抑制の集合は切替で消える。押しっぱなしのボタンの扱いは、Fresh-press ガードに従う（安全弁は変えない。規約 2.2） |
| R9 | プロファイルで無効な SA、実行に失敗する SA | 無効な SA は束縛がないため、抑制しない。成立した SA は、実行の成否にかかわらず、抑制する |
| R10 | Controls に割り当てた SA 専用の種別（Profile 切替、DisconnectBT など） | Controls の割り当てとして、条件 1・2 の対象。例: Controls の L1 → DisconnectBT と、SA の L1+R1 があるとき、L1+R1 の成立では SA だけが実行される |

### 7.3 検証

- Step2 の特性化テストで、フレーム列（押す・離すの順序とタイミング）を入力し、出力と抑制の集合を固定する。対象: 7.1 の 2 条件、R1〜R10。
- 現行のコードが 7.1 条件 2 の後半（最後のボタンが同じフレームで抑制される）を満たすかは、未確認のため、最初に確認する。満たさない場合は、不具合として扱い、是正の承認を得る。

