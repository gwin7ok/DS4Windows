# Phase7c 完了報告書: Phase8 前の競合の是正・特性化テスト・文書の整合

作成日: 2026-10-19  
対象ブランチ: `For-DI-migration-work`  
計画書: `Phase7c-Plan.md`（Q1〜Q6 を推奨どおり承認）。上位: `Phase7-Completion-Report.md` §5、`Phase7b-Completion-Report.md` §5  
状態: **完了（W2 の一部を Phase8 の Step1 へ移管。実機確認 U-24 は未）**。W1・W2 前半・W3 のビルド・テストビルド・テスト実行は成功（ユーザーが実行）。

---

## 1. 結果の要約

- **W1（Stage3 の列挙競合）: 是正した。** 実機での再発確認は U-24（未）。
- **W2（特性化テスト）: 前半を完了。** 実走行ハーネスと 16 件のテストを追加した。残りの項目は Phase8 の Step1 へ移した（承認済み）。
- **W3（文書とコメントの整合）: 完了。** 規約書とモデル図の変更も承認を得て反映した。
- **W4（K7b-1 の切り分け手順）: 整理した（§4）。** 実機作業はユーザー。Phase8 のブロッカーにはしない。
- ラチェットの基準値（Global 2／AppHost 3／rootHub 0／ServiceProviderHolder 17）は変わらない。

## 2. Wave ごとの実績

### W1: Stage3 の列挙競合の是正
- 原因: `Mapping.cs` の Stage3（Button の特殊アクションの同期処理）が、`GetProfileActionsRaw` の返す生の `List<string>` を直接列挙していた。プロファイルの読込（`LegacyProfileXmlSerializer`）が同じリストを `Clear()`→`Add()` するため、`Collection was modified` になり、そのフレームの Button SA の出力が途中で打ち切られていた（例外は捕まえられ、クラッシュはしない）。`HaltReportingRunAction` は入力レポートの発火だけを止め、`async void` の `MapCustomAction` の実行中のフレームは止めない。
- 是正（Q1＝A）: Stage3 と、プロファイル切替のチェーン処理（`Count` 固定の添字ループ）を、`ToArray()` による複製に変えた。`MapCustomAction` の冒頭と同じ守り方。
- 旧実装との比較: 上流の元の実装には Stage3 がなく、同じ列挙は複製で守られていた。Stage3 は Phase6-Step10-3d で足したもの。**フォークの機能追加で入った不具合**で、DI 移行そのものが原因ではない。
- テスト: 構造ガード `Phase7cW1Stage3ProfileActionsSnapshotGuardTests`（`GetProfileActionsRaw` の呼び出しがすべて複製を伴うこと）。実走行の並行変更テストは W2 に含めた。

### W2: `MapCustomAction` の特性化テスト（前半）
- ハーネス `SpecialActionTestScope`: `MappingCharacterizationScope` の専用デバイスに、特殊アクションを登録して `MapCustom` を実走行させる。`MapCustomAction` は `async void` だが、初期化待ちが完了済みの `Task` のため、最初の実際の `await` までは同期的に走る。
- テスト `Phase7cW2SpecialActionTriggerCharacterizationTests`（16 件）:
  - Button の Press（トリガー成立で出力、トリガーのボタンは消える、離すと出力なし）。
  - トリガーの AND 条件（片方だけでは不成立で素通し）。
  - Button の Press は Fresh-press ガードを見ない（現在の挙動の固定。Toggle の反転だけが見る）。
  - 抑制の追加（構成するすべてのボタン）と、ボタン単位の解除。
  - Fresh-press ガードの解除条件（成立中は維持して OFF タイマーをリセット、最初の未成立フレームは時刻を記録、`FreshPressReleaseHoldMs` 以上の連続で解除、未満では維持）。
  - Profile／Program のブロック（ガード中、`IsExecuting` 中は、実行も `BeingTriggered` の設定もしない）。
  - Key の Press（成立→保持→離すの `BeingTriggered` と `UntriggerIndex` の遷移）、ガード中は発火しない、Toggle は `UntriggerIndex` を登録しない。
  - Stage3 の並行変更への耐性（`Clear()`→`Add()` を繰り返す別スレッドと、3,000 フレームの実走行で、エラーのログが出ないこと）。
