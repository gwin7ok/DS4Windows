# 日本語表示の翻訳の計測（PhaseX3-0）。リポジトリのルートで実行: python scripts/audit/Measure-Localization.py [出力先フォルダー]
# 出力: 未訳キー、日本語側が英語のままの値、XAML の直書きの英語（ファイル・行・属性・文言）の一覧（TSV）と、件数の概要。
import re, sys, os, glob
import xml.etree.ElementTree as ET

ROOT = os.getcwd()
OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, 'scripts', 'audit', 'out')
os.makedirs(OUT, exist_ok=True)

def load(p):
    d = {}
    for e in ET.parse(p).getroot().findall('data'):
        v = e.find('value')
        d[e.get('name')] = v.text if v is not None and v.text is not None else ''
    return d

def has_ja(s):
    return re.search(r'[぀-ヿ㐀-鿿]', s) is not None

summary = []
for en_p, ja_p, label in [('DS4Windows/Translations/Strings.resx', 'DS4Windows/Translations/Strings.ja.resx', 'Strings'),
                          ('DS4Windows/Properties/Resources.resx', 'DS4Windows/Properties/Resources.ja.resx', 'Resources')]:
    en, ja = load(en_p), load(ja_p)
    missing = [k for k in en if k not in ja]
    english_left = [k for k in ja if k in en and ja[k] == en[k] and re.search(r'[A-Za-z]{3,}', ja[k])]
    no_ja_char = [k for k in ja if not has_ja(ja[k]) and re.search(r'[A-Za-z]{3,}', ja[k]) and k not in english_left]
    extra = [k for k in ja if k not in en]
    with open(os.path.join(OUT, f'{label}-missing.tsv'), 'w', encoding='utf-8') as f:
        for k in missing:
            f.write(f'{k}\t{en[k]!r}\n')
    with open(os.path.join(OUT, f'{label}-english-left.tsv'), 'w', encoding='utf-8') as f:
        for k in english_left + no_ja_char:
            f.write(f'{k}\t{ja[k]!r}\n')
    summary.append(f'{label}: en={len(en)} ja={len(ja)} missing={len(missing)} same-as-en={len(english_left)} no-ja-char={len(no_ja_char)} ja-only={len(extra)}')

# XAML の直書き
ATTRS = ['Content', 'Header', 'Text', 'ToolTip', 'Title', 'Label', 'Description', 'Watermark', 'Hint', 'Caption', 'Tag']
pat = re.compile(r'\b(%s)\s*=\s*"([^"{][^"]*)"' % '|'.join(ATTRS))
tip = re.compile(r'<ToolTip[^>]*>([^<{]+)</ToolTip>')
rows = []
for p in sorted(glob.glob('DS4Windows/**/*.xaml', recursive=True)):
    if '/obj/' in p.replace(chr(92), '/') or '/bin/' in p.replace(chr(92), '/'):
        continue
    for i, line in enumerate(open(p, encoding='utf-8-sig'), 1):
        for m in pat.finditer(line):
            v = m.group(2).strip()
            if m.group(1) == 'Tag':
                continue
            if re.search(r'[A-Za-z]{2,}', v) and not v.startswith('{') and not re.fullmatch(r'[\w\.\-/:]+', v) or (re.fullmatch(r'[A-Za-z][A-Za-z ]+', v) and m.group(1) in ('Content', 'Header', 'Text', 'ToolTip', 'Title')):
                rows.append((p.replace(chr(92), '/'), i, m.group(1), v))
        for m in tip.finditer(line):
            rows.append((p.replace(chr(92), '/'), i, 'ToolTip(text)', m.group(1).strip()))
with open(os.path.join(OUT, 'xaml-hardcoded.tsv'), 'w', encoding='utf-8') as f:
    for r in rows:
        f.write('\t'.join(str(x) for x in r) + '\n')
files = sorted(set(r[0] for r in rows))
summary.append(f'XAML 直書き: {len(rows)} 件 / {len(files)} ファイル')
open(os.path.join(OUT, 'summary.txt'), 'w', encoding='utf-8').write('\n'.join(summary) + '\n')
print('\n'.join(summary))
