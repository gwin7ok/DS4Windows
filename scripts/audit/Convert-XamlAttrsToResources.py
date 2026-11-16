"""XAML の属性に直接書かれた表示文字を、リソースキー（lex:Loc）へ置き換える（PhaseX3-1）。

使い方: python scripts/audit/Convert-XamlAttrsToResources.py [--apply]
- 既存の Strings.resx に同じ英語の値があれば、そのキーを使う。
- なければ新しいキーを Strings.resx へ足し、Strings.ja.resx へも同じ値を入れる（訳は X3-3 で直す）。
- 画面ごとのキー: <画面名>_<意味>。2 画面以上で使う文字は Ui_<意味>。
"""
import glob
import os
import re
import sys
from xml.sax.saxutils import escape, unescape
import html

ROOT = r'G:\Cursor_Folder\DS4Windows-Vader4Pro'
os.chdir(ROOT)
APPLY = '--apply' in sys.argv
ATTRS = ('Content', 'Header', 'Text', 'Title', 'ToolTip')
SKIP_FILES = ('/Themes/',)
SPECIAL = {'\u00b0': 'Unit_Degree', '\u20ac': 'Unit_Euro'}
EN = 'DS4Windows/Translations/Strings.resx'
JA = 'DS4Windows/Translations/Strings.ja.resx'


def read(p):
    return open(p, 'rb').read().decode('utf-8')


def parse_resx(text):
    d = {}
    for m in re.finditer(r'<data name="([^"]+)"[^>]*>\s*<value>(.*?)</value>', text, re.S):
        d[m.group(1)] = unescape(m.group(2))
    return d


en_text = read(EN)
ja_text = read(JA)
en = parse_resx(en_text)
ja = parse_resx(ja_text)
by_value = {}
for k, v in en.items():
    by_value.setdefault(v, k)

attr_re = re.compile(r'(\s)(' + '|'.join(ATTRS) + r')="([^"{][^"]*)"')


def decoded(v):
    return html.unescape(v)


files = []
for p in sorted(glob.glob('DS4Windows/**/*.xaml', recursive=True)):
    q = p.replace(os.sep, '/')
    if '/obj/' in q or '/bin/' in q or any(s in q for s in SKIP_FILES):
        continue
    files.append(q)

# 1 回目: 値ごとに使われる画面数を数える
usage = {}
hits = {}
for q in files:
    s = read(q)
    for m in attr_re.finditer(s):
        v = decoded(m.group(3))
        if not re.search(r'[A-Za-z]{2,}', v) and v not in SPECIAL:
            continue
        usage.setdefault(v, set()).add(q)
        hits.setdefault(q, []).append(v)


def slug(v):
    if v in SPECIAL:
        return SPECIAL[v]
    words = re.findall(r'[A-Za-z0-9]+', v)
    s = ''.join(w[:1].upper() + w[1:] for w in words)
    return s[:40] or 'Text'


new_keys = {}
assigned = {}
used = set(en) | set()


def key_for(v, q):
    if v in by_value:
        return by_value[v]
    if (v) in assigned:
        return assigned[v]
    screen = os.path.splitext(os.path.basename(q))[0]
    base = slug(v)
    if v in SPECIAL:
        k = base
    elif len(usage[v]) >= 2:
        k = 'Ui_' + base
    else:
        k = screen + '_' + base
    n = 2
    k0 = k
    while k in used:
        k = '%s%d' % (k0, n)
        n += 1
    used.add(k)
    assigned[v] = k
    new_keys[k] = v
    return k


root_re = re.compile(r'<([A-Za-z]+)\b([^>]*)>', re.S)
stats = {}
for q in files:
    if q not in hits:
        continue
    raw = open(q, 'rb').read()
    s = raw.decode('utf-8')

    def rep(m):
        v = decoded(m.group(3))
        if not re.search(r'[A-Za-z]{2,}', v) and v not in SPECIAL:
            return m.group(0)
        k = key_for(v, q)
        return '%s%s="{lex:Loc %s}"' % (m.group(1), m.group(2), k)

    s2 = attr_re.sub(rep, s)
    # ルートに lex の名前空間と既定の辞書を足す
    if 'xmlns:lex=' not in s2:
        s2 = re.sub(r'(<[A-Za-z]+\b)', r'\1 xmlns:lex="http://wpflocalizeextension.codeplex.com"', s2, count=1)
    if 'DefaultDictionary="Strings"' not in s2:
        # 最初の要素の開始タグの末尾（> の直前）へ入れる
        m = re.search(r'<[A-Za-z]+\b[^>]*?(/?)>', s2, re.S)
        tag = m.group(0)
        nl = '\r\n' if '\r\n' in s2 else '\n'
        add = (nl + '        lex:ResxLocalizationProvider.DefaultAssembly="DS4Windows"' +
               nl + '        lex:ResxLocalizationProvider.DefaultDictionary="Strings"')
        new_tag = tag[:-1] + add + '>' if not m.group(1) else tag[:-2] + add + '/>'
        s2 = s2.replace(tag, new_tag, 1)
    stats[q] = len(hits[q])
    if APPLY:
        open(q, 'wb').write(s2.encode('utf-8'))

# resx へ追記
def add_entries(text, entries):
    nl = '\r\n' if '\r\n' in text else '\n'
    block = ''
    for k, v in entries.items():
        block += '  <data name="%s" xml:space="preserve">%s    <value>%s</value>%s  </data>%s' % (
            k, nl, escape(v), nl, nl)
    i = text.rindex('</root>')
    return text[:i] + block + text[i:]


if APPLY:
    open(EN, 'wb').write(add_entries(en_text, new_keys).encode('utf-8'))
    open(JA, 'wb').write(add_entries(ja_text, new_keys).encode('utf-8'))

print('files', len(stats), 'attrs', sum(stats.values()), 'new keys', len(new_keys),
      'reused', sum(stats.values()) - 0)
for k, v in list(new_keys.items()):
    print(k, '=', v)
