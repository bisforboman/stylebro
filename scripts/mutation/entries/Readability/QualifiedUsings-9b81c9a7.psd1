# StyleCop's open bugs (2026-10-04)
@{
    File = 'src/StyleBro.Analyzers/Readability/QualifiedUsings.cs'
    Find = 'if (written is NameSyntax argument && StartsWithAlias(argument, model, cancellationToken))'
    Replace = 'if (written is NameSyntax argument && false)'
    Tests = 'QualifiedUsingsTests'
}
