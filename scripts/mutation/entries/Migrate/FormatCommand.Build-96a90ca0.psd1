# Trial 58 (LightInject): formats ids in the folders migrate leaves alone are compared with the untouched build, not findings
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Build.cs'
    Find = 'ids.Contains(message.Id) && leftAlone?.Any(f => message.RelativePath.StartsWith(f, StringComparison.OrdinalIgnoreCase)) != true'
    Replace = 'ids.Contains(message.Id)'
    Tests = 'MigrationTests.BuildCheck_FormatsRulesInAFolderFormatLeavesAlone_AreComparedWithTheUntouchedCode'
}
