# Phase34 V2: ProfileEditor の業務の参照の解消（計画書）

作成日: 2026-11-16 / 対象: `DS4Windows/UI/ProfileEditor.xaml.cs`（1,671 行）/ 規則: V2

## 1. 現状（基準の 9 件）

IEnvironmentService、IProfileRepository、IProfileSettingsService、IControllerSlotCommands、IRuntimeFeatureSwitch（業務のサービス 5）、
DS4ControlSettings、SpecialAction、StickDeadZoneInfo、ProfileSlotNames（設定の型 4）。

## 2. 方針（決定事項）

型を別の名前空間へ動かして隠さない。次の窓口へ処理を移す。

1. 新設 `UI/Services/ProfileEditorBackend`: 実機のスロットの範囲判定（`IsValidSlot`）、プロファイルの読み込み（作業スロットへ）、有効な SA 名の一覧の取得、
   編集中のプロファイルの SA 参照の差し替え、他プロファイルへの参照の置換・削除・適用済みの照会（`ProfilesUpdatedExternally` へ渡す一覧を返す）、
   振動の停止、入力中のボタンの取得（`GetActiveInputControl`）、ジャイロ較正のリセット、`RumbleTestController` の生成。
   `ProfileEditor` のコンストラクターは、このバックエンドを 1 つ受ける（引数 12 → 少なくなる）。
2. `StickDeadZoneInfo`: `StickSettingsViewModel` に `IsAxialDeadZone` を足し、画面は bool を読む。
3. `SpecialAction`／`DS4ControlSettings`: `IEditorWindowFactory` に、`SpecialActionItem`・`MappedControl` を受けるオーバーロード
   （`CreateAssignmentEditor(…, SpecialActionItem)`、`CreateControlActionEditor(…, MappedControl)`）を足す。`SpecialActionItem.OriginalName`、`MappedControl.ResetSetting()` を足す。
4. `ProfileSlotNames`: 触る処理は `ProfileEditorBackend` へ移す。
5. 従来の順序・挙動・ログ文言は変えない。例外の握りつぶし（`catch {}`）の位置も同じ。

## 3. 手順

1. バックエンドと ViewModel・項目型の追加。
2. 画面の呼び出しを置き換える。工場 `ProfileEditorFactory` を更新。
3. ソース文字列を検査する既存テスト（`ProfileEditorTargetDeviceGuardTests`、`ProfileEditorGlobalReferenceGuardTests`、`ProfileEditorCallSitesTests` ほか）を、移した先へ更新。
4. 基準 9 件を削除、DI 計画書 §9.3、台帳、ビルド 0/0、全テスト。

## 4. 実機確認（先延ばし。台帳へ記載）

プロファイルの新規・編集・保存、SA の追加・編集・名前変更の反映、Controls の割り当てのリセット、振動テスト、コントローラー入力による割り当て編集の起動、ジャイロの較正、スティックの較正。
