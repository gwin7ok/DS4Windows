# Phase10-Step3b 計画書: Touchpad・ジャイロのタブを左ペインへ移し、Axis Config の入口を Controls タブへ追加する

作成日: 2026-10-21  
対象ブランチ: `For-DI-migration-work`  
上位: `Phase10-Assignment-UI-Unification-Plan.md`（§5 Step3b）、`Phase10-Step3-Plan.md`（§3.4 二重に開かない仕組み、§10.4 引き継ぎ）、`Phase10-Status.md`  
状態: **案（Q1〜Q3 は確定。ユーザーの承認後、Wave 0 から着手する）**

---

## 1. 目的と範囲

割り当てエディターを開ける入口を、**すべて、左ペインのタブにあるボタン**にそろえる（ユーザーの方針、2026-10-21）。

- 割り当てエディターは、左ペイン全体に重なって表示され、左ペインのタブを隠す（Step3）。入口のボタンが、すべて左ペインのタブにあれば、編集中は、どの入口のボタンも押せないため、**構造として、二重に開けない**。
- Step3 では、右ペインに残る入口（Tilt・Swipe［Touchpad・ジャイロのタブ］、Axis Config の 4 つ）のため、「編集中は新しく開かない」判定（`leftPaneEditor`）を入れた。本 Step で、右ペインの入口をなくす。判定は、コントローラーの入力（タイマー）経由の安全装置として残す。

範囲内:
1. **Touchpad タブとジャイロタブを、右ペインから左ペイン（`sidebarTabControl`）へ移す**。Output Mode などの割り当て以外の設定も、タブごと移す（ユーザーの確認）。Axis Config、Lightbar、Other は、右ペインに残す。
2. **Axis Config の 4 つの入口**（LS／RS の Outer Bind、L2／R2 の Full Pull）を、左ペインの **Controls タブに、ボタンとして追加する**（ユーザーの指示。一覧への項目追加ではない）。Axis Config からは、ボタンを外す（§3.3）。
3. 移したタブの、データの結び付け（`DataContext`）、リソース、見た目の調整。

範囲外: 割り当てエディターの動作・保存形式の変更（Step4）。Touchpad・ジャイロの設定項目の変更（機能の追加・削除）。ロジックは変えない（レイアウトの変更のみ）。

## 2. 現状の調査（2026-10-21）

`DS4Windows/DS4Forms/ProfileEditor.xaml`、`ProfileEditor.xaml.cs`。

### 2.1 画面の構成
- 左ペイン: `sidebarTabControl`（`Grid.Row=1 / Column=0`）。タブは、Controls［固定幅 470 の `Grid`］、Special Actions、Controller Readings（`contReadingsTab`、`IsEnabled="False"` が既定）。
- 右ペイン: `profileSettingsTabCon`（`Column=2`）。タブは、Axis Config、Lightbar、Touchpad、ジャイロ、Other。`DataContext` は、コードから `profileSettingsVM` を設定（`ProfileEditor.xaml.cs` の 744・805・865・891 行付近）。
- 左の列は、幅が `Auto`（`MinWidth=300`）。中身の幅で決まる。仕切り（`GridSplitter`）は、列 1。
- Controls タブの `Grid` は、2 行（行 0 ＝ `Canvas`［240］、行 1 ＝ `ListBox`［`*`］）。

### 2.2 移すタブの特徴
| タブ | 内容 | 注意点 |
|---|---|---|
| Touchpad（`Header={lex:Loc Touchpad}`、1360 行付近） | Output Mode、出力モードごとの設定、Swipe の割り当てボタン（Controls モード）、Passthru Click、Button Activation Mode | `Height="22" VerticalAlignment="Top"` は、右ペインのタブ見出しの高さをそろえるための調整。`TabItem.Resources` に `spaceMargin` を持つ。`touchContentControl2／4` に、コードが部品（`touchButtonUC`）を差し込む |
| ジャイロ（`Header={lex:Loc Gyro}`、1654 行付近） | Output Mode、Tilt の割り当てボタン（Controls［Accel］モード）、トリガー、感度、較正ほか | `spaceMargin` を、右ペインの `TabControl.Resources` から取る（`StaticResource`）。移すと、見つからなくなる |

どちらも、中身は `ScrollViewer` の中の `StackPanel` で、縦にスクロールする。左ペインの幅（470）に全メニューが収まることは、ユーザーが画面で確認済み（2026-10-21）。

