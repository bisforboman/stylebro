# Only a run that changed line endings or byte order marks only is the last.
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '&& changed.All(f => File.Exists(f) && beforeText.TryGetValue(f, out var was) && was == Hash(File.ReadAllBytes(f), endingsAside: true));'
    Replace = ';'
    Tests = 'KeptFindingsTests.Format_ARunThatChangedOnlyLineEndings_IsTheLast'
}
