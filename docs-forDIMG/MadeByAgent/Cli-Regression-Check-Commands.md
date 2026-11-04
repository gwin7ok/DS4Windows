# CLI 確認コマンド一覧（リリース前の回帰確認用）

作成日: 2026-11-04
目的: `-command ChangeLedColor`／`LoadProfile` が反応しない件（`Phase9-Deferred-Ledger.md` の Phase13b-1）が、以前からの不具合か、Phase13 以降の回帰かを切り分ける。
実装の根拠: `DS4Windows/Core/Utilities/ArgumentParser.cs`（`-command` の受け取り）、`DS4Windows/Processing/Services/IpcCommandProcessor.cs`（コマンドの実行）。

## 0. 前提

- **本体を起動したまま**、別のコンソールから同じ `DS4Windows.exe` を `-command` 付きで実行する（2 つ目のプロセスが、起動中の本体へコマンドを送る方式）。
- コマンド文字列は 256 文字以内。区切りは `.`（ドット）。大文字小文字は区別されない。
- デバイス番号は **1 始まり**（1 = 1 番目のコントローラー）。
- 比較する版: ①リリース済みの最終版、②今の master のビルド（`DS4Windows\bin\x64\Release\DS4Windows.exe`）。**同じコントローラー・同じ設定フォルダー・同じプロファイル**で試す。2 つの版を同時には起動しない（1 つずつ）。
- 以下では、実行ファイルのあるフォルダーで `.\DS4Windows.exe` を使う。

## 1. 準備

1. コントローラーを 1 台つなぐ（USB でも Bluetooth でもよい）。
2. 本体を起動し、メイン画面でコントローラーが 1 番目に表示されていることを確認する。
3. プロファイル名を 1 つ決める（例: `ProfileA`。`Profiles\ProfileA.xml` が実在すること）。別の名前のプロファイル `ProfileB` も用意しておくと違いが分かる。

## 2. 確認するコマンド（優先順）

| # | 目的 | コマンド | 期待される結果 |
|---|---|---|---|
| 1 | **問題の件**: ライトバーの色 | `.\DS4Windows.exe -command ChangeLedColor.1.255.0.0` | 1 番目のコントローラーのライトバーが赤になる（`ChangeLedColor.<デバイス>.<R>.<G>.<B>`） |
| 2 | 同上（別の色で変化を見る） | `.\DS4Windows.exe -command ChangeLedColor.1.0.0.255` | 青になる |
| 3 | **問題の件**: プロファイルの読み込み | `.\DS4Windows.exe -command LoadProfile.1.ProfileA` | 1 番目のコントローラーのプロファイルが `ProfileA` に変わる（メイン画面の選択、ログで確認） |
| 4 | 同上（別のプロファイル） | `.\DS4Windows.exe -command LoadProfile.1.ProfileB` | `ProfileB` に変わる |
| 5 | 一時プロファイル | `.\DS4Windows.exe -command LoadTempProfile.1.ProfileA` | 一時的に `ProfileA` が適用される |
| 6 | 問い合わせ（コマンドが届くかの切り分け） | `.\DS4Windows.exe -command Query.1.ProfileName` | 現在のプロファイル名が返る（受け取れる環境のとき） |
| 7 | 出力スロット | `.\DS4Windows.exe -command OutputSlot.1.Unplug` → `.\DS4Windows.exe -command OutputSlot.1.PlugDS4` | 仮想パッドが抜ける → DS4 として挿さる |
| 8 | サービス | `.\DS4Windows.exe -command Stop` → `.\DS4Windows.exe -command Start` | 入力処理が止まる → 再開する |

注: `Query` の書式は `Query.<デバイス>.<項目>`（項目の例: `ProfileName`、`Battery`、`ConnType`、`AppRunning`）。#6 は結果の受け取り方が環境により違うため、受け取れなければ省略してよい。

## 3. 結果の記録

各版について、#1〜#8 を ○（期待どおり）／×（反応なし）／△（一部）で記録する。

