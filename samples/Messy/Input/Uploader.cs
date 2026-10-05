namespace Messy;

public interface IUploader
{
    Task Upload(string path, int retries);
}

public class Uploader : IUploader
{
    private const int MaxRetries = 3;
    private static readonly string Prefix = "up-";

    /// <inheritdoc/>
    public Task Upload(string file, int retries) => Send(Prefix + file, retries > MaxRetries ? MaxRetries : retries);

    private Task Send(string name, int count) => Task.Delay(count + name.Length);
}
