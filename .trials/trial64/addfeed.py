# addfeed.py <nuget.config>: add the trial feed after the first <clear /> that isn't inside an XML comment
import re, sys
p = sys.argv[1]
t = open(p, encoding='utf-8-sig').read()
for m in re.finditer(r'<clear\s*/>', t):
    if t.rfind('<!--', 0, m.start()) <= t.rfind('-->', 0, m.start()):
        t = t[:m.end()] + '\n    <add key="stylebro-trial" value="/home/user/feed64" />' + t[m.end():]
        open(p, 'w', encoding='utf-8').write(t)
        break
# a packageSourceMapping needs the feed mapped too (NU1102 otherwise)
t = open(p, encoding='utf-8').read()
m = re.search(r'<packageSourceMapping>', t)
if m and 'stylebro-trial' in t and '<packageSource key="stylebro-trial">' not in t:
    t = t[:m.end()] + '\n    <packageSource key="stylebro-trial"><package pattern="StyleBro.*" /></packageSource>' + t[m.end():]
    open(p, 'w', encoding='utf-8').write(t)
