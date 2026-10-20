using System.Collections.Generic;

namespace DS4WinWPF.DS4Forms.ViewModels
{
    /// <summary>
    /// Phase10-Step2 W1（Phase10-Step2-Plan.md §3.1）: 共通の編集画面の、トリガーの供給元。SA のモード（<see cref="SpecialActionTriggerSource"/>）は、
    /// 複数のボタンの組み合わせを、選んで変えられる。Controls のモード（W3 で追加）は、対象のボタン 1 つに固定で、変えられない。
    /// トリガーは、従来どおり、名前（<c>DS4ControlNames</c> の名前。例: <c>Cross</c>、<c>L2 Full Pull</c>）の文字列で表す（Actions.xml の書式）。
    /// </summary>
    public interface IActionTriggerSource
    {
        /// <summary>ユーザーが、トリガーを選んで変えられるか（Controls のモードは偽）。</summary>
        bool CanChange { get; }

        /// <summary>選ばれているトリガー（名前）。</summary>
        IReadOnlyList<string> Triggers { get; }

        /// <summary>解除トリガー（名前）。画面には出さないが、保存済みの値を保って書き戻すために持つ。</summary>
        IReadOnlyList<string> UnloadTriggers { get; }

        /// <summary>トリガーを、選ぶ（<c>true</c>）／外す（<c>false</c>）。<see cref="CanChange"/> が偽なら、何もせず、偽を返す。</summary>
        bool SetTrigger(string name, bool selected);

        /// <summary>解除トリガーを、選ぶ／外す。<see cref="CanChange"/> が偽なら、何もせず、偽を返す。</summary>
        bool SetUnloadTrigger(string name, bool selected);

        /// <summary>トリガーを <c>/</c> で連結した文字列（<c>SpecialAction.controls</c> と同じ書式）。</summary>
        string TriggersText { get; }

        /// <summary>解除トリガーを <c>/</c> で連結した文字列。1 つもなければ <c>null</c>（<c>SpecialAction.ucontrols</c> を、書き換えない）。</summary>
        string UnloadTriggersText { get; }

        /// <summary>トリガーの検証。有効なら真。無効なら、偽と、エラーの文言。</summary>
        bool Validate(out string error);
    }
}
