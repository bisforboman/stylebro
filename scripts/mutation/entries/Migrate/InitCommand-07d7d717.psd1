# IDE0009 enforced only in .globalconfig: init says format doesn't fix it
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = '!= ".globalconfig" ? string.Empty'
    Replace = '== ".globalconfig" ? string.Empty'
    Tests = 'MigrationTests.Init_ThisRequiredInTheGlobalConfig_SaysFormatWontFixIt'
}
