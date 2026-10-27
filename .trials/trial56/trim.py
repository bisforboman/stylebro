# trim.py <log>...: a large build or test log cut down to what the trial compared: its first 20 lines, the errors and
# warnings counted by code, the failing tests, and its last 40 lines (the full logs were 50 MB in all).
import collections, re, sys

for path in sys.argv[1:]:
    lines = open(path, encoding='utf-8', errors='replace').read().splitlines()
    codes = collections.Counter(m.group(1) + ' ' + m.group(2) for l in lines for m in [re.search(r': (error|warning) ([A-Za-z]+\d+)', l)] if m)
    failed = sorted({l.strip() for l in lines if re.match(r'^\s*(Failed |failed )\S', l)})
    out = [f'[trimmed by trim.py from {len(lines)} lines: first 20, errors and warnings by code, failing tests, last 40]']
    out += lines[:20] + ['', '[errors and warnings by code]'] + [f'{n:8} {c}' for c, n in codes.most_common()]
    out += ['', '[failing tests]'] + failed + ['', '[last 40 lines]'] + lines[-40:]
    open(path, 'w', encoding='utf-8').write('\n'.join(out) + '\n')
