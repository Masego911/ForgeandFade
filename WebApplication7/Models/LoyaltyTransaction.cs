namespace ForgeAndFade.Api.Models // Places LoyaltyTransaction in the same model namespace as Customer, Booking and the other entities.
{
    public class LoyaltyTransaction // Represents one increase or decrease in a customer's loyalty points.
    {
        public int LoyaltyTransactionId { get; set; } // Defines the primary key that uniquely identifies each loyalty transaction.

        public int LoyaltyAccountId { get; set; } // Stores the ID of the loyalty account affected by this transaction.

        public int? BookingId { get; set; } // Stores an optional booking ID because some loyalty transactions may not come from a booking.

        public int Points { get; set; } // Stores the number of loyalty points added or removed.

        public string TransactionType { get; set; } // Stores the category of transaction, such as Earned or Redeemed.

        public string Description { get; set; } // Stores a readable explanation of why the loyalty points changed.

        public DateTime CreatedAt { get; set; } // Stores when the loyalty transaction was created.

        public LoyaltyAccount LoyaltyAccount { get; set; } // Links this transaction to the loyalty account that owns it.

        public Booking? Booking { get; set; } // Links the transaction to a booking when a booking caused the points change.


        public LoyaltyTransaction() // Defines the parameterless constructor Entity Framework Core can use.
        {
            TransactionType = string.Empty; // Prevents TransactionType from starting as null.

            Description = string.Empty; // Prevents Description from starting as null.

            CreatedAt = DateTime.Now; // Records the current date and time when a transaction object is created.

            LoyaltyAccount = null!; // Indicates that Entity Framework will populate the required loyalty-account relationship.

            Booking = null; // Starts the optional booking relationship with no booking attached.
        }


        public LoyaltyTransaction( // Defines an overloaded constructor for creating a populated loyalty transaction.
            int loyaltyTransactionId, // Receives the transaction's unique identifier.
            int loyaltyAccountId, // Receives the ID of the related loyalty account.
            int? bookingId, // Receives an optional booking ID.
            int points, // Receives the number of points added or removed.
            string transactionType, // Receives the type of loyalty transaction.
            string description, // Receives the explanation for the transaction.
            DateTime createdAt) // Receives the time at which the transaction was created.
        {
            LoyaltyTransactionId = loyaltyTransactionId; // Stores the supplied transaction ID.

            LoyaltyAccountId = loyaltyAccountId; // Stores the supplied loyalty-account ID.

            BookingId = bookingId; // Stores the optional booking ID.

            Points = points; // Stores the supplied points value.

            TransactionType = transactionType; // Stores the supplied transaction type.

            Description = description; // Stores the supplied description.

            CreatedAt = createdAt; // Stores the supplied creation timestamp.

            LoyaltyAccount = null!; // Leaves this relationship for Entity Framework to populate.

            Booking = null; // Leaves the optional Booking navigation property empty initially.
        }
    }
}