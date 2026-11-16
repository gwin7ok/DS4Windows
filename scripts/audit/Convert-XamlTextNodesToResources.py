"""XAML の要素内に直接書かれた表示文字を、リソースキーへ置き換える（PhaseX3-1）。

- 文章の断片は <Run Text="{lex:Loc キー}"/> にする（Bold などの入れ子は保つ）。
- <TextBlock.Text>文</TextBlock.Text> だけを持つ TextBlock は、Text 属性へ直す。
- 対象外: TextBox の中身（ライセンス全文・翻訳者の名前）、URL、人名のリンク。
"""
import os
import re
import sys
from xml.sax.saxutils import escape, unescape
import html

os.chdir(r'G:\Cursor_Folder\DS4Windows-Vader4Pro')
FILES = ['About', 'ControllerRegisterOptionsWindow', 'StickCalibrationWindow', 'TriggerSettingsPanel']
EN = 'DS4Windows/Translations/Strings.resx'
JA = 'DS4Windows/Translations/Strings.ja.resx'
SKIP_TEXT = {'gwin7ok', 'Jays2Kings'}


def read(p):
    return open(p, 'rb').read().decode('utf-8')


en_text = read(EN)
ja_text = read(JA)
en = {}
for m in re.finditer(r'<data name="([^"]+)"[^>]*>\s*<value>(.*?)</value>', en_text, re.S):
    en[m.group(1)] = unescape(m.group(2))
by_value = {}
for k, v in en.items():
    by_value.setdefault(v, k)
new_keys = {}
assigned = {}
used = set(en)


def slug(v):
    words = re.findall(r'[A-Za-z0-9]+', v)
    return (''.join(w[:1].upper() + w[1:] for w in words[:6]))[:40] or 'Text'


def key_for(v, screen):
    if v in by_value:
        return by_value[v]
    if v in assigned:
        return assigned[v]
    k0 = k = screen + '_' + slug(v)
    n = 2
    while k in used:
        k = '%s%d' % (k0, n)
        n += 1
    used.add(k)
    assigned[v] = k
    new_keys[k] = v
    return k


def norm(raw):
    t = ' '.join(html.unescape(raw).split())
    lead = ' ' if raw[:1] in (' ', '\t') else ''
    trail = ' ' if raw[-1:] in (' ', '\t') else ''
    return lead + t + trail


text_re = re.compile(r'(<(?P<tag>[\w\.:]+)(?:\s[^<>]*?)?/?>|</(?P<ctag>[\w\.:]+)>)(?P<body>[^<>{]*[A-Za-z]{2,}[^<>{]*)(?=<)')
prop_re = re.compile(r'<TextBlock(?P<attrs>(?:\s[^<>]*?)?)>\s*<TextBlock\.Text>\s*(?P<t>[^<>{]*[A-Za-z]{2,}[^<>{]*?)\s*</TextBlock\.Text>\s*</TextBlock>')

total = 0
for name in FILES:
    q = 'DS4Windows/UI/%s.xaml' % name
    s = read(q)

    def prop(m):
        global total
        v = ' '.join(html.unescape(m.group('t')).split())
        total += 1
        return '<TextBlock%s Text="{lex:Loc %s}"/>' % (m.group('attrs'), key_for(v, name))

    s = prop_re.sub(prop, s)

    def rep(m):
        global total
        tag = m.group('tag') or ''
        body = m.group('body')
        if tag == 'TextBox' or tag.endswith('.Text') or '://' in body or ' '.join(body.split()) in SKIP_TEXT:
            return m.group(0)
        v = norm(body)
        if not v.strip():
            return m.group(0)
        total += 1
        return '%s<Run Text="{lex:Loc %s}"/>' % (m.group(1), key_for(v, name))

    s = text_re.sub(rep, s)
    open(q, 'wb').write(s.encode('utf-8'))


def add_entries(text, entries):
    nl = '\r\n' if '\r\n' in text else '\n'
    block = ''
    for k, v in entries.items():
        block += '  <data name="%s" xml:space="preserve">%s    <value>%s</value>%s  </data>%s' % (
            k, nl, escape(v), nl, nl)
    i = text.rindex('</root>')
    return text[:i] + block + text[i:]


open(EN, 'wb').write(add_entries(en_text, new_keys).encode('utf-8'))
open(JA, 'wb').write(add_entries(ja_text, new_keys).encode('utf-8'))
print('nodes', total, 'new keys', len(new_keys))
for k, v in new_keys.items():
    print(k, '=', v[:60])
