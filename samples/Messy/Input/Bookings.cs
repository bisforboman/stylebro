namespace Messy.Bookings
{
    public class Bookings
    {
        public DateTime? CheckedIn { get; set; }

        public DateTime? CheckedOut { get; set; }

        public bool IsActive => CheckedIn.HasValue && !CheckedOut.HasValue;

        public bool IsOpen(bool flag)
        {
            if ((CheckedOut.HasValue))
            {
                return false;
            }

            return flag == CheckedIn.HasValue;
        }
    }
}
