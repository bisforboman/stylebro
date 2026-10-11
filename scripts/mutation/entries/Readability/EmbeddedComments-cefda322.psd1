# BRO1132 (embedded comments)
@{
    File = 'src/StyleBro.Analyzers/Readability/EmbeddedComments.cs'
    Find = 'when block.Parent is not null && BlockOwners.Any(block.Parent.IsKind)'
    Replace = ''
    Tests = 'EmbeddedCommentTests'
}
