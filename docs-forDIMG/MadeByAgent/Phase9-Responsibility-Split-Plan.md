# Phase9 計画書: 巨大クラスの責務分割（1 クラス 1 責務）

作成日: 2026-10-19  
対象ブランチ: `For-DI-migration-work`  
上位計画書: `docs-forDIMG/DI-App-Wide-Migration-Plan.md`（本書を Phase9 として追加する）  
関連: `Phase8-Unified-Trigger-Dispatch-Plan.md`（2026-10-19 追記［実行順序の改訂］）、`docs-forDIMG/Model-Diagram/01-Component-Dependency-Diagram.md` §2（解体の検証マトリクス）  
状態: **計画確定（Q1〜Q6 を推奨どおり承認、2026-10-19）。Phase8・Phase10 は完了。Phase9-0 を 2026-10-22 に実施。9-A(1)・9-A(2) は完了し、ステアリング＋vJoy の削除も W1〜W5 を実施済み（W2〜W4 は実機確認待ち）。`Mapping.cs` は実測 2,568 行（§11）。次の候補は 9-B（設計を説明し、承認を得てから着手）**

---

## 1. 目的と位置づけ

- 「1 ファイル 1 型」（規約 §3.3 原則 1）は、全 `.cs` で達成済み（Phase6-Step10b）。残る課題は**「1 クラス 1 責務」**と、**巨大な単一メソッド**である。
- 上位ファイルの調査（2026-10-19）: `Mapping.cs` 7,611 行（メンバー約 80、`SetCurveAndDeadzone` 1,194 行ほか）、`LegacyProfileXmlSerializer.cs` 3,093 行（`LoadProfile` 2,323 行）、`ControlService.cs` 3,436 行、`ProfileSettingsViewModel.cs` 3,652 行（公開プロパティ約 258）、`ProfileEditor.xaml.cs` 2,522 行、`MainWindow.xaml.cs` 2,253 行、`DS4Device.cs` 2,040 行（`performDs4Input` 511 行）、`Mouse.cs` 1,960 行、`JoyConDevice.cs` 1,944 行（`ReadInput` 486 行）ほか。
- 目標像はモデル図 01 §2 の「解体の検証マトリクス」（`IInputMappingPipeline` と各 Processor、`IMouseEngine`、`InputLoopCoordinator`、`ILightbarService`、サブ ViewModel、`IHidTransport` とパーサーほか）。ただし、**本 Phase は同図の名前をそのまま作ることを目的にしない**。責務の境界を、実コードの分かれ目に沿って決め、同図は実装結果に合わせて更新する（Phase8 の Step2 で整理した「実在／未実装」の区別を使う）。
- 範囲外: 特殊アクションの統一ディスパッチ（Phase8）、`LoadProfile`／`SaveProfile` の分解（Phase8 の Step4b）、機能の追加・削除、保存形式の変更。

## 2. 前提と原則

- **着手の前提**: Phase8 の完了（Step9 の総合検証が GO）。`Mapping.cs` から旧ディスパッチ約 2,000 行が消えた後の姿を出発点にする（行数・メンバーは、着手時に再計測する）。
- **特性化テストが先**: 各領域の分割の前に、現在の入力 → 出力を固定するテストを足す（「値の特性化」）。Phase7 までに `Mapping` に対して存在するのは、引数の受け渡しとホットパスの割り当てを見るテスト（`MappingArgumentPassThroughTests`、`MappingHotPathAllocationTests`）が中心で、`SetCurveAndDeadzone`・`getMouseMapping` などの**値の入出力を固定するテストはない**。
- **ホットパスの制約**: `MapCustom`／`SetCurveAndDeadzone` は毎秒 250〜1000 回呼ばれる。分割後も、毎フレームの割り当て 0（`MappingHotPathAllocationTests`）と、既存の速度特性を維持する。インターフェース越しの仮想呼び出しの増加は許容するが、新しいオブジェクトの `new` は禁止する。
- **Strangler Fig**: 全体を再生成せず、メソッド単位で置換する（規約 §3.2）。1 Wave ＝ 1 クラスの切り出しとし、毎回、ビルド・テストを通す。
- **規約**: 1 ファイル 1 型、ファイル名＝型名、配置は §3.4（R1〜R7）、名前空間は §3.3 原則 3（既存の名前空間を変更しない）。切り出しで増えたファイルに、ラチェット（`Global.*`／`AppHost`／`Program.rootHub`／`ServiceProviderHolder`）の基準値を引き継ぐ（合計を増やさない）。
- **DI**: 新しいクラスは、コンストラクター注入（Pure DI）。静的な窓口を新設しない（規約 §2.1）。状態を持つものは、`SyntheticInputState`／`MacroRuntimeState`／`StickMotionState` と同じく保持クラスに分ける。
- **挙動を変えない**: 機能・ログ・安全弁を維持する（規約 §2.2・§2.3）。不具合を見つけた場合は、旧実装と比較し、承認を得てから別途是正する。

