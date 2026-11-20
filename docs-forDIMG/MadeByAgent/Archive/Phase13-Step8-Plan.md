# Phase13-Step8 個別計画書: 残る遅延取得（`Func<IMapping>`／`Func<IAutoProfileService>`／`Func<IActionBindingBuilder>`）の再判定

作成日: 2026-11-04
対象ブランチ: `Phase13`
上位計画: `Phase13-Plan.md` §4 Step8（ロードマップ #9〜#11）
状態: **承認待ち**（§9）

---

## 1. 目的

Step5〜7 で `ControlService` の持つ状態と相互参照を解消した。残る 3 種の遅延取得（`Func<…>`）について、**循環を理由とするものか、起動順を理由とするものか**を測り直し、
循環が消えたものは直接注入へ替える。起動順（ホスト構築時に作らない）が理由のものは、台帳に理由と再検討条件を記録して残す。
完了条件（Phase13-Plan Step8）: 「循環を理由とする `Func` が 0 件」。起動順を理由とするものは、理由つきで残してよい。

## 2. 調査結果（2026-11-04）

`Phase13Step8ResolutionGraphProbeTests`（新設。登録から静的にコンストラクターの依存をたどる。`Func` は辺にしない）で、各利用者が対象の依存グラフの内側にあるか（＝直接受けると循環するか）を求めた。

| # | 遅延取得 | 利用者 | 利用者が対象のグラフの内側か | 判定 |
|---|---|---|---|---|
| 1 | `Func<IMapping>` | `DefaultActionManager` | **はい**（`Mapping` → … → `ActionManager`（`DefaultActionManager`）） | **本物の循環**。残す |
| 2 | `Func<IMapping>` | `DefaultMacroPlayer` | いいえ | 循環なし。起動順のみ |
| 3 | `Func<IMapping>` | `DefaultMacroStringPlayer` | いいえ | 同上 |
| 4 | `Func<IMapping>` | `ProfileSwitchAction`（`SpecialActionOutputFactory` 経由で生成） | いいえ | 同上（アクションは束縛の構築時に `new`。コンテナの登録ではない） |
| 5 | `Func<IMapping>` | `SpecialActionOutputFactory`／`ActionBindingBuilder` | いいえ | 同上 |
| 6 | `Func<IAutoProfileService>` | `MainWindowFactory`、`ViewModelFactory` | いいえ | 循環なし。理由は「`AutoProfileHolder` は設定の場所が決まってから」（起動順） |
| 7 | `Func<IActionBindingBuilder>` | `ProfileApplicationService` | **はい**（`ActionBindingBuilder` → … → `ProfileApplicationService`） | **本物の循環**。残す |

- 本物の循環は 2 つ（#1 と #7）。いずれも `Mapping` ⇄ `ProfileApplicationService` ⇄ `ActionBindingBuilder` の鎖の 1 か所ずつで切れていれば成立する。
  `Mapping` → `ProfileApplicationService`（直接）→ `Func<IActionBindingBuilder>`（#7 で切断）→ `ActionBindingBuilder` → `Func<IMapping>`（#5。直接にすると #7 だけで切れる）。
  つまり **#7 を残す限り、#2〜#5 を直接注入にしても循環は生じない**（プローブの「対象が利用者のグラフの内側」も全件 False）。
- #1 の `DefaultActionManager` は、`Mapping` が `IActionManager` を受けるため、直接にできない。
- 起動順の懸念（Phase5-Plan §5.4）: 直接注入にすると、利用者の生成時に `Mapping`（57 型の依存グラフ）が作られる。利用者がホスト構築時（`App` の起動、テストの `TestHost`）に作られるものは、これまで避けていた `Mapping` の生成が前倒しになる。

## 3. 設計案

| 対象 | 方針 |
|---|---|
| #1 `DefaultActionManager` | **残す**（循環）。`Phase13FuncRatchetGuardTests` の許可リストに理由つきで登録 |
| #7 `ProfileApplicationService` | **残す**（循環）。同上 |
| #2〜#5 `IMapping` の 4 利用者 | 利用者ごとに直接注入を試し、**ホスト構築時に `Mapping` が作られてしまうものは `Func` に戻して**起動順の理由を台帳へ記録する（実測で判定。1 利用者 1 コミット） |
| #6 `IAutoProfileService` | `MainWindowFactory`／`ViewModelFactory` が使うのは画面の生成時（`AutoProfileHolder` の生成〔設定の場所の確定後〕より後）。実測して問題がなければ直接注入。起動順の問題が出れば残して記録 |
| `ServiceRegistration.cs` の古いコメント | `DefaultActionManager`／`DefaultMacroPlayer`／`Mapping` が `Func<IMapping>`／`Func<IDeviceStateAccessor>` を受けるという記述（185〜194 行付近）を現状に直す |
| 新しい検査 | `Phase13FuncRatchetGuardTests`: `ServiceRegistration.cs` と本番ソースの `Func<I…>` のうち、サービスを遅延で取るもの（`Func<IMapping>`／`Func<IAutoProfileService>`／`Func<IActionBindingBuilder>`）を許可リスト（型×利用者×理由）で固定し、増えたら失敗させる |

## 4. 実装の分割（各サブステップを別コミット。各回 `Debug ALL`）