### 2.3 Axis Config の 4 つの入口
| 入口 | 場所（XAML） | 関連 |
|---|---|---|
| LS の Outer Bind | 535 行付近（`Click="StickOuterBindButton_Click"`、`Tag={Binding LSOuterBind, Source={StaticResource controlIndexCheck}}`） | 右の表示ラベル `lsOuterBindLb`（`DataContext` は、コードで `mappingListVM.ControlMap[DS4Controls.LSOuter]`） |
| RS の Outer Bind | 844 行付近 | `rsOuterBindLb` |
| L2 の Full Pull | 1118 行付近（`Click="TriggerFullPullBtn_Click"`、`Tag=L2FullPull`） | `l2FullPullLb` |
| R2 の Full Pull | 1124 行付近 | `r2FullPullLb` |

4 つのボタンは、いずれも、`Tag` のコントロール番号で、`ShowControlAssignmentEditor` を呼ぶ（Step3 W4）。

### 2.4 コードの依存
- 左ペインのタブの番号に依存するコード: `PresetBtn_Click` の `sidebarTabControl.SelectedIndex = 0`（Controls タブ）。新しいタブは、Controls を **0 番のまま**にし、後ろに足す。
- `SidebarTabControl_SelectionChanged` は、Special Actions と Controller Readings の `SelectedItem` で判定する（番号ではない）。タブが増えても動く。
- 右ペインのタブの番号に依存するコードは、見つかっていない（W0 で、ソース走査で確認する）。

## 3. 設計

### 3.1 タブの移動（Touchpad・ジャイロ）
- 2 つの `TabItem` を、`profileSettingsTabCon` から `sidebarTabControl` へ移す。位置は、**Special Actions の次**（Controls、Special Actions、Touchpad、ジャイロ、Controller Readings の順。§8 Q1）。
- 右ペインのタブ見出しの高さ調整（Touchpad の `Height="22" VerticalAlignment="Top"`）は、移したタブから外す（左ペインの既定の見出しに合わせる）。右ペインに残るタブ（Axis Config の `Height="24" Margin="-2,0,-2,0"`）の調整は、見た目を実機で確認して、必要なら外す（§8 Q3）。
- **`DataContext`**: 移した 2 つのタブにも、`profileSettingsVM` を設定する。`profileSettingsTabCon.DataContext = …`（4 か所）の隣に、移したタブ（または、タブ内の最上位の要素）への設定を足す。解除（`= null`）も、同じ箇所で行う（リークと古い参照を避ける）。
- **リソース**: ジャイロの `spaceMargin` は、ジャイロの `TabItem.Resources` に定義する（Touchpad は、すでに持つ）。ほかに、右ペインの `TabControl.Resources` や、`UserControl.Resources` から取っているものがないか、W0 で走査して確認する。

### 3.2 左ペインの幅
- 左の列の幅は `Auto`。移した 2 タブの内容が、470 を超えると、列が広がり、画面が動く。ユーザーは、全メニューが収まることを確認済み。W1 で、実機（Output Mode の各値）で、幅が変わらないことを確認する。
- 割り当てエディターのホスト（`controlEditorHost`）は、開いた時点の `sidebarTabControl` の幅に固定している（Step3）。タブが増えても、そのまま動く。

### 3.3 Axis Config の 4 つの入口を、Controls タブへ
- **設計の変更（ユーザーの指示、2026-10-21。W2 の実機確認で）**: 当初は、Controls タブの絵と一覧の間に、専用のボタン 4 つ（2 行 × 2 列、横に割り当て名）を足した。ユーザーから、「絵の下の文字の一覧に、ほかのボタンと同じ形式で、4 つを表示してほしい」との指示があり、**専用のボタンは作らず、一覧の末尾に、4 つの項目を加える形**に改めた。
- `MappingListViewModel` が、`LS Outer`、`RS Outer`、`L2 Full Pull`、`R2 Full Pull` の `MappedControl` を、一覧（`mappings`）の末尾（添字 34〜37）に加える（`ControlMap`・`ControlIndexMap` も、同じループで作る。以前の別扱い `extraControls` は、なくなった）。コントローラーの入力による選択の添字（0〜33）は、変わらない。
- 入口は、ほかの項目と同じ（一覧のダブルクリック → `ShowControlBindingWindow` → `ShowControlAssignmentEditor`）。専用のボタンのクリックのハンドラー（`StickOuterBindButton_Click`、`TriggerFullPullBtn_Click`）は、不要になったため、なくした。Controls タブの行の構成（Canvas と一覧の 2 行）は、変えていない。
- Axis Config からは、4 つのボタンを外す。割り当て名のラベル（`lsOuterBindLb` ほか）は、**読み取り専用の表示として残す**（`DataContext` の設定も、そのまま）。「Outer Btn Dead」（デッドゾーン）、「Invert」など、割り当て以外の設定は、Axis Config に残す。
- 表示（Q2。専用のボタンでの「割り当て名の常時表示」）は、一覧の「LS Outer: 未割り当て」の形式の表示で満たされる。

