namespace Messy;

/// <summary>A connection.</summary>
public class Connection
{
    /// <summary>Opens a connection.</summary>
    /// <param name="host">The host.</param>
    public Connection(string host)
    {
        Host = host;
    }

    /// <summary>Gets the host.</summary>
    public string Host { get; }

    /// <summary>Closes it.</summary>
    /// <returns>Nothing.</returns>
    public void Close()
    {
    }
}