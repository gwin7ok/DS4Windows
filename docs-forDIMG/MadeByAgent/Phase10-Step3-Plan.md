# Phase10-Step3 計画書: Controls タブを割り当てエディターへ接続する

作成日: 2026-10-21  
対象ブランチ: `For-DI-migration-work`  
上位: `Phase10-Assignment-UI-Unification-Plan.md`（§5 Step3［2026-10-21 の順序の改訂で、旧 Step4 を繰り上げた］、§6 決定事項）、`Phase10-Step2-Plan.md`（§9.3 未接続のコード）、`Phase10-Status.md`  
状態: **案（Q1〜Q4 は確定。ユーザーの承認後、Wave 0 から着手する）**

---

## 1. 目的と範囲

**2 つのタブ（Controls と Special Actions）から、共通の割り当て設定画面（割り当てエディター）を開く**ようにする（ユーザーの目標。UI の統一）。Step2 で作った割り当てエディターの Controls のモードを、`ProfileEditor` の Controls の割り当ての編集へ接続し、従来の選択ダイアログ `BindingWindow`（Full のモード）を、その役目から外す（`BindingWindow` は、出力ピッカーとして、エディターから開く）。

- **保存形式は変えない**。Controls で今保存できる種別（Key・Button・Macro・Extras・Default・Unbound）だけを、割り当てエディターで編集する。ほかの種別（Launch Program など）は、一覧に出るが、グレーアウトのまま（選べない）。Controls の機能は、減らない。
- 範囲内:
  1. **選択ダイアログ `BindingWindow` の統一**（名前: **出力ピッカー**）: Controls 用の Full のモードの役目をなくす。**「Record A Macro」「Toggle（Mode）」「Scan Code」だけ**をダイアログから外す。**Extras のパネルは、出力ピッカーに残す**（ユーザーの確認、2026-10-21）。Default／Unbound ボタンは、Controls のとき（`Control` のモード）だけ出す。
  2. **Press Key の入力**が、Default／Unbound の選択結果を扱えるようにする。
  3. **`ProfileEditor` の呼び出し 6 か所**（＋コントローラーの入力の経路）を、すべて割り当てエディターへ切り替える（左ペイン全体に重ねて表示）。
  4. Controls のモードで、SA 専用の項目（Macro タブの Run on release／Sync／Keep key state）を隠す（保存する場所がないため）。
- **範囲外**: 保存形式の拡張と、SA 専用の種別を Controls で実行すること（Step4）。SA 側の Extras・アナログ出力（Step4・5）。`BindingWindow` の Full のモードのコードの撤去（Step6。この Step では、使わなくするだけ）。実行（エンジン）とログは、変えない。

## 2. 現状の調査（2026-10-21）

### 2.1 Controls の割り当ての編集の呼び出し

`ProfileEditor` が、`CreateBindingWindow`（Full）を呼ぶ箇所は **6 か所**。どれも、同じ流れ（ダイアログを開く → 閉じたあと `UpdateMappingName`、`UpdateHighlightLabel`、`RefreshCustomsFlags`）。

| 入口 | メソッド | 画面上の場所 |
|---|---|---|
| Controls タブの絵のボタン | `HoverConBtn_Click` | Controls タブ |
| Controls タブの一覧のダブルクリック／コントローラーの入力 | `ShowControlBindingWindow` | Controls タブ。**コントローラーのボタンを押して開く経路**（`UseControllerReadout` が有効なとき、押したボタンの項目を選んで開く）もある |
| 傾き（Tilt）の割り当てボタン | `TiltControlsButton_Click` | **Gyro タブ** |
| スワイプの割り当てボタン | `SwipeControlsButton_Click` | **Touchpad／Gyro 系のタブ** |
| トリガーの Full Pull | `TriggerFullPullBtn_Click` | **Axis Config タブ** |
| スティックの Outer Bind | `StickOuterBindButton_Click` | **Axis Config タブ** |

画面の名前（ユーザーの確認、2026-10-21）:
- **割り当てエディター**（Assignment Editor）: 1枚目の共通画面。左ペイン全体に重ねて表示する。割り当て全部（種別、Toggle、マクロの詳細など）を編集する。
- **出力ピッカー**（Output Picker）: 2枚目の選択ダイアログ `BindingWindow`（別ウィンドウ）。出力側（キーボード、マウス、パッドのボタン）を選ぶ。**Extras、Default／Unbound を持つ**。Record A Macro、Toggle、Scan Code は持たない。

入口は、**1 種類**にそろえる（ユーザーの確認、2026-10-21。経過: 初版は「6 つとも割り当てエディター」、次に「右ペインの入口は出力ピッカー」としたが、最終的に、すべて割り当てエディターを開く形に決定）。

