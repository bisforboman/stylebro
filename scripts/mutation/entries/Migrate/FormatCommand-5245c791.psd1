# Trial 65: a hidden IDE0055 is off where CSharpier formats
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'ReportDiagnostic.Hidden && csharpier'
    Replace = 'ReportDiagnostic.Hidden && !csharpier'
    Tests = 'TheWhitespacePass_LeavesOutAHiddenIde0055_OnlyWhereCSharpierFormats'
}
