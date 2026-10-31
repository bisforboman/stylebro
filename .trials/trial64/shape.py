# shape.py <dir>...: what trial 63's fixes look at: using aliases with a nullable type argument (BRO1126 kept '?'),
# neighbouring one-line members (init's stylebro_allow_adjacent_single_line_members), multi-targeted projects
import os, re, sys
alias = re.compile(r'^\s*(global\s+)?using\s+\w+\s*=\s*[^;]*<[^;]*\?[^;]*;')
oneline = re.compile(r'^\s*(public|private|internal|protected)[^;{]*(=>[^;]*;|\{\s*get;[^}]*\})\s*$')
for top in sys.argv[1:]:
    files = aliases = pairs = 0; tfms = set(); multi = 0; ex = []
    for root, dirs, fs in os.walk(top):
        dirs[:] = [d for d in dirs if d not in ('obj', 'bin', '.git')]
        for f in fs:
            p = os.path.join(root, f)
            if f.endswith('.csproj'):
                t = open(p, encoding='utf-8-sig', errors='replace').read()
                for m in re.findall(r'<TargetFrameworks?[^>]*>([^<]*)<', t):
                    tfms.update(x for x in m.split(';') if x and '$' not in x)
                    if ';' in m: multi += 1
            if not f.endswith('.cs'): continue
            files += 1
            lines = open(p, encoding='utf-8-sig', errors='replace').read().splitlines()
            for i, l in enumerate(lines):
                if alias.match(l):
                    aliases += 1
                    if len(ex) < 3: ex.append(l.strip()[:120])
                if i and oneline.match(l) and oneline.match(lines[i - 1]): pairs += 1
    print(f'{top}: {files} cs; nullable using aliases {aliases}; adjacent one-line member pairs {pairs}; multi-target csproj {multi}; tfms {sorted(tfms)}')
    for e in ex: print('   ', e)
