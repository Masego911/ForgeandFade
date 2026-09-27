namespace ForgeAndFade.Api.Models; // Places the entity beside the Customer model.
public sealed class PasswordResetToken // Represents one password-reset request.
{ // Begins the entity definition.
    public int PasswordResetTokenId { get; set; } // Gives each request a primary key.
    public int CustomerId { get; set; } // Links the request to a customer.
    public Customer Customer { get; set; } = null!; // Defines the required customer relationship.
    public string TokenHash { get; set; } = string.Empty; // Stores a hash instead of the emailed token.
    public DateTime CreatedAtUtc { get; set; } // Records when the request was created.
    public DateTime ExpiresAtUtc { get; set; } // Records when the reset link expires.
    public DateTime? UsedAtUtc { get; set; } // Marks a reset link as used.
} // Ends the entity definition.
