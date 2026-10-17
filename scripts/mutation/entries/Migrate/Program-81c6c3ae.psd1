# Trial 28 (2026-10-17): init/migrate
@{
    File = 'src/StyleBro.Migrate/Program.cs'
    Find = 'Console.WriteLine(blockedLine);
            if (write)
            {
                return 1;
            }
        }

        Console.WriteLine(suppressions'
    Replace = 'Console.WriteLine(blockedLine);
        }

        Console.WriteLine(suppressions'
    Tests = 'MigrationTests.Migrate_WithALockedFile_WritesNothing'
}
