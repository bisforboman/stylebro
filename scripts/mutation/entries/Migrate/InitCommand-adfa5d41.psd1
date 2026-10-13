# Trial 18: no IDE0055 crash hint where CSharpier turns it off
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'if (targets.FirstOrDefault()?.FormatsWhitespace != false)'
    Replace = 'if (true)'
    Tests = 'MigrationTests.Init_SaysNothingAboutIde0055Crashes_WhenCSharpierTurnsItOff'
}
