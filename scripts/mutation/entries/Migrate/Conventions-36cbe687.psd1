# CSharpier set up above a root = true folder covers all of it (AzureEventGridSimulator).
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'scope == "." || scope.StartsWith("..", StringComparison.Ordinal) ? string.Empty : scope;'
    Replace = 'scope == "." ? string.Empty : scope;'
    Tests = 'StyleBro.Tests.MigrationTests.Init_WritesCSharpierLines_IntoARootTrueFolderBelowCSharpiersFolder'
}
