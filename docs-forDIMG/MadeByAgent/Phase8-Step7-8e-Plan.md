# Phase8-Step7-8e 計画書: Controls タブの Extras と Lightbar マクロの新経路への移行

作成日: 2026-10-20  
対象ブランチ: `For-DI-migration-work`  
上位: `Phase8-Step2b-ControlsTab-Design.md`（§3.3 の C・D、§10。Q5＝A［ポート方式］、Q8＝B［実機確認は Step9］、Q9＝A［値渡し］）、`Phase8-Step7-8a-Plan.md`（土台）、`Phase8-Step7-8b-Plan.md`（Key）、`Phase8-Step7-8c-Plan.md`（Macro）、`Phase8-Step7-8d-Plan.md`（シフトの廃止）、`Phase8-Step7-6-Plan.md`（ポート方式の前例）  
状態: **Q1〜Q8 を承認済み（2026-10-20。ユーザー。Q1・Q2・Q5＝A、Q3＝B、Q4＝C［Q8 により不要になった］、Q6＝B、Q7＝B、Q8＝B）。W0・W1 完了。Lightbar マクロ機能の廃止（W2）完了［ビルド・テスト成功。§5.3］。W3（エンジンの 1 ボタン複数束縛と Extras の新経路）完了［ビルド・テスト成功。§5.4］。W4 の内容［割り当て・ロールバック・ソースのガード］は、W3 のテストに含めた。W5（文書の仕上げ）も完了。7.8e は、コード・テスト・文書とも完了［実機確認は Step9 の U-44］**

---

## 0. 改訂（2026-10-20。Q8＝B の承認）

W1 の調査で、**Lightbar マクロはプロファイルに保存されず、実質使えない**ことが分かり、ユーザーが機能の廃止を承認した（Q8＝B。規約 2.2 の機能廃止。実機での確認なしに承認）。経緯: 以前の `ProfileDTO` の形式（上流）は `<LightbarMacro>` を保存していたが、このリポジトリは 2025-11-11（`142c24c8`）に旧形式を正とし、旧形式には保存・読込がない。したがって、編集画面で作っても、保存して適用すると消える。廃止しても、利用者のデータは失われない。

この改訂で、7.8e は **Extras だけ**になった。次の項目は取り消した: Lightbar マクロの束縛・出力・ポート（`ILightbarMacroPort`）・生の状態の判定（`RawPress`／`RawRelease`）、`ControlBindingKind.LightbarMacro`、`TriggerInput.Update` の生の現在の状態の引数、Q4（デバイスごとの実行状態）。**Q7＝B** により、Extras のライトバーの不具合（`LightbarMacro` が null だと強制色が出ない）は、条件を外して直した（旧経路で処理する割り当ても直る）。

## 1. 目的と範囲

Controls タブの「Extras」（振動・ライトバーの強制色と点滅・ボタン感度の一時変更）を、旧経路（`Mapping.ProcessControlSettingAction`）から統一エンジンへ切り替える。運用は 7.8b・7.8c と同じ: `MigratedControlKinds` に区分を足す 1 行で有効化し、外すと旧経路に戻る。旧コードは Step8 まで残す。

**Extras と Key／Macro は直交する**: 旧経路では、同じ dcs（`DS4ControlSettings`）が「Key＋Extras」「Macro＋Lightbar マクロ」のように、複数の部品を同時に持てる。7.8b・7.8c は「Extras なし、Lightbar マクロなし」の dcs だけを所有したので、Extras か Lightbar マクロを持つ dcs は、すべて旧経路が処理している。7.8e で、これらを新経路に取り込む。

**範囲（ボタン単位の所有。移行中の仕組み）**: dcs が次のすべてを満たすとき、新経路が dcs 全体を処理する（部品の一部だけを新経路にすることはしない）。

| 通常の割り当て | Extras | 担当 |
|---|---|---|
| Default（割り当てなし） | あり | **新経路（7.8e）**。Extras の束縛だけを作る。通常の出力は消さない（Default は消さない規則） |
| Key／Macro | あり／なし | **新経路（7.8b／7.8c の条件を満たすもの）**。部品ごとに束縛を作る（§3.2） |
| Button | あり | **旧経路のまま**（7.8f で Button と一緒に移行する） |

