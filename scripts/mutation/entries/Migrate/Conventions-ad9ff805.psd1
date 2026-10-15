# Non-private fields are counted against an enforced naming rule
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'if (styles.NonPrivate is null)'
    Replace = 'if (styles.NonPrivate is not null)'
    Tests = 'MigrationTests.Conventions_CountsNonPrivateFieldsAgainstTheirNamingRule'
}
