# DS4Windows モダン WPF UI & デザインシステム仕様書
**〜 XAMLの文字切れ・配置ズレ・テーマ崩れを根絶するUI基盤設計 〜**

- **対象リポジトリ**: `gwin7ok/DS4Windows`
- **文書番号**: 06
- **文書種別**: UI/UXデザインシステム & XAML実装仕様書 (Design System Specification)
- **前提文書**: `01_DS4Windows_Architecture_Analysis_Report.md`, `02_DS4Windows_Refactoring_Design_Proposal.md`

---

## 1. デザインシステムの基本方針

本仕様書は、今回発生した「ダークテーマの視認性不良」「ライトテーマでグレーの座布団が出る」「数値が255から25に切れる」「フォント色の型不一致クラッシュ」などのUI課題を恒久的に防ぎ、**Windows 11（Fluent Design）に調和した洗練された外観** を確立するための設計規約を定めます。

```text
[ 3大デザイン原則 ]
1. セマンティック・トークン化: XAML内に純白(#FFF)や純黒(#000)などのハードコード色を禁止する。
2. 対称型テーマ定義        : DarkTheme.xaml と DefaultTheme.xaml のキーを1対1で完全対応させる。
3. エレベーション（階層感）  : 奥にある背景ほど暗く、手前（カード・入力欄）ほど明るくして奥行きを出す。
```

---

## 2. デザイントークン体系（Color Tokens）

### 2.1 セマンティック・カラーパレット一覧

コントロール側では具体的なカラーコードを意識せず、以下の「役割（Token）」のみを参照します。

| トークン名 (Brush Key) | 役割 | Dark Theme (`DarkTheme.xaml`) | Light Theme (`DefaultTheme.xaml`) |
| :--- | :--- | :---: | :---: |
| **`WindowBackground`** | ウィンドウ最背面 | `#FF1F1F24` (スモーキーダーク) | `#FFF9F9FB` (オフホワイト) |
| **`SurfaceCardBackground`** | パネル、カード、枠の背景 | `#FF282830` (一段明るいグレー) | `#FFFFFFFF` (純白カード) |
| **`ControlBackground`** | ボタン、入力ボックス背景 | `#FF32323A` (ミディアムグレー) | `#FFF0F0F3` (ライトグレー) |
| **`ControlHoverBackground`** | マウスホバー時 | `#FF3E3E48` | `#FFE4E4E8` |
| **`ControlBorderBrush`** | コントロール境界線 (控えめ) | `#FF3F3F46` (上品な枠線) | `#FFD1D5DB` (境界グレー) |
| **`TextPrimaryBrush`** | 主要テキスト | `#FFE4E4E7` (眩しくない白) | `#FF111827` (落ち着いた黒) |
| **`TextSecondaryBrush`** | 単位・未選択テキスト | `#FF9CA3AF` (補助グレー) | `#FF6B7280` (補助グレー) |
| **`AccentBrandBrush`** | ブランド・アクティブ線 | `#FF2563EB` (モダンブルー) | `#FF0066CC` (ブルー) |
| **`SelectionIndicatorBrush`**| 選択中のキー/ボタン | `#FF059669` (エメラルド緑) | `#FF10B981` (エメラルド緑) |

---

## 3. 型安全性の確保と命名規則

### 3.1 クラッシュを防止するリソース命名ルール
WPFで `Color` 属性と `Brush` 属性を取り違えてクラッシュ（`EvaluateExpression` 例外）するミスを防ぐため、サフィックスを強制します。

* **ブラシリソース (`SolidColorBrush`)**: 末尾に必ず **`Brush`** を付ける。
  * ○: `TextPrimaryBrush`, `ControlBackgroundBrush`
  * ×: `ForegroundColor`, `BackgroundColor`（型が曖昧になり誤認する原因）
* **生カラーリソース (`Color`)**: 末尾に必ず **`Color`** を付ける。
  * ○: `TextPrimaryColor`, `WindowBackgroundColor`

```xml
<!-- テーマファイル定義例 -->
<Color x:Key="TextPrimaryColor">#FFE4E4E7</Color>
<SolidColorBrush x:Key="TextPrimaryBrush" Color="{StaticResource TextPrimaryColor}" />
```

---

## 4. 共通UIコンポーネント詳細仕様

### 4.1 数値見切れを物理的に根絶する `InputNumberBox` スタイル

WPF標準の `Label` のデフォルトPadding（左右計10px）による文字見切れを防止するため、数値表示専用の標準スタイルを定義します。

```xml
<!-- 読み取り専用の数値表示用TextBlockスタイル -->
<Style x:Key="ReadingValueText" TargetType="{x:Type TextBlock}">
    <Setter Property="FontFamily" Value="Consolas, Segoe UI, sans-serif" />
    <Setter Property="FontSize" Value="12" />
    <Setter Property="Foreground" Value="{DynamicResource TextPrimaryBrush}" />
    <Setter Property="TextAlignment" Value="Right" />
    <Setter Property="VerticalAlignment" Value="Center" />
    <!-- 3桁（-128〜255）が絶対に欠けない最小幅を保証 -->
    <Setter Property="MinWidth" Value="36" />
    <Setter Property="Margin" Value="2,0,4,0" />
</Style>
```

---

### 4.2 コントローラー座布団問題を解決する `SurfaceCard` テンプレート

ControlsタブやBindingWindowで「ライトモード時にグレーの座布団が出る」問題を解決するための標準カード外枠です。

```xml
<!-- カード/座布団の標準スタイル -->
<Style x:Key="SurfaceCardBorder" TargetType="{x:Type Border}">
    <Setter Property="Background" Value="{DynamicResource SurfaceCardBackground}" />
    <Setter Property="BorderBrush" Value="{DynamicResource ControlBorderBrush}" />
    <Setter Property="BorderThickness" Value="1" />
    <Setter Property="CornerRadius" Value="6" />
    <Setter Property="Padding" Value="8" />
</Style>
```

#### 各テーマでの見え方：
* **Dark Theme**: 背景（`#1F1F24`）の上にカード（`#282830`）が一段浮き上がり、コントローラーの輪郭を綺麗に引き立てる。
* **Light Theme**: 背景（`#F9F9FB`）の上に白いカード（`#FFFFFF`）と薄いボーダー（`#D1D5DB`）が載り、**従来の灰色座布団（DimGray）のような野暮ったさが完全に消え、清潔感のある外観** になる。

---

## 5. UI実装チェックリスト（開発者向けガイドライン）

新規画面作成やXAML修正時に順守すべきルール：

1. [ ] **インラインカラー禁止**: `Background="#1F1F1F"` や `Foreground="White"` をXAMLに直接書かない。必ず `{DynamicResource ...Brush}` を使う。
2. [ ] **Labelの幅固定禁止**: 幅を固定する場合は、必ず `Padding="0"` または `Padding="2,1"` を明示するか、`TextBlock` を使用する。
3. [ ] **テーマの対称性**: `DarkTheme.xaml` に新しいブラシを追加した場合、必ず `DefaultTheme.xaml` にも同じキーのブラシを対になる色で定義する。
4. [ ] **コントローラー画像**: コントローラーの線画や画像を描画する際は、直接CanvasのLeft/Topにハードコードせず、`ControllerVisualizerControl` を使用する。