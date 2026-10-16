# The preview says not clean when CSharpier puts back what the fixes change.
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = ': putBack > 0 ? $"not clean: CSharpier puts back'
    Replace = ': putBack > 99 ? $"not clean: CSharpier puts back'
    Tests = 'MigrationTests.Preview_SaysNotClean_WhenCSharpierPutsBackWhatTheFixesChange'
}
