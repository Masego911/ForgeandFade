namespace ForgeAndFade.Api.Models // Places the Barber class inside the Models namespace used throughout the application.
{
    public class Barber // Represents an individual barber who can be selected for appointments.
    {
        public int BarberId { get; set; } // Defines the primary key; EF Core recognises the ClassName + Id naming convention automatically.

        public string FirstName { get; set; } // Stores the barber's first name.

        public string LastName { get; set; } // Stores the barber's surname.

        public string Bio { get; set; } // Stores descriptive information about the barber for their public profile.

        public string Speciality { get; set; } // Stores the barber's primary area of expertise.

        public string ProfileImageUrl { get; set; } // Stores the location of the barber's profile image.

        public bool IsActive { get; set; } // Determines whether the barber should currently appear as available in the application.


        public Barber() // Defines the parameterless constructor required by Entity Framework Core.
        {
            FirstName = string.Empty; // Initialises FirstName so the property does not begin as null.

            LastName = string.Empty; // Initialises LastName so the property does not begin as null.

            Bio = string.Empty; // Initialises Bio so the property does not begin as null.

            Speciality = string.Empty; // Initialises Speciality so the property does not begin as null.

            ProfileImageUrl = string.Empty; // Initialises ProfileImageUrl so the property does not begin as null.

            IsActive = true; // Makes newly created barbers active by default.
        }


        public Barber( // Defines an overloaded constructor for creating a barber with supplied values.
            int barberId, // Receives the barber's unique identifier.
            string firstName, // Receives the barber's first name.
            string lastName, // Receives the barber's surname.
            string bio, // Receives the barber's biography.
            string speciality, // Receives the barber's speciality.
            string profileImageUrl, // Receives the profile image location.
            bool isActive) // Receives the barber's current active status.
        {
            BarberId = barberId; // Assigns the supplied identifier to the BarberId property.

            FirstName = firstName; // Assigns the supplied first name.

            LastName = lastName; // Assigns the supplied surname.

            Bio = bio; // Assigns the supplied biography.

            Speciality = speciality; // Assigns the supplied speciality.

            ProfileImageUrl = profileImageUrl; // Assigns the supplied image location.

            IsActive = isActive; // Assigns the supplied active status.
        }
    }
}