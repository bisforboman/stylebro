# Format host: a load warning naming no loaded project (one that didn't load) belongs to every view
@{
    File = 'src/StyleBro.FormatHost/Views.cs'
    Find = '!Names(d, all) || '
    Replace = ''
    Tests = 'FormatHostTests.AView_HasTheLoadWarnings_OfItsProjects_AndOfProjectsThatDidntLoad'
}
