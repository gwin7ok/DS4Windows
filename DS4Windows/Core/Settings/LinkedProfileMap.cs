using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DS4Windows.DI;

namespace DS4Windows
{
    /// <summary>
    /// Phase10-Step4d W5d（Phase10-Step4d-Plan.md §4 W5）: リンクプロファイル（コントローラーのシリアル → プロファイル）の、実行時の保持。<c>BackingStore.linkedProfiles</c> の型
    /// （以前は <c>Dictionary&lt;string, string&gt;</c>）。使い方（インデクサー、<c>ContainsKey</c>、<c>Remove</c>、<c>TryGetValue</c>、<c>Keys</c>、列挙）は辞書と同じで、
    /// 値は<b>プロファイルの名前を読み書きする窓口</b>（<see cref="ProfileSlotNames"/> と同じ。保管は固定 ID と名前の控え）。
    /// 書くと、名前の控えと、登録簿から引いた ID を保存し、読むと、ID から現在の名前を返す（プロファイルの名前を変えても、リンクを書き換えなくてよい）。
    /// 登録簿がなければ、従来の辞書と同じに動く。登録簿は <see cref="BackingStore.AttachProfileIdentity"/> が結ぶ。
    /// </summary>
    public sealed class LinkedProfileMap : IEnumerable<KeyValuePair<string, string>>
    {
        private readonly Dictionary<string, ProfileSlotNames> _cells = new Dictionary<string, ProfileSlotNames>();
        private IProfileIdentityService _identity;

        public int Count => _cells.Count;

        /// <summary>シリアルの一覧（その時点の写し。列挙中にこの辞書を変えてもよい）。</summary>
        public List<string> Keys => _cells.Keys.ToList();

        /// <summary>登録簿を結ぶ。すでに入っているリンクの ID を、引き直す。null なら、結びを外す。</summary>
        public void AttachIdentity(IProfileIdentityService identity)
        {
            _identity = identity;
            foreach (ProfileSlotNames cell in _cells.Values) cell.AttachIdentity(identity);
        }

        public string this[string serial]
        {
            get
            {
                if (!_cells.TryGetValue(serial, out ProfileSlotNames cell)) throw new KeyNotFoundException(serial);
                return cell[0];
            }

            set
            {
                if (!_cells.TryGetValue(serial, out ProfileSlotNames cell))
                {
                    cell = new ProfileSlotNames(1);
                    cell.AttachIdentity(_identity);
                    _cells[serial] = cell;
                }

                cell[0] = value;
            }
        }

        public bool ContainsKey(string serial) => _cells.ContainsKey(serial);

        public bool Remove(string serial) => _cells.Remove(serial);

        public void Clear() => _cells.Clear();

        public bool TryGetValue(string serial, out string profile)
        {
            if (_cells.TryGetValue(serial, out ProfileSlotNames cell))
            {
                profile = cell[0];
                return true;
            }

            profile = null;
            return false;
        }

        /// <summary>その時点の現在の名前の辞書（写し）。</summary>
        public Dictionary<string, string> ToDictionary()
        {
            var result = new Dictionary<string, string>(_cells.Count);
            foreach (KeyValuePair<string, ProfileSlotNames> pair in _cells) result[pair.Key] = pair.Value[0];
            return result;
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            foreach (KeyValuePair<string, ProfileSlotNames> pair in _cells.ToList())
                yield return new KeyValuePair<string, string>(pair.Key, pair.Value[0]);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
