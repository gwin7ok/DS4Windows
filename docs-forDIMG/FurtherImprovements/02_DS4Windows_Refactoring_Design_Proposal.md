# DS4Windows リファクタリング詳細設計書
**〜 PureDI深化・部品共通化・機能カートリッジ化・テスト堅牢化の実現仕様 〜**

- **対象リポジトリ**: `gwin7ok/DS4Windows`
- **対象ドキュメント**: `01_DS4Windows_Architecture_Analysis_Report.md` に対応する処方箋
- **文書種別**: 詳細設計・リファクタリング提案仕様書 (To-Be Design Proposal)

---

## 1. 全体アーキテクチャビジョン（To-Beターゲットモデル）

本リファクタリングでは、現行の堅牢な基盤を破壊することなく、以下の3層アーキテクチャへの段階的進化を目指す。

```text
┌────────────────────────────────────────────────────────┐
│  Presentation Layer                                    │
│   ControllerVisualizerControl / LabeledSliderControl   │ (完全部品共通化)
│   Tokenized Dark / Light Themes                        │ (完全セマンティック化)
└───────────────────────────┬────────────────────────────┘
                            │
┌───────────────────────────▼────────────────────────────┐
│  Application / Domain Layer (Pure DI 3大コーディネーター) │
│   ┌─────────────────────┐┌────────────────────────┐    │
│   │InputDeviceCoord.    ││VirtualOutputCoord.     │    │
│   └──────────┬──────────┘└───────────┬────────────┘    │
│              ▼                       ▼                 │
│   ┌───────────────────────────────────────────────┐    │
│   │ProfileStateCoordinator                        │    │
│   └───────────────────────────────────────────────┘    │
└───────────────────────────┬────────────────────────────┘
                            │
┌───────────────────────────▼────────────────────────────┐
│  Device & Infrastructure Cartridges (プラグイン化)     │
│   [IHidTransport]  ──(Raw Bytes)──>  [IInputReportParser]│ (Layer 1 分離)
│   [IVirtualControllerDevice]         [IActionPlugin]   │ (仮想出力/Action)
└────────────────────────────────────────────────────────┘
```

---

## 2. 【設計1: UI共通化】メタデータ駆動型レイアウト & 共通コントロール設計

### 2.1 コントローラー描画コンポーネントの完全独立化
コントローラーの画像表示と、その上の各ボタン（当たり判定・ホバー表示）を単一の再利用可能なカスタムコントロールへ集約する。

#### ① レイアウト定義メタデータ（C#）
ボタンの座標を絶対ピクセルではなく、**「画像左上を (0.0, 0.0)、右下を (1.0, 1.0) とする正規化相対比率」** で定義する。

```csharp
namespace DS4WinWPF.DS4Forms.Controls
{
    public sealed record HitboxRegion(
        string ButtonTag,          // "Cross", "Circle", "A", "Guide" 等
        double CenterXRatio,       // 0.0 〜 1.0
        double CenterYRatio,       // 0.0 〜 1.0
        double WidthRatio,         // 画像幅に対するボタン幅比率
        double HeightRatio,        // 画像高さに対するボタン高さ比率
        string DefaultDisplayName
    );

    public interface IControllerLayoutProfile
    {
        string ImageResourceKey { get; }
        IReadOnlyList<HitboxRegion> Hitboxes { get; }
    }

    // DS4用のレイアウト定義（メタデータ）
    public sealed class DualShock4LayoutProfile : IControllerLayoutProfile
    {
        public string ImageResourceKey => "DS4ConfigImg";

        public IReadOnlyList<HitboxRegion> Hitboxes { get; } = new List<HitboxRegion>
        {
            new("Cross",    0.730, 0.585, 0.059, 0.105, "Cross"),
            new("Circle",   0.785, 0.485, 0.059, 0.105, "Circle"),
            new("Square",   0.675, 0.480, 0.059, 0.105, "Square"),
            new("Triangle", 0.730, 0.380, 0.059, 0.105, "Triangle"),
            new("DpadUp",   0.260, 0.380, 0.045, 0.140, "Up"),
            new("DpadDown", 0.260, 0.575, 0.045, 0.140, "Down"),
            new("DpadLeft", 0.205, 0.480, 0.080, 0.080, "Left"),
            new("DpadRight",0.315, 0.480, 0.080, 0.080, "Right"),
            new("L1",       0.230, 0.140, 0.110, 0.080, "L1"),
            new("R1",       0.770, 0.140, 0.110, 0.080, "R1"),
            // ... 他のボタンもすべて相対比率で定義
        };
    }
}
```

