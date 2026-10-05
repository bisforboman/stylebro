namespace Messy;

public interface IUploader
{
    Task UploadAsync(string path, int retries);
}

public class Uploader : IUploader
{
    private const int maxRetries = 3;
    private static readonly string _prefix = "up-";

    /// <inheritdoc/>
    public Task UploadAsync(string path, int retries) => SendAsync(_prefix + path, retries > maxRetries ? maxRetries : retries);

    private Task SendAsync(string name, int count) => Task.Delay(count + name.Length);
}
