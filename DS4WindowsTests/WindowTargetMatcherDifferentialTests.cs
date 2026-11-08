using System.Collections.Generic;
using Xunit;
using DS4Windows.Core.Platform;
using DS4Windows.Core.Profiles;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase12-Step2 2b-1: <see cref="WindowTargetMatcher"/> は、<c>AutoProfileEntity</c> が持っていた照合の規則を取り出した部品。
    /// 取り出し前の実装（下の <c>LegacyEntity</c>。コードを変えずに写したもの）と、同じ入力に同じ結果を返すこと（差分テスト）、
    /// および <c>AutoProfileEntity</c> が部品へ委譲しても結果が変わらないことを確かめる。
    /// </summary>
    public class WindowTargetMatcherDifferentialTests
    {
        private class LegacyEntity
        {
            public string path = string.Empty;
            public string title = string.Empty;
            private string path_lowercase;
            private string title_lowercase;

            public LegacyEntity(string pathStr, string titleStr)
            {
                SetSearchPath(pathStr);
                SetSearchTitle(titleStr);
            }

            public bool IsMatch(string searchPath, string searchTitle)
            {
                bool bPathMatched = true;
                bool bTitleMwatched = true;

                if (!string.IsNullOrEmpty(path_lowercase))
                {
                    bPathMatched = (path_lowercase == searchPath
                        || (path[0] == '^' && searchPath.StartsWith(path_lowercase))
                        || (path[path.Length - 1] == '$' && searchPath.EndsWith(path_lowercase))
                        || (path[0] == '*' && searchPath.Contains(path_lowercase))
                       );
                }

                if (bPathMatched && !string.IsNullOrEmpty(title_lowercase))
                {
                    bTitleMwatched = (title_lowercase == searchTitle
                        || (title[0] == '^' && searchTitle.StartsWith(title_lowercase))
                        || (title[title.Length - 1] == '$' && searchTitle.EndsWith(title_lowercase))
                        || (title[0] == '*' && searchTitle.Contains(title_lowercase))
                       );
                }

                return bPathMatched && bTitleMwatched;
            }

            private void SetSearchPath(string pathStr)
            {
                if (!string.IsNullOrEmpty(pathStr))
                {
                    path = pathStr;
                    path_lowercase = path.ToLower().Replace('/', '\\');

                    if (path.Length >= 2)
                    {
                        if (path[0] == '^') path_lowercase = path_lowercase.Substring(1);
                        else if (path[path.Length - 1] == '$') path_lowercase = path_lowercase.Substring(0, path_lowercase.Length - 1);
                        else if (path[0] == '*') path_lowercase = path_lowercase.Substring(1);
                    }
                }
                else path = path_lowercase = string.Empty;
            }

            private void SetSearchTitle(string titleStr)
            {
                if (!string.IsNullOrEmpty(titleStr))
                {
                    title = titleStr;
                    title_lowercase = title.ToLower();

                    if (title.Length >= 2)
                    {
                        if (title[0] == '^') title_lowercase = title_lowercase.Substring(1);
                        else if (title[title.Length - 1] == '$') title_lowercase = title_lowercase.Substring(0, title_lowercase.Length - 1);
                        else if (title[0] == '*') title_lowercase = title_lowercase.Substring(1);
                    }
                }
                else title = title_lowercase = string.Empty;
            }
        }

        private static readonly string[] Specs =
        {
            null, "", "a", "^", "$", "*", "^c:\\games", "c:\\games\\game.exe$", "*game", "C:/Games/Game.EXE", "^C:/GAMES", "game.exe$", "**", "^$", "*$", "^*",
            "c:\\games\\game.exe", "Test Game Window", "^test", "window$", "*game win", "x"
        };

        private static readonly string[] Inputs =
        {
            "", "a", "c:\\games\\game.exe", "c:\\games\\game.exe.bak", "c:\\games", "d:\\c:\\games\\game.exe", "game.exe", "test game window", "test", "window",
            "my test game window 2", "x", "^", "$", "*", "game win", "c:\\games\\"
        };

        [Fact]
        public void Matcher_ReturnsSameResultAsLegacyImplementation_ForAllCombinations()
        {
            int compared = 0;
            foreach (string pathSpec in Specs)
            {
                foreach (string titleSpec in Specs)
                {
                    var legacy = new LegacyEntity(pathSpec, titleSpec);
                    var matcher = new WindowTargetMatcher(pathSpec, titleSpec);
                    var entity = new AutoProfileEntity(pathSpec, titleSpec);

                    Assert.Equal(legacy.path, matcher.Path);
                    Assert.Equal(legacy.title, matcher.Title);
                    Assert.Equal(legacy.path, entity.path);
                    Assert.Equal(legacy.title, entity.title);

                    foreach (string searchPath in Inputs)
                    {
                        foreach (string searchTitle in Inputs)
                        {
                            bool expected = legacy.IsMatch(searchPath, searchTitle);
                            Assert.True(expected == matcher.IsMatch(searchPath, searchTitle),
                                $"matcher differs: path='{pathSpec}' title='{titleSpec}' searchPath='{searchPath}' searchTitle='{searchTitle}'");
                            Assert.True(expected == entity.IsMatch(searchPath, searchTitle),
                                $"entity differs: path='{pathSpec}' title='{titleSpec}' searchPath='{searchPath}' searchTitle='{searchTitle}'");
                            compared++;
                        }
                    }
                }
            }

            Assert.True(compared > 100000);
        }

        [Fact]
        public void Entity_PathAndTitleSetters_DelegateToMatcherAndKeepOriginalText()
        {
            var entity = new AutoProfileEntity("^C:/Games", "*Window");
            Assert.Equal("^C:/Games", entity.Path);
            Assert.Equal("*Window", entity.Title);
            Assert.True(entity.IsMatch("c:\\games\\game.exe", "my window 1"));

            entity.Path = "game.exe$";
            entity.Title = string.Empty;
            Assert.Equal("game.exe$", entity.Path);
            Assert.Equal(string.Empty, entity.Title);
            Assert.True(entity.IsMatch("c:\\games\\game.exe", "anything"));
            Assert.False(entity.IsMatch("c:\\games\\game.exe.bak", "anything"));
        }
    }
}
