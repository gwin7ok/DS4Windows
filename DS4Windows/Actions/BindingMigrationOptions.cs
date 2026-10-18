using System.Collections.Generic;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step4 W1: 種別ごとの移行のスイッチ（Phase8-Step2-Design.md §23.2。DI の Singleton。静的な窓口にしない）。
    /// <see cref="MigratedTypes"/> に含まれる種別の特殊アクションだけを、新しい経路（エンジン）が処理する。既定は<b>空</b>（本番の挙動は旧経路のまま）。
    /// Step7 の種別ごとの PR で、該当の種別を足す。ロールバックは、一覧から外すだけ。Step8 で、このクラスごと削除する。
    /// 起動時とテストでだけ書き換える前提（フレームの処理中に書き換えない）。
    /// </summary>
    public sealed class BindingMigrationOptions
    {
        public ISet<SpecialAction.ActionTypeId> MigratedTypes { get; } = new HashSet<SpecialAction.ActionTypeId>();

        public bool IsMigrated(SpecialAction.ActionTypeId type) => MigratedTypes.Count > 0 && MigratedTypes.Contains(type);
    }
}
