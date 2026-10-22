# Controls タブの割り当て一覧の表示の変更案（承認待ち）

作成日: 2026-10-22
状態: 提案のみ

## 1. 方針（ユーザー指示）
- 今の 1 行（「Cross: Triangle」）を、**表**にする。列は 2 つ（ラベル、設定値）。どちらも左寄せ。
- 設定値は、出力ピッカーを開くボタンのラベル（`PressKeyViewModel.DescribeText`）と同じ形式にする。
- Output Key/Mouse/Button 以外（Macro、SA 専用の種別、Default、Unbound）も、なるべく詳しく表示する。
- SA タブの一覧（名前、トリガー、アクション）の表示と揃え、全体として情報量を増やす。

## 2. 表の形（案）
| ラベル（左寄せ） | 設定値（左寄せ） |
|---|---|
| Cross | Default (A) |
| Triangle | Default (Triangle) (Toggle) |
| L2 | W (Press) |
| L2 Full Pull | Macro: 3 steps (Repeat) |
| Share | Unbound |

- ラベル: ボタンの名前（今と同じ。表示の言語は、今の `ControlName`）。
- 設定値: 下の §3。行の色・太字などの装飾は、しない（変更した項目だけを目立たせる案は、別途）。

## 3. 設定値の表示（種別ごと）
出力ピッカーのボタンのラベルと同じ書式を基本にし、**`( )` の中に、モード・オプションを順に足す**。

| 種別 | 表示 | 例 |
|---|---|---|
| Default | `Default (既定の出力名)` ＋ `(Toggle)`（Toggle のとき）。Default は、Toggle にすると Button へ変わるため、通常、Toggle は付かない | `Default (A)` |
| Key | `キー名 (Press／Toggle)` ＋ ` (SC)`（Scan Code） | `W (Press)`、`Q (Toggle) (SC)` |
| Button（パッド） | `出力名 (Press／Toggle)` | `A (Toggle)`、`LS Right (Press)` |
| Button（マウス・ホイール・タッチパッドクリック） | 同上 | `Left Mouse Button (Toggle)` |
| Button（マウス移動） | 同上 | `Mouse Right (Press)` |
| Unbound | `Unbound` | `Unbound` |
| Macro | `Macro: N steps` ＋ 再生の指定（`Play once`／`Repeat while held`）、`(SC)`、SA 専用の 3 項目（`Run on release`／`Sync`／`Keep key state`） | `Macro: 12 steps (Repeat while held) (SC)` |
| Launch Program | `Launch Program: ファイル名` ＋ 押し続ける秒数 | `Launch Program: notepad.exe (hold 1.0s)` |
| Load Profile | `Load Profile: プロファイル名` ＋ `(Auto untrigger)` | `Load Profile: Game` |
| Disconnect BT | `Disconnect Controller` ＋ 押し続ける秒数 | `Disconnect Controller (hold 2.0s)` |
| Check Battery | `Check Battery` ＋ 秒数 | `Check Battery` |
| Multi-action | `Multi-action: タップ=…／長押し=…／ダブルタップ=…`（設定された項目だけ） | `Multi-action: Tap=A, Hold=B` |
| Calibrate Gyro | `Calibrate Gyro` | |
| Extras がある | 末尾に ` + Extras`（振動、ライトバー、マウス感度のどれかを設定済み）。詳細は ToolTip | `A (Press) + Extras` |

## 4. SA タブとの統一
- SA タブの「アクション」列は、今は種別名（`Key`、`Macro`、`Button`、…）だけ。Controls と**同じ書式の関数**（種別 → 表示文字列）を共通にし、SA 側のアクション列も、種別名に続けて詳細を出す（例: `Key: W (Toggle)`、`Macro: 12 steps (Repeat while held)`）。
- 共通の関数: `PressKeyViewModel.DescribeText`（ボタンのラベル）を元に、Macro・SA 専用の種別を足した「割り当ての説明」の部品（`AssignmentDescriber`）を 1 つ作り、Controls の一覧、出力ピッカーのボタン、SA の一覧が同じ関数を呼ぶ。

## 5. 実装の範囲（承認後）
1. `AssignmentDescriber`（新規。種別ごとの表示。単体テストつき）。
2. Controls の一覧を `ListBox` から、2 列の `ListView`（`GridView`。左寄せ）へ。`MappedControl` の `MappingName` を、新しい表示へ。
3. SA の一覧のアクション列を、同じ書式へ（任意。別 Step でもよい）。

## 6. 決めてほしいこと
- Q1: 表示の言語は、アプリの言語（今の `Strings` を使う）に合わせる。英語の固定語（`Press`、`Toggle`、`steps`）も、言語リソースにする（推奨）／英語固定。
- Q2: `+ Extras` の表示は要るか。
- Q3: SA の一覧のアクション列も、今回、同じ書式へ変えるか（推奨）／Controls だけ先に。
- Q4: 行の装飾（Default 以外を太字にして、変更した項目を目立たせる）は、今回入れるか。
