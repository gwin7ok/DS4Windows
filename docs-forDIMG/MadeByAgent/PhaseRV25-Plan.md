# PhaseRV25 計画書（見送った 3 件を挙動のテストへ）

作成日: 2026-11-22。承認: 「得るものが少なくても実施せよ」（ユーザー指示）。

## 実施（完了）
- 起動の順序: `IAppStartupSteps` と `AppStartupSequence`（`Composition/Startup`）へ順序を分け、`App` は手順を明示的に実装（`App.StartupSteps.cs`）。プロファイルの ID 確定 → SA の読み込みは `ProfileStartupLoader`。`AppStartupSequenceTests`・`ProfileStartupLoaderTests` が順序と早期終了を挙動で確かめる。
- 切断の後始末: `DeviceDisconnectHandlerBehaviorTests`（実機の型 `TestDs4Device` と `RecordingProxy.OnCall`）。順序、スロットの解放、一時プロファイルの状態の消去。
- ホバー: `HoverSelectDelay<T>`（`UI/Services`、`TimeProvider`）へ遅延の判断を分け、`ProfileEditor` はそれを使う。`HoverSelectDelayTests` が 500ms の遅延・取り消し・クリックの即時選択を確かめる。
- 負の確認: 取り消しの処理を壊すと 2 件が失敗することを確認し、元へ戻した。
- 文字列走査の削減: 起動・切断・ホバーの走査テストを撤去（`CodeHealthBaseline.PhaseReferenceLines` 218→215）。

## 残る文字列走査（正直な記録）
- `StartupOrderGuardTests` の 1 件（手順の内部の順序）、`SlotLifecycleTests` の一部、`MouseTapClickHoldTests` の 3 件目、`DisconnectGuardTests` の購読順の 1 件、行数・URL の字句検査。
- 実機確認: `Device-Check-Checklist.md` の RV25（後回し）。
