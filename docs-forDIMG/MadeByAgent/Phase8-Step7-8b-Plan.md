# Phase8-Step7-8b 計画書: Controls タブの Key の新経路への移行

作成日: 2026-10-20  
対象ブランチ: `For-DI-migration-work`  
上位: `Phase8-Step2b-ControlsTab-Design.md`（§3.3 の F、§3.5、§10。Q4＝B［共有をやめる］、Q8＝B［実機確認は Step9］）、`Phase8-Step7-8a-Plan.md`（土台と値渡し化。完了）  
状態: **Q1〜Q5 を承認済み（全て推奨［A］。2026-10-20 ユーザーが直接承認）。W0（調査）完了。W1（特性化）完了［ビルド・テスト成功。2026-10-20］。次は W2（実装）**

---

## 1. 目的と範囲

Controls タブで「ボタン → キー」を割り当てた dcs（`DS4ControlSettings`）を、旧経路（`ProcessControlSettingAction` の Key の枝 ＋ 合成 SpecialAction ＋ 旧ディスパッチャ）から、統一エンジンへ切り替える。運用は 7.1〜7.7 と同じ: `MigratedControlKinds` に `Key` を足す 1 行で有効化し、外すと旧経路に戻る。旧コードは Step8 まで残す。

**この PR の範囲（Q1＝A）**: 次のすべてを満たす dcs だけを新経路が処理する（所有する）。それ以外は、旧経路が全体を処理する（Q2＝A［ボタン単位の所有］。設計書 §3.5）。

- 通常の割り当てが Key（`actionType == Key`）。
- シフトの割り当てがない（`shiftActionType == Default`）。シフトは 7.8d。
- Extras が空（`extras`、`shiftExtras`。振動・ライトバー・感度は 7.8e）。
- Lightbar マクロが有効でない（7.8e）。
- `actionKey` が `ushort` の範囲に収まる（旧経路は範囲外で例外になる。新経路は、そのような dcs を所有せず、旧経路の挙動を保つ）。

**この PR でやらないこと**: Macro（7.8c）、シフト（7.8d）、Extras／Lightbar マクロ（7.8e）、Button／マウス／ホイール（7.8f）、`ProcessControlSettingAction` の旧コードの削除（Step8）。

### 1.1 暫定の範囲と最終形（別セッションからの補足と訂正。2026-10-20）

- §1 の範囲と、ボタン単位の所有（Q2＝A）は、**移行中の仕組み**である。7.8c（Macro）、7.8d（シフト）、7.8e（Extras／Lightbar マクロ）、7.8f（Button／マウス／ホイール）で区分を 1 つずつ移し、最終的に Controls タブの割り当てはすべて新経路（エンジン）で同じ扱いになる。全区分が移行済みになれば、所有の判定は常に真になり、旧経路との振り分けは不要になる。旧経路は Step8 で削除する。
- **抑制の判定**（特殊アクションが押しているボタンの通常の割り当てを止める）: データ（抑制の集合）はエンジン所有。呼び出しは、今も旧経路の `ProcessControlSettingAction` の先頭に残っている。区分の移行（7.8b〜f）が終わり、Step8 で旧経路を削除するときに、呼び出しがエンジンの Controls 処理の中へ移る（機能は残る）。
- **通常出力の消去**（`ErasesOutputEveryFrame`）の最終形: 割り当てのあるボタンすべてを、エンジンが毎フレーム消す。**Default（未設定）と Unbound（割り当て無し）は別物**:

