# 'false:warning' doesn't require 'this.'
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = '&& on.Trim().Equals("true", StringComparison.OrdinalIgnoreCase) && Shown(severity)'
    Replace = '&& Shown(severity)'
    Tests = 'MigrationTests.Init_KeepsAnEnforcedThisQualification'
}
