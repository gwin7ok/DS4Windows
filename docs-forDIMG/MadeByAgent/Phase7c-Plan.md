# Phase7c 計画書: Phase8 前の競合の是正・特性化テスト・文書の整合

作成日: 2026-10-19  
対象ブランチ: `For-DI-migration-work`  
上位: `Phase7-Completion-Report.md` §5、`Phase7b-Completion-Report.md` §5、`Phase8-Unified-Trigger-Dispatch-Plan.md`  
発端: Phase8 着手前の全体レビュー（2026-10-19。コード健全性・文書整合・テストの穴の並行調査）  
状態: **実施中（2026-10-19）。W1 完了、W2 前半完了［残りは Phase8 の Step1 へ。§8］、W3 実施中**。Q1〜Q6 は推奨どおり承認

---

## 1. 目的と位置づけ

- Phase8（Controls／SpecialActions の統一トリガーディスパッチ）の前に、次の 3 点を済ませる。
  1. Stage3 の列挙競合（`Collection was modified`）の是正（A1）。
  2. `MapCustomAction` の特性化テストの追加（A2）。Phase8 計画書 §6 の最初のリスク対応（Step3/4 の前に特性化テストを用意）の前倒し。
  3. Phase8 計画書ほか文書の事実誤認と古い TODO コメントの訂正（A3）。
- 理由: Phase8 の実機確認で競合が出ると、新経路の不具合と見分けにくい。特殊アクションの「トリガー成立→実行」を走らせるテストが 0 本のため、Phase8 の書き換えが無防備になる。古い前提が残ると、Phase8 の作業者が誤読する。
- 範囲外: Phase8 の本体、先送りした実機確認、K9-3、孤立コードの §2.4 判断（Phase8 の Step1。Q4）、`TriggerBinding` 方式の本格的なリスト差し替え（Q1）、モデル図の変更（指示を得てから）。
- ルール: ビルド・テスト・コミットはユーザーが実行する。既存の不具合の是正は、旧実装と比較し、承認を得て行う（A1 は本計画の承認で足りる）。機能の削除はしない。

## 2. 決定事項（2026-10-19、すべて推奨どおり）

- Q1＝A: 競合の是正は最小案（複製のみ）。本格案（読込側のリスト差し替え）は Phase8 の `TriggerBinding` で解消する。
- Q2＝A: K7b-1 はブロッカーにしない。切り分け手順の整理だけ行う。
- Q3＝A: 古い TODO コメントの訂正を Phase7c（W3）に含める。コメントのみで動作は変えない。
- Q4＝A: 孤立コードの §2.4 判断は Phase8 の Step1 で行う。
- Q5＝A: Phase8 計画書は事実誤認の訂正のみ（追記の形式）。設計の見直しは Step1・Step2。
- Q6＝A: 新しいテストは `DS4WindowsTests`（xunit）に置く。

## 3. Wave 構成

### W1: Stage3 の列挙競合の是正（A1）

- 原因（確認済み）:
  - `Mapping.cs:2673-2682` の Stage3（Button の特殊アクション）が、`ProfileActionProvider.GetProfileActionsRaw`（`ProfileActionProvider.cs:72`）の返す生の `List<string>` を `foreach` で列挙する。
  - `LegacyProfileXmlSerializer.cs:2118-2127` が、プロファイルの読込のたびに同じリストを `Clear()` → `Add()` する。ロックなし。
  - `HaltReportingRunAction` が止めるのは入力レポートの発火だけで、`async void` の `MapCustomAction` の実行中のフレームは止めない。
  - `Mapping.cs:4255` は `ToArray()` で複製して守っている（コメントに理由あり）。Stage3 にだけ複製がない。
- 旧実装との比較: 上流の元の実装には Stage3 がなく、同じ列挙は複製で守られていた。Stage3 は Phase6-Step10-3d で足された。**フォークの機能追加で入った不具合**で、DI 移行そのものではない。
- 影響: 例外は `Mapping.cs:2823` で捕まるためクラッシュはしない。ただし、そのフレームの Button SA の出力が途中で打ち切られる（Press 型で 1 フレーム分の誤った解放になりうる）。
- 是正（Q1＝A）:
  - Stage3 の列挙を、`ToArray()` による複製に変える（`Mapping.cs:4255` と同じ形）。
  - `Mapping.cs:4653-4660` の `Count` 固定の添字ループも、複製に変える（縮むと範囲外例外になりうる）。
  - ログ・ログレベル・出力の順序は変えない。