## 3. 対象と Wave 構成（案）

各領域は独立しており、順序は入れ替えられる。推奨は、リスクの小さい順（9-A → 9-B ほか）。

### 9-A: `Mapping` のスティック・トリガー・ジャイロの補正（約 2,600 行）
- 対象: `SetCurveAndDeadzone`（1,194 行）、`ApplyStickCalibration`、`CalcStickAxisFuzz`、`CalcAntiSnapbackStick`、`ProcessTwoStageTrigger`（196 行。**`MapCustom` の流れにあるため、MapCustom の分割 Wave で扱う**）、`ProcessFlickStick`／`HandleFlickStickAngle`／`WarpEaseOut`。
- **削除済み（2026-10-22。ステアリング機能ごと廃止。`Phase9-SteeringWheel-Removal-Plan.md`）**: ~~`CalcWheelFuzz`、`Scale360degreeGyroAxis`（253 行）、`SAWheelEmulationCalibration`、`CalculateControllerAngle`~~（ステアリング較正）。切り出し先の案の「ステアリング較正」も不要になった。
- 切り出し先の案: スティック補正、トリガー補正（2 段階含む）、ジャイロ→コントロール、フリックスティック、ステアリング較正。状態は `StickMotionState` に既にある。
- 前提: 値の特性化テスト（デッドゾーン［Radial／Axial］、アンチデッドゾーン、カーブ、ドリフト補正、2 段階トリガー、フリックの境界値）。Phase7b で是正した丸め（`Math.Round`）の挙動も固定する。

### 9-B: `Mapping` のマウス・ホイールへの変換と、入力の解決（約 1,400 行）
- 対象: `getMouseMapping`（318 行）、`calculateFinalMouseMovement`、`GetMouseWheelMapping`、`TempMouseJoystick`、`GetByteMapping`、`GetBoolMapping`、`GetXYAxisMapping`、`getBool*Mapping` 群、`GetBoolMappingExternal`。
- 前提: 値の特性化テスト。特殊アクション側の `getBoolSpecialActionMapping`／`GetBoolActionMapping` は、Phase8 の後の姿を見て扱う。

### 9-C: マクロの再生（約 360 行。2026-10-21 に再計測）
- **`Phase8-Fix-MacroRepeatUnify`（2026-10-21）の影響**: 死んだ経路（`macrodone`、`DispatchInFlight`、`EndMacro` の 3 オーバーロード）は削除済み、`PlayMacroTask` の重複は `RunMacroOnce` に統合済み、`HoldMacro` は `RepeatMacro` に統合済み。K3 の暫定対策も置き換え済みのため、**「K3 の恒久的な再設計」は 9-C から外れる**。`EndMacroDirect` は空実装で、TODO コメントを残してある。
- 対象: `PlayMacro`（`Mapping.cs` 2964 行付近）、`PlayMacroTask`、`RunMacroOnce`、`PlayMacroCodeValue`（約 160 行）、`EndMacroDirect`、`RunLightbarMacro`。規模は約 700 行から約 360 行に縮小。
- 注意: Phase8 の後の `IMacroPlayer`／`MacroAction` との境界を確認してから（Phase8 の Step7 で Macro 種別を切り替える際に、`PlayMacro` の呼び出しがどう変わるかに依存する）。

### 9-D: `Mapping` の合成入力の確定と Controls タブのボタン割り当て（**再計測で、ほぼ消滅。廃止を提案**）
- 2026-10-21 の再計測: `ProcessControlSettingAction` は 405 行 → 約 30 行、`ShiftTrigger` は廃止済み。残るのは `Commit`（約 176 行）と `MapCustom`（約 505 行）。9-A〜9-C の各 Wave に吸収し、9-D 自体は廃止する案（Q7）。
- 旧計画の対象: `Commit`（176 行）と `SyntheticState` まわり、`MapCustom` の Controls タブ側、`ProcessControlSettingAction`（405 行）、`ShiftTrigger`。
- 注意: Phase8 は Controls タブのトリガー部分（Step3 以降で `ComboTriggerInputAction` に統一）を扱う。残る「ボタン → 出力の割り当て」部分を対象にする。

