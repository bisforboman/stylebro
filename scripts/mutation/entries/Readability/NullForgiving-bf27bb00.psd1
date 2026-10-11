# BRO1147 (redundant null-forgiving '!')
@{
    File = 'src/StyleBro.Analyzers/Readability/NullForgiving.cs'
    Find = '|| node.Parent is ArgumentSyntax { RefKindKeyword.RawKind: not 0 }'
    Replace = ''
    Tests = 'NullForgivingTests'
}
