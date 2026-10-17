# A name in a log key hole keeps its name (owner, 2026-10-17)
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = 'if (await GetLogKeyHandlerAsync(reference.Document, reference.Location.SourceSpan, cancellationToken).ConfigureAwait(false) is { } handler)'
    Replace = 'if (await Task.FromResult<string?>(null).ConfigureAwait(false) is { } handler)'
    Tests = 'KeptFindingsTests.ANameInALogKeyHole'
}