- Extras の値が読めない dcs（9 個未満など）は、所有せず、旧経路が処理する（7.8b の「キー番号が範囲外」と同じ方針。旧経路の例外の挙動を保つ）。
- ジャイロスワイプの割り当ては対象外（7.8b と同じ）。

**やらないこと**: Button／マウス／ホイール（7.8f）、旧コード（`Mapping` の Extras と Lightbar マクロの枝、`held`、`extrasRumbleActive`、`threadCts`）の削除（Step8）、動作の改善（§4 の承認なしには変えない）。

## 2. 現状の調査（2026-10-20。`Mapping.cs` 3759〜3879 行、4122〜4161 行）

### 2.1 旧経路の Extras

- **対象**: `extras` が空でない dcs（`IsExtrasEmpty`: null か `0,0,0,0,0,0,0,0,0` が空）。通常の割り当て（Key／Button／Macro／Default）とは無関係に、同じ dcs で処理される。
- **押下の判定**: `GetBoolActionMapping`（Controls の Key／Button と同じ判定。新経路の `ControlJudgement.Assignment`／`IsControlActive` の元）。
- **値**（カンマ区切り 9 個。`int.TryParse` が失敗した欄は 0）: [0] 強い振動、[1] 弱い振動、[2] ライトバーの強制色（1 で有効）、[3][4][5] 色（R,G,B）、[6] 点滅、[7] 感度の変更（1 で有効）、[8] 感度。
- **押している間、毎フレーム**: `held[device] = control`。振動が両方 0 でなければ `ctrl.setRumble(byte, byte, device)` と `extrasRumbleActive[device] = true`。ライトバーが有効で、かつ**この dcs の Lightbar マクロが有効でない**とき、`DS4LightBar.forcedColor／forcedFlash／forcelight` を設定。感度が有効で、`tempButtonSensitivity == -1` のときだけ、一時感度を設定（1 回だけ）。全体を `try { } catch { }` で囲む（9 個未満だと途中で例外になり、そこまでの効果が残る）。
- **離した最初のフレーム**（`held[device] == control` のときだけ）: `forcelight = false`、`forcedFlash = 0`、一時感度を元に戻す、`extrasRumbleActive` なら振動を 0,0、`held = None`。
- **癖（再現する対象）**:
  1. **`held` はデバイスにつき 1 つ**: Extras のボタンを 2 つ同時に押すと、最後に処理したほうだけが `held`。先に離したボタンでは、元に戻す処理が走らない。
  2. 離したときの元に戻す処理は、ライトバーが有効でなかった dcs でも `forcelight = false` にする（ほかの強制色の使い手と競合しうる）。
  3. 押したままプロファイルを適用・切断しても、元に戻す処理は走らない可能性がある（W1 で実測。§5）。
  4. Fresh-press ガードはない。

### 2.2 旧経路の Lightbar マクロ

- **対象**: `dcs.LightbarMacro.Active` の dcs のうち、`BoolDS4Controls`（ボタン 24 種）のもの。
- **判定**: **Controls の判定ではなく**、デバイスの生の状態（`GetRawPreviousStateRef`／`GetRawCurrentStateRef`。コメント「`cState` を使うと入力が重複することがある」）の、1 フレーム前と現在の変化。`Trigger` が Press なら押した瞬間、Release なら離した瞬間。
- **実行**: 変化の瞬間に、`Task.Run(RunLightbarMacro)`。`CancelCurrent` なら実行中のタスクを取り消して待ち、新しく始める。`CancelCurrent` でなければ、前のタスクが終わっているか、一度も動いていないときだけ始める。
- **共有状態（癖）**: 取り消しの元（`threadCts`）とタスク（`lightbarMacroTask`）は、**`Mapping` に 1 つ**。デバイスにもボタンにも依らない（複数台で、ある台のマクロが別の台のマクロを取り消す）。
- **`RunLightbarMacro`**: `lock (forcedColor) lock (forcelight)` の中で、`forcelight[device] = true` → 各要素の色を、長さ（ms）ずつ → `forcelight[device] = false`。10ms 刻みで取り消しを見る。

