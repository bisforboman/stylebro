# CSharpier turns IDE0036 (modifier order) off
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = '        "IDE0036",'
    Replace = ''
    Tests = 'MigrationTests.Init_CSharpier_TurnsTheModifierOrderOff'
}
