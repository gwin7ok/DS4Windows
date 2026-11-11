# PhaseX 計画書（下書き）: 入力側のボタン名・画像の機種別対応（KI-7）

作成日: 2026-11-11
最終更新日: 2026-11-12（現在のコードと照合して、現状・設計の方針・決めること・Step 案を改めた。変更点は末尾の「2026-11-12 の見直し」）
状態: **下書き。実施するかどうかは未定**（入力側は DS4 のボタン名・画像だけで維持する可能性がある）。実施する場合は、一番最後の Phase として行い、そのとき番号を振る。実施しないと決めたら、KI-7 を「対応しない（仕様）」として解消済みへ移す。
関連: `Known-Issues-Ledger.md` の KI-7、`Phase22-Xbox-Parity-Plan.md` §7、`Model-Diagram/02-Extension-Guide.md`、全体計画書 `DI-App-Wide-Migration-Plan.md` §6.16
調査の対象: `master`（コミット `070095f9` の時点のコード）。本文の `ファイル:行` は、その時点の行番号。着手時に再確認する。

## 1. 目的と、実施しない場合の姿
- 目的: 割り当て編集画面などで、入力機種（DS4・DualSense・Switch Pro・Xbox）ごとに、その機種の図とボタン名（例: Xbox の A・B・X・Y）を出す。
- 実施しない場合（現状の維持）: 全機種が DS4 の図と名前（Cross・Circle・Square・Triangle など）で出る。保存するプロファイルの名前は DS4 の名前で、互換のため変えない。Xbox の A は Cross として出る。

## 2. 現状（2026-11-12 に照合）

### 2.1 入力側の図
| 場所 | 現状 |
|---|---|
| プロファイル編集画面（Controls タブ） | `UI/ProfileEditor.xaml:155` が、配置表 `ControllerLayouts.DualShock4Editor`（440×220）に固定 |
| 出力ピッカー（`BindingWindow`）の入力側 | `UI/BindingWindow.xaml.cs:815` が、`ControllerLayouts.DualShock4`（630×247）に固定 |
| 配置表の型 | `UI/Controls/ControllerLayout`（名前、基準の大きさ、領域 `HitboxRegion` の列）。定義は `UI/Controls/ControllerLayouts.cs` の 3 つ（`Xbox360`、`DualShock4`、`DualShock4Editor`） |
| 画像（`DS4Windows/Resources`） | DS4 は `DS4 Config.png` と、ボタンごとの強調 `DS4-Config_*.png`。**Xbox 360 は、出力側のために、すでにある**（`360 map.png`、`360 highlight.png`、`360 fades.png`、`xbox_360_controller.png`）。DualSense・Switch Pro の画像はない |
| 出力側の図（先例） | `UI/BindingWindow.xaml:191` が `ControllerLayouts.Xbox360`。出力ピッカーは、仮想パッドの種類ごとに図を出し分けている |

### 2.2 入力側のボタン名（表示名）の出どころ
表示名は 1 か所ではなく、次の 5 か所にある。どれも英語の固定の文字列で、翻訳のリソースではない。

| # | 場所 | 使う画面 | 備考 |
|---|---|---|---|
| 1 | `UI/ViewModels/InputControlCatalog.cs` | Controls タブの一覧（`MappingListViewModel`）、特殊アクションのトリガーの一覧（`TriggerCatalog`） | クラスのコメントに「表示順と表示名の唯一の定義」とある。各項目は、表示名と、保存する名前（`InputControlEntry.Name`）を別に持つ。**表示名と保存名の分離は、ここではすでにできている** |
| 2 | `Core/Model/DS4ControlNames.ds4inputNames` | 割り当て編集の対象の名前（`UI/ViewModels/ControlEditTarget.cs:25`）、出力ピッカーのタイトル（`UI/BindingWindow.xaml.cs:106`） | 1 と別の表。同じ入力に、1 と 2 で別の文字列を持つものがある（例: 左スティックの左は、1 が「LS Left」、2 が「Left X-Axis-」） |
| 3 | `UI/ViewModels/GyroSettingsViewModel.cs:25`〜`35` | ジャイロのトリガーのメニュー | **メニューの並びが、そのまま保存する番号になる**。並びは変えられない（変えてよいのは文字だけ） |
| 4 | `UI/TouchpadSettingsPanel.xaml:225`〜`282` | タッチパッドのメニュー（13 項目） | XAML に直書き |
| 5 | 各パネルの見出し・ラベル | `UI/TriggerSettingsPanel.xaml`（「L2 & R2」「L2」「R2」）、スティックのパネル（LS／RS）、`UI/ControllerReadingsControl.xaml`（「L2」「R2」）など | XAML に直書き |

