namespace Messy;

public class Uploader
{
    public void Send(byte[] data)
    {
        Send(data, 0);
    }

    private void Send(byte[] data, int retries)
    {
        if (retries > 3)
        {
            Cancel();
        }
    }

    public void Cancel()
    {
    }
}