| 区分 | 実体 | 消去 | 今 → 最終形 |
|---|---|---|---|
| Default（未設定） | `actionType == Default` | **消さない**（元のボタン出力がそのまま出る。消すと未設定のボタンが全部効かなくなる） | 今も最終形も同じ |
| Unbound（割り当て無し） | `actionType == Button` で、`X360Controls.Unbound`（`keyType` にも Unbound の印。`OutBinding.cs` 448 行、`BindingWindow.xaml.cs` 943 行） | **毎フレーム無条件で消す**（`Mapping` の Button の枝は、Unbound をどの出力先にも当てはめず何も出力せず、末尾の共通の `ResetToDefaultValue` で元の出力を消す） | 今は旧経路 → 7.8f（Button 区分）でエンジンへ |
| Key（§1 の条件を満たすもの） | `actionType == Key` | 毎フレーム消す | 7.8b からエンジン |
| Macro | `actionType == Macro` | 毎フレーム消す | 今は旧経路 → 7.8c でエンジンへ |
| Button（X360、マウス、ホイール） | `actionType == Button` | 毎フレーム消す | 今は旧経路 → 7.8f でエンジンへ |
| シフト付き／Extras 付き | — | 割り当ての区分に従い消す | 今は旧経路 → 7.8d／7.8e でエンジンへ |

7.8b の時点でエンジンが消すのは、新経路が所有するボタン（Key）だけ。

## 2. 現状の調査（2026-10-20）

### 2.1 旧経路の Key（`Mapping.cs` 3940〜3956 行）

dcs 1 つにつき毎フレーム、次を行う。

1. 抑制（特殊アクションが押しているボタンなら、通常の割り当てを消して終了）。
2. 論理キー ＝ `Convert.ToUInt16(action.actionKey)`。ネイティブキー ＝ `OutputKBMMapping.GetRealEventKey(actionAlias != 0 ? actionAlias : key)`。ScanCode と Toggle は `keyType` から。
3. 押下 ＝ `GetBoolActionMapping(control)`（**特殊アクションのトリガー判定とは閾値が違う**。§2.2）。
4. 合成 SpecialAction を得る: `FindSpecialActionForLogicalKey(key) ?? GetOrCreateSyntheticKeyAction(device, control, key, toggle, useScan)`。
5. `DispatchOrSetBeingTriggered(sa, device, isPressed, …)` → `DispatchInputEdge`（押下・解放の**エッジ**を、名前のハッシュ ＋ デバイスで検出）→ `ActionManager.DispatchTriggerEdge`（`BeingTriggered` を設定）→ `DefaultActionManager.DispatchTriggerEstablished`／`Released`（**名前で引いた**アクションの `OnTrigger`／`OnRelease`）→ `KeyActionAdapter` → `KeyAction` → `KeyButtonActionController`。
6. **毎フレーム**、`ResetToDefaultValue(control)`: 押していなくても、そのボタンの通常の出力を消す（スティックの方向なら 128 に、トリガーなら 0 に戻す）。

### 2.2 新経路に足りないもの（発見）

| 項目 | 旧経路 | 新経路（今） | 対応 |
|---|---|---|---|
| 押下の判定 | `GetBoolActionMapping`: スティックは**角度の扇形**（例: `LX < 128 かつ 角度 112.5〜247.5°`）、L2／R2 は `> 0`、ジャイロは `> 0`／`< 0` | `TriggerInput.IsActive`: スティックは**±55**、L2／R2 は `> 100`、ジャイロは `±67`（特殊アクションのトリガー用） | 判定が違う。Controls 用の入力アクションと判定を足す（W2。Q2） |
| 通常出力の消去 | 押していなくても**毎フレーム**消す | 成立中だけ（`ResetsTriggerOutput`） | 方針に「毎フレーム消す」を足す（W2。Q3） |
| ネイティブキー | `GetRealEventKey(alias があれば alias、なければ論理キー)` | `KeyOutputAction` は論理キーだけから求める | `KeyBindingValues` にエイリアスを足す（W2） |
| ログ | Key の枝自身は、SA タブ形式の Debug ログ（`SpecialAction KEY entry` など）を**出さない** | `KeyOutputAction` は SA タブ形式を出す | Controls では出さない指定を値に足す（W2。Q4） |
| 状態の鍵 | 合成 SA の**名前**（`Synthetic_Key_{device}_{control}`）。`FindSpecialActionForLogicalKey` により、同じ論理キーの Key 特殊アクションがあればその状態を共有 | 束縛の `Source`（識別用 SA）の名前 | 識別用 SA を同じ名前で作る。共有はしない（Q4＝B の承認済みの動作変更。§2.3） |

