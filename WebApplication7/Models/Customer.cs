

namespace ForgeAndFade.Api.Models // Defines the namespace that contains the application's domain models.
{
    public class Customer // Represents a registered customer who can log in, make bookings and earn loyalty points.
    {
        public int CustomerId { get; set; } // Defines the primary key that uniquely identifies each customer.

        public string FirstName { get; set; } // Stores the customer's first name.

        public string LastName { get; set; } // Stores the customer's surname.

        public string Email { get; set; } // Stores the customer's email address, which we can later use for login and booking communication.

        public string PhoneNumber { get; set; } // Stores the customer's mobile number.

        public string PasswordHash { get; set; } // Stores the hashed version of the customer's password instead of storing the real password.

        public DateTime CreatedAt { get; set; } // Stores the date and time when the customer account was created.

        public int? PreferredBarberId { get; set; } // Stores the customer's preferred barber when one has been selected; the question mark allows this value to be null.

        public Barber? PreferredBarber { get; set; } // Defines an optional navigation property linking the customer to their preferred barber.

        public ICollection<Booking> Bookings { get; set; } // Stores all bookings that belong to this customer.

        public LoyaltyAccount? LoyaltyAccount { get; set; } // Defines the one-to-one relationship between the customer and their loyalty account.


        public Customer() // Defines the parameterless constructor required by Entity Framework Core.
        {
            FirstName = string.Empty; // Prevents the FirstName property from starting as null.

            LastName = string.Empty; // Prevents the LastName property from starting as null.

            Email = string.Empty; // Prevents the Email property from starting as null.

            PhoneNumber = string.Empty; // Prevents the PhoneNumber property from starting as null.

            PasswordHash = string.Empty; // Prevents the PasswordHash property from starting as null.

            CreatedAt = DateTime.Now; // Records the current date and time when the Customer object is created.

            PreferredBarberId = null; // Indicates that a new customer does not have a preferred barber yet.

            PreferredBarber = null; // Indicates that the preferred barber relationship is initially empty.

            Bookings = new List<Booking>(); // Creates an empty booking collection so bookings can be added without causing a null-reference error.

            LoyaltyAccount = null; // Indicates that the loyalty account relationship has not yet been assigned.
        }


        public Customer( // Defines an overloaded constructor for creating a complete customer object.
            int customerId, // Accepts the unique identifier for the customer.
            string firstName, // Accepts the customer's first name.
            string lastName, // Accepts the customer's surname.
            string email, // Accepts the customer's email address.
            string phoneNumber, // Accepts the customer's mobile number.
            string passwordHash, // Accepts the already-hashed password.
            DateTime createdAt, // Accepts the account creation date and time.
            int? preferredBarberId) // Accepts the optional ID of the customer's preferred barber.
        {
            CustomerId = customerId; // Assigns the supplied customer ID to the CustomerId property.

            FirstName = firstName; // Assigns the supplied first name to the FirstName property.

            LastName = lastName; // Assigns the supplied surname to the LastName property.

            Email = email; // Assigns the supplied email address to the Email property.

            PhoneNumber = phoneNumber; // Assigns the supplied phone number to the PhoneNumber property.

            PasswordHash = passwordHash; // Assigns the supplied hashed password to the PasswordHash property.

            CreatedAt = createdAt; // Assigns the supplied account creation timestamp.

            PreferredBarberId = preferredBarberId; // Assigns the optional preferred barber ID.

            PreferredBarber = null; // Leaves the PreferredBarber navigation property for Entity Framework to populate.

            Bookings = new List<Booking>(); // Creates an empty collection for the customer's bookings.

            LoyaltyAccount = null; // Leaves the loyalty account relationship unassigned until it is created.
        }
    }
}