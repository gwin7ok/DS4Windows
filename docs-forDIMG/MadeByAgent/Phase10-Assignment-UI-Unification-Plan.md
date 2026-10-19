# Phase10 計画書（案）: 割り当ての UI の統一（Controls タブと SpecialActions タブ）

作成日: 2026-10-20  
対象ブランチ: `For-DI-migration-work`  
上位: `Phase8-Unified-Trigger-Dispatch-Plan.md`（D2＝B1: 違いはトリガーのボタン数だけ）、`Phase8-Step2b-ControlsTab-Design.md`、`Phase8-Step7-8c-Plan.md`（Controls の Macro を SA の仕様に合わせる）、`Phase9-Responsibility-Split-Plan.md`  
状態: **計画の骨子（ユーザーの指示［2026-10-20］により、位置づけを決めた段階。詳細な設計は着手の直前に行う）**

---

## 1. 目的

Controls タブ（トリガー＝単体のボタン）と SpecialActions タブ（トリガー＝複数のボタンの組み合わせ）の、Key・Button・Macro の割り当ては、**トリガーが単体か複数かの違いだけ**で、設定する内容は同じはずである。Phase8 で、実行（トリガーの成立 → 出力）は統一エンジンに統一される。しかし、設定の画面は、別々に作られていて、**設定できる項目も違う**。これを、UI の水準で統一できる部分から統一する。

**現時点の範囲は、マクロの割り当てだけ**（ユーザーの指示）。Key・Button は、マクロの統一の結果を見てから、Step を足す。

## 2. 現状の違い（Macro。2026-10-20 の調査）

| 項目 | Controls タブ（`RecordBox`。割り当ての画面の中で編集） | SA タブ（`SpecialActionEditor` の Macro。記録は `RecordBox` を開く） |
|---|---|---|
| 記録・クリア・手順の編集・Insert Wait・Record Delays・プリセットの読込／保存 | あり | 記録は同じ `RecordBox` |
| 再生の種別 | ドロップダウン（Play Once／Repeat While Held） | チェックボックス（Repeat While Held） |
| Scan Code | あり | あり |
| **トリガーを離したときに実行（Run on release）** | なし | あり |
| **同期実行（Sync）** | なし | あり |
| **キーの状態を保持（Keep key state）** | なし | あり |
| 振動・ライトバーの手順の挿入、マウスの 4／5 ボタン | あり（`RecordBox` の機能） | 同じ `RecordBox` |
| 保存先 | `Profile.xml` の割り当て（`DS4ControlSettings.action.actionMacro` ＋ `keyType`） | `Actions.xml`（`SpecialAction.details` の文字列 ＋ 追加の指定） |

## 3. 位置づけ（Q）

- **新しい Phase（Phase10）とし、Phase8 の完了後に着手する**。理由:
  1. Phase8 は「実行」の統一。Phase9 は「責務分割」で、**機能の追加・保存形式の変更を範囲外**にしている。UI の統一は、設定項目（Run on release、Sync、Keep key state など）を Controls タブにも出すなど、**機能の追加・保存形式の変更を含み得る**ため、別の Phase にする。
  2. Controls の Macro を SA の仕様に合わせた（Phase8-Step7.8c）ことで、Controls の Macro も SA と同じ実行の仕組みに載る。**UI で SA と同じ項目を出しても、実行側が対応できる**状態になる。
  3. 保存形式に触れるため、Phase8 の旧コード削除（Step8）と総合検証（Step9）の後の、安定した状態を出発点にしたい。
- **Phase9 との順序**: Phase9-F（アプリ層・UI のコードビハインドの整理。`ProfileEditor`、`MainWindow`）と、同じファイルを触る。**Phase10-Step1（マクロの UI の統一）を、Phase9-F の前に行う**（コードビハインドの二重の変更を避けるため）。Phase9-A〜9-E は独立しており、Phase10 と並行・前後どちらでもよい。Phase9-C（マクロの再生の再設計）は、UI の統一と独立（実行側と画面側）。
- Phase9 の `Phase9-Responsibility-Split-Plan.md` に、この前後関係の注記を追記した。

## 4. Step の構成（案）

### Phase10-Step1: マクロの割り当ての UI の共通化（Controls と SA）

- **共通の編集部品**（UserControl と ViewModel）を作り、Controls と SA の両方が使う。差は、トリガーの選び方（単体／複数）だけにする。
- 項目の統一: 再生の種別（Play Once／Repeat While Held の表現を 1 つにする）、Scan Code、Run on release、Sync、Keep key state の扱い。**Controls に出す項目は、保存形式に追加が要る**（`DS4ControlSettings` と `Profile.xml` に、対応する欄がない）。保存形式の追加は、ユーザーの承認が要る（規約 2.2）。選択肢: (A) 共通の部品にして、Controls には、実行側が既に対応する項目だけを出す（保存形式の追加なし。Run on release などは SA だけ）、(B) Controls の保存形式を広げて、全項目を出す。
- 完了条件: 同じ編集部品で、両方のタブの Macro を編集できる。既存のプロファイル・`Actions.xml` を、そのまま読める（互換）。実機（画面）での確認が要る。

### Phase10-Step2 以降（未定）

- Key・Button の割り当ての UI の統一（Step1 の結果を見て判断）。
- 長期: 保存形式（`DS4ControlSettings` と `SpecialAction`）の統合の可否。

## 5. 決めること（着手の直前に）

- Q1: Controls に出す項目の範囲（上記 A／B）。
- Q2: 画面の見た目（Controls の割り当て画面の中に編集部品を埋め込むか、SA と同じく別ウィンドウにするか）。
- Q3: 既存のプロファイル・`Actions.xml` の互換の方針（保存形式を変える場合は、読込のテストを先に作る）。
