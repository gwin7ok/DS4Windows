using System;
using System.Collections;
using System.Collections.Generic;
using DS4Windows.DI;

namespace DS4Windows
{
    /// <summary>
    /// Phase10-Step4d W5b（Phase10-Step4d-Plan.md §4 W5）: スロットごとのプロファイルの<b>名前を読み書きする窓口</b>で、保管は<b>プロファイルの固定 ID</b>
    /// （と、見つからないときの名前の控え）。<c>BackingStore</c> の <c>profilePath</c>（現在）、<c>olderProfilePath</c>（直前）、<c>selectedProfile</c>（選択）の型。
    /// <list type="bullet">
    /// <item>書く（<c>slots[i] = 名前</c>）: 名前の控えを保存し、登録簿（<see cref="IProfileIdentityService"/>）から ID を引いて保存する。見つからない名前（まだ作っていない、空、「(none)」）は、ID なしで名前の控えだけ。</item>
    /// <item>読む（<c>slots[i]</c>）: ID があって登録簿が引ければ、<b>現在の名前</b>を返す（プロファイルの名前を変えても、保管を書き換えなくてよい）。引けなければ、名前の控えを返す
    /// （削除されたプロファイルは、最後の名前が残る。これまでと同じ）。ID がないとき・ID が無効になったときは、名前から ID を引き直す。</item>
    /// </list>
    /// 登録簿がなければ（テストの新しい <c>BackingStore</c>）、名前の控えだけで、従来の <c>string[]</c> と同じに動く。
    /// 登録簿は、DI の登録が <see cref="BackingStore.AttachProfileIdentity"/> で結ぶ（登録簿が <c>BackingStore</c> に依存しない向き）。
    /// 並行して読み書きされても壊れない（要素ごとの代入のみ）が、従来の配列と同じく、スロットの値の整合は呼び出し側が保つ。
    /// </summary>
    public sealed class ProfileSlotNames : IEnumerable<string>
    {
        private readonly string[] _names;
        private readonly string[] _ids;
        private volatile IProfileIdentityService _identity;

        public ProfileSlotNames(int length)
        {
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
            _names = new string[length];
            _ids = new string[length];
            for (int i = 0; i < length; i++) _names[i] = string.Empty;
        }

        public int Length => _names.Length;

        /// <summary>登録簿を結ぶ。すでに入っている名前の ID を、引き直す。null なら、結びを外す（名前の控えだけになる）。</summary>
        public void AttachIdentity(IProfileIdentityService identity)
        {
            _identity = identity;
            for (int i = 0; i < _names.Length; i++)
                _ids[i] = identity != null && !string.IsNullOrEmpty(_names[i]) ? identity.GetId(_names[i]) : null;
        }

        public string this[int index]
        {
            get
            {
                IProfileIdentityService identity = _identity;
                if (identity == null) return _names[index];

                string id = _ids[index];
                if (id != null)
                {
                    string current = identity.GetName(id);
                    if (current != null)
                    {
                        if (!string.Equals(_names[index], current, StringComparison.Ordinal)) _names[index] = current;   // 控えも更新する（削除されたとき、最後の名前が残る）
                        return current;
                    }
                }

                // ID がない・ID が無効（削除された）: 名前の控えから ID を引き直す（あとで作られたプロファイル、同じ名前で作り直したプロファイルに結ぶ）
                string name = _names[index];
                _ids[index] = !string.IsNullOrEmpty(name) ? identity.GetId(name) : null;
                return name;
            }

            set
            {
                _names[index] = value;
                IProfileIdentityService identity = _identity;
                _ids[index] = identity != null && !string.IsNullOrEmpty(value) ? identity.GetId(value) : null;
            }
        }

        /// <summary>保管している ID。ない（登録簿がない、プロファイルが見つからない）ときは null。</summary>
        public string IdAt(int index) => _ids[index];

        /// <summary>名前の配列（その時点の現在の名前）。</summary>
        public string[] ToArray()
        {
            var result = new string[_names.Length];
            for (int i = 0; i < result.Length; i++) result[i] = this[i];
            return result;
        }

        public IEnumerator<string> GetEnumerator()
        {
            for (int i = 0; i < _names.Length; i++) yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