### 2.3 表示ではない名前の表（触らない）
| 場所 | 役割 |
|---|---|
| `DS4ControlNames.getDS4ControlsByName`、`Core/Settings/SpecialAction.cs:253` | プロファイルの XML と特殊アクションの、**保存する名前の読み取り**（`ProfileButtonControlLoadSection`、`BackingStore.cs:842`〜`894`）。互換のため変えない |
| `DS4ControlNames.macroDS4Values` | マクロの値の表（`UI/ViewModels/RecordBoxViewModel.cs:431`）。表示名ではない |
| `Processing/Macro/MacroParser.InputName(value, OutContType)` | マクロの各手順の表示名。**出力の種類（Xbox 360／DS4）で出し分ける、出力側の名前**であり、入力機種とは関係がない |

### 2.4 使える既存の表
- Xbox のボタン名は `Core/Model/X360ControlTables.xboxDefaultNames`（`X360Controls` → 名前）にある。`defaultButtonMapping`／`reverseX360ButtonMapping` で `DS4Controls` と対応づけられる。Xbox の入力側の表示名は、新しい表を書かずに、ここから作れる見込み（着手時に、全項目が対応するかを確かめる）。

### 2.5 画面が機種を知る手段
- プロファイル編集画面は、実機のスロット番号 `targetDevice` を持つ（`UI/ProfileEditor.xaml.cs:70`。プロファイル一覧から開いたときは -1＝実機なし）。
- **UI は、スロットの機種（系統）を知る窓口を持っていない。** UI 向けの契約（`Processing/Abstractions` の `IControllerSlotView`、`IControllerSlotQuery`）に、機種の種類・系統を返すメンバーがない。UI は第 1 層を参照できない（規則 L7）。

## 3. 設計の方針（案）
1. **表示の名前と、保存する名前を分ける**。保存・読み込みは今のまま DS4 の名前（`DS4Controls`）で行い、画面に出す表示名だけを機種ごとに変える。`DS4Controls` の列挙型と、§2.3 の表は変えない。
2. **機種ごとの図と表示名は、系統（`InputDeviceFamily`）をキーにした表で持つ**。置き場所は、表示名の表が `Core/Model`、配置表が `UI/Controls`（今の `ControllerLayouts` の隣）。系統の画面の文言を `[InputDeviceFamilyUi]`（`Core/Model/InputDeviceFamilyUiAttribute.cs`）で持つ前例にそろえる。
   - 当初の案（機種のカートリッジに契約 `IInputDevicePresentation` を持たせる）は採らない。カートリッジは第 1 層（`Input/Devices`）にあり、UI から参照できない（L7）。配置表は UI の型（`ControllerLayout`）なので、第 1 層にも置けない（L3）。
3. **表示名の出どころを 1 つにまとめてから、機種別にする**。§2.2 の 1 と 2 の二重を先に解き、「機種（系統）と `DS4Controls` から表示名を返す」1 つの窓口にする。既定は DS4 で、この段階では表示は変わらない。
4. **画面が機種を知る窓口を、第 2 層の契約に足す**（例: `IControllerSlotQuery` に、スロットの系統を返すメンバー）。実機なし（-1）・未接続のときは、既定の DS4 にする（D4）。
5. 保存する番号が並びで決まる一覧（§2.2 の 3）は、並びを変えず、文字だけを差し替える。
6. 既存の 1 型 = 1 ファイル、名前空間 = フォルダ、ログの規則（新しい文言は `AppLogger.LogUser` とリソース）に従う。