### 9-E: `ControlService` の責務分割（約 3,400 行）
- 切り出しの案（Wave を分ける）: OSC（`CreateOSCCallback`、`OSCPre/PostMappingStep`、`CompareAndSendChangesToOSC`、`ChangeOSC*Status`、約 500 行）、UDP モーション（`ChangeUDPStatus`、`PrepareDevUDPMotion`、スムージング、約 250 行）、HidHide（`CheckHidHidePresence`、`Update*Attributes`、約 150 行）、仮想パッドの出力デバイス（`EstablishOutFeedback` 228 行、`PluginOutDev`、`UnplugOutDev` ほか、約 600 行。`OutputSlotService` へ寄せる。過渡期の逆依存の解消を含む）、レポート処理（`On_Report` 255 行）と起動・停止・ホットプラグ（`Start` 174 行、`Stop`、`HotPlug`）。
- 前提: OSC は競合の回帰テストのみで、送信内容を固定するテストがない。UDP・HidHide・出力デバイスは、実機が要る箇所が多い。ガードレール（6 大ガードレール。同一 XML 排他、適用時 Halt、スレッド直列化、On-Demand パス評価、ドライバ破棄順序、切断時クリーンアップ）を確実に守る。

### 9-F: アプリ層・UI のコードビハインド
- **順序の注記（2026-10-21 改訂）**: `Phase10-Assignment-UI-Unification-Plan.md` は、範囲を「マクロだけ」から「割り当て編集画面の統合」（共通の編集画面、`BindingWindow` と `SpecialActionEditor` の置き換え）に拡大した。`RecordBox`・`SpecialActionEditor`・`BindingWindow`・`ProfileEditor` のコードビハインドを大きく作り直すため、**Phase10 全体を 9-F の前に行う**（二重の変更を避ける）。9-G も Phase10 の後に判断する。
- 対象: `App.xaml.cs` の `Application_Startup`（393 行）と `CheckOptions`（136 行）→ 起動手順のクラス（Phase7-Step4 の起動順序・終了順序を維持）。`MainWindow` の `WndProc`（265 行）、電源イベント、コンストラクター（151 行・引数 18）。`ProfileEditor` の残りのイベントハンドラー（`Phase8` の Step4b で表を出した後）。
- 前提: 起動順序の特性化（ホスト構築、ブートストラップ、設定の復元の順序）。実機（画面）での確認が必須。

### 9-G: `ProfileSettingsViewModel` のサブ ViewModel 化（任意）
- 対象: 公開プロパティ約 258。モデル図 01 のサブ ViewModel（スティック、トリガー、ボタン、特殊アクション）への分割。
- リスク: XAML の `DataContext` の参照パスを多数書き換える必要があり、実機（画面）での全面的な確認が要る。効果とコストを比べて、**実施するかどうかを 9-A〜9-F の後に判断する**（Q3）。

### 9-H: 第 1 層のデバイスクラス（条件付き）
- 対象: `DS4Device.performDs4Input`（511 行）、`JoyConDevice.ReadInput`（486 行）、`DualSenseDevice`、`SwitchProDevice`。通信（`IHidTransport`）、解析（パーサー）、出力の組み立て（エンコーダー）の分離。
- 条件: **K7b-1（異常終了。メモリ破壊の疑い）の切り分けが済むこと**。ポインタ操作と非同期 I/O を含む領域のため、原因が不明なまま構造を変えると、不具合の帰属が不明になる。実機がないとテストできない。

### 9-I: 最終クリーンアップ
- モデル図 01〜04 の更新（実在／未実装の区別、層の依存の向きの確認）。
- 規約 §3.3 原則 3（名前空間＝フォルダ）と §3.4 の最終目標（層別のフォルダ構成への再編。`DI/` は Composition Root 専用）。ただし、これは差分が大きいため、別の Phase にする案を検討する（Q4）。
- 完了報告書。

## 4. 決定してほしい点（質問と推奨）

- **Q1（着手順）**: 9-A（補正）→ 9-B（マウス・入力の解決）→ 9-E（`ControlService`）→ 9-C（マクロ）→ 9-D → 9-F の順（推奨）。理由: 9-A・9-B は純粋な計算で、値の特性化テストが作りやすく、実機の影響が小さい。9-C は Phase8 の Macro 種別の切替の結果に依存する。9-F は画面の実機確認が要る。
- **Q2（Wave の粒度）**: 1 クラスの切り出し＝1 Wave（推奨）。各 Wave の前に、対象の値の特性化テストを足す。まとめて切り出さない。
- **Q3（9-G のサブ ViewModel 化）**: 9-A〜9-F の後に、効果とコストを比べて判断する（推奨）。先に決めない。
- **Q4（9-I の名前空間・フォルダ再編）**: 別の Phase（Phase10）に分ける（推奨）。本 Phase の範囲を責務分割に絞る。
- **Q5（9-H の扱い）**: K7b-1 の切り分けが済むまでは、着手しない（推奨）。切り分けの結果によっては、別の計画書にする。
- **Q6（ホットパス性能の検証）**: 毎フレームの割り当て 0 のテスト（既存）に加え、各 Wave の前後で、1 フレームあたりの処理時間を簡易に計測して比較する（推奨）。許容範囲（例: 悪化 5% 以内）をユーザーが決める。

