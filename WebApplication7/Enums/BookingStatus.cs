namespace ForgeAndFade.Api.Enums // Defines the namespace that groups the application's enumeration types together.
{
    public enum BookingStatus // Defines a fixed set of valid states that a booking can have.
    {
        Pending, // Means the booking has been created but has not yet been confirmed.

        Confirmed, // Means the booking has been accepted and is scheduled to take place.

        Completed, // Means the appointment took place successfully.

        Cancelled, // Means the booking was cancelled before the appointment took place.

        NoShow // Means the customer did not arrive for the scheduled appointment.
    }
}