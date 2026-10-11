# Kept findings (2026-10-10): a rename the fix keeps on purpose offers no code action
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseNamingCodeFixProvider.cs'
    Find = '|| await CamelCaseRenamer.GetKeptReasonAsync(context.Document.Project.Solution, context.Document, diagnostic, context.CancellationToken).ConfigureAwait(false) is not null)'
    Replace = ')'
    Tests = 'FieldNamingTests'
}
