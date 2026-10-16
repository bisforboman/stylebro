# A line-break finding can write several line breaks (a blank line too).
@{
    File = 'src/StyleBro.Migrate/IfBodies.cs'
    Find = 'characters with ''(?:(?:\\r)?\\n)+'''
    Replace = 'characters with ''(?:\\r)?\\n'''
    Tests = 'MigrationTests.IfBodies_TheirWhitespaceFindingsDontCount'
}
