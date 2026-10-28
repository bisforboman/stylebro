# bytes.py <clone>: BOM and final-newline changes between HEAD and the working tree, per changed C# file
import subprocess, sys

d = sys.argv[1]
files = subprocess.run(['git', '-C', d, 'diff', '--name-only', '--', '*.cs'], capture_output=True, text=True).stdout.split()
for f in files:
    old = subprocess.run(['git', '-C', d, 'show', f'HEAD:{f}'], capture_output=True).stdout
    new = open(f'{d}/{f}', 'rb').read()
    bom = lambda b: b[:3] == b'\xef\xbb\xbf'
    if bom(old) != bom(new):
        print('BOM', 'removed' if bom(old) else 'added', f)
    end = lambda b: b.endswith(b'\n')
    if end(old) != end(new):
        print('FINAL-NEWLINE', 'removed' if end(old) else 'added', f)
