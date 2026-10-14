# Phase6-Step10-3c 計画書: スペシャルアクションの「キー押下」でコントローラーのボタンを割り当てられない件の調査

作成日: 2026-10-14  
状態: 調査待ち（未着手。計画案）  
位置づけ: Step10-3 の実機確認で判明した、DI 化の範囲外の機能の問題（`Phase6-Step10-3-Plan.md` §12）。DI 化の変更とは別のコミットで扱う  
準拠: `.github/copilot-instructions.md` §2.2、§2.4、§4

## 1. 現象
- スペシャルアクションの編集画面の「Press Key」で開くボタン割り当て画面（`SpecialActionEditor.PressKeySelectBtn_Click` → `BindingWindow.ExposeMode.ForPressToggle`）に、コントローラーの図は表示されるが、キーボードとマウスしか割り当てられない。コントローラーのボタン（Xbox 360／DS4 の表記）を割り当てられない。

## 2. 調査項目（案）
1. `git log -S"ForPressToggle"` で、このモードが追加された経緯と、当時の意図（コントローラーのボタンを割り当てられること）を確認する。
2. `BindingWindow` の `ForPressToggle` の分岐（`InitButtonBindings` と、116〜135 行付近のモード別の表示制御）が、実際に実行されているかを、ログと実機で確認する。
3. 置換前（Phase5 以前）から同じ現象だったか、リファクタで失われたかを判定する（§2.2 の機能維持に関わるため）。
4. `PressKeyViewModel` のボタン種別（`ActionType.Button`、`SaveAction` の `mode == 10`）の保存・読み込みが、一連の経路で動くかを確認する。

## 3. 判定後の進め方
- 置換前からの不具合、または機能の欠落であれば、是正の可否と方法を、選択肢と推奨を添えて確認する（承認後に実装）。
- 確認項目（割り当てたボタン名が出力種別に合った表記で表示されること）は、`Phase6-Step11-Plan.md` §3.3 の台帳のとおり。
