# Migration: files a nearer .editorconfig decides a whitespace key for do not vote on the root block
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'foreach (var group in nearer)'
    Replace = 'foreach (var group in nearer.Take(0))'
    Tests = 'MigrationTests.Migrate_ANestedEditorConfigTheFilesFollow_StaysAsItIs'
}
