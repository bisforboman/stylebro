# scan.py <clone>...: the targeted lenses: committed .nupkg files (and what is next to them), .editorconfig charset /
# insert_final_newline, and how many C# files follow them (BOM, final newline)
import os, re, subprocess, sys

for d in sys.argv[1:]:
    files = subprocess.run(['git', '-C', d, 'ls-files'], capture_output=True, text=True).stdout.split('\n')
    nupkgs = [f for f in files if f.endswith('.nupkg')]
    cs = [f for f in files if f.endswith('.cs')]
    bom = sum(1 for f in cs if open(os.path.join(d, f), 'rb').read(3) == b'\xef\xbb\xbf')
    nofinal = 0
    for f in cs:
        b = open(os.path.join(d, f), 'rb').read()
        if b and not b.endswith(b'\n'):
            nofinal += 1
    print(f'== {d}: {len(cs)} .cs, BOM {bom}, no final newline {nofinal}, nupkg {len(nupkgs)}')
    for n in nupkgs[:10]:
        folder = os.path.dirname(n)
        near = [f for f in files if os.path.dirname(f) == folder and f.endswith(('.cs', 'proj', '.sln', '.props'))]
        print('   nupkg', n, 'next to', near[:4])
    for e in [f for f in files if f.endswith('.editorconfig')]:
        sec = None
        for line in open(os.path.join(d, e), encoding='utf-8-sig', errors='replace'):
            line = line.strip()
            if line.startswith('['):
                sec = line
            m = re.match(r'(charset|insert_final_newline|end_of_line)\s*=\s*(\S+)', line)
            if m:
                print(f'   {e} {sec} {m.group(1)} = {m.group(2)}')
