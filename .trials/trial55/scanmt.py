# scanmt.py <clone>...: the targeted lens (trial 54's fixes): projects whose ProjectReference lacks one of their target
# frameworks, build-time packages that bring props/targets (buildMultiTargeting), and designer files next to .dbml/.xsd/.edmx
import os, re, subprocess, sys

PKGS = ['ReferenceTrimmer', 'Nerdbank.GitVersioning', 'Microsoft.SourceLink', 'DotNet.ReproducibleBuilds', 'MinVer',
        'Microsoft.Build.CopyOnWrite', 'PolySharp', 'Microsoft.CodeAnalysis.PublicApiAnalyzers', 'GitVersion.MsBuild',
        'Microsoft.DotNet.ApiCompat', 'Polyfill', 'Meziantou.Polyfill', 'ThisAssembly', 'Fody', 'ConfigureAwait.Fody']

def tfms(text):
    m = re.search(r'<TargetFrameworks?>([^<]*)</TargetFrameworks?>', text)
    return set(x.strip() for x in m.group(1).split(';') if x.strip()) if m else None

for d in sys.argv[1:]:
    files = subprocess.run(['git', '-C', d, 'ls-files'], capture_output=True, text=True).stdout.split('\n')
    cs = [f for f in files if f.endswith('.cs')]
    projs = [f for f in files if f.endswith('.csproj')]
    texts = {p: open(os.path.join(d, p), encoding='utf-8-sig', errors='replace').read() for p in projs}
    build = [f for f in files if f.endswith(('.props', '.targets'))]
    allt = ''.join(texts.values()) + ''.join(open(os.path.join(d, f), encoding='utf-8-sig', errors='replace').read() for f in build)
    pk = sorted({p for p in PKGS if re.search(r'Include="' + re.escape(p), allt)})
    gpr = 'GlobalPackageReference' in allt
    designer = [f for f in files if f.lower().endswith('.designer.cs') and any(os.path.exists(os.path.join(d, f[:-len('.designer.cs')] + e)) for e in ('.dbml', '.xsd', '.edmx'))]
    print(f'== {d}: {len(cs)} .cs, {len(projs)} csproj, packages {pk}{" (GlobalPackageReference)" if gpr else ""}, designer {designer[:3]}')
    for p, t in texts.items():
        mine = tfms(t)
        if not mine:
            continue
        for ref in re.findall(r'<ProjectReference\s+Include="([^"]+)"', t):
            rp = os.path.normpath(os.path.join(os.path.dirname(p), ref.replace('\\', '/')))
            if rp in texts:
                theirs = tfms(texts[rp])
                if theirs and not mine <= theirs:
                    print(f'   {p} {sorted(mine)} -> {rp} {sorted(theirs)} lacks {sorted(mine - theirs)}')