**通常出力の消去の対象**: §1.1 の表のとおり。Default（未設定）は消さず、Unbound を含む割り当てのあるボタンは毎フレーム消す。7.8b では Key だけがエンジンの消去に移る。

状態（`ActionInstanceState`）とアクションの実体は、`DefaultActionManager` が **`SpecialAction.name`（大文字小文字を区別しない）**で引く。したがって、識別用 SA は、合成と同じ名前にすれば同じ状態の入れ物を使い、旧経路と新経路が同じ dcs を同時に処理しない限り（ボタン単位の所有で保証）、衝突しない。

### 2.3 Q4＝B（共有をしない）の具体的な影響

旧経路では、Controls タブの Key が、同じ論理キーを出す Key 特殊アクションを見つけると、**その特殊アクションの状態**（押下中、トグルのオン）を共有していた。新経路では、Controls の Key は常に自分専用の状態を持つ。影響が出る例: 特殊アクション A（Key、論理キー 65、Toggle）と、Controls タブの Cross → 65（Toggle）がある場合、旧経路では両者のトグルが連動していたが、新経路では独立する。承認済みの動作変更（規約 2.2）。完了報告書に記録し、変更前後の差をテストで固定する。

## 3. 設計

### 3.1 層の割り振り（7.7b W2b と 7.8a の考え方を踏襲）

| 層 | 担当 |
|---|---|
| 入力監視層 | `TriggerInput`（押下状態。Controls 用の判定を足す） |
| 信号変換層 | `ControlBindingBuilder`（dcs → 束縛。**所有する dcs の選別、キーの解釈、識別用 SA の生成**）、`ControlButtonInputAction`（1 ボタンの入力アクション）、エンジン（エッジ検出、ガード、消去） |
| 出力層 | `KeyOutputAction`（解釈済みの値を実行するだけ。7.8a で値渡し化済み） |

### 3.2 追加・変更するもの

- **`IControlTriggerInput : ITriggerInput`（新設。Q2＝A）**: `bool IsControlActive(DS4Controls control)`（旧 `GetBoolActionMapping` と同じ判定）。`TriggerInput` が実装する。既存の `ITriggerInput`（と、それを実装する偽物）は変えない。
- **`ControlButtonInputAction : IInputAction`（新設）**: 1 ボタン。`Evaluate` は、入力が `IControlTriggerInput` なら `IsControlActive`、そうでなければ未成立。`Triggers` は 1 要素。
- **`BindingPolicy.ErasesOutputEveryFrame`（追加。既定は偽。Q3＝A）**: 真のとき、エンジンは、成立の有無によらず毎フレーム `frame.ResetTriggerControl` する。Controls の Key は `respectsFreshPressGuard: false`、`suppressesTriggerButtons: false`、`erasesOutputEveryFrame: true`。
- **`KeyBindingValues`（拡張）**: `AliasKey`（`actionAlias`。0 は「なし」）と `SuppressSpecialActionLogs`（Controls は真。Q4＝A）を追加する。`KeyOutputAction` は、ネイティブキーを `GetRealEventKey(AliasKey != 0 ? AliasKey : LogicalKey)` で求め、ログの抑制を尊重する。SA タブ側の既定は従来どおり（エイリアス 0、ログあり）。
- **`ControlBindingBuilder.Build(device)`（実装）**: `ControlSettingsGroup` を走査し、§1 の条件を満たす dcs ごとに束縛を作る。識別用 SA は `Synthetic_Key_{device}_{control}`（旧合成と同じ名前。種別 Key、`keyType` は dcs の ScanCode／Toggle）。`MigratedControlKinds` に `Key` がなければ、何も読まず空を返す。
- **束縛の置き場（Q5＝A）**: SA タブの束縛と**同じレジストリの一覧**に連結して差し替える。連結は、`ProfileApplicationService` の Replace の呼び出し元（現在 `IActionBindingBuilder.Build` の結果を渡している箇所）で行う（W0 で位置を確認）。Controls の束縛は、入力が `ControlButtonInputAction` であることで見分ける。SA のループ（`Owns`／`ProcessOwned`）は、プロファイルの特殊アクションの名前だけを引くため、Controls の束縛には触れない。
- **エンジン**: `OwnsControl(device, control)` を実装する（`Rebuild` で作る「ボタン → 束縛の添字」の表を引く。ロックの下。割り当て 0）。`ProcessControl(device, control, input, frame)`（新設）は、その束縛を `ProcessBinding` で処理する。
- **`Mapping`**: 7.8a で足した `OwnsControl` の分岐の中で `ProcessControl` を呼んで戻る（旧本体は素通り）。抑制の判定は共通（旧経路の先頭のまま）。
- **`ServiceRegistration`**: 既定の `MigratedControlKinds` に `Key` を 1 行追加（ロールバックは、この 1 行を外す）。

