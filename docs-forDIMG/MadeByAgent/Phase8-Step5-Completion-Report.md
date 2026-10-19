# Phase8-Step5 完了報告書: 束縛のビルダー（`IActionBindingBuilder`）と、登録簿への `Replace`

作成日: 2026-10-19  
対象ブランチ: `For-DI-migration-work`  
計画書: `Phase8-Step5-Plan.md`（Q1〜Q8 を推奨どおり承認）  
結果: **W1〜W3 のビルド・テスト成功（ユーザー実行）。移行対象（`BindingMigrationOptions.MigratedTypes`）は空のままなので、本番の挙動は変わらない**

---

## 1. 成果

| Wave | 内容 | ファイル |
|---|---|---|
| W1 | ビルダーの契約と実装 | `DS4Windows/Actions/IActionBindingBuilder.cs`、`ActionBindingBuilder.cs`。`ComboTriggerInputAction` に読み取り専用の `DelaySeconds`／`InvertsOnRelease` を追加 |
| W2 | DI 登録と、適用への接続 | `ServiceRegistration`（`IActionBindingBuilder`、`Func<IActionBindingBuilder>`）、`ProfileApplicationService`（`PublishBindings`、`CompleteProfileApplication` の最後で呼ぶ）、`IProfileApplicationService.PublishBindings`、`MainWindow`（パイプの一時プロファイル読込の成功後に呼ぶ） |
| W3 | 結合テスト | `Phase8Step5W3PublishBindingsTests`（偽の登録簿・ビルダー、実ホスト、構造） |

- 作る種別は Button／Key／Macro／Program／Profile の 5 種（Q2＝A）。構成は設計書 §6 のとおり: Button は Press が Fresh-press ガードなし／Toggle がガードあり、Program と Profile は `blocksWhileExecuting`、Profile の復帰は Automatic（自動）→ Buttons（`uTrigger`）→ None の優先。それ以外の種別は束縛を作らない。
- 移行対象が空の間、`Build` は何も読まずに空のリストを返す。順序は `ProfileActions` の並び、見つからない名前とトリガーのない特殊アクションは飛ばす。

## 2. 検証結果

- 単体テスト（`Phase8Step5W1BuilderTests`）: 種別ごとの入力・出力・方針、`Index`、`Source`、順序、フィルタ、未対応の種別、コンストラクターの null 検査。
- 結合テスト（`Phase8Step5W3PublishBindingsTests`）:
  - 適用のたびに 1 回だけ差し替える（Halt あり・なし）。存在しないプロファイルでは呼ばない。範囲外のデバイス（`-1`、`ControllerSlotLimit`、編集スロット、1000）では何もしない。
  - ビルダーが失敗しても適用は成功し、そのデバイスは `Clear`（空）になる（Q5＝A）。
  - 実ホスト: 移行対象が空なら登録簿も空。Button を足すと束縛が入り、適用のたびに新しいリストへ差し替わり（古い参照は壊れない）、外して適用し直すと空に戻る（ロールバック）。
  - ホストがビルダーと適用サービスを、循環なしで解決できる。ソース走査で、差し替えが掃除（`PreallocateForProfileApply`）の後であること、パイプ経路が読込の成功後に呼ぶことを固定した。
- 既存の全テストが期待値を変えずに成功。変えた既存テストは 1 件のみ（`ProfileApplicationService` のコンストラクター引数の数 8 → 10。登録簿とビルダーの遅延解決の追加による）。
- ホットパス: ビルダーと `Replace` は適用時の 1 回だけで、毎フレームの処理には入らない（`Phase8Step4MapCustomBenchmarkTests` の割り当て 0 の検査も成功）。

## 3. 計画との差・判断の記録

1. **`IProfileApplicationService` に `PublishBindings` を追加した**（Q6＝A）。テスト用の偽の実装 3 つ（`AutoProfileServiceTests`、`ProfileApplicationServiceTests`、`RecordingProfileApplicationService`）にも追従した（`RecordingProfileApplicationService` は呼び出しを記録しない空の実装ではなく、`PublishBindingsCalls` に記録する）。
2. **`OnProfileApplied` は接続していない**（Q4＝A）。適用時に抑制の集合を空にするかは、最初の種別の切替（Step7.1）で、特性化テストとあわせて決める。`PublishBindings` に、その旨の TODO を付けた（規約 2.4）。
3. **`ActionBindingBuilder` の各種別の構成の細部は、Step7 で確定する**: Key の `uTrigger`（解除用ボタン）は Step7.2、Macro の同期実行（`synchronized`）・RunOnRelease の細部は Step7.3。クラスの注記に TODO として記した。
4. **`ComboTriggerInputAction` に追加した 2 つのプロパティ**は、テストと、後の Step の診断のための読み取り専用。動作は変えていない。

## 4. 完了条件の確認

- [x] W1〜W3 のビルド・テストが成功。既存テストの期待値は不変（コンストラクター引数の数の 1 件を除く）。
- [x] プロファイルを適用すると、移行対象が空のとき登録簿は空、足すと束縛が入る（実ホストで確認）。
- [x] 掃除の順序が不変。パイプ経路も同じ差し替えを通る。
- [x] 本番の挙動が変わらない。ベンチマークの割り当て 0 が不変。
- [x] 本報告書、`Phase8-Status.md` の更新。

## 5. 次の Step への引き継ぎ・注意

- **Step6（接続・切断との連携）**: 切断時に `registry.Clear`／`engine.OnDeviceDisconnected`。`ClearState`／`ClearPendingRestore` の意味（`OlderProfilePath` の扱い）を調べて接続する（`IProfileSwitcher.ClearState` の TODO を解消）。接続時の紐付け（`IControllerRegistry` の要否）を確定する。コントローラーの接続時の適用は `ProfileSlotApplier` → `ApplyProfileWithoutHalt` → 後始末を通るため、束縛の差し替えは Step5 で済んでいる。
- **Step7.1（Button）**: `MigratedTypes` に `Button` を足す PR。抑制の集合と `OnProfileApplied` の接続（本報告書 §3-2）、Stage3 の TRACE ログのエンジンへの引き継ぎ（Step4 完了報告書 §5）を、その PR で行う。
- **モデル図の更新案**（図の変更は、ユーザーの指示があるまで行わない）: `IActionBindingBuilder`／`ActionBindingBuilder` を点線から実線へ。`ProfileApplicationService` → `IActionRegistry`・`IActionBindingBuilder`（遅延）の依存を足す。
