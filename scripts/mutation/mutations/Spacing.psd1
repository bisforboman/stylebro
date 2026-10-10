# Mutations for Spacing (format: scripts/mutation/Invoke-Mutations.ps1).
@{
    Mutations = @(
        # StyleCop's open bugs (2026-10-04)
        @{ File = 'src/StyleBro.Analyzers/Spacing/CommentSpacing.cs'; Find = '|| comment.StartsWith("//-:", System.StringComparison.Ordinal)'; Replace = ''; Tests = 'CommentSpacingTests' }
        @{ File = 'src/StyleBro.Analyzers/Spacing/CommentSpacing.cs'; Find = '|| comment.StartsWith("//+:", System.StringComparison.Ordinal)'; Replace = ''; Tests = 'CommentSpacingTests' }
    )
}