## 4. 決める必要のあること（着手前にユーザーへ確認）
| # | 決めること | 選択肢 |
|---|---|---|
| D1 | そもそも実施するか | 実施する／DS4 の見た目で維持する（KI-7 を仕様として解消） |
| D2 | 対象の機種 | Xbox だけ（実機あり。画像も既存）／DualSense・Switch Pro も（実機なし。画像の用意が必要） |
| D3 | 画像の用意 | Xbox は、出力側の既存の画像（`360 map.png` ほか）を使える。プロファイル編集画面の大きさ（440×220）に合う配置表は新しく要る。DualSense・Switch Pro は、ライセンスに合う画像を探すか、新しく描く |
| D4 | どの機種の図を出すかの決め方 | 編集画面の実機（`targetDevice`）の系統に従い、実機なしは DS4／画面に切り替え（選択）を置く／両方 |
| D5 | 表示名を変える範囲 | (A) 図と、一覧・タイトル（§2.2 の 1・2）だけ／(B) メニュー（3・4）も／(C) パネルの見出し・ラベル（5。「L2」→「LT」など）まで。範囲が広いほど、XAML の直書きの置き換えが増える |
| D6 | その機種にない入力の扱い（例: Xbox のタッチパッド、ジャイロ、Mute、背面のパドル） | そのまま出す（今と同じ）／一覧から隠す／灰色にする。保存済みの割り当てがある場合の見せ方も決める |
| D7 | 表示名の言語 | 英語の固定のまま（今と同じ）／この機会にリソース化する（範囲が大きく広がる。別の作業にするのを勧める） |

（当初の D5「マクロ記録の表示名」は取り下げた。マクロの手順の名前は出力側の名前で、入力機種とは関係がない。§2.3）

## 5. Step 案（実施する場合）
| Step | 内容 | 状態 |
|---|---|---|
| X-0 | 調査の再確認（§2 を、その時点のコードで確かめ直す）と D1〜D7 の決定。画像の調達（D3）。`xboxDefaultNames` から Xbox の表示名を全項目作れるかの確認 | 未着手 |
| X-1 | 表示名の出どころの一本化: 「系統と `DS4Controls` から表示名を返す」窓口を `Core/Model` に作り、`InputControlCatalog`（§2.2 の 1）と `ds4inputNames` の利用側（2）がそれを使う。既定は DS4。**表示は変えない**（1 と 2 で文字列が違うボタンは、今の画面ごとの表示を保つか、どちらかにそろえるかを、差分の一覧で承認を得る） | 未着手 |
| X-2 | 画面が機種を知る窓口: 第 2 層の契約に、スロットの系統を返すメンバーを足す（実装は `ControllerSlotQuery`）。実機なし・未接続は DS4 | 未着手 |
| X-3 | 系統ごとの表示名の表（Xbox を最初に）を足し、X-1 の窓口が系統で引く。全系統・全 `DS4Controls` に名前があることを、型の検査で守る | 未着手 |
| X-4 | 系統ごとの配置表（Xbox の、編集画面用 440×220 と、出力ピッカー用。後者は既存の `Xbox360` を使えるか確かめる）を足し、`ProfileEditor.xaml:155` と `BindingWindow.xaml.cs:815` の固定を、系統で選ぶ形にする。図の切り替えの判断は、WPF なしでテストできる部品へ出す | 未着手 |
| X-5 | D5 の範囲に応じて、メニュー（ジャイロのトリガー、タッチパッド）と、パネルの見出し・ラベルの表示名を差し替える。並びで番号が決まる一覧は、並びを変えない | 未着手 |
| X-6 | D6 の決定に応じて、その機種にない入力の見せ方を実装する | 未着手 |
| X-7 | DualSense・Switch Pro を足す（D2 で対象にした場合） | 未着手 |
| X-8 | 文書（`02-Extension-Guide.md` の「入力機種を足す」に、表示名の表と配置表を足す手順。`01-Structure.md`、規則）を更新し、KI-7 を解消済みへ | 未着手 |

