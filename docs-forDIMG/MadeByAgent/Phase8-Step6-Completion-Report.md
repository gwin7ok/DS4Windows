# Phase8-Step6 完了報告書: 切断・接続との連携

作成日: 2026-10-19  
対象ブランチ: `For-DI-migration-work`  
計画書: `Phase8-Step6-Plan.md`（Q1〜Q7 を推奨どおり承認。`ClearState`／`ClearPendingRestore` の削除は、対象を再掲して個別に承認）  
結果: **W1〜W3 のビルド・テスト成功（ユーザー実行）。移行対象（`BindingMigrationOptions.MigratedTypes`）は空のままなので、本番の挙動は変わらない**

---

## 1. 成果

| Wave | 内容 | ファイル |
|---|---|---|
| W1 | 現状の特性化（コードは変えない） | `Phase8Step6W1DisconnectCharacterizationTests`（7 本） |
| W2 | 切断・接続の口 | `IProfileSlotApplier.ReleaseSlot`／`PublishSlot`、`ProfileSlotApplier`（`ITriggerDispatchEngine` を注入）、`ControlService`（`On_DS4Removal` と `PrepareConnectedInputControllerSettingEvents` からの呼び出し） |
| W3 | 結合テスト、不要な契約の削除 | `Phase8Step6W3SlotLifecycleTests`（9 本）。`ClearState`／`ClearPendingRestore` を削除（本番の定義 4 か所、テスト用の偽の実装・テスト） |

- **切断**: `On_DS4Removal` で `_mapping.HandleDeviceDisconnect(ind)` の直後に `ReleaseSlot(ind)` を呼ぶ。エンジンの `OnDeviceDisconnected`（抑制の集合、束縛ごとの実行時の状態、復帰の合図の消去と、登録簿の `Clear`）。範囲外のスロットは無視し、例外はログに残して切断処理を止めない。
- **接続**: プロファイルを適用しない経路（一時プロファイル／AutoProfile の使用中）で `PublishSlot` を呼び、束縛を差し替える。適用する経路は、Step5 の `PublishBindings` で差し替わる。
- `ControlService` のコンストラクター、`Mapping.HandleDeviceDisconnect` は変えていない。

## 2. 検証結果

- 特性化（W1）: 切断（`HandleDeviceDisconnect`）がスロットのプロファイル選択（`OlderProfilePath`／`SelectedProfile`／`ProfilePath`／一時プロファイルの状態）に触れないこと、適用の後始末も同じ切断の後始末を通ること、`Stop` は `HandleDeviceDisconnect` を呼ばないこと、`OlderProfilePath` の使用ファイルの一覧、初回接続のフラグはアプリの起動時だけで立つこと。
- 結合（W3）: 範囲外の無視、エンジンの例外の握りつぶし、実ホストで切断後に登録簿とエンジンの状態が消えること・プロファイル選択が不変であること・適用しない再接続で束縛が戻ること、移行対象が空なら登録簿は空のままであること、`HandleDeviceDisconnect` にエンジンの切断を足していないこと（ソース走査）。
- 既存の全テストが期待値を変えずに成功。変えた既存テスト: `Step10FourTwoGlobalReferenceGuardTests`（削除したメンバーの検査を外した）、`Phase7Step3cApplyProfileCharacterizationTests`／`Phase7Step10eW1SlotLimitServicesGuardTests`／`TestRepositoryFactory`（`ProfileSlotApplier` のコンストラクターに追従）。

## 3. 計画との差・判断の記録

1. **`ClearState`／`ClearPendingRestore` は接続せずに削除した**（Q3＝A。ユーザー承認）。調査で、`OlderProfilePath` が「スロットの通常のプロファイル」（初回接続の既定、画面のリンク解除・名前変更）であり、切断で空にすると壊れると分かった。Profile 種別の「離したら復帰」は `OlderProfilePath` ではなく `Mapping` の復帰の合図（`prevProfileName`）で行われ、その合図はエンジンの `OnDeviceDisconnected` が消す。記録: `Phase8-Step1-Orphan-Decisions.md` の追記。
2. **計画書の事実の誤りを是正した**: 当初、「サービスの再起動後の初回接続で `OlderProfilePath` を使う」と書いたが、初回接続のフラグはアプリの起動時だけで立つ（`ResetConnectionFlags` の呼び出しは `App.xaml.cs` のみ）。計画書 §2.3 を直した。
3. **`ReleaseSlot` を `IProfileSlotApplier` に置いた**（Q1＝A）。`ControlService` が既に持つ依存で、コンストラクターを変えずに済む。`Mapping.HandleDeviceDisconnect`（適用のたびにも呼ばれる）には足せない。
4. **`IAutoProfileService.cs` の古いコメント**（`IProfileSwitcher.ClearState(int)` に言及）は、Shift-JIS のファイルのため編集していない。コメントのみで、動作に影響しない。
5. **`IControllerRegistry` の接続時の紐付けは不要と確定**（Q4＝A）。新経路は `IOutputContext.OutputHandler` で出力する。系統 B の登録簿は Step8 の削除まで使う。
6. **`OnProfileApplied` は接続していない**（Q7＝A）。Step7.1 で判断する。サービスの停止（`Stop`）の経路は変更していない（Q5＝A）。

## 4. 完了条件の確認

- [x] W1〜W3 のビルド・テストが成功。既存テストの期待値は不変（上の差分を除く）。
- [x] 切断のスロットで登録簿とエンジンの状態が消え、`OlderProfilePath`／`SelectedProfile` は不変。適用しない再接続でも束縛が差し替わる。
- [x] `HandleDeviceDisconnect` に、エンジンの切断を足していない。
- [x] `ClearState`／`ClearPendingRestore` の扱いが決まり（削除）、TODO が解消された。
- [x] 本番の挙動が変わらない（`MigratedTypes` が空）。
- [x] 本報告書、`Phase8-Status.md` の更新。

## 5. 次の Step への引き継ぎ・注意

- **Step7（種別ごとの切替）**: 最初の PR は Button（7.1）。そこで、`OnProfileApplied` の接続（適用時に抑制の集合を消すか）、Stage3 の TRACE ログのエンジンへの引き継ぎ（Step4 完了報告書 §5）を判断する。切断・再接続で古い状態が残らないこと、束縛が空にならないことは、Step6 で用意した口とテストが担う。
- **Step7.5（Profile）**: 復帰の合図は、エンジンが持ち、`OnDeviceDisconnected` で消える。`RestoreFromAction`（`OlderProfilePath` へ戻る経路）は、台帳 §8 項目 11 のとおり、本番では通らない。復帰の設計と合わせて、Step7.5 で扱いを確定する。
- **モデル図の更新案**（図の変更は、ユーザーの指示があるまで行わない）: `IProfileSlotApplier` に `ReleaseSlot`／`PublishSlot` を足し、`ProfileSlotApplier` → `ITriggerDispatchEngine` の依存を足す。
