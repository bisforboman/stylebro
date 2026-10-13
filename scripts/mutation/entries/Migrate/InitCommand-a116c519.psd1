# Trial 15 (Fluid): the using verdict goes where the last init wrote it.
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'kept.Insert(usingsAt, detected[at]);'
    Replace = 'kept.Add(detected[at]);'
    Tests = 'MigrationTests.Init_ACommentOnlyUsingVerdict_IsWrittenOnceInItsPlace'
}
