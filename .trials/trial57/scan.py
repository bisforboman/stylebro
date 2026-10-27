import os, re, sys

# For each repo folder: count lines that start with a comma in .cs files, and MSBuild files using
# item metadata inside property functions (Regex.Replace(%(...)) or %(...) inside $([...])).
lead = re.compile(r'^\s*,')
func = re.compile(r'\$\(\[[^\]]+\]::[^)]*%\(|\$\([^)]*%\(|->\s*\'?\$\(|Regex\]::Replace\(\s*\'?%\(')
for root in sys.argv[1:]:
    commas = 0
    files = 0
    msb = []
    for d, dirs, fs in os.walk(root):
        dirs[:] = [x for x in dirs if x not in ('.git', 'obj', 'bin', 'node_modules')]
        for f in fs:
            p = os.path.join(d, f)
            if f.endswith('.cs'):
                try:
                    n = sum(1 for l in open(p, encoding='utf-8', errors='ignore') if lead.match(l))
                except OSError:
                    continue
                commas += n
                files += n > 0
            elif f.endswith(('.props', '.targets', '.csproj')):
                t = open(p, encoding='utf-8', errors='ignore').read()
                for m in re.finditer(r'.*%\(.*', t):
                    line = m.group(0)
                    if '$([' in line or 'Regex' in line:
                        msb.append((os.path.relpath(p, root), line.strip()[:160]))
    print(f'{root}: leading-comma lines {commas} in {files} files; item functions {len(msb)}')
    for x in msb[:8]:
        print('   ', x)
