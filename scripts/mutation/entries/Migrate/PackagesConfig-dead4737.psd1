# Trial 18: a one-line packages.config gets the entry on a line of its own
@{
    File = 'src/StyleBro.Migrate/PackagesConfig.cs'
    Find = 'return end < 0 ? config : config.Insert(end, (alone ? string.Empty : newLine) + "  " + entry + newLine);'
    Replace = 'return end < 0 ? config : config.Insert(end, "  " + entry + newLine);'
    Tests = 'MigrationTests.PackagesConfig_EntryGetsALineOfItsOwn_AndNuGetsTargetIsFoundHoweverItIsWritten'
}
