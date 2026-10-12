# verify's copy of a git repository is one too (Husky's install on restore).
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'PreviewCommand.Git(copy, "init", "-q");'
    Replace = ''
    Tests = 'KeptFindingsTests.Verify_TheCopyOfAGitRepository'
}
