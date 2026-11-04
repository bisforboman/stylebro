Set-Location /home/user/stylebro
& ./scripts/mutation/New-Mutation.ps1 -File 'src/StyleBro.Migrate/FormatCommand.Speed.cs' -Find '(xunit.Count == 0 ? string.Empty : $"; xunit.analyzers {string.Join(",", xunit)}")' -Replace 'string.Empty' -Tests 'Verify_RecordKey_HoldsTheSdkAndPackageVersions' -Comment 'Trial 67: xunit.analyzers'' version is part of the record key (its note is in the record).'
