using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Xunit;
using Xunit.Abstractions;
using DS4Windows;
using DS4Windows.DI;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step10-3-1: グループ3（スペシャルアクション系）の View／ViewModel の Global 直接参照解消に先立って
    /// 追加した契約が、<c>Global.SaveAction</c>／<c>Global.RemoveAction</c> と同じ動作（薄い委譲）であることを検証する。
    /// - ISpecialActionRepository.SaveAction: Actions.xml へ保存し、メモリ上のリストが読み直されること
    /// - ISpecialActionRepository.RemoveActionAndSave: メモリ上のリストとファイルの両方から削除されること
    ///   （メモリ上のリストだけを変える RemoveAction とは別物）
    /// Global 状態を変更するため、保存先とアクションの一覧を必ず元に戻す（Phase6-Status.md §6.4-1）。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase6Step10GroupThreeContractTests
    {
        private readonly ITestOutputHelper _output;

        public Phase6Step10GroupThreeContractTests(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>Global.store の Actions.xml の保存先と、アクションの一覧を、退避・復元する</summary>
        private sealed class ActionsStoreScope : IDisposable
        {
            private readonly string _originalPath;
            private readonly List<SpecialAction> _originalActions;
            public string Dir { get; }
            public string ActionsPath { get; }

            public ActionsStoreScope()
            {
                Dir = Path.Combine(Path.GetTempPath(), "Phase6Step10_3_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(Dir);
                ActionsPath = Path.Combine(Dir, "Actions.xml");

                _originalPath = Global.store.m_Actions;
                _originalActions = new List<SpecialAction>(Global.GetActions());

                Global.store.m_Actions = ActionsPath;
                Global.GetActions().Clear();
                // 空の Actions.xml から始める（存在しない場合は既定のアクションが作られるため）
                Global.store.SaveActions();
            }

            public void Dispose()
            {
                Global.store.m_Actions = _originalPath;
                var list = Global.GetActions();
                list.Clear();
                list.AddRange(_originalActions);
                try { Directory.Delete(Dir, true); } catch { }
            }
        }

        private static string[] ActionNamesInFile(string path)
        {
            var doc = new XmlDocument();
            doc.Load(path);
            return doc.SelectNodes("/Actions/Action")
                .Cast<XmlNode>()
                .Select(n => n.Attributes?["Name"]?.Value ?? n.SelectSingleNode("Name")?.InnerText)
                .Where(n => n != null)
                .ToArray();
        }

        [Fact]
        public void SaveAction_AddsNewAction_SavesFileAndNotifies()
        {
            using var scope = new ActionsStoreScope();
            var repo = new SpecialActionRepository();
            int notified = 0;
            repo.ActionsChanged += (s, e) => notified++;

            repo.SaveAction("Step10_3_Macro", "Cross", 2, "C:/a.exe?0", false);

            Assert.True(repo.ActionExists("Step10_3_Macro"));
            SpecialAction action = repo.GetAction("Step10_3_Macro");
            Assert.Equal("Cross", action.controls);
            Assert.Equal("C:/a.exe", action.details);
            Assert.Contains("Step10_3_Macro", ActionNamesInFile(scope.ActionsPath));
            Assert.True(notified > 0);
        }

        [Fact]
        public void SaveAction_Edit_ReplacesExistingAction()
        {
            using var scope = new ActionsStoreScope();
            var repo = new SpecialActionRepository();
            repo.SaveAction("Step10_3_Edit", "Cross", 2, "C:/a.exe?0", false);

            repo.SaveAction("Step10_3_Edit", "Circle", 2, "C:/b.exe?0", true);

            Assert.Equal(1, repo.ActionList.Count(a => a.name == "Step10_3_Edit"));
            SpecialAction action = repo.GetAction("Step10_3_Edit");
            Assert.Equal("Circle", action.controls);
            Assert.Equal("C:/b.exe", action.details);
        }

        [Fact]
        public void SaveAction_MatchesGlobalSaveAction()
        {
            using var scope = new ActionsStoreScope();
            var repo = new SpecialActionRepository();

            Global.SaveAction("Step10_3_ViaGlobal", "Square", 2, "C:/c.exe?0", false);
            repo.SaveAction("Step10_3_ViaRepo", "Square", 2, "C:/c.exe?0", false);

            SpecialAction viaGlobal = repo.GetAction("Step10_3_ViaGlobal");
            SpecialAction viaRepo = repo.GetAction("Step10_3_ViaRepo");
            Assert.Equal(viaGlobal.controls, viaRepo.controls);
            Assert.Equal(viaGlobal.details, viaRepo.details);
            Assert.Equal(viaGlobal.typeID, viaRepo.typeID);
        }

        [Fact]
        public void RemoveActionAndSave_RemovesFromListAndFile()
        {
            using var scope = new ActionsStoreScope();
            var repo = new SpecialActionRepository();
            repo.SaveAction("Step10_3_Remove", "Cross", 2, "C:/a.exe?0", false);
            repo.SaveAction("Step10_3_Keep", "Circle", 2, "C:/d.exe?0", false);
            Assert.Contains("Step10_3_Remove", ActionNamesInFile(scope.ActionsPath));

            bool existed = repo.RemoveActionAndSave("Step10_3_Remove");

            Assert.True(existed);
            Assert.False(repo.ActionExists("Step10_3_Remove"));
            Assert.True(repo.ActionExists("Step10_3_Keep"));
            string[] names = ActionNamesInFile(scope.ActionsPath);
            Assert.DoesNotContain("Step10_3_Remove", names);
            Assert.Contains("Step10_3_Keep", names);
        }

        [Fact]
        public void RemoveActionAndSave_ReturnsFalseForUnknownAction()
        {
            using var scope = new ActionsStoreScope();
            var repo = new SpecialActionRepository();
            repo.SaveAction("Step10_3_Keep", "Circle", 2, "C:/d.exe?0", false);

            Assert.False(repo.RemoveActionAndSave("Step10_3_Missing"));
            Assert.True(repo.ActionExists("Step10_3_Keep"));
        }

        [Fact]
        public void RemoveAction_InMemoryOnly_DoesNotChangeFile()
        {
            // 既存の RemoveAction（メモリ上のリストだけ）との違いを固定する
            using var scope = new ActionsStoreScope();
            var repo = new SpecialActionRepository();
            repo.SaveAction("Step10_3_MemOnly", "Cross", 2, "C:/a.exe?0", false);

            Assert.True(repo.RemoveAction("Step10_3_MemOnly"));

            Assert.False(repo.ActionExists("Step10_3_MemOnly"));
            Assert.Contains("Step10_3_MemOnly", ActionNamesInFile(scope.ActionsPath));
        }
    }
}
