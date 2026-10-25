# addref.py <file> <xml>: insert an ItemGroup with <xml> before the last </Project>
import sys
p, xml = sys.argv[1], sys.argv[2]
t = open(p, encoding='utf-8-sig').read()
i = t.rindex('</Project>')
nl = '\r\n' if '\r\n' in t else '\n'
open(p, 'w', encoding='utf-8').write(t[:i] + '  <ItemGroup>' + nl + '    ' + xml + nl + '  </ItemGroup>' + nl + t[i:])
