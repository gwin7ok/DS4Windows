# PhaseX 計画書（下書き）: 入力側のボタン名・画像の機種別対応（KI-7）

作成日: 2026-11-11
状態: **下書き。実施するかどうかは未定**（入力側は DS4 のボタン名・画像だけで維持する可能性がある）。実施する場合は、一番最後の Phase として行い、そのとき番号を振る。実施しないと決めたら、KI-7 を「対応しない（仕様）」として解消済みへ移す。
関連: `Known-Issues-Ledger.md` の KI-7、`Phase22-Xbox-Parity-Plan.md` §7、`Model-Diagram/02-Extension-Guide.md`

## 1. 目的と、実施しない場合の姿
- 目的: 割り当て編集画面などで、入力機種（DS4・DualSense・Switch Pro・Xbox）ごとに、その機種の図とボタン名（例: Xbox の A・B・X・Y）を出す。
- 実施しない場合（現状の維持）: 全機種が DS4 の図と名前（Cross・Circle・Square・Triangle など）で出る。保存するプロファイルの名前は DS4 の名前で、互換のため変えない。Xbox の A は Cross として出る。

## 2. 現状（調査の要約。着手時に再確認する）
| 場所 | 現状 |
|---|---|
| 入力側の図（配置表） | `UI/Controls/ControllerLayouts.cs` の `DualShock4Editor`（プロファイル編集画面の Controls タブ。440×220）、`DualShock4`（`BindingWindow` の入力側）。図の画像は DS4 のもの |
| 出力側の図 | `ControllerLayouts.Xbox360`（`BindingWindow.xaml`）。出力ピッカーが仮想パッドの種類ごとに図を出し分けている（これが先例） |
| ボタン名 | `Core/Model/DS4ControlNames.cs`（`ds4inputNames`、`macroDS4Values`、`getDS4ControlsByName`）。画面の表示（`ControlEditTarget`、`BindingWindow` のタイトル）、マクロ記録（`RecordBoxViewModel`）、プロファイルの読み込み（`BackingStore`、`ProfileButtonControlLoadSection`）が使う |
| 保存する名前 | プロファイルの XML は DS4 の名前（`Cross` など）を保存している。**互換のため変えない** |

## 3. 設計の方針（案）
1. **表示の名前と、保存する名前を分ける**。保存・読み込みは今のまま DS4 の名前（`DS4Controls`）で行い、画面に出す表示名だけを機種ごとに変える。`DS4Controls` の列挙型は変えない。
2. **機種ごとの図と表示名の表を、入力機種のカートリッジの側に持たせる**（`AddInputDevice<TCartridge, TModule>()` の仕組みに沿う）。契約の候補: `IInputDevicePresentation`（配置表の取得、`DS4Controls` → 表示名）。第 1 層から UI の型を参照しないよう、配置表は `UI` 側、表示名の表は `Core/Model` または第 1 層に置く（`LayerDependencyRuleTests` を守る）。
3. 画面は、いま編集中のプロファイルがどの機種に結びつくかで、図と名前を引く。機種を特定できない場合（接続なし、複数機種）の既定は DS4。
4. 既存の 1 型 = 1 ファイル、名前空間 = フォルダ、ログの規則（新しい文言は `AppLogger.LogUser` とリソース）に従う。

## 4. 決める必要のあること（着手前にユーザーへ確認）
| # | 決めること | 選択肢 |
|---|---|---|
| D1 | そもそも実施するか | 実施する／DS4 の見た目で維持する（KI-7 を仕様として解消） |
| D2 | 対象の機種 | Xbox だけ（実機あり）／DualSense・Switch Pro も（実機なし。画像の用意が必要） |
| D3 | 画像の用意 | 既存のライセンスに合う画像があるか、新しく描く（SVG などの配置表）か |
| D4 | プロファイルと機種の結びつけ | プロファイルは機種に依存しない。編集中にどの機種の図を出すかの決め方（最後に接続した機種、選択中のデバイスなど） |
| D5 | マクロ記録（`RecordBoxViewModel`）の表示名 | 機種別にする／DS4 のままにする |

## 5. Step 案（実施する場合）
| Step | 内容 | 状態 |
|---|---|---|
| X-0 | 調査の再確認と D1〜D5 の決定。画像の調達（D3） | 未着手 |
| X-1 | 表示名の表を `DS4ControlNames` から分離（表示名の取得を、機種を引数に取る窓口へ。既定は DS4。動作は変えない） | 未着手 |
| X-2 | 機種ごとの配置表と表示名の契約（`IInputDevicePresentation`）を足し、カートリッジの登録に載せる。Xbox を最初の実装にする | 未着手 |
| X-3 | プロファイル編集画面の図を、機種に応じて切り替える（`ProfileEditor.xaml` の固定の `DualShock4Editor` を差し替える） | 未着手 |
| X-4 | `BindingWindow` の入力側、マクロ記録の表示名を機種別にする | 未着手 |
| X-5 | DualSense・Switch Pro を足す（D2 で対象にした場合） | 未着手 |
| X-6 | 文書（`02-Extension-Guide.md`、`01-Structure.md`、規則）を更新し、KI-7 を解消済みへ | 未着手 |

## 6. テストの方針
- 保存・読み込みの互換: 既存のプロファイル（DS4 の名前）を読み書きして、結果が変わらないこと（Step ごとに確認）。
- 全入力機種に配置表と表示名があること、すべての `DS4Controls` に名前があることを、型の検査で守る（`ControlTablesGuardTests` の拡張）。
- 画面の切り替えは、判断を部品（Phase26 の例）へ出して単体テストし、見た目は実機で確認する。

## 7. リスク
- 影響が広い（割り当て編集画面、保存する名前の互換、マクロ記録）。表示名と保存名の分離（方針 1）を最初に固めないと、互換を壊す。
- 画像・配置表の座標合わせは手作業で、実機のない機種は確認できない。

## 履歴
- 2026-11-11: 下書きを作成。実施するかは未定。
