---
name: ds4w-build-test-release
description: DS4Windows のビルド・テスト高速化・WPF STAハング対策・Publish（実機確認用ビルド）・Git/スクリプトの落とし穴を扱うときに参照する
---

# ビルド・テスト・Publish・Git 実務スキル

## 1. 開発中の高速ループと最終確認
- **Step 途中の高速ループ**（2026-11-05 承認）:
  - `Remove-bin.bat` は叩かずインクリメンタルビルド（`pwsh -NoProfile -File ./scripts/Build-Debug.ps1` ＋ `dotnet build ./DS4WindowsTests/DS4Windows.Actions.Tests.csproj -c Debug -p:Platform=x64 --no-dependencies`）し、`--filter "Category!=Slow&Category!=Benchmark"` で実行する。
  - 0.3 秒以上かかる新テストには `[Trait("Category", "Slow")]`、assert なしの性能計測には `[Trait("Category", "Benchmark")]` を付ける。
- **Step 完了時（コミット前）**:
  - `scripts/Run-DebugAll.ps1` で本体・テスト双方のクリーンビルドと全件テスト（`Category!=Benchmark`）を実行し、すべてのビルド出力で `0 エラー`・警告 0 を確認する。
  - *注意*: `Remove-bin.bat` は `DS4Windows\bin`/`obj` のみ消すため、テストプロジェクトのビルドに失敗した状態で `--no-build` を走らせると古いテスト DLL が実行される。不審なときは `DS4WindowsTests\bin`/`obj` も消す。また Release ビルドの `Remove-bin` 後は Debug を再ビルドしてからテストする。
  - `wpftmp` / `InitializeComponent` エラーが出た場合は `DS4Windows\obj` を削除して再ビルドする。

## 2. WPF 特有のテスト・クラッシュ知見
- **STA スレッドハング対策（`SharedStaThreadRuleTests`）**:
  - WPFLocalizeExtension は生成部品を弱参照で静的保持し、別部品読込時に全 Dispatcher へ `Invoke` するため、終了済み自前 STA スレッド上の部品が残ると 60 秒ハングが多発する。必ず `WpfPanelTestRunner.Run(() => ...)` を使う。
  - 全件実行が極端に遅い（11 分等）場合は `dotnet-dump collect` / `analyze`（`~/.dotnet/tools/dotnet-dump.exe`）で STA スレッドの待ち先を確認する。
- **CollectionSynchronization 弱参照罠**:
  - `BindingOperations.EnableCollectionSynchronization` はコールバックを弱参照保持するため、ラッパー内でラムダを作ると GC 後にトレイ停止等でクラッシュする。`ConditionalWeakTable` 等で寿命を保持すること。
- **実機クラッシュ報告時の初動**:
  - インストール版は実行ファイル横にログを出すため、まず **`C:\Program Files\DS4Windows\Logs\ds4windows_log.txt`** を確認する。

## 3. 実機確認前の Release / Publish 手順
- **通常の作業中は Publish 不要**。実機確認をユーザーへ依頼する直前のみ行う。
1. Release ビルドを出力する（`dotnet build DS4WindowsWPF.sln -c Release -p:Platform=x64`）。
2. Publish 手順を実行する:
   `scripts\Remove-bin.bat` → `scripts/publish-win-x64.ps1` → `scripts/post-build.ps1 <root>/DS4Windows/bin/x64/Release <root>/DS4Windows`
3. worktree でビルドした場合は、`DS4Windows\bin\x64\DS4Windows` の中身を本体チェックアウト（`G:\Cursor_Folder\DS4Windows-Vader4Pro\DS4Windows\bin\x64\DS4Windows`）へ `robocopy /MIR` で同期し、EXE/DLL のタイムスタンプが最新か確認する（読み込み中に 1 バイトの `DS4Windows.dll` が見えることがあるので完了後に再確認）。
4. 最後の管理者コピー（`scripts\CopyDS4WindowsToJK.bat`）は権限エラーになるため実行せず、ユーザーに実行を依頼して止める。

## 4. Git / スクリプト上の細かい罠
- Windows の `core.ignorecase=true` 環境で大文字小文字だけのファイル名変更をする場合は、`git rm --cached <old>` → `git add <new>` を使う。
- `Measure-Nullable` / `Fix-NullableMechanical` を使う場合、MSBuild ログの先頭 `N>` プレフィックスを除去（`sed -E 's/^ *[0-9]+>//'`）し、`--only 'Core\Settings'` のようにバックスラッシュを指定する。なお同ツールはコンストラクタに誤って `public?` を付けることがあるため必ず diff を目視する。