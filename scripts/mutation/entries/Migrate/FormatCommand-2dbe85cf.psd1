# The run targets cut white space out of TargetFrameworks.
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'Regex]::IsMatch(''$(TargetFrameworks)'', ''\s''))">'
    Replace = 'Regex]::IsMatch(''$(TargetFrameworks)'', ''NEVER''))">'
    Tests = 'RunTargets_CutWhiteSpaceOutOfTargetFrameworks'
}
