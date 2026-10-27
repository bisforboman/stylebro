# scan2.py <clone>...: trial 56's targeted lens (#306's fix): a project P targeting framework f references B, which lacks f,
# and both P (another path) and B reach a project A that has f, so a run of f sees A twice (A(f) and the instance B's
# nearest framework brings). Also: where the solutions sit (the baseline location fix), and checked-in generated-looking
# files whose name is inside a longer project name (trial 55's generated-file fix).
import os, re, subprocess, sys

def tfms(text):
    m = re.search(r'<TargetFrameworks?>([^<]*)</TargetFrameworks?>', text)
    return [x.strip() for x in m.group(1).split(';') if x.strip()] if m else None

for d in sys.argv[1:]:
    files = subprocess.run(['git', '-C', d, 'ls-files'], capture_output=True, text=True).stdout.split('\n')
    projs = [f for f in files if f.endswith('.csproj')]
    texts = {p: open(os.path.join(d, p), encoding='utf-8-sig', errors='replace').read() for p in projs}
    refs = {}
    for p, t in texts.items():
        refs[p] = [r for r in (os.path.normpath(os.path.join(os.path.dirname(p), x.replace('\\', '/')))
                               for x in re.findall(r'<ProjectReference\s+Include="([^"]+)"', t)) if r in texts]
    def reach(p, seen=None):
        seen = set() if seen is None else seen
        for r in refs.get(p, []):
            if r not in seen:
                seen.add(r); reach(r, seen)
        return seen
    sln = [f for f in files if f.endswith(('.sln', '.slnx', '.slnf'))]
    cs = [f for f in files if f.endswith('.cs')]
    print(f'== {d}: {len(cs)} .cs, solutions {sln}')
    hits = 0
    for p, t in texts.items():
        mine = tfms(t)
        if not mine:
            continue
        for b in refs[p]:
            theirs = tfms(texts[b]) or []
            if not theirs:
                continue
            for f in [x for x in mine if x not in theirs]:
                other = set()
                for r in refs[p]:
                    if r != b:
                        other |= {r} | reach(r)
                for a in sorted(reach(b) & other):
                    if f in (tfms(texts[a]) or []):
                        hits += 1
                        if hits <= 8:
                            print(f'   {p} ({f}) -> {b} {theirs} -> {a}; also via another reference')
    if hits > 8:
        print(f'   ... {hits} in all')
    names = [os.path.splitext(os.path.basename(p))[0] for p in projs]
    for f in cs:
        b = os.path.basename(f)[:-3]
        if ('Generated' in f or '.g.' in b or b.endswith('.g')) and any(b != n and b in n for n in names):
            print('   generated-looking', f)
