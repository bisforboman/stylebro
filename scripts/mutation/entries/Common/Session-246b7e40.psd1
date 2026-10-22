# Format host: every run sees the files as they are on disk now
@{
    File = 'src/StyleBro.FormatHost/Session.cs'
    Find = 'var current = await loaded.SyncAsync(this.measure).ConfigureAwait(false);'
    Replace = 'var current = loaded.Solution;'
    Tests = 'FormatHostTests.AHostFix_WritesWhatDotnetFormatWrites'
}
