# StyleBroPreset in the file's indentation (CSharpier checks .props files).
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'return existing.Insert(group.Index + group.Length, newLine + (groupIndent.Trim().Length == 0 ? groupIndent : child) + unit + property);'
    Replace = 'return existing.Insert(group.Index + group.Length, newLine + "    " + property);'
    Tests = 'StyleBro.Tests.MigrationTests.Init_WritesCSharpierLines_IntoARootTrueFolderBelowCSharpiersFolder'
}
