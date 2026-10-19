# Phase8-Step7-8a 計画書: Controls タブの土台と、Key／Button の出力の値渡し化

作成日: 2026-10-20  
対象ブランチ: `For-DI-migration-work`  
上位: `Phase8-Step2b-ControlsTab-Design.md`（§3.4 の 7.8a、§3.5、§3.6、§10。Q1〜Q9 承認済み）、`Phase8-Step7-Plan.md`（7.8）、`Phase8-Step7-7-Plan.md`（W2b の考え方）  
状態: **Q1〜Q4 を承認済み（推奨どおり。2026-10-20）。W1〜W4 を実装済み。ビルド・テストの結果待ち（ユーザーが実行）**

---

## 1. 目的

7.8b 以降（Controls タブの Key／Macro／シフト／Extras／Button を新エンジンへ）の前提を整える。**動作・ログは変えない。ユーザーから見える変化はない。**

1. **出力の値渡し化（Q9＝A）**: `KeyOutputAction`、`ButtonOutputAction` が、`SpecialAction.details` などの「設定の解釈」を自分で持っている。解釈をビルダー（変換層）へ移し、出力は解釈済みの値だけを受け取る。SA タブと Controls タブが同じ出力を使えるようにする。
2. **土台**: Controls タブの割り当てから束縛を作る入口、移行済みかの判定、エンジンの入口を、**空のまま**（何も移行しない）配線する。Default の割り当てが何もせず抜けること、割り当て 0 を保つことを固定する。

## 2. 現状の調査（2026-10-20）

### 2.1 解釈が出力に入っている箇所

| 出力 | 出力が持っている解釈 |
|---|---|
| `KeyOutputAction` | `details` → 論理キー（`ushort`）、`keyType` → Toggle の判定（ログの出し分け）、`trigger`／`uTrigger` → ログ。さらに内部で `new KeyAction(sa, -1)` を作り、`KeyAction` が `details`、`keyType`（ScanCode）を読み、`ActionManager.GetStateFor(sa, …)`／`GetOrCreateControllerForAction(device, sa)`／`KeyActionBinding(sa)` に `sa` を渡す。`KeyButtonActionController`／`…Adapter` が `sa.keyType`／`KeyButtonSwitchMode` を読んで Press／Toggle を決める |
| `ButtonOutputAction` | `details` → `X360Controls`、`KeyButtonSwitchMode` → Toggle の判定、`_manager.GetStateFor(sa, device)` → Toggle の状態 |
| `MacroAction` | 出力自身は解釈しない（`IMacroPlayer.Play(device, sa)` に `sa` を渡すだけ）。解釈は `DefaultMacroPlayer` 側。ログに `sa.pressRelease`、`details` |

### 2.2 重要な発見（設計書 §3.6 の補足）

値渡しは「設定の解釈」を出力から外せるが、**`SpecialAction` が「状態の鍵」と「系統 B への入力」も兼ねている**ため、`SpecialAction` を完全になくすことは 7.8a ではできない。

- **状態の鍵**: `ActionInstanceState`（`BeingTriggered`、`IsExecuting`、`IsToggledOn`、Fresh-press）は `IManagedActionManager.GetStateFor(SpecialAction, device)` で引く。エンジンの `ProcessBinding`、`Rebuild`（`Dictionary<SpecialAction, int>`）、`IsOwnedBinding`（`source.typeID`）、`ProcessUntrigger`（`action.trigger`）も `binding.Source`（`SpecialAction`）を前提にしている。
- **系統 B**: Key の実際の送出は `KeyAction` → `ActionManager`（静的）→ `KeyButtonActionController` で、`SpecialAction` の `keyType` を読む。これらは Step8 で整理する対象（`KeyOutputAction` が委譲しているため、Step8 の「系統 B の削除」は、委譲先の置き換えを含む。別途確認が要る）。
- したがって、Controls タブの割り当てを束縛にするとき、**束縛は `SpecialAction` 型の「識別用」を持つ**ことになる（旧経路の `GetOrCreateSyntheticKeyAction` が作る合成と同種）。