| 入口 | 開く画面 |
|---|---|
| Controls タブの絵のボタン（`HoverConBtn_Click`）、一覧のダブルクリック | **割り当てエディター** |
| Tilt（ジャイロ）、Swipe（Touchpad）、Full Pull・Outer Bind（Axis Config）、コントローラーのボタンを押して開く経路 | **割り当てエディター**（同じ） |

- **出力ピッカーを開くのは、割り当てエディターの「Select a Key」だけ**。入口から直接は開かない。
- Tilt／Swipe などで、Record A Macro・Toggle・Scan Code が設定できなくなる、という機能の減少は、**なくなる**（割り当てエディターで全部設定できる。旧 Q4 は不要になった）。Extras は、出力ピッカーで設定する（エディター自身には Extras の欄を作らない）。
- 閉じたあとの更新は、従来と同じ（`UpdateMappingName`、`UpdateHighlightLabel`、`RefreshCustomsFlags`）。

**タブの配置（Step3b で変更予定）**: ユーザーの方針により、Touchpad タブとジャイロタブを、右ペインから左ペインへ移す（Axis Config、Lightbar、Other は右ペインに残す。左ペインの幅［470］に収まることは、ユーザーが画面で確認済み）。Step3 は、**タブが今の位置（右ペイン）にあっても、移したあとでも**同じ動きになるよう、表示場所を 1 か所にまとめる（§3.4）。

### 2.2 `BindingWindow`

- Controls 用（Full）: 絵（Xbox のボタン、キーボード、マウス）の選択、**Default／Unbound ボタン**（`modePanel`）、**Extras のパネル**（右側。振動のテスト、重い／軽い、ライトバーの色・点滅、マウス感度）、「Record A Macro」、Toggle・Scan Code。**閉じると、設定を直接書き込む**（キャンセルがない）。
- SA の Press Key 用（`ForPressToggle`）: Step1 で、Toggle・Scan Code を隠した。Default／Unbound・Extras・Record も隠れている。
- Extras の読み書きは、`OutBinding`（`ParseExtras`／`CompileExtras`／`IsUsingExtras`）と、`BindingWindowViewModel`（振動のテスト、色のプレビュー）にある。

### 2.3 Step2 で、すでにできていること

割り当てエディターの Controls のモード（`SpecialActionEditor` の Controls のコンストラクター、`ControlActionEditorViewModel`、`ControlSettingsEditTarget`）: 名前＝ボタン名（固定）、トリガー 1 つ（固定）、種別の一覧（Macro と Output Key/Mouse/Button だけ有効）、`DS4ControlSettings` との変換（Key・Button［Toggle を含む］・Macro・Default）、編集用のコピー（Save で反映、Cancel で捨てる）。**呼び出し元がない**（本 Step で接続）。`extras` は、編集せず、そのまま保つ。

## 3. 設計

### 3.1 Extras

- **出力ピッカーに残す**（ユーザーの確認）。Extras のコードは、この Step では動かさない（移設も、新しい部品も作らない）。
- 割り当てエディターから出力ピッカーを開いたときも、Extras は出力ピッカーで編集する。エディターが持つ編集用のコピー（`ControlSettingsEditTarget`）に、出力ピッカーの結果［Key・Button・Default・Unbound と Extras］が反映される経路を、W2 で作る。エディター自身の Extras のパネルは、作らない（Step4 以降で、必要なら判断）。
- SA のモードでは、Extras は、**今は**出さない（Q2。SA に保存先がないため）。SA に Extras の保存先を作る Step（Step4・5）で、出す。**最終的に、出力ピッカーの 2 つのモードの違いは、Default／Unbound の表示だけになる**（ユーザーの確認、2026-10-21）。

### 3.2 出力ピッカー（`BindingWindow`）の統一

- モードを、`SpecialAction`（SA の Press Key 用。今の `ForPressToggle`）と `Control`（Controls 用）の 2 つにする。ほかに、旧来の `Keyboard`（現状どおり）。Full は、使わなくする（コードの撤去は Step6）。
- どのモードも、「Record A Macro」「Toggle」「Scan Code」は出さない（割り当てエディターで設定する）。`Control` のモードだけ、Default／Unbound ボタンを出す。Extras のパネルは、`Control` のモードで出し、`SpecialAction` のモードでは、SA の保存先ができるまで出さない（§3.1）。
- **割り当てエディターのモード**（ユーザーの確認、2026-10-21）: 特別なアクション（SA）タブは、トリガーを選ぶ部分が要る。Controls、Touchpad、ジャイロの 3 つのタブ（と Axis Config の Full Pull・Outer Bind）は、同じ Controls のモードで開き、トリガーの場所に、割り当てを行うボタン名を表示する（Step2 で実装済み）。出力ピッカーのモードは、Default／Unbound の表示だけを制御する。
- ダイアログは、選択結果（キー・ボタン・Default・Unbound と Extras）を、設定の箱（`DS4ControlSettings`）で返す（SA と同じ流れ）。割り当てエディターは、その結果を編集用のコピーへ取り込み、Save で反映する。

