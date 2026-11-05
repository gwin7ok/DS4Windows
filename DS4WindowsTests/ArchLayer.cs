namespace DS4WindowsTests
{
    /// <summary>
    /// Phase14b-Step1（R-01）: 層の依存の検査で使う層の区分（規約 §1、§3.4 R1）。名前空間から <see cref="LayerMap.Classify"/> が決める。
    /// </summary>
    public enum ArchLayer
    {
        /// <summary>本体の外（.NET、第三者のライブラリー、コンパイラーが作る名前空間なしの型）。検査の対象外。</summary>
        External,

        /// <summary>どの層からも使ってよい共有の資源（<c>DS4Windows.Properties</c>、<c>DS4Windows.Translations</c>）。</summary>
        Shared,

        /// <summary>第1層（<c>DS4Windows.Input</c>）。</summary>
        Input,

        /// <summary>第2層の契約（<c>DS4Windows.Processing.Abstractions</c>）。第3層が実装してよいポートを含む（§3.4 R2）。</summary>
        ProcessingAbstractions,

        /// <summary>第2層（<c>DS4Windows.Processing</c>。<c>Abstractions</c> を除く）。</summary>
        Processing,

        /// <summary>第3層（<c>DS4Windows.Output</c>）。</summary>
        Output,

        /// <summary>4-a（<c>DS4Windows.UI</c>）。</summary>
        UI,

        /// <summary>4-b（<c>DS4Windows.Composition</c> と、ルート名前空間 <c>DS4Windows</c> のアプリの起点 <c>App</c>）。Phase14b-Step15: 旧 <c>StartupMethods</c> は <c>Core/Platform/StartupEntryService</c> へ移した。</summary>
        Composition,

        /// <summary>横断基盤・永続化（<c>DS4Windows.Core</c>）。</summary>
        Core,
    }
}
