# shape.py <dir>: what trial 62's init fixes look at: tab-indented files, snake_case multi-word private fields,
# '//' comment frames (an empty '//' line above and below a comment block in code), and MSBuild file encodings
import os, re, sys
tabs = spaces = 0; snake = camel = 0; frames = plain = 0; files = 0; enc = {}
field = re.compile(r'^\s*(private\s+|internal\s+|protected\s+)?(static\s+|readonly\s+|volatile\s+)*[\w<>\[\],.? ]+\s+(_?[a-z][A-Za-z0-9_]*)\s*(=|;)')
for root, dirs, fs in os.walk(sys.argv[1]):
    dirs[:] = [d for d in dirs if d not in ('obj', 'bin', '.git')]
    for f in fs:
        p = os.path.join(root, f)
        if f.endswith(('.csproj', '.props', '.targets', '.sln')):
            b = open(p, 'rb').read(4)
            k = 'utf16' if b[:2] in (b'\xff\xfe', b'\xfe\xff') else 'bom' if b[:3] == b'\xef\xbb\xbf' else 'plain'
            enc[k] = enc.get(k, 0) + 1
        if not f.endswith('.cs'): continue
        files += 1
        lines = open(p, encoding='utf-8-sig', errors='replace').read().splitlines()
        t = sum(1 for l in lines if l.startswith('\t')); s = sum(1 for l in lines if l.startswith('    '))
        if t > s: tabs += 1
        elif s: spaces += 1
        for l in lines:
            m = field.match(l)
            if m and '(' not in l and not l.strip().startswith(('return', 'var ', 'using')):
                n = m.group(3).lstrip('_')
                if '_' in n: snake += 1
                elif re.search('[a-z][A-Z]', n): camel += 1
        for i in range(1, len(lines) - 1):
            if lines[i].strip() == '//' and lines[i - 1].strip().startswith('//') and lines[i - 1].strip() != '//':
                frames += 1
            elif lines[i].strip().startswith('// ') and not lines[i - 1].strip().startswith('//'):
                plain += 1
print(f'{sys.argv[1]}: {files} cs; tab files {tabs}, space files {spaces}; fields snake {snake}, camel {camel}; frame closers {frames}, plain comment starts {plain}; msbuild encodings {enc}')
