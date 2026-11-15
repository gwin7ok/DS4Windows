# Phase39〜54 の実施の記録

作成日: 2026-11-15
目的: Phase39 以降は、個別の計画書を作らずに、実機確認で見つかった不具合と要望に、小さな Phase として対応した（ブランチごとに作成し、`master` へ `--no-ff` で統合。push はしていない）。計画書のない Phase の内容・根拠・結果を、ここに 1 か所で残す。Phase29〜40 の計画書の状態は、各計画書の冒頭と、全体計画書 §6.15 を参照。
関連: `Known-Issues-Ledger.md`（不具合の台帳）、`Device-Check-Checklist.md`（実機確認のチェックリスト）、`PhaseX4-Custom-Exe-Name-Plan.md`

## 1. 実施の一覧
| Phase | 日付 | 内容 | 根拠・結果 |
|---|---|---|---|
| 39 | 2026-11-15 | Phase36 の C1・C6・C7 の調査（実装はしない）と、DSU クライアントの登録簿（`DsuClientRegistry`。Phase38-3）の切り出し | `Phase36-Plan.md` の実施の記録。C1 は Phase40 へ |
| 40 | 2026-11-15 | 使われていなかった `ITouchpadBehaviour` の削除（規則 2.4。承認済み）と、第 3 層が使う入力機種の契約の計画（C1＝A） | `Phase40-Plan.md`。40-1 以降は未着手 |
| 41 | 2026-11-15 | KI-19（CLI の結果がプロンプトの後ろに出る）の調査 | 原因は、`DS4Windows.exe` が GUI アプリで、cmd.exe が終了を待たないこと。コード順では直せない |
| 42 | 2026-11-15 | CLI を、コンソール種別の `DS4WindowsCli.exe` へ切り出し（KI-19 の対応） | `DS4Windows.exe -command` は何もせず終了する。実機確認 OK |
| 43 | 2026-11-15 | Output Slots タブの整理: スロット番号の固定列、XInput # 列の廃止、入力 # を全機種で表示 | 実機確認 OK |
| 44 | 2026-11-15 | Output Slots の列を「出力スロット #／入力スロット #／入力コントローラー ID」に整理 | コントローラータブの ID と同じ表示 |
| 45 | 2026-11-15 | USB の Xbox の ID を、インスタンス ID の連番でなく、親（USB ポート）から作る（抜き差しで ID が増え続けないように） | `XInputTransport.ChooseIdentitySource`。`XboxIdentitySourceTests` |
| 46 | 2026-11-15 | コントローラータブの列幅の既定値を、設定ファイルの値へ。Output Slots タブの列幅を設定へ保存 | 設定の保存の黄金ファイルを更新 |
| 47 | 2026-11-15 | 仮想パッドを、予約したスロット・利用者が選んだスロットへ差す（最初の空きへ差していた不具合の修正） | `OutputSlotManager.DeferredPlugin` の `preferredSlot`。`OutputSlotPlugTargetTests` |
| 48 | 2026-11-15 | Auto Profiles タブと Log タブの列幅を設定へ保存（これで、列を持つタブはすべて保存される） | — |
| 49 | 2026-11-15 | コントローラータブの右端の列に「ライトバーの色」の見出しを追加 | — |
| 50 | 2026-11-15 | KI-3（CLI の `LoadTempProfile`・`ChangeLedColor`）、KI-15（Xbox の出力遅延）、KI-16（振動テストの日本語化）、KI-18（編集可否の変更通知） | 実機確認 OK。`LoadTempProfile` は通常の適用経路（`ApplyProfile`）へ |
| 51 | 2026-11-15 | 一時プロファイルの間は、設定ファイルへ通常のプロファイル名を保存する（`BackingStore.GetPersistedProfileName`） | 一時の名前が Profiles.xml に保存され、次回起動の既定になる問題の予防。`PersistedProfileNameTests` |
| 52 | 2026-11-15 | KI-13: 「Hide DS4 Controller」の表示名（英・日）とツールチップ | 実機確認 OK。他の言語は旧表示名のまま |
| 53 | 2026-11-15 | KI-17: タッチボタンの診断ログ（Trace の `[TOUCHBTN]`） | 結論: 上端はセンサー範囲外のため、押し込み（Click 方式）だけが検出できる。コントローラーの限界としてクローズ |
| 54 | 2026-11-15 | 全機器の電池を、5 分ごとに同じ形式でログに出す共通部品（`BatteryStatusLogger`）。Bluetooth の Xbox の読み取りを 5 分間隔に | 実機確認 OK。`BatteryStatusLoggerTests` |
| - | 2026-11-15 | 台帳の整理: KI-1・2・3・5・6・13・15〜19 を解消済みへ（KI-2・5 は、原因を特定せず「一旦解決済み」）。KI-14 は `PhaseX4` として起票、KI-7 は最後の Phase | `Known-Issues-Ledger.md` |

## 2. 現在の数値（2026-11-15）
- 自動テスト: 3,567 件（`--filter "Category!=Benchmark"`）。ビルドの警告・エラーは 0。
- 行数の多いファイル（行数）: `MainWindow.xaml.cs` 1,884、`ProfileEditor.xaml.cs` 1,737、`BackingStore.cs` 1,205、`AppSettingsDTO.cs` 1,178、`InputDeviceCore.cs` 1,097、`DualSenseDevice.cs` 1,088、`SwitchProDevice.cs` 998、`BindingWindow.xaml.cs` 943、`App.xaml.cs` 937、`RecordBox.xaml.cs` 901、`Mapping.cs` 895（2026-09 の 8,827 行から約 1/10）。自動生成の `Strings.Designer.cs`（4,272）・`Resources.Designer.cs`（1,677）は対象外。

## 3. 残っている作業
| 項目 | 状態 | 備考 |
|---|---|---|
| Phase34（画面の MVVM 化） | 34-1（土台）まで。34-2 以降は未着手 | 最大の残り。大きな画面の code-behind（上の `MainWindow`・`ProfileEditor`・`BindingWindow`・`RecordBox`・`AssignmentEditor`・`App`）が約 7,400 行 |
| Phase35 | 35-3・35-4・35-4b は未着手 | 終了の実機確認のあとに行う |
| Phase36 | C 部の判断項目が残る | C1 は Phase40 へ。C6・C7 は実装しない |
| Phase37 | 37-3 以降は未着手 | 保存の形に触れる。実機のデータでの確認を前提 |
| Phase40 | 40-1 以降は未着手 | 第 3 層が使う入力機種の契約（C1＝A。承認済み） |
| PhaseX（KI-7） | 下書き | 一番最後の Phase。入力側の画像・ボタン名の機種別対応 |
| PhaseX3 | 下書き | 日本語表示の完全化。時期は未定 |
| PhaseX4（KI-14） | 起票のみ | カスタム実行ファイル名の立て直し。元の資料は `PhaseX2` |
| 実機で未確認の項目 | `Known-Issues-Ledger.md` §3、`Device-Check-Checklist.md` | タッチ → スティック、UDP（Cemuhook）、DualSense・Switch Pro（実機なし）ほか |
| 多言語 | KI-13 の表示名は英・日のみ | 他の言語は旧表示名のまま |

## 履歴
- 2026-11-15: 作成。Phase39〜54 の実施を、コミット履歴と台帳から整理した。
