# A run whose symbols are unknown does not count.
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '.Where(c => c.Defines is not null)'
    Replace = ''
    Tests = 'MigrationTests.IfBodies_AreFoundWithTheSymbolsOfTheRunsThatCompileThem'
}
