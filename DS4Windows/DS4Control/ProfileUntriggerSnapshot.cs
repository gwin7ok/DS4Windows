namespace DS4Windows
{
    /// <summary>
    /// Phase7b W1: プロファイル切替の特殊アクション（「Unload on trigger release」など）の「復帰の合図」の退避用。
    /// <c>Mapping</c> がプロファイル切替時に保存する <c>UntriggerAction</c>／<c>UntriggerIndex</c> を、
    /// <c>ActionManager.ClearDeviceState</c>（内部で <c>Mapping.HandleDeviceDisconnect</c> が <c>deviceRuntime</c> を作り直す）の
    /// 前後で保つために使う。
    /// </summary>
    public readonly struct ProfileUntriggerSnapshot
    {
        public ProfileUntriggerSnapshot(SpecialAction action, int index)
        {
            Action = action;
            Index = index;
        }

        /// <summary>復帰の合図となる特殊アクション。無いときは null。</summary>
        public SpecialAction Action { get; }

        public int Index { get; }
    }
}
