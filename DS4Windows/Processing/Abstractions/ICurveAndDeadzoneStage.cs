using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;

namespace DS4Windows.Processing.Abstractions
{
    /// <summary>
    /// 入力レポートの補正の段: スティックの回転・アンチスナップバック・ファズ・ドリフト補正、デッドゾーン、感度、スクエアスティック、出力カーブ、トリガーのデッドゾーン、ジャイロの補正。
    /// 入力の状態から、補正した状態（<paramref name="dState"/>）を作って返す。割り当て（マッピング）の前に、毎フレーム呼ぶ。
    /// </summary>
    public interface ICurveAndDeadzoneStage
    {
        ControllerState Apply(int device, ControllerState cState, ControllerState dState, IProfileSettingsService settings);
    }
}
