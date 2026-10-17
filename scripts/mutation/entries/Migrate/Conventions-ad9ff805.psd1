# Non-private fields are counted against an enforced naming rule
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = '(styles.NonPrivate is null ? null : StyleBro.Analyzers.Naming.FieldNames.GetNonPrivateCasing(field, styles))'
    Replace = '(true ? null : StyleBro.Analyzers.Naming.FieldNames.GetNonPrivateCasing(field, styles))'
    Tests = 'MigrationTests.Conventions_CountsNonPrivateFieldsAgainstTheirNamingRule'
}
