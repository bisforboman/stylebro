# Allowed leading-lambda calls don't count for BRO1107/BRO1108's conventions
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'if (counts.Remove(key + LeadingLambdaBucket, out var bucket) && !allowed)'
    Replace = 'if (counts.Remove(key + LeadingLambdaBucket, out var bucket))'
    Tests = 'MigrationTests.Conventions_LeadingLambdaCalls_DontCountForTheListRules'
}