### 3.3 状態遷移の取り扱い（旧経路との一致の確認が要る点。W1 で特性化する）

- **エッジ**: 旧経路は名前のハッシュで押下・解放のエッジを検出し、`BeingTriggered` を併用する。新経路は `BeingTriggered` と前フレームの成立で検出する。差が出るのは、プロファイル適用・切断の前後（`ClearDeviceState`、エンジンの `ResetState`）。押したままプロファイルを適用した場合に、キーが離される／残る／再度押されるかを、旧・新で比べる。
- **Toggle**: 押下ごとの反転は `KeyButtonActionController` が行う（旧・新で同じ）。
- **Fresh-press ガード**: 旧経路の Controls の Key は見ない。新経路も見ない（`respectsFreshPressGuard: false`）。

## 4. 作業（Wave）

- **W0（調査。コードは変えない）**: (1) `DS4ControlSettings` を実行中に書き換える箇所（`UpdateSettings`、`Reset`、プロファイルエディターのライブ反映など）を洗い出し、束縛を再構築する必要があるかを判断する。(2) `ProfileApplicationService` の Replace の呼び出し位置と、連結の入れ方。(3) `GetBoolActionMapping` の `IsUsingGyroForControls` と `TriggerInput` の `GyroOutputMode` の一致。結果をこの計画書に追記する。
- **W0 の結果（2026-10-20。コードは変えていない）**:
  1. **実行中の設定の書き換え**: プロファイルエディターは、ライブのスロットではなく**専用の編集スロット**（`ControllerLimits.TestProfileIndex`、`ProfileEditor.xaml.cs` 157 行）で `DS4ControlSettings` を編集する（`OutBinding`、`PresetMenuHelper`、`ProfilePresets` はこのスロットの設定を書く）。ライブのスロットの `DS4ControlSettings` を書き換えるのは、プロファイルの読込（`ResetProfile` ＋ 読込）だけで、その後に `ApplyProfile` が `PublishBindings`（`ProfileApplicationService.cs` 310〜333 行）を呼ぶ。`BackingStore.UpdateDS4CSetting`／`UpdateDS4CExtra`／`UpdateDS4CKeyType` は呼び出し元が 0 件。結論: **SA タブの束縛と同じ時点（プロファイル適用の完了時）で作れば、追随が要る経路はない**。読込の途中の書き換えを旧経路は直接読んでいたが、新経路は適用時のスナップショットを使うため、むしろ安全になる。
  2. **束縛の差し替えの位置**: 差し替えは `ProfileApplicationService.PublishBindings` の 1 か所だけ（`_actionRegistry.Replace(device, _bindingBuilderProvider().Build(device))`）。連結は、`ActionBindingBuilder` が省略可能な依存 `IControlBindingBuilder` を受け取り、`Build(device)` の結果に Controls の束縛を連結して返す形にする（`IActionBindingBuilder` と `ProfileApplicationService` のコンストラクターは変えない。影響が最小）。**Q5＝A の方針（同じ一覧に連結）のまま、実装の位置を確定した**。
  3. **ジャイロの判定**: `IsUsingGyroForControls`（`GyroOutputMode == Controls`）は `TriggerInput` の判定と同じ。違いは閾値のみ（`GetBoolActionMapping` は `gryodirs > 0`／`< 0`、`TriggerInput` は ±67）。Controls 用の判定は、`GetBoolActionMapping` どおり `> 0`／`< 0` を使う（W2 の一致テストで全コントロールを比べる）。
