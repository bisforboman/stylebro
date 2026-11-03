# The record answers Not verified (3) for a kept raw string file.
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Speed.cs'
    Find = 'return NotVerifiedExitCode;
        }

        record.Verdict.ForEach(log);'
    Replace = 'return 0;
        }

        record.Verdict.ForEach(log);'
    Tests = 'Verify_SaysWhatFormatSaidAboutAFileTheRawStringGuardKept'
}
