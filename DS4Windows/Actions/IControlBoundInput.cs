namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase10-Step6 W8 相当: Controls タブの束縛（ボタン 1 つの割り当て）の入力アクションの印。<see cref="TriggerDispatchEngine"/> は、この印を持つ入力の束縛を、
    /// ボタンごとの処理の表（<c>ProcessControl</c>／<c>OwnsControl</c>）に登録する（トリガーは 1 つのボタン＝<see cref="IInputAction.Triggers"/> の先頭）。
    /// 以前は <see cref="ControlButtonInputAction"/> の型で見分けていた。Controls の Multi-action（<see cref="ControlMultiTapInputAction"/>）も同じ表に載せるために、印にした。
    /// SA の束縛は、この印を持たない（SA の一覧の順に、<c>ProcessOwned</c> が処理する）。
    /// </summary>
    public interface IControlBoundInput : IInputAction
    {
    }
}
