# init keeps a repository's enforced 'this.' (no IDE0009 = none)
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'if (!migrated && RequiresThisQualification(folder.Length == 0 ? WithAnalysisModeAll(repository, settings) : settings))'
    Replace = 'if (false)'
    Tests = 'MigrationTests.Init_KeepsAnEnforcedThisQualification'
}
