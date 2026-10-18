# Phase8-Step1 W4: 孤立コード（呼出元 0 件）の §2.4 判断案

作成日: 2026-10-19  
対象ブランチ: `For-DI-migration-work`  
上位: `Phase8-Step1-Plan.md` 項目 C、`.github/copilot-instructions.md` §2.4、`Phase8-Unified-Trigger-Dispatch-Plan.md` 2026-10-19 追記 §8  
状態: **判断案（承認待ち）。コード・コメントの変更は、承認後の W5 で行う**（Q3＝A: 一括で承認を得る）

---

## 1. 方法

- 参照件数: `DS4Windows/`（`.cs`、`.xaml`）、`DS4WindowsTests/`、`StandaloneTests/` を `grep -w` で実測（2026-10-19）。コメント内の言及は参照に数えない。
- 経緯: `git log --follow --diff-filter=A`（追加コミット）と、`docs-forDIMG/` の計画書・報告書の記述。
- 判断基準（§2.4）: 本プロジェクトの目的（Pure DI 化と、全体 4 層モデルの依存方向の整理）に照らして、**本来その API を使うべきか**。
  - 使うべき: 削除しない。`[Obsolete]` も付けない。§3.3 原則 4 の TODO を付け、接続作業を Step の計画書へ入れる。
  - 使うべきでない（孤立した並行実装、廃止済みの残骸）: 削除してよい。
- いずれも、利用者が触れる機能ではない（呼び出し元が 0 件）ので、§2.2 の「機能の削除」には当たらない。ただし §2.4 手順 4 に従い、承認を得てから実施する。

## 2. 判断案の一覧

凡例: **残す＋TODO**（コメントのみ追加）、**削除**、**温存**（現状のまま。理由を記録）。

### A. 系統 A（Phase8 の統一ディスパッチで本採用する骨組み。D1／D5）

| 対象 | 実測 | 経緯 | 判断案 |
|---|---|---|---|
| `IActionBinding`（`OnTriggered`／`OnReleased`／`Outputs`）、`IInputAction.Evaluate`、`IActionRegistry`（`Register`／`GetBindingsForDevice`）、`IControllerRegistry.GetControllersForDevice` | 本体の呼び出し 0 件。`IActionRegistry` は実装クラスなし。`IActionBinding` は 3 テスト、`IControllerRegistry` は 2 テストが参照 | 2025-12-19（`d650c4fc`、フォーク）。統一抽象化の骨組みとして作られ、稼働中の系統 B（`Actions.Action`）に配線されないまま残った（Phase8 計画書 §1.1） | **残す＋TODO**: 使うべき（D1＝A2 で本採用）。各型・メンバーに `TODO(Phase8-Step2〜7)`（経緯、未接続の理由、接続予定の Step）を付ける |
| `KeyActionBinding`、`KeyOutputAction` | `KeyActionBinding` が `KeyOutputAction` を生成（`KeyAction` が `KeyActionBinding` を生成）。`Execute`／`Stop` が呼ばれない | 2025-12-19（`5564c2d6`、`efcfa29e`）。`KeyOutputAction` は `KeyAction` に委譲せず独自実装（D1 の注記） | **残す＋TODO**: 使うべき。`KeyOutputAction` は Step3 で `KeyAction` へ委譲する形に書き直す（D1 の決定どおり） |
| `ITriggerContext`／`TriggerContextImpl` | 利用あり（`KeyAction`、`KeyOutputAction`、テスト） | 同上 | 対象外（孤立ではない） |

### B. 系統 A の周辺で、D2 などにより置き換わる、または重複するもの

