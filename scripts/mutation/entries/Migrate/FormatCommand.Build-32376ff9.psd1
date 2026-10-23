# trial 49: an emit-time compiler error (CS8002) without a place in the code doesn't mean nothing compiled
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Build.cs'
    Find = '&& !EmitErrors.Contains(message.Id)'
    Replace = ''
    Tests = 'MigrationTests.BuildCheck_AnEmitErrorTheUntouchedCodeHasToo_IsClean'
}
