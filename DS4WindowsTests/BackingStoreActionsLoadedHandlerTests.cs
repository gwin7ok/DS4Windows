using DS4Windows.Core.Platform;
using DS4Windows.Core.Settings;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// <c>BackingStore.ReplaceActionsLoadedHandler</c> の振る舞い。
    /// コンテナが作り直されても（登録のラムダが何度呼ばれても）、<c>ActionsLoaded</c> の購読者は最後の 1 つだけになる。
    /// </summary>
    public class BackingStoreActionsLoadedHandlerTests
    {
        [Fact]
        public void ReplacingTheHandler_KeepsOnlyTheLastOne()
        {
            var store = new BackingStore(new EnvironmentService());
            int first = 0, second = 0;

            store.ReplaceActionsLoadedHandler(() => first++);
            store.RaiseActionsLoaded();
            store.ReplaceActionsLoadedHandler(() => second++);
            store.RaiseActionsLoaded();

            Assert.Equal(1, first);
            Assert.Equal(1, second);
        }

        [Fact]
        public void ReplacingWithTheSameHandler_DoesNotSubscribeTwice()
        {
            var store = new BackingStore(new EnvironmentService());
            int count = 0;
            void Handler() => count++;

            store.ReplaceActionsLoadedHandler(Handler);
            store.ReplaceActionsLoadedHandler(Handler);
            store.RaiseActionsLoaded();

            Assert.Equal(1, count);
        }

        [Fact]
        public void OtherSubscribers_AreLeftAlone()
        {
            var store = new BackingStore(new EnvironmentService());
            int other = 0, replaced = 0;
            store.ActionsLoaded += () => other++;

            store.ReplaceActionsLoadedHandler(() => replaced++);
            store.ReplaceActionsLoadedHandler(null);
            store.RaiseActionsLoaded();

            Assert.Equal(1, other);
            Assert.Equal(0, replaced);
        }
    }
}
