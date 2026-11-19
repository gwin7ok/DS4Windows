using System;
using System.Windows;
using System.Windows.Controls;
using DS4Windows.UI.Services;
using DS4Windows.UI.ViewModels;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// トレイのメニューの項目（ViewModel の <see cref="TrayMenuItemModel"/>）を WPF のメニューへ変換する View 側の部品の検査。
    /// 見出しの _ のエスケープ（アクセスキーの無効化）、状態の反映、クリックの委譲、組み直しで古い項目が残らないことを確かめる。
    /// </summary>
    public class TrayContextMenuBuilderTests
    {
        [Fact]
        public void Rebuild_MapsHeaderStateSeparatorAndChildren()
        {
            RunSta(() =>
            {
                int clicked = 0;
                var parent = new TrayMenuItemModel { Header = "Controller 1" };
                parent.Children.Add(new TrayMenuItemModel { Header = "my_profile", IsChecked = true, Click = () => clicked++ });
                var open = new TrayMenuItemModel { Header = "Open", IsBold = true };
                var disabled = new TrayMenuItemModel { Header = "Start", IsEnabled = false };

                var menu = new ContextMenu();
                TrayContextMenuBuilder.Rebuild(menu, new[] { parent, TrayMenuItemModel.Separator(), open, disabled });

                Assert.Equal(4, menu.Items.Count);
                var top = Assert.IsType<MenuItem>(menu.Items[0]);
                Assert.Equal("Controller 1", top.Header);
                var child = Assert.IsType<MenuItem>(top.Items[0]);
                Assert.Equal("my__profile", child.Header);
                Assert.True(child.IsChecked);
                child.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                Assert.Equal(1, clicked);

                Assert.IsType<Separator>(menu.Items[1]);
                Assert.Equal(FontWeights.Bold, Assert.IsType<MenuItem>(menu.Items[2]).FontWeight);
                Assert.False(Assert.IsType<MenuItem>(menu.Items[3]).IsEnabled);
            });
        }

        [Fact]
        public void Rebuild_ReplacesPreviousItems()
        {
            RunSta(() =>
            {
                var menu = new ContextMenu();
                TrayContextMenuBuilder.Rebuild(menu, new[] { new TrayMenuItemModel { Header = "A" }, new TrayMenuItemModel { Header = "B" } });
                TrayContextMenuBuilder.Rebuild(menu, new[] { new TrayMenuItemModel { Header = "C" } });

                Assert.Single(menu.Items);
                Assert.Equal("C", Assert.IsType<MenuItem>(menu.Items[0]).Header);
            });
        }

        // 画面部品は、共有の STA スレッドで作る（自前のスレッドで作ると、終わったスレッドの部品が、他のテストの読み込みを止める）
        private static void RunSta(Action action)
        {
            WpfPanelTestRunner.Run(action);
        }
    }
}
