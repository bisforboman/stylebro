# StyleBroPreset in the file's indentation (CSharpier checks .props files).
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'var indent = groupIndent.Trim().Length == 0 ? groupIndent : child;'
    Replace = 'var indent = string.Empty;'
    Tests = 'StyleBro.Tests.MigrationTests.Init_WritesCSharpierLines_IntoARootTrueFolderBelowCSharpiersFolder'
}