| # | リリース済みの最終版（版数: ___） | 今の master のビルド |
|---|---|---|
| 1 | | |
| 2 | | |
| 3 | | |
| 4 | | |
| 5 | | |
| 6 | | |
| 7 | | |
| 8 | | |

## 4. 結果の読み方

- **最終版も今のビルドも ×**（#1〜#4）: 以前からの不具合。既知の問題として CHANGELOG に書いてリリースしてよい。原因調査はリリース後に回す。
- **最終版は ○、今のビルドは ×**: Phase13 以降の回帰。リリース前に直す。まず #8（Stop／Start）と #7 が ○か×かを見る。これらも × なら、コマンドが本体に届いていない（`ArgumentParser`、起動中インスタンスへの送信）。#8 が ○で #1〜#4 だけ ×なら、`IpcCommandProcessor` 内の `ChangeLedColor`／`LoadProfile` の処理が疑わしい。
- **#1〜#4 の一部だけ ×**: ×のコマンドと、○のコマンドの違い（引数が 5 個以上必要か、プロファイルのファイル存在検査など）を教える。
- **反応しないときに見るもの**: 本体のログ（ログタブ）と、ログファイル（保存場所は私が未確認です。見つからなければ教えてください）に、`Command` から始まる行やエラーが出ていないか。出ていれば、その行を教える。

## 5. 実装メモ（調査の足がかり）

- `LoadProfile` は、`<AppDataPath>\Profiles\<名前>.xml` が実在し、デバイス番号が範囲内のときだけ実行する。名前の大文字小文字、拡張子なしで指定していることを確認する。
- `ChangeLedColor` は引数が 5 個（コマンド名を含む）以上ないと無視される。`ChangeLedColor.1.255.0.0` は 5 個。
- 受け付けの順序や条件は、Phase13-Step11 で `MainWindow.Ipc.cs` から `IpcCommandProcessor` へ「そのまま」移した。移動前後で挙動が変わっていないかが論点。

## 6. 全コマンド（DS4Windows.exe から始まる形。コピーして実行）

実行ファイルのあるフォルダーで、PowerShell から実行する。デバイス番号は 1 始まり。`ProfileA` は実在するプロファイル名に置き換える。

### 6.1 届くかの切り分け（引数の検査が緩いコマンド）

```powershell
.\DS4Windows.exe -command Stop
.\DS4Windows.exe -command Start
.\DS4Windows.exe -command Cycle
```

### 6.2 問題の件

```powershell
.\DS4Windows.exe -command ChangeLedColor.1.255.0.0
.\DS4Windows.exe -command ChangeLedColor.1.0.255.0
.\DS4Windows.exe -command ChangeLedColor.1.0.0.255
.\DS4Windows.exe -command LoadProfile.1.ProfileA
.\DS4Windows.exe -command LoadProfile.1.ProfileB
.\DS4Windows.exe -command LoadTempProfile.1.ProfileA
```

### 6.3 出力スロット

```powershell
.\DS4Windows.exe -command OutputSlot.1.Unplug
.\DS4Windows.exe -command OutputSlot.1.PlugDS4
.\DS4Windows.exe -command OutputSlot.1.PlugX360
```

### 6.4 問い合わせ

```powershell
.\DS4Windows.exe -command Query.1.ProfileName
.\DS4Windows.exe -command Query.1.Battery
.\DS4Windows.exe -command Query.1.Charging
.\DS4Windows.exe -command Query.1.ConnType
.\DS4Windows.exe -command Query.1.DisplayName
.\DS4Windows.exe -command Query.1.MacAddress
.\DS4Windows.exe -command Query.1.OutContType
.\DS4Windows.exe -command Query.1.OutputSlotType
.\DS4Windows.exe -command Query.1.OutputSlotAttachedStatus
.\DS4Windows.exe -command Query.1.AppRunning
```

### 6.5 切断・終了（最後に実行する）

```powershell
.\DS4Windows.exe -command Disconnect.1
.\DS4Windows.exe -command Disconnect
.\DS4Windows.exe -command Shutdown
```

注: `Disconnect`（番号なし）は全ワイヤレスコントローラーを切断する。`Shutdown` は本体を終了するので、最後に実行する。
