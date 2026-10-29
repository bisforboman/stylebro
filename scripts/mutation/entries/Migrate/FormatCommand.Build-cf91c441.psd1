# Trial 62: the untouched build's stopping errors are listed in a fixed order.
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Build.cs'
    Find = '.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).Take(5).ToList();'
    Replace = '.Distinct(StringComparer.Ordinal).Take(5).ToList();'
    Tests = 'StyleBro.Tests.MigrationTests.StoppedLines_AreSortedAndDistinct'
}