## 6. テストの方針
- 保存・読み込みの互換: 既存のプロファイル（DS4 の名前）を読み書きして、結果が変わらないこと（Step ごとに確認。保存の黄金テスト）。
- X-1 は「表示が変わらない」ことを、全ボタンの表示名を変更の前後で比べるテストで固定する。
- 全系統に配置表と表示名があること、すべての `DS4Controls` に名前があることを、型の検査で守る（既存の `ControlTablesGuardTests`、`ControllerVisualizerLayoutTests` の拡張）。
- 並びで番号が決まる一覧（ジャイロのトリガー）は、項目の数と並びが変わらないことを固定する。
- 画面の切り替えは、判断を部品（Phase26 の例）へ出して単体テストし、見た目は実機で確認する。

## 7. リスク
- 影響が広い。表示名の出どころが 5 か所あり（§2.2）、D5 の範囲しだいで、XAML の直書きの置き換えが多くなる。X-1（一本化。表示は不変）を最初に固めないと、機種別の差し替えが画面ごとにばらつく。
- 画像・配置表の座標合わせは手作業で、実機のない機種は確認できない。
- 同じボタンの呼び名が、画面によって変わる（図と一覧は機種の名前、保存したファイルやログは DS4 の名前）。利用者が混乱しないよう、D5 の範囲をそろえて決める。

## 8. ほかの計画との関係（2026-11-12 追加）
- 「一番最後の Phase」とする（ユーザーの指示）。次の計画が同じファイルに触れるため、先に済ませてから行うのがよい。
  - `Phase32-Plan.md`（左右スティックの設定画面の一本化）: スティックのパネルの XAML と ViewModel を作り直す。見出し・ラベル（D5 の (C)）は、そのあとの形に対して差し替える。
  - `Phase26-Plan.md` の残り（`ProfileEditor`・`BindingWindow` の code-behind の整理）: 図の固定をしている箇所（`BindingWindow.xaml.cs:815`）が動く。
  - `Phase31-Plan.md` の 31-B3（出力ピッカーの `use360Mode` を、出力の種類から表示を引く形にする）: 出力側の図の出し分けの整理。入力側（本計画）は、同じ考え方を系統で行う。
- `Phase30-Plan.md`（機種の名指しの解消）とは独立。

## 2026-11-12 の見直し（変更点）
現在のコードと照合して、次を改めた。
1. **表示名の出どころ**: 当初は `DS4ControlNames` だけを挙げていたが、Controls タブの一覧とトリガーの一覧の表示名は `InputControlCatalog` が持ち、ほかにメニューと XAML の直書きがあると分かった（§2.2）。`DS4ControlNames` のうち `getDS4ControlsByName` は保存の読み取り、`macroDS4Values` はマクロの値の表で、表示名ではない（§2.3）。
2. **マクロ記録**: 当初の D5 と X-4 の「マクロ記録の表示名を機種別にする」を取り下げた。マクロの手順の名前は、出力の種類で出し分ける出力側の名前である。
3. **画像**: Xbox 360 の画像と配置表は、出力側のためにすでにあると分かった（§2.1）。D3 を改めた。
4. **設計の方針 2**: 「カートリッジに契約 `IInputDevicePresentation` を持たせる」をやめ、系統（`InputDeviceFamily`）をキーにした表（表示名は `Core/Model`、配置表は `UI/Controls`）にした。カートリッジは第 1 層で、UI から参照できないため。
5. **抜けていた前提**: 画面がスロットの機種を知る窓口がないこと（§2.5）と、表示名の一本化（X-1）を、Step として足した。
6. **決めること**: 表示名を変える範囲（D5）、その機種にない入力の扱い（D6）、表示名の言語（D7）を足した。
7. **ほかの計画との関係**（§8）を足した。

## 履歴
- 2026-11-11: 下書きを作成。実施するかは未定。
- 2026-11-12: 現在のコードと照合して見直した（上の「2026-11-12 の見直し」）。コードは未着手。実施するかは未定のまま。
