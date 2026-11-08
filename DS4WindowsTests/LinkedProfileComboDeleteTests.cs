using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Threading;
using Xunit;
using DS4Windows.Core.Platform;
using DS4Windows.Core.Profiles;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase11 W6 K-W6-9（重大・クラッシュ）: リンク（Linked Profile）のコンボが選択しているプロファイルを一覧から削除すると、アプリが落ちていた
    /// （<c>TargetException: Object does not match target type</c>。<c>ValueChangedEventManager.AddHandler → ReflectEventDescriptor.AddEventHandler</c>）。
    /// 原因: <see cref="ProfileEntity"/> は <see cref="INotifyPropertyChanged"/> ではなく、旧式の <c>NameChanged</c> イベントで名前の変更を通知していた。
    /// WPF はこの形のオブジェクトの <c>{Binding Name}</c> に、リフレクションによるイベントの購読を使う。
    /// 修正: <see cref="ProfileEntity"/> に <see cref="INotifyPropertyChanged"/> を実装する（<c>NameChanged</c> の既存の購読先は維持）。
    /// </summary>
    public class Phase11W6LinkedProfileComboDeleteTests
    {
        /// <summary>MainWindow.xaml の linkedProfCombo の DataContext（CompositeDeviceModel）の、必要な部分だけを持つ代役</summary>
        private sealed class LinkRowModel : INotifyPropertyChanged
        {
            private ProfileEntity linked;
            public ObservableCollection<ProfileEntity> ProfileListCol { get; } = new ObservableCollection<ProfileEntity>();
            public ProfileEntity LinkedProfileName
            {
                get => linked;
                set { linked = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LinkedProfileName))); }
            }
            public event PropertyChangedEventHandler PropertyChanged;
        }

        /// <summary>実物の linkedProfCombo と同じ形（SelectedItem の TwoWay、項目のテンプレートが Name を読む）で、コンボを表示して、選択中の項目を削除する</summary>
        private static void SelectThenRemove(Action<LinkRowModel> afterRemove = null)
        {
            string dir = Path.Combine(Path.GetTempPath(), "ds4w-k-w6-9-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var paths = new PathService(dir);
                var model = new LinkRowModel();
                model.ProfileListCol.Add(new ProfileEntity(paths) { Name = "verifyA" });
                model.ProfileListCol.Add(new ProfileEntity(paths) { Name = "verifyF" });
                model.ProfileListCol.Add(new ProfileEntity(paths) { Name = "verifyG" });

                var template = new DataTemplate();
                var text = new FrameworkElementFactory(typeof(TextBlock));
                text.SetBinding(TextBlock.TextProperty, new Binding("Name") { Mode = BindingMode.OneWay, FallbackValue = string.Empty });
                template.VisualTree = text;

                var combo = new ComboBox { ItemTemplate = template, DataContext = model };
                combo.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("ProfileListCol"));
                combo.SetBinding(Selector.SelectedItemProperty, new Binding("LinkedProfileName") { Mode = BindingMode.TwoWay });

                var window = new Window
                {
                    Content = combo,
                    Width = 200,
                    Height = 100,
                    Left = -10000,
                    Top = -10000,
                    ShowInTaskbar = false,
                    WindowStartupLocation = WindowStartupLocation.Manual
                };
                try
                {
                    window.Show();
                    Flush(window.Dispatcher);

                    model.LinkedProfileName = model.ProfileListCol[1];   // verifyF（リンクが指すプロファイル）
                    Flush(window.Dispatcher);
                    Assert.Same(model.ProfileListCol[1], combo.SelectedItem);

                    // MainWindow.DeleteProfBtn_Click の RemoveAt と同じ
                    model.ProfileListCol.RemoveAt(1);
                    Flush(window.Dispatcher);

                    afterRemove?.Invoke(model);
                }
                finally
                {
                    window.Close();
                }
            }
            finally
            {
                try { Directory.Delete(dir, true); } catch { }
            }
        }

        private static void Flush(Dispatcher dispatcher)
        {
            dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() => { }));
        }

        [Trait("Category", "Slow")]   // 0.3 秒以上（実時間で待つ）。途中の確認では除外し、Debug ALL では実行する
        [Fact]
        public void RemovingTheProfileSelectedByTheLinkedProfileCombo_DoesNotThrow()
        {
            RunOnSta(() => SelectThenRemove(model =>
            {
                Assert.Equal(2, model.ProfileListCol.Count);
                Assert.Null(model.LinkedProfileName);   // 選択が外れて、バインディング先も null に戻る
            }));
        }

        [Fact]
        public void ProfileEntity_NotifiesNameChangesThroughBothPaths()
        {
            string dir = Path.Combine(Path.GetTempPath(), "ds4w-k-w6-9-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var entity = new ProfileEntity(new PathService(dir)) { Name = "a" };
                int legacy = 0;
                var changed = new System.Collections.Generic.List<string>();
                entity.NameChanged += (s, e) => legacy++;
                Assert.IsAssignableFrom<INotifyPropertyChanged>(entity);
                ((INotifyPropertyChanged)entity).PropertyChanged += (s, e) => changed.Add(e.PropertyName);

                entity.Name = "b";
                entity.Name = "b";   // 同じ値は通知しない

                Assert.Equal(1, legacy);
                Assert.Equal(new[] { nameof(ProfileEntity.Name) }, changed.ToArray());
            }
            finally
            {
                try { Directory.Delete(dir, true); } catch { }
            }
        }

        private static void RunOnSta(Action action)
        {
            Exception failure = null;
            var thread = new Thread(() =>
            {
                try { action(); }
                catch (Exception ex) { failure = ex; }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            if (failure != null)
            {
                if (failure is Xunit.Sdk.XunitException) throw failure;
                throw new InvalidOperationException("STA スレッドで失敗: " + failure.Message, failure);
            }
        }
    }
}
