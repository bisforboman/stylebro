# Kept findings (2026-10-10): a method the fix keeps public on purpose offers no code action
@{
    File = 'src/StyleBro.CodeFixes/Maintainability/InternalTypeMethodCodeFixProvider.cs'
    Find = '&& await GetKeptReasonAsync(context.Document, method, model, context.CancellationToken).ConfigureAwait(false) is not null)'
    Replace = '&& false)'
    Tests = 'InternalTypeMethodTests'
}
