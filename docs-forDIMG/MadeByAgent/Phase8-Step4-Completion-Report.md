# Phase8-Step4 完了報告書: エンジンと出力の型、`Mapping` への空配線

作成日: 2026-10-19  
対象ブランチ: `For-DI-migration-work`  
計画書: `Phase8-Step4-Plan.md`（Q1〜Q8 を推奨どおり承認）  
結果: **W1〜W5 のビルド・テスト成功（ユーザー実行。DS4Windows.Actions.Tests 970 件成功、StandaloneTests 13 件成功）。移行対象は空のため、本番の挙動は変わらない**

---

## 1. 成果

| Wave | 内容 | ファイル（`DS4Windows/Actions/`） |
|---|---|---|
| W1 | 契約・登録簿・移行スイッチ | `IFrameOutput`、`IFrameOutputAction`、`IHeldOutputAction`、`FrameResult`、`IOutputContext`（`Frame`／`RequestEndFrame` を追加）、`OutputContextImpl`（追従）、`IActionRegistry`（改訂）、`ActionBindingRegistry`、`BindingMigrationOptions`、`ITriggerDispatchEngine` |
| W2 | 毎フレームの出力 | `FrameOutput`（`ResetToDefaultValue` と Stage3 の出力分岐を移した）、`ButtonOutputAction`（`ButtonSpecialActionOutput` と同じ規則） |
| W3 | エンジン | `TriggerDispatchEngine`（`Owns`／`ProcessOwned`／`ProcessTriggers`／`ApplyFrameOutputs`／抑制の集合／`OnProfileApplied`／`OnDeviceDisconnected`。デバイスごとのロック［Q8］、排他グループ［Q6］） |
| W4 | `Mapping` への配線 | `Mapping`: コンストラクターを 11→12 引数に（Q7）、`suppressedTriggerButtons`・`nowAction`・`oldnowAction` を削除（Q3・Q4）、`_triggerInputs`／`_frameOutputs` を追加、旧ループと Stage3 に `Owns` のフック、Stage3 の後に `ApplyFrameOutputs`。DI 登録（`IActionRegistry`、`BindingMigrationOptions`、`ITriggerDispatchEngine`） |
| W5 | 検証 | ベンチマーク、割り当て 0、新旧の出力比較 |

- 本番の挙動: `BindingMigrationOptions.MigratedTypes` が空のため、フックは常に素通し。`OnProfileApplied`／`OnDeviceDisconnected`／`ProcessTriggers`／`Replace` は本番から呼ばれない（TODO に接続予定の Step を明記）。
- 静的な窓口は新設していない。ラチェットの基準値は不変で、一致検査も成功。

## 2. 検証結果

- **既存の特性化テスト**（Phase7c・Step1 ほか）が、期待値を変えずに成功。抑制の集合・遅延の基準時刻の置き換えで、テスト側の変更は、アクセスの経路（エンジン、`Delay`）の追従だけ。
- **新旧の出力の比較（Step7.1 の予行）**: Button の Press／Toggle を、実物の束縛［`ComboTriggerInputAction`＋`ButtonOutputAction`］で新経路に流し、旧経路と**同じ出力の列**になること（`Phase8Step4W4WiringTests`）。新経路のフレームあたりの割り当てが、旧経路以下であること。
- **フックが効くこと**: 偽の束縛を登録した種別は、旧ループを通らずエンジンが 1 回だけ実行し、トリガーのボタンの出力が消え、抑制がエンジンの集合で観測できること。
- **割り当て 0**: エンジンの 1 フレーム（`ProcessTriggers`＋`ApplyFrameOutputs`）、`ProcessOwned`／`Owns`、`FrameOutput`、配線で足した処理のすべて。
- **性能**（開発機。`Phase8Step4MapCustomBenchmarkTests`）:

| 項目 | 結果 |
|---|---|
| `MapCustom` 30 アクション、未操作 | 48.76 µs／フレーム |
| `MapCustom` 30 アクション、押下 | 109.05 µs／フレーム（別の実行で 110.61 µs） |
| 配線で足した処理（入力の更新、`Owns` を 60 回、`ReleaseSuppressed`、`ApplyFrameOutputs`） | **1.275 µs／フレーム ＝ 1.15%（許容 +5% 以内）、割り当て 0** |

  配線の前のコードを別に実行せず、足した処理だけを同じ実行の中で測って割合を出した（W1 の基準値は、出力がコンソールに出ない設定のため取れていなかった）。