- 実行中に判明した問題と対処: `Phase7bTriggerDeadZoneRoundingTests` が `UriFormatException: Invalid port specified` で失敗した。`ProfileSettingsViewModel` のコンストラクターが `pack://application:,,,/...` の URI を作り、WPF の `Application` が作られていないと使えないため。実行順に依存したテストで、Phase7b から潜在していた（`Application` を作る別のテストクラスが先に動いたときだけ通っていた）。`ProfileSettingsViewModelTestFactory` の静的初期化で `Application` を作ることで解消した。
- **Phase8 の Step1 へ移した項目**（承認済み。`Phase8-Unified-Trigger-Dispatch-Plan.md` 2026-10-19 追記 §9）:
  1. `uTrigger`／`delayTime`／`pressRelease` と「Unload on trigger release」の通し復帰。
  2. 一時プロファイルのチェーンの条件。
  3. Controls タブの `shiftTrigger` と `MapCustom` の Key 出力。
  4. `ApplyProfile` 後の掃除の順序。
  5. `ControlTabAndSpecialActionKeyTests.cs` の 2 本の Assert の補強（`MockVirtualKBM` の押下・解放の回数）。
- 理由: 1・2・4 は実際の `ProfileApplicationService` と `Task.Run` を通すため重く、Phase8 の設計と一緒に扱うほうが自然。3・5 は、実際の挙動を確かめながら書く必要がある。

### W3: 文書とコメントの整合
- Phase8 計画書に、2026-10-19 の追記を足した（事実誤認の訂正。設計の決定 D1〜D5 は変更しない）: 「未配線 12 メンバー」の内訳（`IKeyController` の 3 つは未配線ではない）、`IActionRegistry` の実装クラスは存在しない、`KeyOutputAction` は `KeyActionBinding` が生成している、`ProfileActionChainService` は削除済み、行番号・行数、Phase7・7b・7c の影響、孤立コードの §2.4 判断の対象一覧、Step1 で書き足す特性化テスト。
- 12 と 11 の食い違いを、各文書に訂正の注記として追記した（原文は履歴として残した）。
- `Phase6-Status.md`、`DI-App-Wide-Migration-Plan.md`（§6.8、§6.8.2、§6.9.4、§6.10、§9.3）、`Phase7-Status.md`、`Phase7-Residual-Ledger.md`（§17.3 C に `ActionManager.actionInstances`／`actions` を追記）、チェックリスト（`U-18` の重複を `U-26` に改番、U-24 を追加、§12 の U 番号の範囲を訂正）を整合させた。
- 古い TODO コメントを訂正した（動作は変えない）: `Mapping.cs`（ヘッダーと 5 か所）、`App.xaml.cs`（2 か所）、`EnvironmentService.cs`（2 か所）、`PathService.cs`、`IPathService.cs`。`AppPostHostGlobalReferenceGuardTests` が `App.xaml.cs` の「TODO(Phase6-Step7 決定3＝P1)」「決定4＝K」の文字列を要求するため、「旧 TODO(...)（解消済み）」の形で残した。
- 規約書 §3.2 の行数（`ScpUtil.cs` を削除、`Mapping.cs` 約 7,600 行、`ControlService.cs` 約 3,400 行、`BackingStore.cs` 約 1,700 行）とモデル図 03（`IMapping` を 10 メンバーに、`Global` の言及を訂正）を、承認を得て変更した。

## 3. 旧実装との比較と、新たに判明した事項

- Stage3 の競合: フォークの機能追加で入った不具合（上記 W1）。
- Phase7b のテストの実行順への依存: Phase7b で入ったテストの問題（本体の不具合ではない）。
- 静的 `ActionManager.ClearDeviceState` のフォールバック本体は、`IManagedActionManager` の登録により本番では呼ばれない。Phase7b W1 の退避・復元が `DefaultActionManager` と二重になっている。Phase8 の統合時に片方へ寄せる。
- `AppPostHostGlobalReferenceGuardTests.RetainedGlobalReferences_HaveTodoComments` は、存在しない `Global` の TODO を要求している（陳腐化したテスト）。整理は別途判断する。
- 辞書 `profileActionDict` の再読込（`Clear()` してから再代入）は、非原子的なまま。読込側のリスト差し替えは、Phase8 の `TriggerBinding` で解消する（Q1＝A）。

