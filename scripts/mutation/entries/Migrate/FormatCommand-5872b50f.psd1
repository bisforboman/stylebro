# format's hook passes the list (trial 35)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '<AdditionalFiles Include="$(StyleBroFormatOldCSharp)" />'
    Replace = '<AdditionalFiles Include="" />'
    Tests = 'MigrationTests.FormatsHook_AddsTheOldCSharpList'
}
