# The folders above the root, up to the repository, are searched for CSharpier too.
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = '            .Concat(above)'
    Replace = ''
    Tests = 'StyleBro.Tests.MigrationTests.Init_WritesCSharpierLines_IntoARootTrueFolderBelowCSharpiersFolder'
}
