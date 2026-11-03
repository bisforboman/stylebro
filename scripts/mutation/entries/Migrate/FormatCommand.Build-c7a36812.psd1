# Trial 65: check --build in several configurations leaves a restore the next run must redo
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Build.cs'
    Find = 'if (configurationCount < 2)'
    Replace = 'if (configurationCount < 1)'
    Tests = 'CheckBuild_InSeveralConfigurations_MakesTheNextRunRestore'
}
