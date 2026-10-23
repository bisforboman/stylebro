# Format host: a framework's run checks that framework's instance of a multi-targeted project only
@{
    File = 'src/StyleBro.FormatHost/Views.cs'
    Find = ': instances.Where(p => string.Equals(FrameworkOf(p.Name), framework, StringComparison.OrdinalIgnoreCase)));'
    Replace = ': instances);'
    Tests = 'FormatHostTests.AView_HasTheRunsInstances_AndWhatTheyReference'
}
