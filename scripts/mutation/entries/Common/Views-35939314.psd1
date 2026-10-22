# Format host: a load warning about a project goes to the views with that project, not to those its text names
@{
    File = 'src/StyleBro.FormatHost/Views.cs'
    Find = 'd is ProjectDiagnostic project'
    Replace = 'd is null'
    Tests = 'FormatHostTests.AView_HasTheLoadWarnings_OfItsProjects_AndOfProjectsThatDidntLoad'
}
