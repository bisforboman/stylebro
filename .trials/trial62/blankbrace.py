# blankbrace.py <dir>: counts type/namespace '{' lines followed by a blank line (BRO1503's case), and '(' alone on a line
import os, re, sys
decl = re.compile(r'^\s*(\[.*\]\s*)?((public|internal|private|protected|static|sealed|abstract|partial|unsafe|readonly|file)\s+)*(class|struct|interface|enum|record|namespace)\b')
with_, without, paren, files = 0, 0, 0, 0
for root, dirs, fs in os.walk(sys.argv[1]):
    dirs[:] = [d for d in dirs if d not in ('obj', 'bin', '.git')]
    for f in fs:
        if not f.endswith('.cs'): continue
        files += 1
        try: lines = open(os.path.join(root, f), encoding='utf-8-sig', errors='replace').read().splitlines()
        except Exception: continue
        for i, l in enumerate(lines):
            s = l.strip()
            if s == '(' : paren += 1
            if s.endswith('{') and s != '{' and decl.match(l) and i + 1 < len(lines):
                if lines[i+1].strip() == '': with_ += 1
                else: without += 1
                continue
            if s == '{' and i + 1 < len(lines):
                j = i - 1
                while j >= 0 and (lines[j].strip() == '' or lines[j].strip().startswith(':') or lines[j].strip().startswith('where ')): j -= 1
                # walk back over base list continuation lines
                if j >= 0 and decl.match(lines[j]) or (j >= 1 and decl.match(lines[j-1]) and lines[j].strip().startswith(',')):
                    if lines[i+1].strip() == '': with_ += 1
                    else: without += 1
print(f"{sys.argv[1]}: files {files}, type/ns '{{'+blank {with_}, without {without}, '(' alone {paren}")