- 回帰テスト: 読込側（`Clear()`／`Add()`）のスレッドと Stage3 の実走行を並行させ、`Collection was modified` の WARN が出ないことを固定する。修正前に赤になる形で書く（W2 のハーネスを使うため、W2 の土台の後に追加してよい）。
- 影響する基準値: ラチェット（`Global.*`／`AppHost`／`rootHub`／`ServiceProviderHolder`）は変わらない見込み。

### W2: `MapCustomAction` の特性化テスト（A2。本体は変更しない）

- 土台: `DS4WindowsTests` の `TestMappingFactory`、`MappingCharacterizationScope`、`RecordingVirtualKbmBase` などを使い、`DS4State` を数フレーム流す最小の実走行ハーネスを用意する。
- 追加するテスト（優先順）:
  1. Key SA: 立ち上がりで `OnTrigger` が 1 回だけ、保持中は再発火せず、離すと `OnRelease`。
  2. 複数ボタンの AND 条件（片方だけでは不成立）。抑制セットへの追加（`Mapping.cs:4455-4465`）と解除（`Mapping.cs:4258-4277`）。
  3. Fresh-press ガードの実走行（79ms では不成立のまま、80ms 以上で解除、その後の新規成立で初めて実行）。`Mapping.cs:4423-4446`。
  4. `IsExecuting` の再入防止（Profile／Program 分岐。`finally` で false に戻ること）。`Mapping.cs:4514-4551`、`4564-4671`。
  5. `uTrigger`／`delayTime`／`pressRelease` と、「Unload on trigger release」の通し復帰（`Mapping.cs:5361`、`ClearDeviceState` をまたぐ）。
  6. 一時プロファイルのチェーンの条件（`Mapping.cs:4650-4660`、`uTrigger.Count == 0 && !automaticUntrigger`、`controls` 一致）をモックの `IProfileSwitcher` で固定。
  7. Controls タブの Key 出力（Press／Toggle）と `shiftTrigger`（`Mapping.cs:2216`、`3759`、`3776`）。`Phase7Step7MapCustomCharacterizationTests.cs` の拡張。
  8. `ApplyProfile` 後の掃除の順序（`ProfileApplicationService.cs:259-265`: `ClearAllEntries` → `ClearForDevice` → `ClearDeviceState` → `PreallocateForProfileApply`）。
- 既存テストの補強: `ControlTabAndSpecialActionKeyTests.cs:13`、`:53` に Assert を足す（押下・解放回数）。
- 規約: 1 ファイル 1 型、ファイル名＝型名。時間に依存するテストは、閾値の前後を押さえる形にして、不安定にしない。

### W3: 文書とコメントの整合（A3。Q3・Q5）

- Phase8 計画書（追記の形式。既存の決定事項・記述は消さない）:
  - 「未配線 12 メンバー」の訂正: `IKeyController` の 3 つ（`ClearAll`／`OnDown`／`OnUp`）は `KeyButtonActionController.cs:109` の private 入れ子インターフェースで、`:57`、`:99`、`:411`、`:422`、`:451` で呼ばれており、未配線ではない。実際の 0 件は `IActionBinding` の 3 つ、`IActionRegistry.GetBindingsForDevice`、`IControllerRegistry.GetControllersForDevice`、`IInputAction.Evaluate`、`ITrigger.Context`、`IOutputHandler.FakeKeyRepeat`。`IMacroPlayer.IsPlaying` はテスト以外の呼び出しがない。12／11 の食い違い（`IOutputHandler` の有無）の整理。
  - `IActionRegistry` は静的クラスが実装しているのではなく、**実装クラスが存在しない**（`ActionRegistry` は別物）。
  - `KeyOutputAction` は「呼び出し元ゼロ」ではなく、`KeyActionBinding.cs:23` が生成している（`Execute`／`Stop` が呼ばれない）。`KeyInputAction` が `IInputAction` を実装している（`Evaluate` は null を返すだけ）。
  - `ProfileActionChainService` は削除済み。安全弁は Fresh-press ガードと `IsExecuting`（`.github/copilot-instructions.md` §2.2）。チェーンの処理は `Mapping.cs:4650-4660` にインライン。
  - 位置情報: `CheckForSpecialActionSuppression` は `Mapping.cs:6711`、`Mapping.cs` は 7,602 行、`DS4ControlSettings` は `DS4ControlSettings.cs:52`（`ScpUtil.cs` は存在しない）。
  - 着手条件: Phase7・Phase7b・Phase7c の完了後。
