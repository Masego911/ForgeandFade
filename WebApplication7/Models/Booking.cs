using ForgeAndFade.Api.Enums; // Imports the BookingStatus enum so this model can use strongly typed booking states.

namespace ForgeAndFade.Api.Models // Places the Booking model inside the application's Models namespace.
{
    public class Booking // Represents one appointment created by a customer.
    {
        public bool IsForChild { get; set; } // Explicit recipient; legacy bookings remain self appointments.

        public int BookingId { get; set; } // Stores the unique primary-key identifier for the booking.

        public int CustomerId { get; set; } // Stores the foreign-key identifier of the customer who made the booking.

        public int BarberId { get; set; } // Stores the foreign-key identifier of the barber selected for the appointment.

        public int ServiceId { get; set; } // Stores the foreign-key identifier of the service selected by the customer.

        public DateTime BookingDate { get; set; } // Stores the calendar date on which the appointment will take place.

        public TimeSpan StartTime { get; set; } // Stores the time at which the appointment begins.

        public TimeSpan EndTime { get; set; } // Stores the time at which the appointment ends.

        public BookingStatus Status { get; set; } // Stores the booking state using the BookingStatus enum instead of unrestricted text.

        public string CustomerNotes { get; set; } // Stores optional notes supplied by the customer for the appointment.

        public DateTime CreatedAt { get; set; } // Stores the date and time when the booking record was originally created.

        public Customer Customer { get; set; } // Provides navigation from the booking to its related Customer entity.

        public Barber Barber { get; set; } // Provides navigation from the booking to its related Barber entity.

        public Service Service { get; set; } // Provides navigation from the booking to its related Service entity.


        public Booking() // Defines the parameterless constructor required by Entity Framework Core.
        {
            Status = BookingStatus.Confirmed; // Gives newly created bookings the Confirmed status by default.

            CustomerNotes = string.Empty; // Initialises customer notes as an empty string rather than null.

            CreatedAt = DateTime.Now; // Records the current local date and time when the object is created.

            Customer = null!; // Indicates that Entity Framework will populate the Customer navigation property.

            Barber = null!; // Indicates that Entity Framework will populate the Barber navigation property.

            Service = null!; // Indicates that Entity Framework will populate the Service navigation property.
        }


        public Booking( // Defines an overloaded constructor for creating a booking with supplied appointment details.
            int bookingId, // Receives the unique booking identifier.
            int customerId, // Receives the customer identifier.
            int barberId, // Receives the selected barber identifier.
            int serviceId, // Receives the selected service identifier.
            DateTime bookingDate, // Receives the appointment date.
            TimeSpan startTime, // Receives the appointment start time.
            TimeSpan endTime, // Receives the appointment end time.
            BookingStatus status, // Receives one of the valid BookingStatus enum values.
            string customerNotes, // Receives optional customer notes.
            DateTime createdAt) // Receives the timestamp representing when the booking was created.
        {
            BookingId = bookingId; // Assigns the supplied booking identifier to the BookingId property.

            CustomerId = customerId; // Assigns the supplied customer identifier to CustomerId.

            BarberId = barberId; // Assigns the supplied barber identifier to BarberId.

            ServiceId = serviceId; // Assigns the supplied service identifier to ServiceId.

            BookingDate = bookingDate; // Assigns the supplied appointment date.

            StartTime = startTime; // Assigns the supplied appointment start time.

            EndTime = endTime; // Assigns the supplied appointment end time.

            Status = status; // Assigns the supplied BookingStatus enum value.

            CustomerNotes = customerNotes; // Assigns the supplied customer notes.

            CreatedAt = createdAt; // Assigns the supplied creation timestamp.

            Customer = null!; // Leaves the Customer navigation property for Entity Framework to populate.

            Barber = null!; // Leaves the Barber navigation property for Entity Framework to populate.

            Service = null!; // Leaves the Service navigation property for Entity Framework to populate.
        }
    }
}