# Trial 28 (2026-10-17): init/migrate
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'var style = InitCommand.NamingRulesOff(own) ? null : StyleBro.Analyzers.Naming.FieldNames.GetStyles(new Conventions.Options(own)).MutableStatic switch'
    Replace = 'var style = StyleBro.Analyzers.Naming.FieldNames.GetStyles(new Conventions.Options(own)).MutableStatic switch'
    Tests = 'MigrationTests.Migrate_FollowsANamingRuleForStatics_TheCodeDoesntBreak'
}
