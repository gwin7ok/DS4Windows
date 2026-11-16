"""scripts/audit/tr/<辞書>-*.tsv（キー<TAB>日本語。改行は \n）を、<辞書>.ja.resx へ反映する（PhaseX3-3）。"""
import glob, re, sys
from xml.sax.saxutils import escape
BASE = {'Strings': 'DS4Windows/Translations/', 'Resources': 'DS4Windows/Properties/'}
for dic, base in BASE.items():
    tr = {}
    for f in sorted(glob.glob('scripts/audit/tr/%s-*.tsv' % dic)):
        for line in open(f, encoding='utf-8').read().split('\n'):
            if not line.strip():
                continue
            k, _, v = line.partition('\t')
            tr[k] = v.replace('\n', '\r\n')
    p = base + dic + '.ja.resx'
    s = open(p, 'rb').read().decode('utf-8')
    done = 0
    for k, v in tr.items():
        pat = re.compile(r'(<data name="' + re.escape(k) + r'"[^>]*>\s*<value>)(.*?)(</value>)', re.S)
        if pat.search(s):
            s = pat.sub(lambda m: m.group(1) + escape(v) + m.group(3), s, count=1)
        else:
            i = s.rindex('</root>')
            s = s[:i] + '  <data name="%s" xml:space="preserve">\r\n    <value>%s</value>\r\n  </data>\r\n' % (k, escape(v)) + s[i:]
        done += 1
    open(p, 'wb').write(s.encode('utf-8'))
    print(dic, done)
