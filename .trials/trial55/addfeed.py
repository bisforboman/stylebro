# addfeed.py <nuget.config>: add the trial feed after the first <clear /> that isn't inside an XML comment
import re, sys
p = sys.argv[1]
t = open(p, encoding='utf-8-sig').read()
for m in re.finditer(r'<clear\s*/>', t):
    if t.rfind('<!--', 0, m.start()) <= t.rfind('-->', 0, m.start()):
        t = t[:m.end()] + '\n    <add key="stylebro-trial" value="/home/user/feed55" />' + t[m.end():]
        open(p, 'w', encoding='utf-8').write(t)
        break
