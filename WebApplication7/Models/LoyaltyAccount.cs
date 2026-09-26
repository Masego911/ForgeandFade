namespace ForgeAndFade.Api.Models // Defines the namespace that contains the application's domain models.
{
    public class LoyaltyAccount // Represents the loyalty account that belongs to one customer.
    {
        public int LoyaltyAccountId { get; set; } // Defines the primary key that uniquely identifies the loyalty account.

        public int CustomerId { get; set; } // Stores the ID of the customer who owns this loyalty account.

        public int PointsBalance { get; set; } // Stores the customer's current number of available loyalty points.

        public DateTime UpdatedAt { get; set; } // Stores the date and time when the loyalty account was last updated.

        public Customer Customer { get; set; } // Defines the navigation property linking the loyalty account to its customer.

        public ICollection<LoyaltyTransaction> Transactions { get; set; } // Stores all loyalty transactions associated with this account.


        public LoyaltyAccount() // Defines the parameterless constructor required by Entity Framework Core.
        {
            PointsBalance = 0; // Starts a new customer's loyalty balance at zero points.

            UpdatedAt = DateTime.Now; // Records the current date and time as the initial update time.

            Customer = null!; // Tells the compiler that Entity Framework will populate the Customer relationship later.

            Transactions = new List<LoyaltyTransaction>(); // Creates an empty collection for loyalty transactions.
        }


        public LoyaltyAccount( // Defines an overloaded constructor for creating a fully populated loyalty account.
            int loyaltyAccountId, // Accepts the unique identifier for the loyalty account.
            int customerId, // Accepts the ID of the customer who owns the account.
            int pointsBalance, // Accepts the current loyalty points balance.
            DateTime updatedAt) // Accepts the date and time when the loyalty account was last updated.
        {
            LoyaltyAccountId = loyaltyAccountId; // Assigns the supplied loyalty account ID.

            CustomerId = customerId; // Assigns the supplied customer ID.

            PointsBalance = pointsBalance; // Assigns the supplied points balance.

            UpdatedAt = updatedAt; // Assigns the supplied update timestamp.

            Customer = null!; // Leaves the Customer navigation property for Entity Framework to populate.

            Transactions = new List<LoyaltyTransaction>(); // Creates an empty collection for transaction records.
        }
    }
}