# The trimmed list is unescaped, or the inner builds become one (trial 69, Arch).
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '<TargetFrameworks>$([MSBuild]::Unescape($([System.Text.RegularExpressions.Regex]::Replace('
    Replace = '<TargetFrameworks>$([System.String]::Copy($([System.Text.RegularExpressions.Regex]::Replace('
    Tests = 'RunTargets_KeepTheInnerBuildsOfATrimmedTargetFrameworks'
}
