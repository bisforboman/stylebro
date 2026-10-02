namespace Messy.Pooling
{
    public class Connection
    {
        public void Reset()
        {
        }

        public virtual void Close()
        {
        }
    }

    public class PooledConnection : Connection
    {
        public void Return()
        {
            base.Reset();
            base.Close();
        }
    }
}