## 4. W4: K7b-1（異常終了）の切り分け手順

方針（Q2＝A）: Phase8 の前のブロッカーにはしない。次の手順は、任意のタイミング、または Phase8 の実機確認中に異常終了が出たときの出発点として使う。

### 4.1 症状と再現手順（`Phase6-Status.md` §6.5 K7b-1 より）
DS4Windows を起動 → コントローラーを Bluetooth で接続 → プロファイル A を適用 → プロファイル一覧からプロファイル B を編集 → 左ペインを Controller Readings に切り替え → 右ペインを Lightbar に切り替え → 色選択画面（`ColorPickerWindow`）を開く。再現する回としない回がある。

### 4.2 切り分けの手順（この順で実施）
1. 着手前のコミット `360cb75b` のビルドで、同じ手順を 10 回ほど試し、再現するかを確認する（再現すれば Phase6-Step7b と無関係と確定）。
2. 左ペインを Controls のままにして、同じ手順を試す（Controller Readings が条件かを確認する）。
3. Controller Readings が条件なら、`24ded790`（入力受信タイムスタンプの計測）の直前のビルドでも試す。
4. それでも絞れない場合は、環境変数 `DOTNET_HeapVerify=1` で起動し、ヒープを壊した直後に検出させる。
5. 現在のビルドで同じ手順を 10 回ほど試し、再現しなければ、リスク受容として記録して閉じる（Phase7b-Plan.md §2 項目 3 の方針と同じ）。

### 4.3 異常終了が出たときの記録（Phase8 の実機確認中を含む）
次の 3 点を、Windows のイベントビューアー（アプリケーション ログ）から記録する。
- 落ちたスレッド（入力スレッドか UI スレッドか）。
- 例外の種類（`IndexOutOfRangeException`、`AccessViolationException`、`coreclr.dll` のアクセス違反など）。
- 直前の操作と、アプリのログ（`Logs`）の末尾。

特殊アクションのトリガー・実行の経路（Phase8 の対象）で落ちたのか、第 1 層（`DS4Device`／`HidDevice`）や WPF の画面で落ちたのかを、これで区別する。

### 4.4 過去の発生記録（参考）
2026-09-26 の 3 回は、落ちた場所・種類が毎回違った（入力スレッドの `IndexOutOfRangeException`［`DS4Device.sendOutputReport`］、`coreclr.dll` のアクセス違反、UI スレッドの `AccessViolationException`）。メモリ破壊が疑われる。候補は、ポインタ操作や非同期 I/O を使う第 1 層で、Phase6 以降も未変更。

## 5. 未完了・持ち越し

- **実機確認 U-24（未）**: 手動のプロファイル切替（メイン画面）を繰り返し、`Synchronous Button SA handling error: Collection was modified` が出ないこと。Button／Key／マクロ／プロファイル切替と復帰が従来どおり動くこと。
- **Phase8 の Step1 へ**: §2 W2 の 5 項目、孤立コードの §2.4 判断記録（`Phase8-Unified-Trigger-Dispatch-Plan.md` 2026-10-19 追記 §8）。
- **K7b-1**: 先送り（手順は §4）。**K9-3**: 先送りのまま（`Phase7b-Plan.md` §9）。
- 陳腐化したテスト `RetainedGlobalReferences_HaveTodoComments` の整理（§3）。
- `Phase7-Status.md` §1 の Step6・Step7 行の記録列が空欄のまま（軽微）。

## 6. 起動順序・スレッド・ガードレールへの影響

- 起動順序: 変更なし。5.2（適用時 Halt）: 変更なし。W1 は読み取り側の複製のみ。
- 安全弁（Fresh-press ガード、`IsExecuting`、一時プロファイル復帰、排他）: 変更なし。W2 で固定するテストを足した。
- ログの出力・レベル: 変更なし。W1 の後は、`Collection was modified` の WARN が出なくなる見込み（U-24 で確認）。

## 7. 次の Step

- Phase8（`Phase8-Unified-Trigger-Dispatch-Plan.md` の D1〜D5）の Step1（詳細監査）へ。着手前に、同計画書の 2026-10-19 追記と、本書 §5 の引き継ぎを取り込む（ユーザーの指示を待つ）。
