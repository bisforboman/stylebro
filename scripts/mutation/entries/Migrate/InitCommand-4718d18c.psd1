# init: an s_camelCase static style the team sets keeps BRO1307 on
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'v.Trim() == "s_camelCase"'
    Replace = 'false'
    Tests = 'MigrationTests.Init_StaticPrefixStyleSet_KeepsBro1307On'
}
