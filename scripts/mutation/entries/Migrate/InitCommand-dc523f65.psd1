# Trial 52: init says nothing more when the files outvote the key
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'if (formatsWhitespace && gitEndings is null && contradictedEnding is null && rootEnding is not null && Conventions.ConvertedMinorityNote'
    Replace = 'if (formatsWhitespace && gitEndings is null && rootEnding is not null && Conventions.ConvertedMinorityNote'
    Tests = 'MigrationTests.Migrate_AndInit_SayWhichFilesTheirEndOfLineConverts'
}
