namespace Messy.Bookings
{
    public class Bookings
    {
        public DateTime? CheckedIn { get; set; }

        public DateTime? CheckedOut { get; set; }

        public bool IsActive => CheckedIn is not null && CheckedOut is null;

        public bool IsOpen(bool flag)
        {
            if (CheckedOut is not null)
            {
                return false;
            }

            return flag == CheckedIn.HasValue;
        }
    }
}