### 3.3 Press Key の入力が、Default／Unbound を扱う

- `PressKeyViewModel` に、「Default が選ばれた」状態（`SelectedActionType` が `Default`）を足す。表示は「Default」。保存（`ControlSettingsEditTarget.WriteFrom`）は、Default の割り当てを書く。Unbound は、従来どおり、Button の Unbound。
- SA のモードでは、Default は出ない（ダイアログに、ボタンがない）。

### 3.4 `ProfileEditor` の接続

- 共通の入口 `ShowControlAssignmentEditor(MappedControl)` を作り、6 か所とコントローラーの入力の経路が、すべて、これを呼ぶ。Save のあとに、従来と同じ更新（`UpdateMappingName`、`UpdateHighlightLabel`、`RefreshCustomsFlags`）を行う。Cancel は、何もしない。
- **ホスト（表示場所）**（Q1＝左ペイン）: `ProfileEditor` の**左ペイン全体**（タブ領域）の上に、割り当てエディターを重ねて表示する（1 か所だけ）。表示中は、左ペインのタブ領域を隠し、Save／Cancel で元に戻す。どのタブ（Controls、Touchpad、ジャイロ［Step3b のあと］）、どの入口（右ペインの Axis Config を含む）から呼んでも、同じ場所に出るため、タブの配置に依存しない。
- 割り当てエディターが開いているあいだは、コントローラーの入力による「選択して開く」経路は、新しく開かない（二重に開かないため）。

### 3.5 Controls のモードの調整

- Macro タブの「Run on trigger release」「Synchronized Run」「Keep key state」を隠す（Controls に保存する場所がない。Step4 で、保存できるようにしたら、出す）。
- Press Key タブの、SA 専用の項目は、特になし。

### 3.6 動作の変更（ユーザーに見える違い）

- **保存の方式**: 従来は、ダイアログを閉じると、設定が直接書き込まれた（キャンセルがない）。割り当てエディターは、**Save で反映、Cancel で破棄**。Controls の割り当ての操作が、SA の編集と同じになる（§8 Q3）。

## 4. 変えないもの

- 保存形式（`Profile.xml`）、実行（`TriggerDispatchEngine`、`ControlBindingBuilder`）、ログ。
- SA のモードの動作（SA の編集の見た目・保存）。
- Controls の割り当てで、設定できる内容（Key・Button・Macro・Extras・Default・Unbound）。

## 5. 作業の順序（Wave）

各 Wave の後に、ユーザーがビルドと `dotnet test` を実行する。画面を変える Wave には、実機（画面）の確認を付ける。**割り当てエディターの Controls のモードは、W4 まで、ユーザーの操作から開けない**ため、実機確認の中心は W4。

1. **W0: 特性化テスト**（製品コードは変えない）: `OutBinding` の Extras の読み書き（`ParseExtras`／`CompileExtras`／`IsUsingExtras`）と、Key／Button 選択時に Extras が保たれること。`BindingWindowViewModel` の Default／Unbound の書き込み。`ProfileEditor` の呼び出し 6 か所の構成（ソース走査）。
2. **W1: 出力ピッカーのモード整理**: `BindingWindow` の 2 モード（`SpecialAction`／`Control`）。`Control` のモードで、Record A Macro・Toggle・Scan Code を隠し、Default／Unbound と Extras を出す。SA の Press Key の出力ピッカーは、従来どおり（実機確認: SA の Press Key の回帰）。
3. **W2: Press Key の Default と、エディターとの受け渡し**: `PressKeyViewModel` の Default の状態、`ControlSettingsEditTarget` の Default の書き出し、出力ピッカーの結果（Extras を含む）を編集用のコピーへ反映。
4. **W3: 割り当てエディター（Controls のモード）の調整**: Macro タブの SA 専用の項目を隠す。まだ開けない。
5. **W4: `ProfileEditor` の接続**: 割り当てエディターの入口 `ShowControlAssignmentEditor` と、左ペイン全体へ重ねる表示、6 か所とコントローラーの入力の経路の切り替え。**実機（画面）での確認が中心**（§7 の確認項目）。
6. **W5: 文書**: `Phase10-Status.md`、本書の完了の記録、実機確認の台帳、モデル図。