### 3.4 二重に開かない仕組み
- 入口がすべて左ペインのタブのボタンになると、編集中は左ペイン全体が覆われるため、ボタンを押せない（構造による防止）。
- `leftPaneEditor` の判定は、コントローラーの入力（タイマー）経由の安全装置として、**残す**（Step3 W4 のまま）。

## 4. 変えないもの
- 割り当てエディター、出力ピッカーの動作、保存形式（`Profile.xml`）、実行（エンジン）、ログ。
- Touchpad・ジャイロ・Axis Config の、設定項目の値・並び・動作（置き場所だけが変わる）。
- 既存の `Click` ハンドラー（`StickOuterBindButton_Click` ほか）、`ShowControlAssignmentEditor`。

## 5. 作業の順序（Wave）
各 Wave の後に、ユーザーがビルドと `dotnet test` を実行する。画面を変える Wave には、実機（画面）の確認を付ける。

1. **W0: 特性化テスト（ソース走査）**（製品コードは変えない）: 右ペインのタブの番号に依存するコードがないこと、`spaceMargin` など、移すタブが右ペインのリソースを参照している箇所、`profileSettingsTabCon.DataContext` の設定・解除の箇所、4 つの入口のボタンの `Click`・`Tag`。
2. **W1: Touchpad・ジャイロのタブの移動**: 2 つの `TabItem` を `sidebarTabControl` へ移す。`DataContext`・リソースの調整、見出しの高さ調整の整理。**実機確認**: 両タブの全 Output Mode の表示と動作、Swipe・Tilt の入口（割り当てエディターが開く）、左ペインの幅が変わらない、Controller Readings の有効化の動作、プロファイルの保存・再読み込み。
3. **W2: Axis Config の 4 つの入口を Controls タブへ**: ボタンの追加、Axis Config のボタンの撤去（ラベルは、表示として残す）。**実機確認**: 4 つのボタンで割り当てエディターが開く。割り当て名の表示（Axis Config のラベル）が更新される。
4. **W3: 二重に開かない仕組みの確認**: 右ペインに、入口がないこと（ソース走査）。コントローラーの入力（タイマー）経由の判定が、残っていること。**実機確認**: 編集中に、左ペインのタブを切り替えられない（Step3 のまま）。
5. **W4: 文書**: `Phase10-Status.md`、本書の完了の記録、実機確認の台帳、モデル図（`02-Layer-Architecture-Diagram.md` ほか、画面構成が出ていれば）。

## 6. テスト
- ソース走査（ガード）: `sidebarTabControl` のタブの構成と順序、`profileSettingsTabCon` のタブが Axis Config・Lightbar・Other だけであること、移したタブの `DataContext` の設定・解除、Controls タブの追加の行と 4 つのボタン（`Click`・`Tag`）、Axis Config に入口のボタンが残っていないこと。
- 既存のガード（`ProfileEditor` のソースを走査するもの）が、構成の変更で壊れていないこと（実行して確認）。

## 7. 実機（画面）の確認項目
1. Touchpad タブとジャイロタブが、左ペインに表示される（順序、見出し）。右ペインは、Axis Config・Lightbar・Other だけ。
2. 両タブで、Output Mode の全選択肢を切り替えて、表示と動作が従来どおり。左ペインの幅が変わらない。縦にスクロールできる。
3. Swipe（Touchpad）、Tilt（ジャイロ）のボタンで、割り当てエディターが、左ペイン全体に開く。保存・キャンセルで、元のタブ（Touchpad／ジャイロ）に戻る。
4. Controls タブの 4 つのボタン（LS／RS Outer、L2／R2 Full Pull）で、割り当てエディターが開く。保存すると、Axis Config の割り当て名のラベルが更新される。
5. 編集中に、左ペインのタブを切り替えられない。右ペインに入口がなく、コントローラーのボタンを押しても、二重に開かない。
6. プロファイルの保存・再読み込み・Apply で、Touchpad・ジャイロ・Axis Config の設定値が変わらない。
7. Controller Readings タブの有効化・動作（コントローラーの入力表示）が従来どおり。
8. 既存のプロファイル（Touchpad・ジャイロ・Axis Config の設定つき）を開いて、動作が変わらない。

