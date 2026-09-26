namespace ForgeAndFade.Api.DTOs // Places this request object inside the application's DTO namespace.
{
    public class RegisterCustomerDto // Represents only the information a new customer is allowed to submit during registration.
    {
        public string FirstName { get; set; } // Stores the first name supplied during registration.

        public string LastName { get; set; } // Stores the surname supplied during registration.

        public string Email { get; set; } // Stores the customer's email address, which will later also be used for login.

        public string PhoneNumber { get; set; } // Stores the customer's cellphone number.

        public string Password { get; set; } // Temporarily receives the plain-text password so the backend can hash it before storage.


        public RegisterCustomerDto() // Defines the parameterless constructor used when ASP.NET Core converts JSON into this DTO.
        {
            FirstName = string.Empty; // Prevents FirstName from beginning as null.

            LastName = string.Empty; // Prevents LastName from beginning as null.

            Email = string.Empty; // Prevents Email from beginning as null.

            PhoneNumber = string.Empty; // Prevents PhoneNumber from beginning as null.

            Password = string.Empty; // Prevents Password from beginning as null.
        }
    }
}