## 6. テスト

- 単体テスト: Default／Unbound の往復、`ControlSettingsEditTarget` が出力ピッカーの結果と Extras を保つこと、`BindingWindow` のモード、`ProfileEditor` のソース走査（`CreateBindingWindow(` の呼び出しが、Controls の割り当てから、なくなること）。
- 往復: Controls の既存の割り当て（Extras つきを含む）を、割り当てエディターで開いて、保存して、再度開いても、値が変わらないこと（`extras` を含む）。

## 7. 実機（画面）の確認項目（W4 のあと）

1. Controls タブの各ボタン（絵のボタン、一覧のダブルクリック）で、割り当てエディターが開き、名前がボタン名、トリガーが 1 つ（変更不可）であること。
2. 割り当て: キー、パッドのボタン、マウスのボタン、マクロ（Execute Mode、Scan Code）、Toggle、Default、Unbound の設定・保存・再読み込み。**Button の Toggle が保存される**（C-5）。
3. Extras（出力ピッカーで設定）: 振動のテスト、重い／軽い、ライトバーの色・点滅、マウス感度の設定・保存・再読み込み。ライトバーのプレビューが、閉じる・Cancel で戻ること。割り当てエディターを Save／Cancel したとき、Extras が失われない・勝手に変わらないこと。
4. 既存のプロファイル（Extras つき、Macro、Toggle つきを含む）を開いて、保存して、動作が変わらないこと。
5. 6 か所すべて（Controls の絵のボタン・一覧、Tilt、Swipe、Full Pull、Outer Bind、コントローラーのボタンを押して開く経路）で、割り当てエディターが左ペインに開き、開閉・保存・Cancel ができること。Axis Config（右ペイン）から開いたときも、左ペインに出ること。
6. Cancel で、設定が変わらないこと（従来との違い）。
7. SA の Press Key の選択ダイアログが従来どおり（回帰）。

## 8. 確認事項（ユーザーの判断が要る）

- **Q1＝左ペインに表示する（確定）**: 割り当てエディターは、左ペイン全体に重ねて表示する。すべての入口が使う。出力ピッカー（`BindingWindow`）は、エディターの「Select a Key」から開く（§2.1）。
- **Q2＝SA のモードでは Extras を出さない（確定、2026-10-21）**。
- **Q3＝Save／Cancel（確定、2026-10-21）**: Controls の割り当ては、閉じると保存（従来）から、Save／Cancel（SA と同じ）へ変わる。
- **Q4＝不要になった（2026-10-21）**: すべての入口が割り当てエディターを開くため、Record A Macro・Toggle・Scan Code の設定場所の問題は、なくなった。Extras は出力ピッカーで設定する。

## 9. 進捗の記録

- **W0（完了 2026-10-21。ビルド・テスト成功）**: 特性化テスト 3 ファイルを追加した（製品コードは変更なし）。`Phase10Step3W0OutBindingExtrasTests`（Extras の書式・境界・不正値）、`Phase10Step3W0BindingWindowWriteTests`（Default／Unbound／Key／Button の書き込み、Extras の保持、C-5）、`Phase10Step3W0ProfileEditorCallSitesTests`（`CreateBindingWindow` の呼び出しが 6 か所。W4 で 0 か所へ更新する）。
  - **判明した既存の癖**（変更しない）: (1) 色の旗を下ろしても、点滅の間隔（`FlashRate`）は 0 に戻らず、Extras が「使用中」のまま書き出される。(2) `IsUsingExtras` の色の判定は「3 チャンネルすべてが 255 以外」のときだけ真。(3) Button の書き出しは、Toggle を書かない（C-5。割り当てエディターは書く）。
- **W1（完了 2026-10-21。ビルド・テスト成功、実機確認済み）**: 出力ピッカー（`BindingWindow`）の表示モードを整理した。`ForPressToggle` を `SpecialAction` に改名し、`Control` を追加した（`Full`、`Keyboard` は残す。`Full` の撤去は Step6）。`Control` は、Default／Unbound と Extras を出し、Record A Macro・Toggle・Scan Code・マクロ表示を隠す。`SpecialAction` は従来どおり（Extras は、SA の保存先ができる Step4・5 まで出さない。TODO コメントあり）。`ProfileEditor` の呼び出しは、W4 まで変えない。ガードテスト: `Phase10Step3W1BindingWindowModeGuardTests`。実機確認: SA の Press Key の回帰なし。
