using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace StyleBro.Host;

/// <summary>
/// 'dotnet format''s console logger as bro reads it: each message on its own line, from the level its --verbosity shows.
/// Project names a framework's run loads as one project ('Lib(net8.0)', see <see cref="Session"/>) are written the way that
/// run's 'dotnet format' named them ('Lib').
/// </summary>
internal sealed class HostLogger(LogLevel minimum, Action<string> output, IReadOnlyDictionary<string, string> names) : ILogger
{
    /// <inheritdoc/>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!this.IsEnabled(logLevel))
        {
            return;
        }

        var message = formatter(state, exception);
        foreach (var name in names)
        {
            message = message.Replace(name.Key, name.Value, StringComparison.Ordinal);
        }

        output(message);
    }

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) => logLevel >= minimum && logLevel != LogLevel.None;

    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;
}
