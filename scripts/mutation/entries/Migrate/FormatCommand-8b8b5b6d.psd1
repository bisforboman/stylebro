# projects without the run's framework lose it before NuGet's props too (trial 54, Dapper)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '<PropertyGroup Condition="(''$(StyleBroFormatKeep)'''
    Replace = '<PropertyGroup Condition="''false'' == ''true'' and (''$(StyleBroFormatKeep)'''
    Tests = 'MigrationTests.Format_ALibraryWithoutTheRunsFramework'
}