#### ② 共通コントロール（`ControllerVisualizerControl.xaml`）
```xml
<UserControl x:Class="DS4WinWPF.DS4Forms.Controls.ControllerVisualizerControl"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Border Background="{DynamicResource ControllerCanvasBackground}"
            CornerRadius="4"
            ClipToBounds="True">
        <Grid x:Name="RootLayoutGrid" SizeChanged="OnRootGridSizeChanged">
            <!-- コントローラー本体画像 -->
            <Image x:Name="BodyImage"
                   Stretch="Uniform"
                   HorizontalAlignment="Center"
                   VerticalAlignment="Center"/>

            <!-- ボタン当たり判定を乗せる透過Canvas（画像の実際のレンダリングサイズに一致） -->
            <Canvas x:Name="HitboxCanvas"
                    HorizontalAlignment="Center"
                    VerticalAlignment="Center"/>
        </Grid>
    </Border>
</UserControl>
```

#### ③ レンダリング追従ロジック（`ControllerVisualizerControl.xaml.cs`）
```csharp
private void LayoutHitboxes()
{
    if (_currentProfile == null || BodyImage.Source == null) return;

    // Uniform ストレッチされた画像の実際の描画サイズとオフセットを算出
    var imageSize = GetUniformImageRenderSize(BodyImage);
    HitboxCanvas.Width = imageSize.Width;
    HitboxCanvas.Height = imageSize.Height;

    HitboxCanvas.Children.Clear();
    foreach (var region in _currentProfile.Hitboxes)
    {
        var btn = new Button
        {
            Tag = region.ButtonTag,
            Style = (Style)FindResource("NoBGHoverBtn"),
            Width = region.WidthRatio * imageSize.Width,
            Height = region.HeightRatio * imageSize.Height
        };

        btn.Click += (s, e) => ButtonClicked?.Invoke(this, region.ButtonTag);
        btn.MouseEnter += (s, e) => ButtonHovered?.Invoke(this, region.ButtonTag);
        btn.MouseLeave += (s, e) => ButtonUnhovered?.Invoke(this, region.ButtonTag);

        // 中心座標からLeft/Topを逆算配置
        double left = (region.CenterXRatio * imageSize.Width) - (btn.Width / 2.0);
        double top = (region.CenterYRatio * imageSize.Height) - (btn.Height / 2.0);

        Canvas.SetLeft(btn, left);
        Canvas.SetTop(btn, top);
        HitboxCanvas.Children.Add(btn);
    }
}
```

### 2.2 スライダー＋数値表示の共通コンポーネント化（`SettingAxisRow.xaml`）
文字切れの原因となったスライダー行を単一のコンポーネントに集約する。

```xml
<UserControl x:Class="DS4WinWPF.DS4Forms.Controls.SettingAxisRow"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:xctk="http://schemas.xceed.com/wpf/xaml/toolkit">
    <Grid Margin="0,2">
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="100"/>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="65"/>
            <ColumnDefinition Width="Auto"/>
        </Grid.ColumnDefinitions>

        <Label Content="{Binding LabelText, RelativeSource={RelativeSource AncestorType=UserControl}}"
               VerticalAlignment="Center" Padding="2"/>

        <Slider Grid.Column="1"
                Value="{Binding Value, RelativeSource={RelativeSource AncestorType=UserControl}, Mode=TwoWay}"
                Minimum="{Binding Minimum, RelativeSource={RelativeSource AncestorType=UserControl}}"
                Maximum="{Binding Maximum, RelativeSource={RelativeSource AncestorType=UserControl}}"
                VerticalAlignment="Center" Margin="6,0"/>

        <!-- 3桁＋小数点も絶対に切れない固定幅とPadding抑制 -->
        <xctk:DoubleUpDown Grid.Column="2"
                           Value="{Binding Value, RelativeSource={RelativeSource AncestorType=UserControl}, Mode=TwoWay}"
                           FormatString="{Binding FormatString, RelativeSource={RelativeSource AncestorType=UserControl}}"
                           Padding="2,1"
                           HorizontalContentAlignment="Right"/>

        <Label Grid.Column="3"
               Content="{Binding UnitText, RelativeSource={RelativeSource AncestorType=UserControl}}"
               Visibility="{Binding UnitVisibility, RelativeSource={RelativeSource AncestorType=UserControl}}"
               VerticalAlignment="Center" Padding="4,2"/>
    </Grid>
</UserControl>
```

---

## 3. 【設計2: PureDI深化】`ControlService` 3大コーディネーター分割設計

### 3.1 責務の明確化とサブコーディネーターの定義

```text
┌────────────────────────────────────────────────────────┐
│ ControlService (ライフサイクル統括: Start / Stop / 状態) │
└───────────────┬────────────────────────┬───────────────┘
                │                        │
  ┌─────────────▼────────────┐  ┌────────▼─────────────────┐
  │ IInputDeviceCoordinator   │  │ IVirtualOutputCoordinator │
  │ (物理デバイスの認識・切断) │  │ (スロット配分・ViGEm出力)│
  └─────────────┬────────────┘  └────────┬─────────────────┘
                │ (一方向イベント)        │
                └───────────┐┌───────────┘
                            ▼▼
              ┌─────────────────────────────┐
              │ IProfileStateCoordinator    │
              │ (プロファイル読込・設定同期) │
              └─────────────────────────────┘
```

