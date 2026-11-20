# PhaseRV5-0: 設定の項目を足すときに触る場所と、宣言型の比較（承認待ち）

作成日: 2026-11-20

## 1. 項目 1 つを足すときに触るファイル（実測。`DS4Windows` 配下を単語で検索）

| 項目 | 種類 | 触るファイル | 件数 |
|---|---|---|---|
| `RumbleBoost` | プロファイル・数値 | `ProfileGeneralLoadSection`・`ProfileGeneralSaveSection`・`OutputSlotSettings`・`OutputSettingsPanel.xaml`・`OutputSettingsViewModel`・`RumbleTestController` | 6 |
| `TouchpadClickPassthru` | プロファイル・真偽 | `ProfileTouchpadBasicLoadSection`（読み）・`ProfileTriggerStickSaveSection`（書き。別の節）・`TouchpadSettingsPanel.xaml`・`TouchpadSettingsViewModel` | 4（＋領域の設定の型） |
| `UseUdpServer` | アプリ・真偽 | `IAppSettingsService`・`AppSettingsService`・`OutputServicesLifecycle`・`SettingsViewModel`（＋`AppSettingsDTO`・`BackingStore`） | 4〜6 |
| `FlickStick` 系 | プロファイル・列挙型 | `StickMode`・`Mapping`・`StickSideSettingsView.xaml`・`StickSideViewModel`・読み書きの節 | 5〜7 |

読み込みと保存が別の節（別ファイル）にある項目がある（`TouchpadClickPassthru`）。これが「食い違い」の温床で、宣言型で 1 か所にする動機になる。

## 2. `BackingStore` の公開フィールド 107（D3＝(A) 調査のみ）
計画書の概算のとおり（型を参照するファイル約 90＝Core 69、Processing 8、Composition 7、UI 4、Output 1、`App` 1）。主な利用者は永続化の節で、節が定義の並びへ移れば、直接参照が自然に減る。窓口への寄せは、RV5-5 のあとに判断する。

## 3. 宣言型の比較

| 手段 | 保存の形を変えずに使えるか | 評価 |
|---|---|---|
| 既存の `ControllerSetting`（Phase17） | 使える。すでに保存・読み込みが定義の並びで動き、解析失敗の扱いも合わせてある | **推奨の土台**。`ProfileSetting`（要素名・既定値・範囲・読み書き）をこの延長で作る |
| .NET の Options パターン | 構成（appsettings）向け。XML の要素順・手書きの `XmlWriter` と合わない | 不採用 |
| `System.Text.Json` のソース生成 | JSON 用。保存が XML のため使えない | 不採用 |
| CommunityToolkit.Mvvm `[ObservableProperty]` | 変更通知の定型を畳める（`AppSettingsService` の 71 プロパティ）。ただし値の持ち主が `BackingStore` の公開フィールドで、`ref` の `SetIfChanged` のほうが小さい変更で済む | RV5-2 は `SetIfChanged` を推奨。`[ObservableProperty]` は不採用 |
| 属性ベースの `XmlSerializer`（現状の DTO） | 要素順は宣言順で決まる。現状のまま | D4＝(A) ならそのまま。1 項目の追加で DTO にも足す |
| Roslyn のインクリメンタル ソース ジェネレーター | 定義から DTO を生成できる。ただし WPF の 1 回目のビルド（`_wpftmp`）で、アナライザー参照の扱いが不安定になり得る。生成物の順序＝宣言順の保証が要る | 関門つきで可能だが、ビルドの複雑さが最大 |
| 生成したコードをリポジトリへ置く方式（スクリプトで再生成＋最新性の検査テスト） | ビルドの仕組みに触れない。再生成の手順と、最新であることの検査（テスト）が要る | **D4＝(B) をやるなら推奨**（`_wpftmp` を避けられる） |
| T4 | Visual Studio 前提で、CLI のビルド（`dotnet build`）と相性が悪い | 不採用 |
| 他の WPF アプリの設定（例: 設定クラス＋ JSON のソース生成） | 形式が違うため、そのまま流用できない | 参考のみ（考え方は `ControllerSetting` と同じ） |

## 4. 承認をお願いする点
1. **範囲**: 規模が大きいため、RV5 を次の 2 つに分けることを提案する。
   - **RV5**（RV5-1〜RV5-5、RV5-6〜10）: 特性化、`AppSettingsService` の畳み込み、読み込みの補助、解析失敗の修正（D2＝(B)）、プロファイルの宣言型（全領域）。
   - **RV5b**（別 Phase）: RV5-5b（`AppSettingsDTO` のソース生成）。方式は「生成したコードをリポジトリへ置く」を推奨。RV5 の結果を見てから着手する。
2. **土台**: 宣言型は `ControllerSetting` の延長（`ProfileSetting`）で作る。`AppSettingsService` の定型は `SetIfChanged`（`ref`）で畳む。
3. この比較は、手元の知識と、リポジトリの実測によるもの（外部の検索は行っていない）。必要なら、GitHub の類似の実装を探す。