### 2.3 新経路の土台（すでにあるもの）

- 束縛の作り方、値渡し（`Q9=A`）、ボタン単位の所有（`ControlBindingBuilder`）、判定（`ControlButtonInputAction`＋`ControlJudgement`）、消去の規則（`BindingPolicy.ErasesOutputEveryFrame`）、押している間の出力（`IHeldOutputAction.WhileHeld`）、離した瞬間（`Stop`）、ポートの前例（`IBatteryCheckPort`／`DefaultBatteryCheckPort`）。
- **足りないもの（§3）**: ① 1 つのボタンに複数の束縛を持たせること（エンジンの `ControlIndex` は、今は 1 ボタン＝1 束縛）、② Extras の出力とポート、③ Lightbar マクロの出力・ポート・生の状態の変化の判定、④ `held`／`extrasRumbleActive` の置き場。

## 3. 設計

### 3.1 層の割り振り

| 層 | 担当 |
|---|---|
| 入力監視層 | `TriggerInput`（既存の `IsControlActive`。生の状態の前後 `IsRawPressed`／`WasRawPressed` を足す） |
| 信号変換層 | `ControlBindingBuilder`（所有する dcs の選別、Extras の値の解釈、Lightbar マクロの値の解釈、部品ごとの束縛と識別用 SA）、`ControlButtonInputAction`（判定の種類を増やす）、エンジン（1 ボタン複数束縛） |
| 出力層 | `ControlExtrasOutputAction`（Extras）、`ControlLightbarMacroOutputAction`（Lightbar マクロ）。実機・静的な窓口は、ポート（`IControlExtrasPort`、`ILightbarMacroPort`）越し |

解釈（9 個の値、Lightbar マクロの要素・トリガー・取り消し）は変換層が行い、出力には解釈済みの値を渡す（Q9＝A）。

### 3.2 追加・変更するもの（Q8＝B による改訂後）

- **エンジン（1 ボタン複数束縛）**: `ControlIndex[control]` は先頭の束縛の添字とし、束縛ごとに「同じボタンの次の束縛」の添字（`ControlNext`）を持つ。`ProcessControl` は、鎖をたどって順に処理する。`OwnsControl` は先頭の束縛で判定する（ビルダーが、dcs の部品を「全部作る」か「1 つも作らない」かにするため、鎖の中で所有がまちまちにはならない）。
- **束縛の順序（旧経路と同じ）**: Extras → 通常の割り当て（Key／Macro）。
- **識別用 SA（状態の鍵）**: `Synthetic_Extras_{device}_{control}`（既存の `Synthetic_Key_…`／`Synthetic_Macro_…` と同じ形）。
- **Extras の束縛**: 入力 `ControlButtonInputAction(…, Kind=Extras, Judgement=Assignment)`。出力 `ControlExtrasOutputAction`（`IHeldOutputAction`）: 立ち上がりと押している間の毎フレームに同じ「適用」（振動、ライトバー、感度。感度は一時感度が -1 のときだけ）、`Stop` で元に戻す。方針は `respectsFreshPressGuard: false`、`erasesOutputEveryFrame: false`（Extras は通常の出力を消さない。消すのは通常の割り当ての束縛）。値は `ExtrasBindingValues`（新設の構造体。強い振動、弱い振動、ライトバーの有無、色、点滅、感度の有無、感度）。
- **ポート**: `IControlExtrasPort`（`SetRumble`、`SetForcedLightbar`、`ClearForcedLightbar`、`TrySetTemporarySensitivity`、`RestoreSensitivity`）と `DefaultControlExtrasPort`（`ControlService.setRumble`、`DS4LightBar` の静的配列、`ButtonMouseInfo` を、窓口の内側で使う。`ControlService` は遅延で受ける。前例: `DefaultBatteryCheckPort`）。
- **Extras の状態（Q3＝B）**: 旧 `held`／`extrasRumbleActive`（デバイスに 1 つ）は移さない。`ControlExtrasOutputAction` が、自分の分（振動を出したか）を持ち、離したときに自分の分を元に戻す。`Mapping` の旧 `held`／`extrasRumbleActive` は、旧経路が処理する割り当て（Button＋Extras）のために、Step8 まで残す。
- **押したままの適用・切断の後始末（Q6＝B）**: 出力に任意の契約 `IResettableOutputAction`（`OnReset(IOutputContext)`）を足す。エンジンは、`OnProfileApplied` のあとの最初の処理（`ResetState`）と `OnDeviceDisconnected` で、束縛のうち状態が成立中（`BeingTriggered`）のものについて、出力の `OnReset` を呼ぶ。`ControlExtrasOutputAction` は `Stop` と同じ元に戻す処理をする。ほかの出力は実装しない（Key などの押したままの適用の動きは変えない）。適用後も同じボタンが押されたままなら、次のフレームで新しい束縛が立ち上がりとして再び適用する。旧経路で処理する割り当て（Button＋Extras）は、この修正の対象外（7.8f で新経路に入ると直る）。
- **`ControlBindingBuilder`**: Extras の条件の判定（`TryInterpretExtras`: 9 個の値が読める）と、dcs 全体の所有の条件（§1 の表）。Key／Macro の条件（`TryInterpretKey`／`TryInterpretMacro`）の「Extras なし」を、Extras の新経路が有効なときは外す。
- **`Mapping`**: 先頭の所有の判定は 7.8b のまま（`OwnsControl`）。旧経路の Extras の枝は、Lightbar マクロの条件を外した（Q7＝B）以外は変えない（Step8 で削除）。
- **`ServiceRegistration`**: `MigratedControlKinds` に `Extras` の 1 行。ポートの登録。

