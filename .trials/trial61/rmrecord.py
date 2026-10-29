# rmrecord.py <root>: delete format's clean record for <root> (FormatCommand.KeptRecord); prints what it deleted
import hashlib, os, sys, tempfile
root = os.path.abspath(sys.argv[1]).rstrip('/').upper()
p = os.path.join(tempfile.gettempdir(), 'stylebro-kept', hashlib.sha256(root.encode()).hexdigest().upper()[:16] + '.tsv')
print(('deleted ' if os.path.exists(p) else 'no record ') + p)
if os.path.exists(p): os.remove(p)
