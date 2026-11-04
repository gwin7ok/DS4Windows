namespace DS4Windows.DI
{
    /// <summary>
    /// Phase9 MapCustom の分割 M2（Phase9-MapCustom-Plan.md §3）: 2 段階トリガー（L2／R2 の浅い引きと深い引き）の状態機械。
    /// 以前は <c>Mapping.ProcessTwoStageTrigger</c>。モード（Normal／ExclusiveButtons／HairTrigger／HipFire／HipFireExclusiveButtons）に従い、
    /// どちらの割り当て（浅い引き／深い引き）を有効にするかを決める。毎フレーム呼ばれる高頻度の経路のため、割り当てをしない。
    /// </summary>
    public interface ITwoStageTriggerProcessor
    {
        /// <summary>
        /// 入力の浅い引き／深い引きの割り当てから、今のフレームで有効なもの（なければ null）を <paramref name="outputSoftPull"/>／<paramref name="outputFullPull"/> に返す。
        /// 状態（押し始めの時刻、有効なボタンのモードなど）は <paramref name="twoStageData"/> を更新する。
        /// </summary>
        void Process(int device, byte triggerValue, byte triggerRawValue,
            ref DS4ControlSettings inputSoftPull, ref DS4ControlSettings inputFullPull, TriggerOutputSettings outputSettings,
            TwoStageTriggerMappingData twoStageData, out DS4ControlSettings outputSoftPull, out DS4ControlSettings outputFullPull);
    }
}
