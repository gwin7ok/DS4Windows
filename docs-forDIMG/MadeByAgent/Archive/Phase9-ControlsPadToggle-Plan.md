# Phase9 Controls のパッドボタンの Toggle（SA の仕様の調査と、流用の分析）

作成日: 2026-10-22
状態: 調査・分析のみ（実装はユーザーの承認待ち）

## 1. 症状
Controls タブで、割り当てが Button（仮想パッドのボタン）で Switch Mode が Toggle のとき、Toggle にならず Press の動きになる（キーボードのキーの Toggle は動く）。Default のボタンを Toggle にしたとき（`ControlSettingsEditTarget` が既定の出力への Button ＋ Toggle として保存する）も同じ。

## 2. 現状（Controls 側）
- `ControlBindingBuilder.Add` の Button の枝で、Toggle つきは `AddPad` に、`ControlPadOutputAction`（パッドへの出力）に `ControlToggleOutputAction` を足し、入力の区分を `ButtonMouse` にする。
- しかし `ControlToggleOutputAction` が呼ぶのは `FrameOutput.FlipToggle`／`CountToggle` で、書くのは合成状態（`currentClicks.toggle`／`toggleCount`）。これを使うのは `SyntheticInputCommitter.Commit`（旧 `Mapping.Commit`）で、**マウスのボタンの押下・解除**を決めるためだけ。仮想パッドのボタンには、トグルを適用する処理がない。
- そのため、パッドのボタンは `ControlPadOutputAction` が「押されている間だけ」出し続け、Toggle は何も起こさない（Press の動き）。旧経路でも同じ（旧 Controls の Toggle は、マウスとキーだけが対象だった）。

## 3. SA 側の仕様（動作する）
- `ButtonOutputAction`（Button 種別の出力。`ActionBindingBuilder.InterpretButton` が `ButtonBindingValues(hasControl, control, toggle, strength)` を作る）。
- Toggle の規則（`ResolveOutputActive`）: 立ち上がり（`risingEdge`）で、`ActionInstanceState.IsToggledOn` を反転する（Fresh-press ガード中は反転しない）。ON の間は、毎フレーム出力する。状態は `IManagedActionManager`（鍵は SA の名前とデバイス）が持つ。
- 出力: デジタルのボタンは `ctx.Frame.PressX360(control)`。軸・トリガーは、強さの固定値（既定 100%＝端まで）。マウス移動は固定量。
- 状態のクリア: プロファイルの適用、接続・切断で、`ActionManager` が状態を作り直す（ログ `ActionEntry created ... IsToggledOn cleared`）。Key の Toggle も同じ入れ物。

## 4. 流用の分析
結論: **`ButtonOutputAction` は、そのまま Controls の Toggle にも使える。**
- 理由: 入力（立ち上がり・成立中）は、Controls の `ControlButtonInputAction` が `ApplyFrame(ctx, active, risingEdge)` に渡す。`ButtonOutputAction` は `IFrameOutputAction` で、この形を前提にしている。状態の鍵も、Controls の Key が使う合成の名前（`Synthetic_..._{device}_{control}`）と同じ方式で作れる。
- 変更の形（案）: `ControlBindingBuilder.AddPad` で、**Toggle つき、かつ出力先がパッド（ボタン・軸・トリガー）**のとき、`ControlPadOutputAction`＋`ControlToggleOutputAction` の代わりに、SA の出力ファクトリー（`ISpecialActionOutputFactory`）で作る `ButtonOutputAction` を出力として使う。入力の区分は `ButtonPad`（元のボタンの通常の出力を毎フレーム消す方針［`ErasesOutputEveryFrame`］も、そのまま）。合成の名前は `Synthetic_PadToggle_{device}_{control}`、`details` は出力先の X360 の番号、`KeyButtonSwitchMode` は Toggle。
- 範囲: デジタルのボタンは、ON の間、押され続ける。軸・トリガーも、SA と同じく、ON の間は強さ 100%（端まで）で出る（SA の仕様と同じ。Controls の通常の割り当てのような、元のアナログ値への比例ではない）。マウスのボタン・ホイール・タッチパッドクリックの Toggle は、これまでの仕組み（`toggleCount`）のまま。
- 状態のクリア: SA・Key と同じ（プロファイル適用、接続・切断で OFF）。
- 検証: エンジンの束縛のテスト（`Phase8Step7...`／`Phase10...` の系列に、Controls の Button ＋ Toggle のテストを足す）。実機は、パッドのボタン（A など）を Toggle にして、押すたびにオン・オフすること。テストアプリ（Windows のゲームコントローラー）で確認。

