namespace api.Models
{
    // The four states an application can be in.
    // HELD       -> a seat is reserved for a limited time (the countdown is running)
    // CONFIRMED  -> the fee was "paid"; the seat is permanently taken
    // EXPIRED    -> the countdown ran out; the seat went back to the pool
    // CANCELLED  -> the parent cancelled; the seat went back to the pool
    public static class HoldStatus
    {
        public const string Held = "HELD";
        public const string Confirmed = "CONFIRMED";
        public const string Expired = "EXPIRED";
        public const string Cancelled = "CANCELLED";
    }
}