#### ① 各コーディネーターのインターフェース契約
```csharp
public interface IInputDeviceCoordinator : IDisposable
{
    event EventHandler<DeviceConnectedEventArgs> DeviceConnected;
    event EventHandler<DeviceDisconnectedEventArgs> DeviceDisconnected;
    IReadOnlyList<DS4Device> ConnectedDevices { get; }
    void StartEnumeration();
    void StopEnumeration();
}

public interface IVirtualOutputCoordinator : IDisposable
{
    void BindDeviceToSlot(int deviceIndex, OutContType outType);
    void UnbindDeviceFromSlot(int deviceIndex);
    void ProcessInputReport(int deviceIndex, in UniversalControllerState state);
}

public interface IProfileStateCoordinator
{
    void ApplyProfile(int slotIndex, string profileName);
    DS4ControlSettings GetControlSettings(int slotIndex, DS4Controls control);
}
```

### 3.2 `ControlService` のスリム化（34引数 → 4引数へ）
```csharp
public class ControlService : IServiceLifecycle
{
    private readonly IInputDeviceCoordinator _inputCoordinator;
    private readonly IVirtualOutputCoordinator _outputCoordinator;
    private readonly IProfileStateCoordinator _profileCoordinator;
    private readonly ILogger<ControlService> _logger;

    public ControlService(
        IInputDeviceCoordinator inputCoordinator,
        IVirtualOutputCoordinator outputCoordinator,
        IProfileStateCoordinator profileCoordinator,
        ILogger<ControlService> logger)
    {
        _inputCoordinator = inputCoordinator;
        _outputCoordinator = outputCoordinator;
        _profileCoordinator = profileCoordinator;
        _logger = logger;

        // イベントによる疎結合な連携（Func<T> を完全撲滅）
        _inputCoordinator.DeviceConnected += OnDeviceConnected;
        _inputCoordinator.DeviceDisconnected += OnDeviceDisconnected;
    }

    private void OnDeviceDisconnected(object sender, DeviceDisconnectedEventArgs e)
    {
        _outputCoordinator.UnbindDeviceFromSlot(e.SlotIndex);
    }
}
```

### 3.3 `ServiceProviderHolder`（Service Locator）の完全撤廃
`Actions/` サブシステムがサービスロケーターを叩くのではなく、実行コンテキスト（`ActionExecutionContext`）経由で必要な依存を受け取るように統一する。

```csharp
public record ActionExecutionContext(
    int SlotIndex,
    DateTime Timestamp,
    IOutputContext OutputContext,
    IProfileSwitcher ProfileSwitcher,
    IProcessLauncher ProcessLauncher
);

public interface IOutputAction
{
    FrameResult Execute(in TriggerState trigger, in ActionExecutionContext context);
}
```

---

## 4. 【設計3: 機能カートリッジ化】プラグインアーキテクチャ仕様

### 4.1 Layer 1（入力デバイス層）の完全分離仕様

```text
[ 物理デバイス (USB / BT) ]
           │ Raw Data Stream
           ▼
┌──────────────────────────────────────┐
│ IHidTransport (通信ドライバ抽象化)      │
│  - ReadReportAsync(Memory<byte> buf) │
│  - WriteReportAsync(ReadOnlyMemory)  │
└──────────────────┬───────────────────┘
                   │ byte[] (純粋な配列)
                   ▼
┌──────────────────────────────────────┐
│ IInputReportParser (解析カートリッジ)   │ <── OS/Windows非依存の純粋C#
│  - TryParse(span, ref state)         │     (単体テストカバレッジ100%可能)
└──────────────────┬───────────────────┘
                   │ UniversalControllerState (正規化構造体)
                   ▼
[ 共通マッピングエンジン (Mapping.cs) ]
```

### 4.2 コントローラー追加カートリッジの標準実装例
新規コントローラー（例: Flydigi Vader 4 Pro 等）を追加する場合の最小実装セット：

