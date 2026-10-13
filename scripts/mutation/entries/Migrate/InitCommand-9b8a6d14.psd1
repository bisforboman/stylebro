# Trial 15 (Fluid): a comment-only verdict above is another topic.
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = '&& (first == i || Topic(previous[first - 1]) == Topic(previous[i - 1]))'
    Replace = ''
    Tests = 'FullyQualifiedName~MigrationTests.KeptLines_LeaveAnotherTopicsCommentAbove|FullyQualifiedName~MigrationTests.Init_ACommentOnlyUsingVerdict_IsWrittenOnceInItsPlace'
}