## 5. リスクと対応

- 値の特性化テストが、現行の「意図しない挙動」を固定する: 意図が不明なものは、仕様台帳に「現状の挙動」と書く（Phase8 の Step1 と同じ運用）。
- ホットパスの性能悪化・割り当て: 構造体・事前確保のバッファを使い、`new` を避ける。Wave ごとに計測する。
- 切り出しで、静的な窓口や循環参照が再発する: コンストラクター注入に統一し、ラチェットの一致検査で守る。
- 実機でしか確認できない領域（デバイス、画面、起動）: 実機確認の台帳（`Phase6-Step11-RealDevice-Verification-Checklist.md` §11）に、Wave ごとの確認項目を追加する。
- 大規模な差分で、他の作業と衝突する: Phase8 の完了後に着手し、Phase9 の間は、機能追加を並行しない。

## 6. 完了条件

- [ ] 9-A〜9-F（および実施を決めた 9-G・9-H）の各領域で、値の特性化テストが成功し、切り出しの前後で入力 → 出力が一致する。
- [ ] 巨大メソッドの縮小（目安: `SetCurveAndDeadzone`、`getMouseMapping`、`ProcessControlSettingAction`、`PlayMacroTask`、`On_Report`、`Application_Startup` がそれぞれ 200 行以下。計画書で各 Wave の目標を決める）。
- [ ] `Mapping.cs`、`ControlService.cs` の行数が、着手時の半分以下（目安。計画書で確定）。
- [ ] ビルド警告 0、全テスト成功。ラチェットの一致検査が成功する。複数型ファイル 0。
- [ ] 実機確認（台帳に追加した項目）の合格、または先送りの記録。
- [ ] モデル図 01〜04 の更新。完了報告書。

## 7. 次の Step

- Phase8 の完了後、Phase9-0（着手時の再計測と、Wave ごとの個別計画書の作成）から着手する。

---

## 8. 決定事項（2026-10-19、すべて推奨どおり承認）

- Q1＝A: 着手順は 9-A → 9-B → 9-E → 9-C → 9-D → 9-F。
- Q2＝A: 1 クラスの切り出し＝1 Wave。各 Wave の前に値の特性化テストを足す。
- Q3＝A: 9-G（サブ ViewModel 化）は、9-A〜9-F の後に、効果とコストを比べて判断する。
- Q4＝A: 名前空間・フォルダの再編（9-I の一部）は別の Phase（Phase10）にする。
- Q5＝A: 9-H（第 1 層のデバイスクラス）は、K7b-1 の切り分けが済むまで着手しない。
- Q6＝A: 各 Wave の前後で、1 フレームあたりの処理時間を簡易に計測して比較する。許容範囲の初期値は「悪化 5% 以内」とし、Phase9-0 で確定する。

## 10. 再計測と前後関係の改訂（2026-10-21 追記、未承認の提案）

- **`Mapping.cs`**: 4,626 行 → **4,406 行**（`Phase8-Fix-MacroRepeatUnify` で約 220 行減）。9-A（約 2,100 行）、9-B（約 600 行）の対象は、行数がほぼ変わらない。9-C は約 360 行に縮小（上記）。
- **着手順の提案**: 9-A（4 Wave に分割）→ 9-B → 9-C → 9-E → **Phase10（割り当て編集画面の統合）** → 9-F → 9-G（判断）→ 9-I。9-D は廃止して吸収する。9-H は K7b-1 の切り分け後。
- **Phase10 の名前空間・フォルダ再編との衝突**: Q4 は再編を「Phase10」に回すとしたが、Phase10 は割り当て編集画面の統合になった。再編は **Phase11** と読み替える。
- **決定事項（2026-10-21、ユーザー承認）**:
  - **Q7＝廃止して吸収**: 9-D を廃止し、残りの `Commit`／`MapCustom` は 9-A〜9-C の Wave で必要な分を切り出す。
  - **Q8＝Phase10 を最初に**: **Phase10（割り当て編集画面の統合）を、Phase9 のどの Wave よりも先に行う**。着手順は Phase10 → 9-A → 9-B → 9-C → 9-E → 9-F → 9-G（判断）→ 9-I（上の「着手順の提案」を置き換える）。9-F・9-G は、Phase10 で作り直した画面を前提に行う。
  - **Q9＝2 Wave**: 9-A を、(1) スティック・トリガー補正、(2) ジャイロ・フリックスティック・ステアリング較正の 2 Wave に分ける。
  - **Q10＝Phase11 に読み替え**: Q4 の名前空間・フォルダ再編は Phase11。
