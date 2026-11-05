# A framework with a line break around it (a multi-line TargetFrameworks) is still the run framework.
@{
    File = 'src/StyleBro.FormatHost/Views.cs'
    Find = 'name.Length - open - 2).Trim() : null;'
    Replace = 'name.Length - open - 2) : null;'
    Tests = 'FrameworkOf_ReadsTheInstanceName'
}
