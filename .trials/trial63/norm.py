# norm.py <log> <key>: a log as the soak compares it: paths, temp folders and times normalized; progress lines ('Checked'),
# per-run section headers ('== ...') and finding lines compared as sorted sets after the rest (both engines run in parallel)
import re, sys
ordered, unordered = [], []
for line in open(sys.argv[1], encoding='utf-8', errors='replace').read().splitlines():
    line = re.sub(r'/home/user/w63/%s/[a-z]+' % re.escape(sys.argv[2]), 'R', line)
    line = re.sub(r'stylebro-preview-[A-Za-z0-9]+/[a-z]+', 'P', line)
    line = re.sub(r'\d+m \d+s', 'NmNs', line)
    line = re.sub(r'\d+(\.\d+)?\s?(ms|s)\b', 'N', line)
    if re.match(r'^Checked |^== |^\s+\S.*\(\d+,\d+\): ', line):
        unordered.append(re.sub(r' \(run \d+ of \d+\)$', '', line))
    else:
        ordered.append(line)
print('\n'.join(ordered + ['--- unordered ---'] + sorted(unordered)))
