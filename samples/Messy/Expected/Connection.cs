namespace Messy;

/// <summary>A connection.</summary>
public class Connection
{
    /// <summary>Initializes a new instance of the <see cref="Connection"/> class. Opens a connection.</summary>
    /// <param name="host">The host.</param>
    public Connection(string host)
    {
        Host = host;
    }

    /// <summary>Gets the host.</summary>
    public string Host { get; }

    /// <summary>Closes it.</summary>
    public void Close()
    {
    }
}