→ 7.8a では、「識別用の SpecialAction を残しつつ、**解釈（キーの番号、Toggle、X360 のボタン）は値で渡す**」までを行う（Q1）。識別用 SpecialAction の廃止（鍵の型を中立にする）は、系統 B の整理と一緒に Step8 以降で扱う。

### 2.3 Q4＝B（共有をしない）との関係

旧経路は `FindSpecialActionForLogicalKey` で、同じ論理キーの Key 特殊アクションの状態を共有していた。新経路では、Controls タブの割り当てごとに**自分専用の識別用 SpecialAction**（名前は旧合成と同じ規則: `Synthetic_Key_{device}_{control}` など。通常とシフトで別）を作るため、共有は起きない。これは 7.8b で実装する（7.8a では識別用の生成は作らない）。

### 2.4 Macro は 7.8a の対象にしない（Q3）

`MacroAction` は解釈を持たず、解釈は `IMacroPlayer`（`DefaultMacroPlayer`）にある。Controls タブのマクロは、整数配列＋ボタン＋キー種別（`PlayMacro(device, macroControl, "", null, int[], control, keyType)`）で、SA のマクロ（文字列）と呼び出しが異なる。値渡しの形は、**Controls のマクロを実際に載せる 7.8c で、消費側とあわせて設計**する方が確実（消費側のない値渡しは検証できない）。

## 3. 方針と作業（Wave）

### W1: `KeyOutputAction` の値渡し化（動作・ログ不変）

- 受け取る値（案）: 論理キー（`ushort`）、Toggle か、ScanCode か。ログ用の識別（名前、種別、`details` の表示文字列、トリガーの一覧）は、識別用の `SpecialAction`（`_sa`）から従来どおり読む（Q1＝A）。
- 新しいコンストラクターを追加: `KeyOutputAction(SpecialAction identity, ushort logicalKey, bool toggle, bool scanCode, IProfileSettingsService settings, int index, ISpecialActionRepository repo)`。
- `KeyAction` も、論理キー・ScanCode を値で受け取れるようにする（`new KeyAction(identity, index, keyId, useScan)` を追加。従来のコンストラクターは、系統 B のアダプターが使うため残す）。Toggle の判定（`IsToggle`）は、`KeyButtonActionController` が `identity.keyType`／`KeyButtonSwitchMode` を読むため、識別用 SA にその値が入っている必要がある（SA タブでは元の SA のまま）。
- ビルダーの Key の分岐: `sa.details` の解釈（`ushort.TryParse`）、`keyType` の読み取りを、ビルダーの関数（`InterpretKey(sa)`）に移し、値を渡す。
- 旧のコンストラクター（`KeyOutputAction(sa, settings, index, repo)`）は、中で `InterpretKey` を呼ぶ薄い互換として残す（既存テストのため。Step8 で整理。TODO を付ける）。

### W2: `ButtonOutputAction` の値渡し化（動作・ログ不変）

- 受け取る値: `X360Controls`（解釈できたか＝`hasControl`）、Toggle か。識別は `_sa`（Toggle の状態の鍵とログ）。
- ビルダーの Button の分岐: `details` の整数の解釈と、`KeyButtonSwitchMode` の読み取りをビルダーへ。
- 旧のコンストラクターは互換として残す（同上）。

### W3: 土台（空の配線）