- Phase7 関連の整合: 「12」と「11」の統一（`Phase7-Status.md`、`Phase7-Completion-Report.md` §5.2、`DI-App-Wide-Migration-Plan.md` §6.9.4 など）。
- `Phase6-Status.md`: 冒頭の状態と進捗表を、Phase6 完了（2026-10-17）に合わせる。
- `DI-App-Wide-Migration-Plan.md`: §6.8 の「進行中」、§6.8.2 の項目 5〜7、§6.9.4 と §9.3 の Phase7b 是正済み項目（Unload、デッドゾーン）、K4-1／K7-2／K7b-1／K9-3 の記述。
- `Phase7-Status.md`: §3 の 10e の「残りは完了報告書の作成」、§6 項目 4 の U-22 の欠落、§1 の記録列の空欄。
- `Phase7-Residual-Ledger.md`: §17.3 C に `ActionManager.actionInstances`／`actions`（静的な可変辞書）を追記。
- `Phase6-Step11-RealDevice-Verification-Checklist.md`: U-23 の後ろの空の `U-18` 行を `U-26` に改める（実施済み）。§12 の U 番号の範囲（U-11〜U-21／U-14〜U-22）の整理。先送り項目の結果欄への反映。T-14 の連続使用時間の記述（30 分／短縮版／10 分）の整理。
- 古い TODO コメントの訂正（動作は変えない。既存コメントの設計意図は消さない）:
  - `Mapping.cs:34`、`5744`、`5934`、`5957`、`5973`、`6559`
  - `App.xaml.cs:139`、`702-704`
  - `EnvironmentService.cs:28`、`99`
  - `PathService.cs:19`
  - `IPathService.cs:32`
- `.github/copilot-instructions.md` §3.2 の行数（`ScpUtil.cs` は存在しない）: 規約書の変更のため、**案を提示して承認を得てから**行う。
- モデル図 03（`IMapping` の本体の 8 → 10 メンバー、`Global` の言及）: **案の提示にとどめ、指示を得てから変更する**。

### W4: K7b-1 の切り分け手順の整理（Q2＝A。任意・実機作業はユーザー）

- Phase8 の前のブロッカーにはしない。次の手順を `Phase6-Status.md` §6.5 から転記して、Phase7c の完了報告書に載せる。
  1. 着手前のビルド `360cb75b` で、同じ手順を 10 回ほど試す。
  2. 左ペインを Controls のままにして、同じ手順を試す。
  3. `24ded790` の直前のビルドでも試す。
  4. 環境変数 `DOTNET_HeapVerify=1` で起動する。
- Phase8 の実機確認中に異常終了が出た場合の記録方法（Windows イベントログの 3 項目: 落ちたスレッド、例外の種類、`coreclr.dll` の有無）を併記する。

### W5: 完了報告書

- `Phase7c-Completion-Report.md`、`Phase7-Status.md` への Phase7c の追記、チェックリストの U-24（Stage3 の競合の実機確認）の追加。

## 4. 起動順序・スレッド・ガードレールへの影響

- 起動順序: 変更なし。
- 5.2（適用時 Halt）: 変更なし。W1 は読み取り側の複製のみ。
- 安全弁（Fresh-press ガード、`IsExecuting`、一時プロファイル復帰、排他）: 変更なし。W2 はそれらを固定するテストを足す。
- ログ: 出力・レベルは変えない。W1 の修正後は、`Synchronous Button SA handling error: Collection was modified` が出なくなる見込み。