- **W1（特性化。旧・新の両経路の Theory。新経路の指定でも、W1 の時点では旧経路が処理する）**: テスト足場 `ControlsKeyTestScope`（`SpecialActionTestScope` と同じ方式。dcs を設定して、`MapCustom` の 1 フレームを流す）。内容: 押下・解放（ボタン、スティックの方向、L2／R2）、Toggle、ScanCode、エイリアスのネイティブキー、押していないフレームの通常出力の消去（スティックの方向・トリガーを含む）、押したままのプロファイル適用・切断、シフト／Extras を持つ dcs が旧経路に残ること、ログの比較（NLog の `MemoryTarget`）。**Q4＝B の差のテストは、旧・新で期待値を分けて明示する**。
- **W1 の実装（2026-10-20）**: `ControlsKeyTestScope`（足場。dcs を Key にする、本番と同じ経路で束縛を登録、合成 SA の状態を引く、`migrated` で `MigratedControlKinds` に Key を足す）と `Phase8Step78bW1ControlsKeyCharacterizationTests`（旧・新の Theory。押下・解放、Toggle［オンになり、離しても保たれる］、スティックの扇形の判定［幅 28 の傾きで押下］、押していない軸の出力の消去、L2 が 1 で押下、未設定のボタンは素通し、シフト付きの割り当て、同じ論理キーの Key 特殊アクションとの状態の共有［旧経路の癖。W2 で新経路の期待値を「共有しない」に切り替える］、押したままの適用・切断の記録）。ログの比較と、キー送出の記録（リピーターの有無で安定しない）は W2 で扱う。押したままの適用・切断は、結果を `ITestOutputHelper` に出力する（期待値は結果を見て W2 で確定する）。
- **W1 の最初の結果と是正（2026-10-20）**: 9 件中 2 件が失敗（旧経路の挙動の読み違い）。(1) 「押していない軸の出力の消去」: LXNeg を割り当て、右へ倒した（LX＝220）とき、出力は 128 ではなく **220 のまま**だった。反対の方向（LXPos）は未設定（Default）で、その出力が `customMapQueue` で素通しされるため。軸の消去が値として見えるのは、反対の方向を含まない傾きのときに限られる。テストは「反対の方向は、その割り当てに任せる（220 のまま）」に改めた。消去の規則（`ErasesOutputEveryFrame`）自体は、エンジンへ引き継ぐ方針のまま。(2) Toggle の `IsToggledOn`: 押した後も偽だった（出力先のスタブでは、リピーターの有無で結果が分かれると推測。原因は未確認）。テストは、押下のディスパッチ（`BeingTriggered`）を固定し、`IsToggledOn` は値を記録するだけにした。W2 で旧・新の値が一致することを比べる。
- **W2（実装）**: §3.2 のとおり。
- **W3（割り当てとロールバック）**: 押している間／押していない間の割り当て（SA の基準と同じ、同数の未押下を基準にする方式）、`MigratedControlKinds` から `Key` を外すと旧経路に戻ること、ソースのガード（`Mapping` の呼び出しが 1 か所、1 ファイル 1 型）。
- **W4**: 完了報告書、`Phase8-Status.md`、チェックリスト（Step9 の総合確認の項目に、Controls の Key を追加）の更新。**実機確認は作らない（Q8＝B）**。

## 5. リスクと対応

