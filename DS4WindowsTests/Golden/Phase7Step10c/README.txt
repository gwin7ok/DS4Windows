Phase7-Step10c: BackingStore の永続化の切り出しのためのゴールデンファイル。

- 切り出し（Phase7-Step10c W2 以降）の前に、現行のコードで生成した「期待値」を保存している。切り出しの後も、同じ入力から同じ出力・同じ状態になることを照合する。
- ファイルが存在しないとき、テスト（Phase7Step10cGoldenPersistenceTests）は、ファイルを自動で生成して失敗する。内容を確認してコミットし、再実行すること。
- ゴールデンを更新してよいのは、保存形式・既定値を意図して変えたときだけ。切り出しの Wave では更新しないこと（差が出たら、移動で挙動を変えている）。
- このファイル（README.txt）は、テストがゴールデンの置き場所を見つける目印でもあるため、削除しないこと。
- 揺れる値（一時フォルダー、バージョン、日時のコメント）は、照合の前に正規化している。
- コントローラー固有設定（ControllerConfigs.xml）は、実機のデバイス実体が要るため、ゴールデンの対象外（Phase7-Step10c-Plan.md §3 Q2 の補足）。

Phase8-Step4b W1 で追加したゴールデン（Phase8Step4bProfileGoldenGapTests）:
- profile-gap.xml／profile-gap.state.txt／profile-gap.resaved.xml: 自動入力が変えられない項目（曲線のモード、ライトバーのモード、特殊アクション ほか）を変えた状態。
- profile-legacy.state.txt／profile-legacy.resaved.xml: 旧形式の読込専用の要素（UseSAforMouse ほか）の経路。
- profile-minimal.state.txt／profile-minimal.rewritten.xml: 要素がほとんどない XML（欠けた設定の経路と、読込後の追記）。
- 初回の実行は、ファイルを生成して失敗する。内容を確認してコミットし、再実行すること（Step4b の分解の前、製品のコードを変えない状態で生成すること）。