- `BindingMigrationOptions` に、Controls タブ用の移行済みの区分の集合（`MigratedControlKinds`。**空が既定**）と `IsControlMigrated(kind)` を追加する。区分の値（列挙）は 7.8b で `Key` から追加していく。7.8a では列挙のみ（値は空でよい。後続の PR が増やす）。
- `IControlBindingBuilder`（`DS4Windows/DI/`）と実装 `ControlBindingBuilder`（`DS4Windows/DS4Control/Services/`。R1・R2）: `Build(int device)` が `ControlSettingsGroup` から束縛の一覧を作る。**移行済みの区分が空の間は、何も読まずに空を返す**（`ActionBindingBuilder` と同じ作法）。登録は `ServiceRegistration`。
- エンジンの入口: `ITriggerDispatchEngine.OwnsControl(int device, DS4Controls control, bool shifted)`（既定は偽）。`Mapping.ProcessControlSettingAction` の先頭（抑制の判定の**後**）で `_engine.OwnsControl(...)` を調べ、真なら旧経路の本体を素通りする分岐を**1 か所**足す（常に偽。ピンポイントの置換）。
- 束縛の保持・差し替え（`IActionRegistry` への Controls 用の置き場）は 7.8b の Key で必要になったとき実装する（消費側と一緒に設計。7.8a では作らない）。

### W4: 割り当ての固定（テスト）

- 毎フレーム、全 dcs が `ProcessControlSettingAction` を通るため、**追加の分岐が割り当て 0 を保つ**ことと、**Default の dcs は従来と同じ速さで抜ける**ことを、基準つきで固定する（既存のフレームの割り当てテストと同じ方式）。

## 4. テスト

| 種類 | 内容 |
|---|---|
| 既存 | 7.1（Button）、7.2（Key）の新旧の両経路のテスト、ログの一致、割り当て、ロールバックが**期待値を変えずに**成功する（動作不変の証拠） |
| 新規 W1 | `InterpretKey` の単体（`details` の数値、空、不正、Toggle、ScanCode）。新旧のコンストラクターが同じ出力・同じログを出す |
| 新規 W2 | `InterpretButton` の単体（整数、不正、Toggle）。新旧のコンストラクターが同じ出力・同じログを出す |
| 新規 W3 | `OwnsControl` が常に偽、`ControlBindingBuilder` が空の間に空を返す、`ServiceRegistration` の登録、`MigratedControlKinds` が既定で空 |
| 新規 W4 | Default の dcs と追加の分岐が割り当て 0 |
| ガード | 1 ファイル 1 型、ラチェットの一致検査（`Global`／`AppHost` の件数を増やさない）、`IMapping` のメンバー数を変えない |

## 5. リスクと対応

- **既存の Key／Button の動作が変わる**: 解釈の場所が移るだけで、値は同じ。新旧のコンストラクターが同じ結果を出すテストと、既存テストの不変で確認する。
- **`Mapping.cs` への追加**: 1 か所の分岐のみ（常に偽）。割り当て 0 を W4 で確認する。
- **識別用 SpecialAction が残る**: §2.2 のとおり。TODO（規約 3.3 原則 4）を付け、`Phase8-Step2b-ControlsTab-Design.md` と `Phase8-Status.md` に記録する。
- **ロールバック**: 7.8a は動作が変わらないため、問題があればコミットを戻す。移行の切り替えはまだない。

## 6. 質問

- **Q1（識別用 SpecialAction の扱い）**【承認: A。2026-10-20】: A（推奨）: 7.8a では、Controls タブの束縛にも**識別用の `SpecialAction` を使う**（状態の鍵と系統 B の入力のため）。解釈は値で渡す。鍵の型を中立にする大きな変更は、系統 B の整理（Step8 以降）と一緒に行う。B: 7.8a で、鍵の型を中立にする（`ActionInstanceState` の引き方、エンジンの `Source`、`ActionManager`、`KeyButtonActionController` まで変更する。範囲が大きく、7.1〜7.7 の全体に影響する）。
- **Q2（7.8a の分け方）**【承認: A。2026-10-20】: A（推奨）: 7.8a を 1 つの PR にする（W1〜W4）。B: 値渡し化（W1・W2）と土台（W3・W4）を別の PR にする（7.8a-1、7.8a-2。確認しやすいが、PR が増える）。
- **Q3（Macro の値渡し化の時期）**【承認: A。2026-10-20】: A（推奨）: 7.8c（Controls のマクロを載せるとき）に、消費側とあわせて行う（§2.4）。B: 7.8a で `MacroAction` も値渡しにする（解釈が `IMacroPlayer` 側にあり、整数配列の経路との形が定まらないため、消費側なしの設計になる）。**設計書 §3.6／Q9 の「Key／Button／Macro を 7.8a で」からの変更の提案**（Macro だけ 7.8c へ）。
- **Q4（互換のコンストラクターの扱い）**【承認: A。2026-10-20】: A（推奨）: 旧のコンストラクターは、薄い互換として残し、TODO（Step8 で整理）を付ける。B: 呼び出し元（テスト含む）をすべて新しい形に書き換え、旧を削除する（変更が広がる）。

