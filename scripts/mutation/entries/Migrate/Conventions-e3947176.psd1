# Microsoft style (2026-10-16): init --style
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'Clear(StaticPrefixKey) == 1 && !contradicted'
    Replace = 'Clear(StaticPrefixKey) == 1'
    Tests = 'MigrationTests.Init_SuggestsTheStyle_ForRuntimeShapedCode'
}
