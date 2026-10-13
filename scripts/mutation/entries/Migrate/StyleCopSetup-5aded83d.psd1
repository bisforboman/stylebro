# Trial 15: $(MSBuildThisFile) in GetPathOfFileAbove.
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = ': m.Groups[1].Value.Equals("MSBuildThisFile", StringComparison.OrdinalIgnoreCase) ? Path.GetFileName(file)'
    Replace = ': m.Groups[1].Value.Equals("MSBuildThisFile", StringComparison.OrdinalIgnoreCase) ? m.Value'
    Tests = 'FullyQualifiedName~MigrationTests.AnUnquotedGetPathOfFileAbove_IsFollowed'
}