## 4. 動作が変わる点

**変えないことを原則とする**（規約 2.2／2.3）。承認済み（2026-10-20）の変更と、変えないものを分ける。

**承認済みの動作変更**

1. **Extras のボタンを 2 つ同時に押したとき（Q3＝B）**: 旧経路は、デバイスにつき 1 つの記録（`held`）しかなく、両方を離すまで元に戻す処理が走らない。新経路は、ボタンごとに元に戻す。一方を離すと、そのボタンの元に戻す処理（ライトバーの強制色の解除、振動 0、感度の復元）が走り、押したままのもう一方は次のフレームで再び適用する（一瞬の途切れが起こりうる）。
2. **押したままの適用・切断（Q6＝B）**: 旧経路は、元に戻す処理が走らず、振動・強制色・感度が残る（W1 で確認）。新経路は、適用・切断のときに元に戻す。
3. **Extras のライトバー（Q7＝B）**: 旧経路は、`LightbarMacro` が null だと強制色が出なかった（実質、効かなかった）。ライトバーの有効フラグだけで強制色を出す。**新・旧どちらの経路でも**（Lightbar マクロの条件をコードから外したため）。
4. **Lightbar マクロ機能の廃止（Q8＝B）**: 編集画面の「Lightbar マクロ」の項目（ライトバーのモード［単色／マクロ］の選択）と、実行のコード、関連の部品を削除した。単色の設定は、そのまま残る。保存されない機能だったため、利用者のデータは失われない。

**変えないもの**

- Extras の判定（Controls の判定）、値の効き方、毎フレーム適用、感度は 1 回だけ（一時感度が -1 のときのみ）、離したときの元に戻す処理（`forcelight = false`、`forcedFlash = 0` を含む）。
- 値が読めない Extras（9 個未満など）は、新経路は所有せず、旧経路が処理する。
- ログ: Extras に旧経路のログはなく、新経路も出さない。エンジンの例外ログは従来どおり。

## 5. 作業（Wave）