| リスク | 対応 |
|---|---|
| 押下の判定の違い（扇形 ↔ ±55） | Controls 用の判定を別に持ち、`GetBoolActionMapping` と全コントロール・乱数で一致することを `TriggerInputTests` と同じ方式で比べる |
| 実行中の設定の書き換えに、束縛が追随しない | W0 で書き換えの経路を洗い出す。追随が要る箇所では、適用の経路と同じ再構築を呼ぶ。洗い出せない場合は、7.8b を止めてユーザーに相談する |
| 毎フレーム・全 dcs が通る | 所有する dcs だけが `ProcessControl` を通る。判定は添字の表の参照（ロック 1 回）。割り当て 0 を W3 で固定する |
| 押したままのプロファイル適用・切断でキーが残る | W1 で旧経路の挙動を固定し、新経路で同じ結果を保証する。差が出たら記録して決める |
| Q4＝B の動作変更 | 承認済み。変更前後の差のテストと、完了報告書への記録 |
| 識別用 SA の名前の衝突（旧合成と同名） | 同じ dcs を旧・新が同時に処理しない（ボタン単位の所有）。ロールバックで旧経路に戻すと、同じ名前を旧経路が使う（状態は名前で共有されるが、戻した時点でプロファイルを再適用する） |

## 6. 質問

- **Q1（所有の範囲）**【承認: A。2026-10-20】: A（推奨）: §1 の条件（Key、シフトなし、Extras なし、Lightbar マクロなし）を満たす dcs だけ。他は旧経路が全体を処理する。B: Extras を持つ dcs も 7.8b で扱う（7.8e の作業が前倒しになり、範囲が広がる）。
- **Q2（押下の判定の入れ方）**【承認: A。2026-10-20】: A（推奨）: 別のインターフェース `IControlTriggerInput` を新設し、`TriggerInput` が実装する。既存の `ITriggerInput` と偽物は変えない。B: `ITriggerInput` に `IsControlActive` を足す（偽物の更新が要る）。
- **Q3（通常出力の消去）**【承認: A。2026-10-20】: A（推奨）: `BindingPolicy.ErasesOutputEveryFrame` を追加し、エンジンが消す（出力の責務をエンジンに統一）。B: 消去は `Mapping` が従来どおり `ResetToDefaultValue` を呼ぶ（変更は小さいが、消去が 2 か所に分かれる）。
- **Q4（ログ）**【承認: A。2026-10-20】: A（推奨）: Controls の Key は、SA タブ形式のログ（`SpecialAction KEY entry` など）を出さない（旧経路も出していない）。旧ディスパッチャ内部の Trace（`DispatchInputEdge`、`DispatchTriggerEdge: firing established` など）は、7.1〜7.7 と同じく新経路では出ない。`KeyAction` 以降のログは変わらない。B: SA タブ形式のログも出す（旧経路にないログが増える）。
- **Q5（束縛の置き場）**【承認: A。2026-10-20】: A（推奨）: SA タブの束縛と同じレジストリの一覧に連結する（入力アクションの型で見分ける）。B: Controls 用の別の領域をレジストリに足す（インターフェースと偽物の更新が要る）。

## 7. 完了条件

- [ ] W0 の結果を追記し、必要な追加の確認事項があればユーザーに相談済み。
- [ ] ビルド・テストが成功（ユーザーが実行）。W1 の旧・新の Theory、ログの比較、割り当て、ロールバック、全ガード（1 ファイル 1 型、ラチェットの一致検査、`IMapping` のメンバー数）が成功。
- [ ] Q4＝B の差が、テストと完了報告書に記録されている。
- [ ] 実機確認は Step9 でまとめて行う（Q8＝B）。Step9 の項目に Controls の Key を追加済み。
- [ ] `Phase8-Step7-Completion-Report.md`、`Phase8-Status.md` の更新。

## 8. 次

7.8c（Controls の Macro。`MacroAction` の値渡し化を含む。Q3＝A の持ち越し）。
