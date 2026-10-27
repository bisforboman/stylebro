# findings of format's rules there still count
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Build.cs'
    Find = 'bool Unmatched(BuildMessage m) => m.Error && !ids.Contains(m.Id) && unmatched.Contains(m.Project);'
    Replace = 'bool Unmatched(BuildMessage m) => m.Error && unmatched.Contains(m.Project);'
    Tests = 'MigrationTests.BuildCheck_ErrorsInAProjectTheUntouchedBuildDidntCompile_AreNotCompared'
}
