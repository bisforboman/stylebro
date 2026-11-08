# Backlog 085: no .gitattributes outside a git repository
@{
    File = 'src/StyleBro.Migrate/GitAttributes.cs'
    Find = 'FormatCommand.RepositoryRoot(Path.GetDirectoryName(Path.GetFullPath(baseline))!) is not { } repository'
    Replace = '(FormatCommand.RepositoryRoot(Path.GetDirectoryName(Path.GetFullPath(baseline))!) ?? Path.GetDirectoryName(Path.GetFullPath(baseline))) is not { } repository'
    Tests = 'GitAttributesTests.NothingOutsideARepository'
}
