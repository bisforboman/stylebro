#!/bin/bash
# usage: addref.sh <repo dir>  -- getting-started step 1: the package in the root Directory.Build.props
cd $1 || exit 1
V=${STYLEBRO_V:-0.8.0-trial69.1}
REF="  <ItemGroup>\n    <PackageReference Include=\"StyleBro.Analyzers\" Version=\"$V\" PrivateAssets=\"all\" />\n  </ItemGroup>"
if [ -f Directory.Build.props ]; then
  python3 -I -c "import sys;p='Directory.Build.props';s=open(p).read();i=s.rindex('</Project>');open(p,'w').write(s[:i]+sys.argv[1].replace('\\\\n','\n')+'\n'+s[i:])" "$REF"
else
  printf "<Project>\n$REF\n</Project>\n" > Directory.Build.props
fi
cat Directory.Build.props
