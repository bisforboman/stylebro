# A resource or WinForms designer's file isn't rewritten by a build (SSH.NET).
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '        if ((name.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase) && DesignerWritten(file))'
    Replace = '        if ((false)'
    Tests = 'StyleBro.Tests.MigrationTests.GeneratedFileNote_LeavesDesignersOut_AndMentionsOtherProjects'
}