## 5. リスクと対応

- 複製による Stage3 の 1 フレーム分の遅れや、割り当ての増加（毎ティック）: 小さなリスト（プロファイルのアクション名）の `ToArray()` で、`Mapping.cs:4255` が既に毎ティック行っているのと同じ。許容する。
- W2 の時間依存テストが不安定になる: 閾値の前後を押さえる形にし、時刻を差し替えられない場合は、状態の設定で閾値を再現する。再現できないものは、Phase8 の実機確認の項目に回す。
- W3 の文書訂正が、決定済みの Phase8 の D1〜D5 に触れる: 追記の形式にして、決定事項は書き換えない。

## 6. 完了条件

- [ ] W1: Stage3 と `Mapping.cs:4653-4660` が複製を使う。並行変更の回帰テストが成功する。
- [ ] W2: 特性化テスト 8 項目と既存テストの Assert の補強が成功する。ガードテスト（複数型ファイル 0、ラチェットの一致検査）も成功する。
- [ ] W3: 文書とコメントの訂正が済む。規約書とモデル図は、案の提示と承認を経る。
- [ ] ビルド警告 0、全テスト成功（ユーザーが実行）。
- [ ] 実機確認: 手動のプロファイル切替を繰り返し、`Collection was modified` が出ないこと。特殊アクションの通常動作（Key、Button、マクロ、プロファイル切替と復帰）が変わらないこと。
- [ ] W5: 完了報告書。

## 7. 次の Step

- Phase7c の完了後、Phase8 の Step1（詳細監査。孤立コードの §2.4 判断記録を含む）へ進む。

---

## 8. 進捗と計画の変更（2026-10-19 追記）

- **W1: 完了**（ビルド・テストビルド・テスト実行は成功。ユーザーが実行）。`Mapping.cs` の Stage3 と、プロファイル切替のチェーン処理を `ToArray()` による複製に変えた。構造ガード `Phase7cW1Stage3ProfileActionsSnapshotGuardTests`。
- **W2: 前半を完了**（同上）。実走行ハーネス `SpecialActionTestScope`、特性化テスト `Phase7cW2SpecialActionTriggerCharacterizationTests`（16 件）。
  - 固定済み: Button の Press の出力、トリガーの AND 条件、抑制の追加と解除、Fresh-press ガードの解除条件、Profile／Program のブロック、Key の Press／Toggle の状態遷移、Stage3 の並行変更への耐性。
  - 実行中に判明した、テストの実行順への依存（`Phase7bTriggerDeadZoneRoundingTests` が `pack://` の URI で失敗）は、`ProfileSettingsViewModelTestFactory` の静的初期化で `Application` を作ることで解消した。
- **計画の変更（ユーザー承認、2026-10-19）**: W2 の次の 4 項目は、実際の `ProfileApplicationService` と `Task.Run` を通すため重く、Phase8 の設計と一緒に扱うほうが自然なので、**Phase8 の Step1（詳細監査）へ移す**。
  1. `uTrigger`／`delayTime`／`pressRelease` と、「Unload on trigger release」の通し復帰（`Mapping.cs:5361`、`ClearDeviceState` をまたぐ）。
  2. 一時プロファイルのチェーンの条件（`Mapping.cs:4650-4660` 付近、`uTrigger.Count == 0 && !automaticUntrigger`、`controls` 一致）。
  3. Controls タブの `shiftTrigger`（`Mapping.cs:2216`、`3759`、`3776`）と、`MapCustom` の Key 出力。
  4. `ApplyProfile` 後の掃除の順序（`ProfileApplicationService.cs:259-265`）。
  - あわせて、`ControlTabAndSpecialActionKeyTests.cs` の 2 本の Assert の補強（`MockVirtualKBM` の押下・解放の回数）も、実際の挙動を確かめながら行う必要があるため、Phase8 の Step1 へ移す。
  - Phase8 の Step1 は、これらを特性化テストとして書き足してから、Step3 以降に進む（Phase8 計画書 §6 の最初のリスク対応と同じ扱い）。
- **W3: 実施中**。完了条件（§6）の W2 の項目は、上記の変更に合わせて「前半の 16 件が成功」とする。