## 7. 完了条件

- [ ] ビルド・テストが成功（ユーザーが実行）。既存の 7.1・7.2 のテスト、ログの一致、全ガード（1 ファイル 1 型、ラチェットの一致検査）が成功。
- [ ] W1〜W4 の新規テストが成功。
- [ ] 実機確認は作らない（Q8＝B。Step9 でまとめて行う。動作不変のため）。
- [ ] `Phase8-Step7-Completion-Report.md`（7.8a の節）、`Phase8-Status.md`、`Phase8-Step2b-ControlsTab-Design.md`（§2.2 の補足）の更新。

## 8. 次

7.8b（Controls タブの Key。Q4＝B の動作変更を含む。識別用 SpecialAction の生成、束縛の置き場、`OwnsControl` の実装）。

## 9. 実装の記録（2026-10-20。ビルド・テストはユーザーが実行）

- **W1**: 新設 `KeyBindingValues`（論理キー、ScanCode、Toggle）。`ActionBindingBuilder.InterpretKey`（変換層の解釈。公開の静的メソッド）。`KeyOutputAction` に、値を受け取るコンストラクターを追加し、従来のコンストラクターは `InterpretKey` を呼ぶ互換として残した（TODO: Step8 で整理）。`KeyAction` に、論理キーと ScanCode を受け取るコンストラクターを追加（従来のコンストラクターは、系統 B のアダプターが使うため残し、新しい方へ委譲）。`KeyOutputAction` の Toggle のログ判定は、値の `Toggle` を使う。ビルダーの Key の分岐は値を渡す。
- **W2**: 新設 `ButtonBindingValues`（ボタンの有無、ボタン、Toggle）。`ActionBindingBuilder.InterpretButton`。`ButtonOutputAction` に値を受け取るコンストラクターを追加し、従来のコンストラクターは互換として残した。Toggle の判定は、構築時に固定した値（従来は呼び出しごとに SA の設定を読んでいた。SA は束縛の構築後に変更されないため、動作は同じ）。
- **W3**: 新設 `ControlBindingKind`（Key／Macro／Button／Extras）、`IControlBindingBuilder`（`DI/`）、`ControlBindingBuilder`（`DS4Control/Services/`。空を返す）、`BindingMigrationOptions.MigratedControlKinds`／`IsControlMigrated`（既定は空）、`ITriggerDispatchEngine.OwnsControl`（常に偽）、`Mapping.ProcessControlSettingAction` の先頭（抑制の判定の後）に 1 行（`_engine.OwnsControl` が真なら素通り）、`ServiceRegistration` に登録 1 行。
- **W4**: `Phase8Step78aFoundationTests`（解釈の表、互換のコンストラクターとの一致、Button の乱数列での一致、ソースに解釈が残っていないこと、既定が空、ビルダーが空、`OwnsControl` が偽かつ割り当て 0、`Mapping` の呼び出しが 1 か所）。既存の `Phase8Step4W1ContractTests`（エンジンのメンバー名に `OwnsControl` を追加）と `Phase8Step6W3SlotLifecycleTests`（偽のエンジンに `OwnsControl` を追加）を更新。
