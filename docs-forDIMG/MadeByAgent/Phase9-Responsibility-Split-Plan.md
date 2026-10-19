# Phase9 計画書: 巨大クラスの責務分割（1 クラス 1 責務）

作成日: 2026-10-19  
対象ブランチ: `For-DI-migration-work`  
上位計画書: `docs-forDIMG/DI-App-Wide-Migration-Plan.md`（本書を Phase9 として追加する）  
関連: `Phase8-Unified-Trigger-Dispatch-Plan.md`（2026-10-19 追記［実行順序の改訂］）、`docs-forDIMG/Model-Diagram/01-Component-Dependency-Diagram.md` §2（解体の検証マトリクス）  
状態: **計画確定（Q1〜Q6 を推奨どおり承認、2026-10-19）。着手は Phase8 の完了後**

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
- 対象: `SetCurveAndDeadzone`（1,194 行）、`ApplyStickCalibration`、`CalcStickAxisFuzz`、`CalcAntiSnapbackStick`、`CalcWheelFuzz`、`ProcessTwoStageTrigger`（196 行）、`ProcessFlickStick`／`HandleFlickStickAngle`／`WarpEaseOut`、`Scale360degreeGyroAxis`（253 行）、`SAWheelEmulationCalibration`、`CalculateControllerAngle`。
- 切り出し先の案: スティック補正、トリガー補正（2 段階含む）、ジャイロ→コントロール、フリックスティック、ステアリング較正。状態は `StickMotionState` に既にある。
- 前提: 値の特性化テスト（デッドゾーン［Radial／Axial］、アンチデッドゾーン、カーブ、ドリフト補正、2 段階トリガー、フリックの境界値）。Phase7b で是正した丸め（`Math.Round`）の挙動も固定する。

### 9-B: `Mapping` のマウス・ホイールへの変換と、入力の解決（約 1,400 行）
- 対象: `getMouseMapping`（318 行）、`calculateFinalMouseMovement`、`GetMouseWheelMapping`、`TempMouseJoystick`、`GetByteMapping`、`GetBoolMapping`、`GetXYAxisMapping`、`getBool*Mapping` 群、`GetBoolMappingExternal`。
- 前提: 値の特性化テスト。特殊アクション側の `getBoolSpecialActionMapping`／`GetBoolActionMapping` は、Phase8 の後の姿を見て扱う。

### 9-C: マクロの再生（約 700 行）
- 対象: `PlayMacro`、`PlayMacroTask`（230 行）、`PlayMacroCodeValue`（164 行）、`EndMacro*`、`RunLightbarMacro`。K3（マクロの二重実行防止。`MacroRuntimeState`）の恒久的な再設計を含む。
- 注意: Phase8 の後の `IMacroPlayer`／`MacroAction` との境界を確認してから（Phase8 の Step7 で Macro 種別を切り替える際に、`PlayMacro` の呼び出しがどう変わるかに依存する）。

### 9-D: `Mapping` の合成入力の確定と Controls タブのボタン割り当て（約 1,600 行）
- 対象: `Commit`（176 行）と `SyntheticState` まわり、`MapCustom` の Controls タブ側、`ProcessControlSettingAction`（405 行）、`ShiftTrigger`。
- 注意: Phase8 は Controls タブのトリガー部分（Step3 以降で `ComboTriggerInputAction` に統一）を扱う。残る「ボタン → 出力の割り当て」部分を対象にする。

### 9-E: `ControlService` の責務分割（約 3,400 行）
- 切り出しの案（Wave を分ける）: OSC（`CreateOSCCallback`、`OSCPre/PostMappingStep`、`CompareAndSendChangesToOSC`、`ChangeOSC*Status`、約 500 行）、UDP モーション（`ChangeUDPStatus`、`PrepareDevUDPMotion`、スムージング、約 250 行）、HidHide（`CheckHidHidePresence`、`Update*Attributes`、約 150 行）、仮想パッドの出力デバイス（`EstablishOutFeedback` 228 行、`PluginOutDev`、`UnplugOutDev` ほか、約 600 行。`OutputSlotService` へ寄せる。過渡期の逆依存の解消を含む）、レポート処理（`On_Report` 255 行）と起動・停止・ホットプラグ（`Start` 174 行、`Stop`、`HotPlug`）。
- 前提: OSC は競合の回帰テストのみで、送信内容を固定するテストがない。UDP・HidHide・出力デバイスは、実機が要る箇所が多い。ガードレール（6 大ガードレール。同一 XML 排他、適用時 Halt、スレッド直列化、On-Demand パス評価、ドライバ破棄順序、切断時クリーンアップ）を確実に守る。

### 9-F: アプリ層・UI のコードビハインド
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