- **W0（調査。完了。結果は §5.1）**。
- **W1（特性化。完了［ビルド・テスト成功］。§5.2）**: `Phase8Step78eW1ControlsExtrasCharacterizationTests`。
- **W2（Lightbar マクロの廃止。完了［ビルド・テスト成功］。§5.3）**。
- **W3（実装。完了［ビルド・テスト成功］。§5.4）**: §3.2。エンジンの 1 ボタン複数束縛と `IResettableOutputAction` から始め、Extras の出力・ポート・ビルダーを足す。W1 のテストが新経路も走る。承認済みの変更（§4 の 1・2）は、`migrated` で期待値を分ける。
- **W4**: 割り当て（新 ≦ 旧）、ロールバック（`Extras` の 1 行を外す）、ソースのガード。
- **W5**: 完了報告書（7.8e）、`Phase8-Status.md`、チェックリスト（**U-44**: Extras の実機確認。Step9 でまとめて行う）。

### 5.1 W0 の結果（2026-10-20）

1. **観察の方法**: 旧経路で、振動は `ControlService.setRumble`（デバイスがいないと何もしない）を呼ぶ前後に `Mapping.extrasRumbleActive[device]`（公開フィールド）が変わる。ライトバーは `DS4LightBar.forcedColor／forcedFlash／forcelight`（静的な公開配列）、感度は `ProfileSettingsService.ButtonMouseInfos[device].tempButtonSensitivity` で観察できる。したがって、W1 の旧経路は「`extrasRumbleActive`＋静的配列＋`tempButtonSensitivity`」、新経路は「ポートの記録」で比べる。静的配列はテスト間で共有されるため、テストは直列に実行し、前後で初期化する。
2. **生の状態**: 新経路の `TriggerInput` が持つ「前フレーム」（`getPreviousStateRef()`）は、旧経路の Lightbar マクロが使う `GetRawPreviousStateRef()` と同じオブジェクト（`pState`）。現在の状態は、旧経路が `GetRawCurrentStateRef()`（デバイスの `cState`）、`TriggerInput` が持つのは `MapCustom` の引数の `cState`（曲線・デッドゾーン処理後のコピー）で、別。したがって、`Update` に生の現在の状態の引数を足す（§3.2）。
3. **2 段トリガー**: `IsControlActive` は、`MapCustom` が展開した `fieldMapping`（`_fields`）を読む。2 段トリガーの処理が `fieldMapping` を 0 にして Extras を無効にする仕組みは、同じ表を共有するため、新経路でも効く。W1 の Theory に L2 の 2 段（ソフト・フル）を含めて確かめる。
4. **`held`／`extrasRumbleActive` の参照**: `Mapping` の中だけ（Extras の枝）。Q3＝B で、旧経路のものは動かさない。
5. **Lightbar マクロの保存形**: `LightbarMacro`（`DS4WinWPF.DS4Forms.ViewModels`）は、`Active`、`Macro`（色＋長さ ms の要素の列）、`Trigger`（Press／Release）、`CancelCurrent`。プロファイルの文字列は `True/255,255,255:100;255,0,0:50;/Press/False`。ビルダーは、`Active` のとき、要素を配列に写して値にする（`LightbarMacroBindingValues`）。
6. **ついでに分かったこと**: 旧経路の Lightbar マクロの判定は、毎フレーム、リフレクション（`typeof(DS4State).GetField(…)`）で値を引く（割り当てが生じる）。新経路は、表で引き、割り当てを増やさない。

### 5.2 W1 の実装と発見（2026-10-20。ビルド・テスト成功）

