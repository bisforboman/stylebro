# addref.py <file> <xml>: insert an ItemGroup with <xml> before the last </Project>, keeping the file's encoding (UTF-16 too)
import sys
p, xml = sys.argv[1], sys.argv[2]
raw = open(p, 'rb').read()
enc = 'utf-16' if raw[:2] in (b'\xff\xfe', b'\xfe\xff') else ('utf-8-sig' if raw[:3] == b'\xef\xbb\xbf' else 'utf-8')
t = raw.decode(enc)
i = t.rindex('</Project>')
nl = '\r\n' if '\r\n' in t else '\n'
open(p, 'wb').write((t[:i] + '  <ItemGroup>' + nl + '    ' + xml + nl + '  </ItemGroup>' + nl + t[i:]).encode(enc))