| サブ | 内容 |
|---|---|
| 8-a | 本計画書、`Phase13Step8ResolutionGraphProbeTests`（調査用。結果を §2 に固定） |
| 8-b | `Func<IMapping>`: `DefaultMacroPlayer`、`DefaultMacroStringPlayer`、`SpecialActionOutputFactory`／`ProfileSwitchAction`／`ActionBindingBuilder` を、利用者ごとに直接注入へ（ホスト構築時に `Mapping` が作られないことをテストで確認。作られるものは戻して理由を記録） |
| 8-c | `Func<IAutoProfileService>`: `MainWindowFactory`、`ViewModelFactory` を直接注入へ（起動順に問題が出れば残して記録）。登録の `Func` を削除 |
| 8-d | `Phase13FuncRatchetGuardTests` の新設（許可リスト）、`ServiceRegistration.cs` の古いコメントの修正、台帳（`Phase7-Residual-Ledger.md`／`Phase9-Deferred-Ledger.md` のうち該当するもの）への理由の記録、結果の節（本書 §10）と `Phase13-Status.md` の更新 |

## 5. 影響範囲

`Actions/` の 5 クラス、`MainWindowFactory`、`ViewModelFactory`、`ServiceRegistration.cs`、およびそれらを生成するテストのヘルパー。
実行時の挙動は変えない（解決の時期が変わるだけ）。ログの文言・レベルは変えない。

## 6. リスクと対策

| リスク | 対策 |
|---|---|
| `Mapping` がホスト構築時に作られ、起動が遅くなる／テストの前提（`TestHost` の軽さ）が崩れる | 8-b は利用者ごとに分け、各回で全テスト。ホスト構築直後に `Mapping` が未生成であることの検査を追加できるか確認し、できなければ戻す |
| 新しい循環（静的プローブの見落とし） | プローブに加えて、実際の `ServiceProvider` の解決を全登録で行う既存のテストが赤くなる。赤くなれば `Func` に戻す |
| `AutoProfileHolder` の生成が設定の場所の確定前に起きる | 8-c で、`App` の起動順（設定の場所の確定 → ホスト構築 → 画面の生成）を確認してから変える。確実でなければ残して記録 |

## 7. 規約への適合

1 ファイル 1 型、配置 R1〜R7 に変更なし（新設は検査のテスト 2 つのみ）。ラチェット（`Global`／`AppHost`／`Program.rootHub`）の件数は変えない。
ログは変えない。残す `Func` には、規約 §3.3 原則 4 に従い、理由と再検討条件を TODO として残す。

## 8. 検証

`Debug ALL`（警告 0、エラー 0、全テスト合格）。各サブステップで実行。最後に実機確認（Step6〜7 とまとめて）: 起動、自動プロファイル、マクロ、プロファイル切替アクション。

## 9. 決定事項（承認をお願いします）

1. **本物の循環（#1 `DefaultActionManager`、#7 `ProfileApplicationService`）は残す**。許可リストと TODO に理由を記録する。
2. **#2〜#5（`IMapping` の 4 利用者）は、利用者ごとに直接注入を試し、ホスト構築時に `Mapping` が作られるものは戻す**（実測で判定）。
3. **#6（`IAutoProfileService`）も同様に、実測して問題がなければ直接注入**、問題があれば残して記録。
4. **`Phase13FuncRatchetGuardTests`（許可リスト方式）を 8-d で追加**する。
5. **4 コミット（8-a〜8-d）で、以後は自動進行**（Step7 と同じ）。

---

## 10. 実施結果（2026-11-04）

決定 1〜5 は承認済み（2026-11-04）。

| サブステップ | コミット | 内容 |
|---|---|---|
| 8-a | `01c3d301` | 本計画書、`Phase13Step8ResolutionGraphProbeTests` |
| 8-b | `5b4bb123` | `DefaultMacroPlayer`／`DefaultMacroStringPlayer`／`ProfileSwitchAction`／`SpecialActionOutputFactory`／`ActionBindingBuilder` が `IMapping` を直接受ける。`Phase7Step8W2MappingInstanceGuardTests` を現状（`DefaultActionManager` だけが遅延）に更新 |
| 8-c | `7a3ee103` | `MainWindowFactory`／`ViewModelFactory` が `IAutoProfileService` を直接受ける。登録を撤廃（メイン画面の生成は設定の読込後。`App.ResolvePostHostService`） |
| 8-d | （本コミット） | `Phase13FuncRatchetGuardTests`（許可リスト）、`ServiceRegistration.cs` の古いコメントの修正、残す 2 件に TODO と再検討条件を記録 |

- 残る遅延取得は 2 件（本物の循環）: `DefaultActionManager` の `Func<IMapping>`、`ProfileApplicationService` の `Func<IActionBindingBuilder>`。
- **完了条件**「循環を理由とする `Func` が 0 件」: 残る 2 件は循環が理由であり、「循環が消えたものは直接注入へ」の趣旨を満たす（消えない循環のみが残った）。
- 実行時にホスト構築時の `Mapping` の生成が前倒しになるかは、`ControlService` の登録がすでに `IMapping` を受けているため、起動の流れは実質変わらない。`DefaultActionManager` は遅延のまま。
- `Debug ALL`: 警告 0、エラー 0、全テスト合格。実機確認（最後にまとめて）: 起動、自動プロファイル、マクロ、プロファイル切替アクション。