## 5. 残る確認
- アナログの出力先（軸・トリガー）に Toggle を付けたときの挙動は、SA と同じ（ON の間は端まで出す）でよいか。
- 実装前に、`FreshPressGuardTable`、`ControlOutputNatureTable` で、Controls のパッド Toggle の区分（`ButtonPad`）に影響がないかを確認する。

## 6. 追加調査: Controls 側で、SA 側の部品（`ButtonOutputAction`）を使える箇所（2026-10-22）

### 6.1 すでに SA と共有している部品
Key（`KeyOutputAction`）、Macro（`MacroAction`）、SA 専用の種別（Special）、MultiAction の入力、Extras の出力（`ControlExtrasOutputAction`）、そして今回のパッドの Toggle（`ButtonOutputAction`）。

### 6.2 SA の `ButtonOutputAction` で扱えるのに、Controls が別の部品を使っている箇所
`ButtonOutputAction`（`FrameOutput.PressX360` と `ApplyAnalog`）は、仮想パッド（ボタン・軸・トリガー）、マウスのボタン、タッチパッドクリック、ホイール（デジタル）、マウス移動を扱える。Toggle の状態は SA の名前ごとに持つ（`ActionInstanceState`）。

| 箇所（Controls） | 現状 | 問題・可否 |
|---|---|---|
| マウスのボタン／タッチパッドクリック／デジタルのホイール ＋ **Toggle**（`AddMouse`） | `ControlMouseOutputAction` ＋ `ControlToggleOutputAction` | `ControlToggleOutputAction` は、**デバイスに 1 つだけの `currentClicks.toggle` フラグ**を反転する。Toggle を付けたマウスのボタンが 2 つあると、2 つの ON／OFF が連動して干渉する。`ButtonOutputAction` なら、ボタンごとに独立。**使える（効果あり）** |
| マウス移動（MouseUp／Down／Left／Right）、アナログのホイール ＋ **Toggle**（`AddAnalogMouse`） | 移動の出力 ＋ `ControlToggleOutputAction` | Toggle のフラグはマウスのボタンの押下だけに使われるため、**移動には Toggle が効かない**（パッドと同じ、未実装の状態）。`ButtonOutputAction` は、ON の間、固定量（強さ 100%）で動かせる。**使える（効果あり）** |
| 上記の **Toggle なし**（Press） | `ControlMouseOutputAction` など | `PressX360` と同じ動き。統一しても利用者の見た目は変わらない。**使えるが、効果がない（リスクのみ）ため、見送りを推奨** |
| パッドの軸・トリガー（Toggle なし）、マウス移動・アナログのホイール（Toggle なし） | 元の入力のアナログ値に比例して出す（`ControlPadOutputAction` ほか） | SA は固定の強さで出す仕様。Controls の通常の割り当ては、比例が仕様。**統一できない（Controls 固有）** |
| Default の素通し（`ControlPassthroughOutputAction`） | 軸のペアの素通し | **Controls 固有** |

### 6.3 付随する整理
パッドと上記 2 つの Toggle を `ButtonOutputAction` へ移すと、`ControlToggleOutputAction`（と `IToggleFrameOutput`、`Commit` の toggle の処理）は、「Unbound ＋ Toggle」（出力先がなく、意味のない組み合わせ）以外で使われなくなる。使われなくなった後の整理は、規約 2.4 に従い、別の Step で経緯を確認して判断する。

### 6.4 提案
- 提案 A: マウスのボタン等の Toggle を `ButtonOutputAction` へ（干渉の是正）。
- 提案 B: マウス移動・アナログのホイールの Toggle を `ButtonOutputAction` へ（Toggle が効かない不具合の是正）。
- Toggle なしの経路は、変えない。
