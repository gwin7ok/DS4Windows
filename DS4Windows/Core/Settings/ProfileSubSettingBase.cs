using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DS4Windows.Core.Settings
{
    /// <summary>
    /// プロファイルのネストされたサブ設定クラス（スティック詳細、トリガー、ジャイロなど）向けの抽象基底クラス。
    /// INotifyPropertyChanged を実装します。
    ///
    /// 親のプロファイルコンテナへバブリング通知する OnSubPropertyChanged イベントは、
    /// 唯一の購読の仕組み（IProfileSettingsService.WireSubSettingsEvents。Phase7-Step2 W2 で削除）がなくなり、購読者が 0 件のため削除した。
    /// </summary>
    public abstract class ProfileSubSettingBase : ObservableObject
    {

        protected virtual void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
        {
            OnPropertyChanged(propertyName);
        }

        /// <summary>
        /// 全プロパティの変更通知を一括発火します（Reset 処理時など）
        /// </summary>
        protected virtual void RaiseAllPropertiesChanged()
        {
            OnPropertyChanged(string.Empty);
        }
    }
}