# With EnforceCodeStyleInBuild, "nothing enforces it in the build" was wrong (c6-acc-warn).
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = '{(codeStyleInBuild ? "IDE0040 is off in init''s block'
    Replace = '{(false ? "IDE0040 is off in init''s block'
    Tests = 'StyleBro.Tests.MigrationTests.Texts_SayWhatHappens_InOptOutFolders_AndWithCodeStyleInTheBuild'
}