## 3. 計画との差・判断の記録

1. **`Owns` の判定**: `MigratedTypes` に含まれる種別の確認を先に行い、空の間はロックを取らずに false を返す（毎フレーム・アクションごとの呼び出しのため）。含まれるときだけ、デバイスごとのロックの下で登録簿の一覧を引く。
2. **排他グループ**: グループの先頭で成立した束縛が、そのフレームでグループを占有する（Fresh-press／実行中のガードで止まっても占有は維持）。後ろの束縛は実行されず、`Stop` も呼ばれない（`BeingTriggered` が残り、両方が離れたときに止まる）。使われるのは Step7.8（Controls タブ）。設計と異なる挙動が必要なら、そのときに見直す。
3. **フレームの区切り**: 旧ループのフックにはフレームの開始の合図がないため、`ApplyFrameOutputs` の終わりを「フレームの終わり」として評価結果の有効期間と排他グループの記録を切る。`Mapping` は毎フレーム `ApplyFrameOutputs` を呼ぶ。
4. **`TriggerInput.Update` の前フレームの状態は null**: 前フレームの表を使う `WasActive` は、Step7.7（MultiAction）まで使われない。そのときに、前フレームの状態の渡し方を決める。
5. **テストの独立性**: `TestMappingFactory.Create` は、独立のエンジン（空の登録簿・移行スイッチ）を作って渡す。ホストの `Mapping` を使うテストは、ホストのエンジンを共有する（`SpecialActionTestScope.Engine`）。
6. **既存テストの競合の是正（今回の変更とは別件）**: `ProfileRepositoryTests` と `ProfileLegacyXmlCompletenessTests` が、共有のアプリデータのパスを並行して書き換え・読みして、まれに一時フォルダーが消えて失敗した。2 クラスを `ServiceProviderHolder` のコレクションに入れて直列に実行するようにした（テストのみの変更）。他にも同じ共有を書き換えるテストがあれば、同じ症状が出うる。

## 4. 完了条件の確認

- [x] W1〜W5 のビルド・テストが成功（ラチェットの一致検査、1 ファイル 1 型のガードも成功）。
- [x] 既存の全テストが、期待値を変えずに成功。
- [x] `suppressedTriggerButtons`、`nowAction`、`oldnowAction` が削除され、エンジンと `input.Delay` に置き換わっている。
- [x] 配線の前後の比較結果の記録（足した処理が 1.15%）。
- [x] 本報告書、`Phase8-Status.md` の更新。

## 5. 次の Step への引き継ぎ・注意

- **Step5（ビルダーと `Replace`）**:
  - `OnProfileApplied` を適用に接続すると、**プロファイル適用のたびに抑制の集合が空になる**（現行の本番には、適用時に集合を空にする処理がない）。挙動の変更になりうるため、Step5 の計画書で、接続の要否と時期を判断する。
  - `IActionBindingBuilder` は、`BindingMigrationOptions` に含まれる種別の束縛だけを作る。`Source`（`ActionInstanceState` のキー）は必須（`Source` が null の束縛は、エンジンが処理しない）。
- **Step7.1（Button）**: ①現行の Stage3 が出す TRACE ログ（立ち上がり・立ち下がり、出力の組み合わせ）を、エンジン側で引き継ぐ（規約 2.3）。②`BindingPolicy` は Press が `respectsFreshPressGuard: false`、Toggle が `true`（予行のテストで確認済み）。③旧コードは Step8 まで残す。
- **Step7.5（Profile）**: エンジンは `UntriggerPolicy.None` の束縛だけを扱う。復帰の合図の移行、`RequestEndFrame` を呼ぶ出力、`OnProfileApplied` の連携は、そこで足す。
- **モデル図の更新案**（図の変更は、ユーザーの指示があるまで行わない）: 点線（Phase8 の目標像）で描いている `ITriggerDispatchEngine`／`TriggerDispatchEngine`、`IActionRegistry`／`ActionBindingRegistry`、`BindingMigrationOptions`、`IFrameOutput`／`FrameOutput`、`ButtonOutputAction` を、実線（実装済み）に変える。`Mapping` → `ITriggerDispatchEngine` の依存を足す。Step4 の完了時点では「未接続」の矢印（Step5・6 で接続）を残す。
- **Step4b**: `LoadProfile`／`SaveProfile` の分解ほか（計画書は Step4 の完了後に作成）。
