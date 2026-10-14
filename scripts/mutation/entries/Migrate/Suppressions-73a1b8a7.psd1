# a root NoWarn says the option isn't applied (trial 20)
@{
    File = 'src/StyleBro.Migrate/Suppressions.cs'
    Find = 'report[at] = section == "*.cs"'
    Replace = 'report[at] = false'
    Tests = 'MigrationTests.Annotate_SaysWhereANoWarn'
}
