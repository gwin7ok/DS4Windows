#nullable enable
using System.Linq;
using DS4Windows.Core.Profiles;

namespace DS4Windows.UI.ViewModels
{
    /// <summary>
    /// 自動プロファイルの 1 行（<see cref="ProgramItem"/>）の、コントローラーごとの選択位置と、
    /// 保存側（<see cref="AutoProfileEntity"/>）のプロファイル名との対応。
    /// 選択位置は、0 が「なし」、1 からがプロファイル一覧の位置。拡張のコントローラー（5〜8）は、使うときだけ扱う。
    /// </summary>
    public sealed class AutoProfileSelectionMapper
    {
        private const int BasicCount = 4;
        private const int ExpandedCount = 8;

        private readonly ProfileList _profileList;

        public AutoProfileSelectionMapper(ProfileList profileList)
        {
            _profileList = profileList;
        }

        /// <summary>画面の選択位置を、保存側のプロファイル名へ書く。位置が 0 以下なら「なし」。</summary>
        public void WriteNames(ProgramItem item, AutoProfileEntity entry, bool usingExpandedControllers)
        {
            int count = usingExpandedControllers ? ExpandedCount : BasicCount;
            for (int i = 0; i < count; i++)
            {
                int index = GetSelectedIndex(item, i);
                entry.ProfileNames[i] = index > 0
                    ? _profileList.ProfileListCol[index - 1].Name ?? AutoProfileEntity.NONE_STRING
                    : AutoProfileEntity.NONE_STRING;
            }
        }

        /// <summary>
        /// 保存側のプロファイル名を、画面の選択位置へ反映する。名前が空・(none) なら 0。
        /// 一覧に無い名前のときは、位置を変えない。
        /// </summary>
        public void ReadIndexes(ProgramItem item, AutoProfileEntity entry, bool usingExpandedControllers)
        {
            int count = usingExpandedControllers ? ExpandedCount : BasicCount;
            for (int i = 0; i < count; i++)
            {
                string profileName = entry.ProfileNames[i];
                if (!string.IsNullOrEmpty(profileName) && profileName != AutoProfileEntity.NONE_STRING)
                {
                    ProfileEntity? found = _profileList.ProfileListCol.SingleOrDefault(x => x.Name == profileName);
                    if (found != null)
                    {
                        SetSelectedIndex(item, i, _profileList.ProfileListCol.IndexOf(found) + 1);
                    }
                }
                else
                {
                    SetSelectedIndex(item, i, 0);
                }
            }
        }

        private static int GetSelectedIndex(ProgramItem item, int controller)
        {
            return controller switch
            {
                0 => item.SelectedIndexCon1,
                1 => item.SelectedIndexCon2,
                2 => item.SelectedIndexCon3,
                3 => item.SelectedIndexCon4,
                4 => item.SelectedIndexCon5,
                5 => item.SelectedIndexCon6,
                6 => item.SelectedIndexCon7,
                7 => item.SelectedIndexCon8,
                _ => 0,
            };
        }

        private static void SetSelectedIndex(ProgramItem item, int controller, int index)
        {
            switch (controller)
            {
                case 0: item.SelectedIndexCon1 = index; break;
                case 1: item.SelectedIndexCon2 = index; break;
                case 2: item.SelectedIndexCon3 = index; break;
                case 3: item.SelectedIndexCon4 = index; break;
                case 4: item.SelectedIndexCon5 = index; break;
                case 5: item.SelectedIndexCon6 = index; break;
                case 6: item.SelectedIndexCon7 = index; break;
                case 7: item.SelectedIndexCon8 = index; break;
            }
        }
    }
}