- 前提の注意: Phase10 は `Mapping` の補正・マウス変換の内部を変えないため、9-A の特性化テストは Phase10 の後に作っても、挙動の基準は変わらない。ただし Phase10 で `ControlBindingBuilder`／`ActionBindingBuilder` を拡張するため、9-B（マウス変換）の特性化テストは、その後の姿に対して作る。

## 9. Phase8 からの持ち越し（2026-10-20 追記）

`Phase8-Completion-Report.md` §3 の C-1〜C-9。特に、着手前に小さな PR として行うことを推奨するもの: **C-2（テストの `migrated` 引数の整理）**。9-I（最終クリーンアップ）の候補: C-1（`Capture/RestoreProfileUntrigger` ほか）、C-4（廃止機能の通知部品の共通化、未使用の文言）。`Mapping.cs` は Phase8 で 4,626 行になったため、§3 の 9-A〜9-D の行数は、着手時に測り直すこと。

## 11. Phase9 の進捗（2026-10-22 追記）

- **9-A(1)（スティック・トリガー補正）: 完了**（`Phase9-A1-Plan.md`）。**9-A(2)（ジャイロ→コントロール、スティック前処理、フリックスティック）: 完了**（`Phase9-A2-Plan.md`）。
- **ステアリング（SA Steering Wheel Emulation）と vJoy の削除: W1〜W5 実施済み**（W1 は実機確認も合格。W2〜W4 は実機確認待ち）。ステアリング較正の 4 件は、9-A の対象から外れた（上記）。
- **`Mapping.cs` の行数（2026-10-22 実測）: 2,568 行**（Phase9 の着手時 4,430 行。9-A の Wave 後に 3,072 行、ステアリング削除の W2 で 2,568 行）。
- **2 段階トリガー（`ProcessTwoStageTrigger`）は 9-A では扱わず、`MapCustom` の分割 Wave で扱う**（9-A(1) の調査で判明。`SetCurveAndDeadzone` ではなく `MapCustom` の流れにあるため）。
- **次の候補: 9-B**（マウス・ホイール変換と入力の解決: `getMouseMapping` ほか）。着手の前に、設計を説明して承認を得る。
- **9-B（マウス・ホイール変換と入力の解決）: 実装済み（2026-10-22。ビルド・テストはユーザーの実行待ち）**（`Phase9-B-Plan.md`）。`Mapping.cs` は 2,568 → 2,060 行。マウス加速と移動の端数は、デバイスごとに持つ（Q2。挙動の変更）。`getMouseMapping` 相当の 8 方向の繰り返しの畳み込み（Q5）も実装済み（`Calculate` は約 141 行）。残りは実機確認（台帳 §31）。
- **9-C（マクロの再生）: 実装済み（2026-10-22。ビルド・テストはユーザーの実行待ち）**（`Phase9-C-Plan.md`）。`Mapping.cs` は 2,060 → 1,686 行。マクロが押すゲームパッドのボタンのフラグと個数は、デバイスごとに持つ（Q2。挙動の変更）。押下・離上の switch の畳み込み（Q4）も実装済み。残りは実機確認（台帳 §32）。
- **9-E（`ControlService` の責務分割）: E-1（OSC）実装済み（2026-10-22。ビルド・テストはユーザーの実行待ち）**（`Phase9-E-Plan.md`）。`ControlService.cs` は 3,438 → 3,028 行。次は E-2（HidHide）・E-3（UDP モーション）。仮想パッドの出力・入力レポートの処理・ライフサイクルは、別の計画書で設計する。
- **9-E E-2（HidHide）: 実装済み（2026-10-22。ビルド・テストはユーザーの実行待ち）**。`ControlService.cs` は 3,028 → 2,914 行。次は E-3（UDP モーション）。
- **9-E E-3（UDP モーション）: 実装済み（2026-10-22。ビルド・テストはユーザーの実行待ち）**。`ControlService.cs` は 2,914 → 2,707 行（9-E の開始時 3,438 行から 731 行減）。E-1〜E-3 で OSC・HidHide・UDP が完了。仮想パッドの出力・入力レポートの処理・ライフサイクルは、別の計画書で設計する。