## 8. 確認事項（ユーザーの判断が要る）
- **Q1＝Controls、Special Actions、Touchpad、ジャイロ、Controller Readings（確定、2026-10-21）**。Controller Readings（診断用）を最後に置く。
- **Q2＝割り当て名も常時表示（確定、2026-10-21）**: Controls タブの 4 つのボタンの右に、いまの割り当て名を常に出す（§3.3）。Axis Config の割り当て名のラベルは、読み取り専用の表示として残す。
- **Q3＝実機で見て判断（確定、2026-10-21）**: Axis Config の見出しの高さ調整（`Height="24" Margin="-2,0,-2,0"`）は、Touchpad・ジャイロが抜けたあとの見た目を確認して、必要なら外す。

## 9. 完了の記録（2026-10-21）

### 9.1 達成したこと

| Wave | 内容 | 結果 |
|---|---|---|
| W0 | `ProfileEditor.xaml` の画面構成の特性化テスト（ソース走査）: 左右のタブの構成、移すタブのリソース依存、右ペインのタブの番号・選択に依存するコードがないこと、4 つの入口の構成 | 調査で、ジャイロのタブだけが `spaceMargin` を右ペインのリソースに依存していたことが判明 |
| W1 | Touchpad・ジャイロのタブを、左ペイン（Special Actions の次）へ移した。`touchpadTab`／`gyroTab` に名前を付け、`DataContext` の設定・解除を足し、ジャイロに `spaceMargin` を定義、Touchpad の見出しの高さ調整を外した。`SidebarTabControl_SelectionChanged` に、タブの中身から届く選択変更を無視する判定（`e.OriginalSource != sidebarTabControl`）を足した | 実機確認済み（全 Output Mode の表示・動作、左ペインの幅が変わらない、Swipe・Tilt の入口、保存・再読み込み、Controller Readings） |
| W2 | Axis Config の 4 つの入口（LS／RS Outer Bind、L2／R2 Full Pull）を、Controls タブの一覧の末尾（添字 34〜37）へ、ほかの項目と同じ形式で加えた。Axis Config のボタンと、専用のクリックのハンドラーは、なくした（割り当て名のラベルは、読み取り専用の表示として残す） | 実機確認済み |
| W3 | 二重に開かない仕組みの確認: 割り当てエディターの入口（Controls の絵のボタン・一覧、Tilt、Swipe、Full Pull・Outer Bind の一覧の項目）が、すべて左ペインのタブにあること（右ペインに入口がない）を、ソース走査で固定した（W2 のテストに含む）。コントローラーの入力（タイマー）経由の判定は、安全装置として残した | 実機確認は、§9.3 のとおり未確認の項目あり |

### 9.2 設計の変更と決定
- **Q3（Axis Config の見出しの高さ調整）**: Touchpad・ジャイロが抜けたあとの右ペインの見た目を、実機で確認し、問題がなかったため、**外さず、そのまま残した**。
- **W2 の設計の変更（ユーザーの指示）**: 当初の「Controls タブの絵と一覧の間に、専用のボタン 4 つを足す」を取りやめ、「一覧の末尾に、4 つの項目を加える」へ改めた（§3.3）。専用のボタンのハンドラー（`StickOuterBindButton_Click`、`TriggerFullPullBtn_Click`）は、不要になったため、なくした。

### 9.3 未確認の項目
- 二重に開かない仕組みの、Step3b 後の実機での再確認（台帳 P10-26）は、**確認済み（○）**。
- Step3 の台帳の P10-21（Extras、既存プロファイルの回帰）も、未確認のまま。

### 9.4 引き継ぎ
- 左ペインに、割り当てエディターの入口がすべてそろった。これで、割り当て操作が、Controls、Special Actions、Touchpad、ジャイロの 4 つのタブで、同じ編集画面になった。
- **Step4**: 保存形式の拡張（Controls で SA 専用の種別を保存・実行できるようにする）。Controls のモードで隠している Macro タブの 3 項目と、グレーアウトの種別を、順に出す。最終形（共通の割り当て定義と、束縛を作る builder の統合）は、マスター計画書 §5 を参照。
- 使われなくなったもの（撤去・整理の候補）: `ControlIndexCheck` の `LSOuterBind`／`RSOuterBind`／`L2FullPull`／`R2FullPull`（Axis Config のボタンの `Tag` 用だった。使い道がなくなった）。
