namespace Messy;

public class Transfer
{
    public void Send(byte[] data)
    {
        Send(data, 0);
    }

    public void Cancel()
    {
    }

    private void Send(byte[] data, int retries)
    {
        if (retries > 3)
        {
            Cancel();
        }
    }
}
