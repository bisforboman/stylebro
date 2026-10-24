# Trial 51: a multi-targeted project without the run's framework isn't forced onto it
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = ' or (''$(StyleBroFormatOther)'' != '''' and $(StyleBroFormatOther.Contains(''|$(MSBuildProjectFullPath.ToUpperInvariant())|''))))">'
    Replace = ')">'
    Tests = 'MigrationTests.Format_AMultiTargetedProjectWithoutTheRunsFramework_LoadsItsOwnFrameworks'
}