- テスト基盤: `ControlsKeyTestScope` に `MapExtras`、観察窓口（`ForceLight`／`ForcedColor`／`ForcedFlash`／`RumbleActive`／`MouseInfo`）、`ResetExtrasObservables` を足した。`Phase8Step78eW1ControlsExtrasCharacterizationTests`（押下で振動・強制色・点滅・感度が出る、離すと戻る、振動 0、ライトバー無効でも離すと強制が解除される、感度は一時の感度がないときだけ、毎フレーム適用、スティック扇形と L2 の 0 超、**2 つ同時は両方を離すまで戻らない**［W3 で Q3＝B により期待値を分ける］、Default＋Extras は通常の出力を消さない、Key＋Extras、数値でない欄は 0、9 個未満は途中で止まる、**押したままの適用で効果が残る**［W3 で Q6＝B により期待値を分ける］、切断の記録）。
- **発見 1（不具合）**: 旧経路の条件 `extras[2] == 1 && (!dcs.LightbarMacro?.Active ?? false)` は、`LightbarMacro` が null だと偽になり、Extras のライトバーが効かなかった。→ Q7＝B で修正（§4 の 3）。
- **発見 2**: Lightbar マクロは、プロファイルに保存されない。→ Q8＝B で廃止（§0）。W1 で書いた Lightbar マクロのテスト（`Phase8Step78eW1ControlsLightbarMacroCharacterizationTests`、`LightbarHarness`）は、廃止に伴って削除した。

### 5.3 W2 Lightbar マクロの廃止（2026-10-20。ビルド・テスト成功）

- 削除（コード）: `LightbarMacro`、`LightbarMacroElement`、`LightbarMacroTrigger`、`LightbarMacroViewModel`、`LightbarMacroCreator`（XAML と分離コード）。`DS4ControlSettings` の `LightbarMacroString`／`LightbarMacro`。`OutBinding` の `LightbarMacro` と、保存時の `LightbarMacroString` への書き込み。`BindingWindowViewModel` の読み込み。`Mapping` の Lightbar マクロの枝、`BoolDS4Controls`、`threadCts`、`lightbarMacroTask`、`RunLightbarMacro`。`ControlBindingBuilder` の「Lightbar マクロが有効でない」条件。文字列 `CreateLightbarMacro`（`Strings.resx`、`Strings.Designer.cs`）。51 ファイルの `using LightbarMacro = …` の別名。
- 画面: `BindingWindow.xaml` の「ライトバーのモード」（単色／マクロの選択）と、マクロのタブを削除し、単色の設定（色の有効、R／G／B、色の選択）を直接置いた。
- Extras のライトバー: 条件 `(!dcs.LightbarMacro?.Active ?? false)` を外した（Q7＝B）。
- テスト: ゴールデンの状態ファイル 5 つから `LightbarMacro`／`LightbarMacroString` の行を削除（差分は、この 2 項目だけ）。`Phase7Step8W2MappingInstanceGuardTests` の許可リストから `BoolDS4Controls` を外した。W1 の Extras のテストの、Lightbar マクロの有無に依るもの 2 件を、修正後の動き（`TheExtrasLightbar_IsForced_OnlyByItsOwnFlag`）に置き換えた。
- 旧形式の `<LightbarMacro>`（上流の形式）がプロファイルにあっても、読み飛ばす（もともと読んでいない）。

### 5.4 W3 の実装（2026-10-20。ビルド・テスト成功）

- 新設（`Actions/`）: `IResettableOutputAction`（押したままの適用・切断の後始末の契約）、`ExtrasBindingValues`（解釈済みの 9 個の値）、`IControlExtrasPort`／`DefaultControlExtrasPort`（振動は `ControlService.setRumble`、ライトバーは `DS4LightBar` の静的配列、感度は `ButtonMouseInfo`）、`ControlExtrasOutputAction`（`IHeldOutputAction`＋`IResettableOutputAction`。Execute／WhileHeld は同じ「適用」、Stop は旧経路の順［強制の解除、感度の復元、振動 0］で元に戻す。自分の分だけ戻す）。
- 変更: `TriggerDispatchEngine`（1 ボタン複数束縛 ＝ `ControlIndex`［先頭］＋`ControlNext`［鎖］、`ProcessControl` が鎖をたどる。`Prepare` が束縛の差し替え前と `ResetRequested` のときに、`OnDeviceDisconnected` が登録を消す前に、`IResettableOutputAction` の出力へ `OnReset` を知らせる）、`ControlBindingBuilder`（`TryInterpretExtras`［9 個未満は所有しない］、dcs の部品［Extras・通常の割り当て］は「全部扱える」ときだけ束縛にする、Extras → 通常の割り当ての順、識別用 SA `Synthetic_Extras_{device}_{control}`、窓口を省略可能な依存で受ける）、`TryInterpretKey`／`TryInterpretMacro`（Extras の有無を見ない）、`ServiceRegistration`（ポートの登録と、`MigratedControlKinds` の `Extras` の 1 行）。旧経路の Extras の枝は、変更なし（Q7 の条件の修正は W2 で済み）。
- テスト: `Phase8Step78eW2ControlsExtrasTests`（値の解釈、出力［順序、byte のキャスト、元に戻す、後始末］、ビルダーの所有の条件、ロールバック、エンジンの鎖、切断、割り当て、ソースのガード）。W1 のテストは新経路も走り、承認済みの変更［2 つ同時、押したままの適用］だけ `migrated` で期待値を分けた。`ControlsKeyTestScope` に `PublishControlsOnly`（記録用の窓口つきのビルダー）と `ExtrasPort` を足し、`RecordingControlExtrasPort` を新設。7.8b・7.8c・7.8a のテストのうち、Extras を除外する前提だった 5 か所［`TryInterpretKey`／`TryInterpretMacro` の Extras、ビルダーの束縛数、W1 の所有、既定の区分の一覧］を更新。

