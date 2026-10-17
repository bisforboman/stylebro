# init: constants and static readonly fields have conventions of their own (trial 29)
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'else if (!field.IsConst && !(field.IsStatic && field.IsReadOnly) && '
    Replace = 'else if ('
    Tests = 'MigrationTests.Conventions_CountsNonPrivateFieldCasing'
}