| 対象 | 実測 | 経緯 | 判断案 |
|---|---|---|---|
| `KeyInputAction`（`Evaluate` が null を返すだけ） | 参照 0 件（`IInputAction` の実装としてのみ存在） | 2025-12-19（`efcfa29e`）。「将来用のプレースホルダー」 | **削除**（Step3 で `ComboTriggerInputAction` を導入するのと同時）: 使うべきでない。D2＝B1 で、Controls タブとスペシャルアクションのトリガー条件は 1 つの汎用クラスに統一する決定のため、Key 専用のプレースホルダーは置き換えられる。それまでは TODO を付けて残す |
| `ITrigger`／`SingleButtonTrigger` | 参照 0 件（互いにのみ） | 2025-12-18（`5e231d18`、フォーク）。`Context`（`TriggerContext`）を持つトリガーの抽象。2026-10-15 の 1 ファイル 1 型の分割で単独ファイルになった | **削除**（Step3 と同時）: 使うべきでない。D2＝B1 の `ComboTriggerInputAction`（`IInputAction.Evaluate`）が同じ役割を担う並行実装。`ITrigger.Context` は Phase8 追記 §2 の「0 件メンバー」の 1 つ。`TriggerType`／`TriggerContext` は利用中のため残す |
| `IOutputHandler`（`FakeKeyRepeat`、`PerformKeyPress`／`PerformKeyRelease`） | 参照 0 件（実装クラスも、利用者もなし） | 2025-12-19（`d650c4fc`）。「`VirtualKBMBase` をラップする低レベル出力の抽象」 | **削除**（W5 で実施）: 使うべきでない。同じ役割の `IVirtualKBM`（第 3 層、DI 登録済み）が正規の経路で、重複する並行の契約 |
| `ReleasePolicy`（`EvaluateUntrigger`）、`ToggleController`（`ShouldFlipToggle`／`ApplyToggle`） | 参照 0 件、テスト 0 件 | 2025-12-18（`672cb67f`、フォーク）。「`Mapping` から切り出す移行用の小さなヘルパー」。実際の判定は `Mapping` にインラインのまま（`Mapping.cs` の復帰判定、`ShouldClearToggledOn`）で、ヘルパーとは別実装 | **削除**（W5 で実施）: 使うべきでない。静的なヘルパー（§2.1 の新設禁止の趣旨にも反する）で、`Mapping` の実装と仕様が一致していない（例: `ReleasePolicy` の引数の意味が反転する）。仕様は `Phase8-Step1-Trigger-Spec-Ledger.md` §3 に記録済みで、Phase8 の束縛クラスが新しく実装する |
| `MacroManager`（静的。`PlayMacro`、`ExecuteMacroTask` はプレースホルダー） | 参照 0 件、テスト 0 件 | 2025-12-18（`672cb67f`）。「マクロの実行と同期を集約する」との意図だが、本体は `Mapping.PlayMacroTask` に残り、プレースホルダーのまま | **削除**（W5 で実施）: 使うべきでない。並行のキュー（`Mapping.macroTaskQueue` と別）を持つ未完成の実装。マクロの再設計は Phase9-C（`Phase9-Responsibility-Split-Plan.md`） |
| `ToggleActionController`（`[Obsolete]` の空の殻） | 参照 0 件（`Mapping.cs` はコメントのみ） | 2025-12-16（`067d9578`）。「`KeyButtonActionController` に置き換えたため、移行を楽にする情報用のスタブとして残す」 | **削除**（W5 で実施）: 廃止済みの残骸。移行は完了している |

### C. 上流（DS4Windows 本家）由来の死にコード

| 対象 | 実測 | 経緯 | 判断案 |
|---|---|---|---|
| `DateTimeJsonConverter`（`DateTimeConverterUsingDateTimeParse` 含む） | 参照 0 件 | 2021-03-21（上流、「Newtonsoft.Json の依存を除去」）。除去時に作られ、使われないまま | **削除**（W5 で実施）: 使うべきでない |
| `InputMethods`（`MoveCursorBy`、`MouseWheel`、`MouseEvent`） | 参照 0 件 | 2014-03-27（上流、`Revised Custom Mapping`）。現行の出力は `SendInputHandler` など `IVirtualKBM` 系 | **削除**（W5 で実施）: 使うべきでない（旧実装の残骸） |
| `FakerInputMapping.ExpandedKeyboardKeys`（入れ子のクラス。`KEY_NONE` のみ） | 参照 0 件 | 2020（上流）。「VMulti にないキーを定義する用。使わないかもしれない」とコメント | **削除**（W5 で実施）: 使うべきでない |
| `Sensitivity`（X／Y の感度） | 参照 0 件（`Mapping.cs` のコメントに「deprecated Sensitivity modifier」とあるのみ） | 上流由来（`ProfileActions.cs` に同居していたものを、2026-10-15 に単独ファイルへ分割）。XML の `Sensitivity` ノードの読み書きは `LegacyProfileXmlSerializer` が直接行い、このクラスは使わない | **削除**（W5 で実施）: 使うべきでない（上流で deprecated。XML の読み書きとは無関係） |
| `ITouchpadBehaviour` | `Mouse` が実装するのみ。利用者は 0（`ControlService` はコメントアウト済みの行のみ） | 2014-03-27（上流） | **温存＋TODO**: `Mouse` の分割（Phase9-B、`IMouseEngine`）で、この契約を使うか廃止するかを判断する。いま削除する効果が小さく、`Mouse` の宣言も触るため |

### D. フォーク独自の孤立モデル

