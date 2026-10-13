# format notes said once
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'return Explained.Add(key) ? text : null;'
    Replace = 'return text;'
    Tests = 'FullyQualifiedName~ExplainsFSharpProjectsAndWorkspaceWarnings'
}