## 6. 質問と決定

承認: **2026-10-20（ユーザー）。Q1・Q2・Q5＝A（推奨）、Q3＝B、Q4＝C、Q6＝B。**

- **Q1（範囲とスイッチ）【承認: A】**: Extras と Lightbar マクロの両方を 7.8e の範囲とし、スイッチは `Extras` と新設の `LightbarMacro` に分ける。
- **Q2（束縛の構成）【承認: A】**: Extras は、通常の割り当てとは別の束縛にし、エンジンを「1 ボタン複数束縛」にする。
- **Q3（`held` の置き場）【承認: B。推奨の A から変更】**: 束縛ごとに持つ。旧経路の「デバイスに 1 つ」の癖は再現しない。2 つ同時に押したときの動きが変わる（§4 の 1）。
- **Q4（Lightbar マクロの実行状態）【承認: C。ただし Q8＝B により不要になった】**。
- **Q5（ポート）【承認: A】**: `IControlExtrasPort` を 1 つと、`ILightbarMacroPort`。
- **Q6（押したままの適用・切断）【承認: B。W1 の実測を待たずに決定】**: 適用・切断のときに元に戻す（§3.2 の `IResettableOutputAction`）。旧経路が処理する割り当て（Button＋Extras）は対象外で、7.8f で直る。

### 6.1 W1 で見つかった質問【承認済み。2026-10-20。ユーザー】

- **Q7（Extras のライトバー）【承認: B】**: 直す（Lightbar マクロの条件を外し、ライトバーの有効フラグだけで強制色を出す）。新・旧どちらの経路でも直る。
- **Q8（Lightbar マクロ）【承認: B】**: 機能を廃止する（規約 2.2 の廃止の承認）。実機での、編集画面で作ったマクロが保存されるかの確認は行わずに承認された。

## 7. 完了条件

- [x] Q1〜Q6 をユーザーが承認（2026-10-20）。W0 の結果を追記（§5.1）。
- [x] W1 のビルド・テストが成功（2026-10-20）。
- [x] Q7・Q8 の承認（2026-10-20）。
- [x] W2（Lightbar マクロの廃止）のビルド・テストが成功（2026-10-20）。
- [x] ビルド・テストが成功（ユーザーが実行。W1〜W3 とも成功。2026-10-20）。割り当て（新 ≦ 旧）、ロールバック、全ガードを含む。
- [ ] 実機確認は Step9（U-44。チェックリストに登録済み）。
- [x] `Phase8-Step7-Completion-Report.md`、`Phase8-Status.md`、`Phase8-Step2b-ControlsTab-Design.md`、`Phase8-Step7-Plan.md` の更新（W5）。

## 8. 次

7.8f（Button／マウス／ホイール／アナログ。Default の扱い、`customMapQueue`。Button＋Extras の dcs も、ここで新経路に入る）。そのあと Step8（旧コードの削除）、Step9（総合確認）。
