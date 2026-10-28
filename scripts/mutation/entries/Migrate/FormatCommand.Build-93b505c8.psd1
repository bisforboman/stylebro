# findings of format's rules there still count
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Build.cs'
    Find = 'bool Unmatched(BuildMessage m) => m.Error && !IsFinding(m, ids, leftAlone) && unmatched.Contains(m.Project);'
    Replace = 'bool Unmatched(BuildMessage m) => m.Error && unmatched.Contains(m.Project);'
    Tests = 'MigrationTests.BuildCheck_ErrorsInAProjectTheUntouchedBuildDidntCompile_AreNotCompared'
}
