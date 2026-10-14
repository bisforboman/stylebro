# An ItemGroup in the file's indentation.
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = '$"{child}<ItemGroup>{newLine}{string.Concat(items.Select(item => $"{child}{unit}{item}{newLine}"))}{child}</ItemGroup>{newLine}"'
    Replace = '$"  <ItemGroup>{newLine}{string.Concat(items.Select(item => $"    {item}{newLine}"))}  </ItemGroup>{newLine}"'
    Tests = 'StyleBro.Tests.MigrationTests.MSBuildEdits_FollowTheFilesIndentation_AndTheAnalyzersItemGroup'
}