| 対象 | 実測 | 経緯 | 判断案 |
|---|---|---|---|
| `SensitivitySettings` | 製品コードの参照 0 件。テスト 1 件（`SensitivitySettingsTests`） | 2026-09-16（`4bf31c21`、Phase5-Step14 の後続）。感度の 6 値とパイプ区切り文字列を同期するサブ設定モデルで、同系統の他のサブ設定（`StickDeadZoneInfo` ほか）と同じ「`ProfileSubSettingBase` ＋ 同期」の設計の一部。接続先の ViewModel 側は未実装 | **温存＋TODO**: 判断を Phase9-G（`ProfileSettingsViewModel` のサブ ViewModel 化）に送る。本来使うべきかの判断が、サブ ViewModel の分割方針に依存するため。テストは維持 |

### E. 契約だけ配備され、呼び出し側が未接続のメンバー（§2.4 の典型例）

| 対象 | 実測 | 経緯 | 判断案 |
|---|---|---|---|
| **`IProfileSwitcher.ClearState`／`IProfileApplicationService.ClearPendingRestore`** | **本番の呼び出し 0 件**（`ClearState` のテスト 1 件のみ） | 2026-09-04（`59035b12`）。Phase5-Step3 の「タスク Step3-5: 切断時スタック残留防止（§5.6 ガードレール）」で配備。完了報告は「完了」だが、物理切断の経路（`ControlService.On_DS4Removal`）からは呼ばれていない。`OlderProfilePath` は切断で消えず、再接続時に `ControlService` が復元に使う（2232 行付近） | **残す＋TODO（要調査）**: 使うべき（ガードレール 6「切断時クリーンアップ」の実装漏れの可能性）。ただし、切断で `OlderProfilePath` を消すと再接続時の復元の挙動が変わりうるため、意味（何を残し、何を消すか）を Step6（コントローラー接続時の紐付け）で調べてから接続する。**Phase5-Step3 の完了報告と実コードの不一致**として記録する |
| **`IProfileSwitcher.ApplyManualProfile`** | **本番の呼び出し 0 件**（テストの `MockProfileSwitcher` などが実装するのみ） | 2026-09-02（`53a2b16f`）。「`ProfileEditor` の DI 経由の適用」のために追加。その後、UI は `IProfileApplicationService.ApplyProfile` を直接使う形になり、一度も本番から呼ばれていない（`Notification-Legacy-Path-Cleanup-Report.md` も「変更前から本番の呼び出しなし」と記録） | **削除**（W5 で実施。インターフェースの 1 メンバーと、実装、テスト用モック 3 か所）: 使うべきでない。UI → `IProfileApplicationService` が正規の経路で、重複する並行の入口 |
| `IMacroPlayer.IsPlaying` | テストのみ | `DefaultMacroPlayer` が状態を保持するが、利用者が `IMacroPlayer` 側にない | **温存＋TODO**: Phase9-C（マクロの再設計）で、K3 の恒久対応とあわせて判断する |

## 3. まとめ（承認してほしい内容）

- **残す＋TODO（コメントのみ）**: 系統 A の骨組み（`IActionBinding`、`IInputAction`、`IActionRegistry`、`IControllerRegistry`、`KeyActionBinding`、`KeyOutputAction`）、`ClearState`／`ClearPendingRestore`（要調査の旨）。
- **温存＋TODO**: `ITouchpadBehaviour`（Phase9-B）、`SensitivitySettings`（Phase9-G）、`IMacroPlayer.IsPlaying`（Phase9-C）。
- **削除（W5）**: `IOutputHandler`、`ReleasePolicy`、`ToggleController`、`MacroManager`、`ToggleActionController`、`DateTimeJsonConverter`、`InputMethods`、`FakerInputMapping.ExpandedKeyboardKeys`、`Sensitivity`、`IProfileSwitcher.ApplyManualProfile`。
- **削除（Phase8 Step3 で、置き換えと同時）**: `KeyInputAction`、`ITrigger`／`SingleButtonTrigger`。それまでは TODO を付けて残す。
- 削除は 1 ファイル 1 型の規約（§3.3）を満たしたまま、ファイルごと消す。ラチェット（`Global.*` ほか）の基準値に影響はない見込み（削除対象に該当の参照なし）。`Step10FourGlobalRatchetGuardTests` などの一致検査は、削除後のビルド・テストで確認する。

## 4. 確認したい点

1. 上の「削除」「削除（Step3 と同時）」の範囲でよいか（機能の削除ではなく、参照 0 件の整理）。
2. E の `ClearState`／`ClearPendingRestore` を「要調査の TODO」とし、Step6 で扱う方針でよいか。
3. 上流由来の死にコード（C）を削除すると、上流の更新を取り込むときの差分が増える。本リポジトリは既に大きく分岐しているが、問題ないか。