```csharp
// 1. 純粋解析ロジック
public sealed class Vader4ProReportParser : IInputReportParser
{
    public bool CanHandle(ushort vid, ushort pid) => vid == 0x2D79 && pid == 0x0040;

    public bool TryParse(ReadOnlySpan<byte> report, ref UniversalControllerState state)
    {
        if (report.Length < 32 || report[0] != 0x01) return false;

        // スティック正規化 (0〜255 -> -1.0〜+1.0)
        state.LeftStickX = (report[4] - 128) / 128.0f;
        state.LeftStickY = (report[5] - 128) / 128.0f;

        // ボタンフラグ解析
        state.ButtonCross = (report[8] & 0x01) != 0;
        state.ButtonCircle = (report[8] & 0x02) != 0;
        // 追加パドル (C, Z, M1〜M4) もダイレクトに格納
        return true;
    }
}

// 2. デバイス定義モジュール（DIコンテナに登録するだけで自動認識）
public sealed class Vader4ProDeviceModule : IInputDeviceModule
{
    public DeviceType DeviceType => DeviceType.Flydigi;
    public IEnumerable<VidPidInfo> SupportedVidPids { get; } = new[] { new VidPidInfo(0x2D79, 0x0040) };
    public IInputReportParser CreateParser() => new Vader4ProReportParser();
}
```

---

## 5. 【設計4: テスト堅牢化】セマンティックXML構造検証基盤

### 5.1 XAML解析テストヘルパー（`XamlTestHelper.cs`）
正規表現や文字列マッチングを完全排除し、XMLノード構造として安全にテストを行う基盤クラス。

```csharp
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace DS4WindowsTests.TestHelpers
{
    public static class XamlTestHelper
    {
        private static readonly XNamespace XamlNs = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        private static readonly XNamespace XNs = "http://schemas.microsoft.com/winfx/2006/xaml";

        public static XDocument Load(string relativePath)
        {
            string fullPath = SourceFileLocator.Find(relativePath);
            Assert.NotNull(fullPath);
            return XDocument.Parse(File.ReadAllText(fullPath));
        }

        public static XElement GetElementByName(XDocument doc, string xName)
        {
            return doc.Descendants().FirstOrDefault(e => e.Attribute(XNs + "Name")?.Value == xName);
        }

        public static XElement GetFirstDescendant(XDocument doc, string localName)
        {
            return doc.Descendants().FirstOrDefault(e => e.Name.LocalName == localName);
        }
    }
}
```

### 5.2 壊れないUIテストの実装例（`ProfileEditorLayoutTests` の書き換え）

```csharp
[Fact]
public void TheControlsTab_LayoutStructure_IsMaintained()
{
    var doc = XamlTestHelper.Load("DS4Windows/DS4Forms/ProfileEditor.xaml");

    // 1. Controlsタブの親Gridの検証（属性の改行や空白の数に左右されない）
    var controlsGrid = doc.Descendants()
        .FirstOrDefault(e => e.Name.LocalName == "Grid" && e.Attribute("Width")?.Value == "470");
    Assert.NotNull(controlsGrid);

    // 2. RowDefinitions の構造検証
    var rowDefs = controlsGrid.Elements()
        .FirstOrDefault(e => e.Name.LocalName == "Grid.RowDefinitions")
        ?.Elements()
        .ToList();

    Assert.NotNull(rowDefs);
    Assert.Equal(2, rowDefs.Count);
    Assert.Equal("252", rowDefs[0].Attribute("Height")?.Value.Trim());
    Assert.Equal("*", rowDefs[1].Attribute("Height")?.Value.Trim());

    // 3. conCanvas が先頭行（Row 0）にあることを検証
    var canvas = XamlTestHelper.GetElementByName(doc, "conCanvas");
    Assert.NotNull(canvas);
    Assert.Equal("0", canvas.Attribute("Grid.Row")?.Value);
}
```

---

## 6. 段階的マイグレーション計画（移行ロードマップ）

既存のテストスイート（2,728件）を常時グリーンの状態に保ちながら安全に実施するための計画：

```text
[ Phase A: テスト堅牢化 & UI修正 ]
  ├── 1. XamlTestHelper をテストプロジェクトに導入
  ├── 2. 壊れやすい文字列/正規表現アサートをノード解析テストへ置換
  └── 3. LabeledSlider / SettingAxisRow を導入し、数値見切れバグを根絶
         │
         ▼ (回帰テスト常時成功確認)
[ Phase B: コントローラー描画の共通化 ]
  ├── 1. ControllerLayoutProfile (DS4, Xbox) の定義
  ├── 2. ControllerVisualizerControl の作成
  └── 3. ProfileEditor と BindingWindow の Canvas を当コントロールへ統合
         │
         ▼ (UI結合テストのパス確認)
[ Phase C: PureDI の純度向上 ]
  ├── 1. IInputDeviceCoordinator 等の3大インターフェース切り出し
  ├── 2. 双方向参照をイベント購読へ変換し、遅延 Func<T> を全廃
  └── 3. ControlService のコンストラクタ引数を4個に集約
         │
         ▼ (DI配線テスト・ライフサイクルテスト確認)
[ Phase D: Layer 1 カートリッジ化 ]
  ├── 1. IHidTransport と IInputReportParser の分離
  └── 2. 新規コントローラー（サードパーティ製パッド）のプラグイン基盤を確立
```

---
**本提案書の作成完了**