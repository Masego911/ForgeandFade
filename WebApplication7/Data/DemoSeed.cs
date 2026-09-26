using ForgeAndFade.Api.Models; // Provides the Barber and Service entity classes.
using Microsoft.EntityFrameworkCore; // Provides asynchronous database queries such as AnyAsync.

namespace ForgeAndFade.Api.Data; // Places this class alongside the database context.

public static class DemoSeed // Groups the sample catalogue setup in one class.
{ // Begins the DemoSeed class.
    public static async Task Apply(ApplicationDbContext db) // Receives the database context used to read and save catalogue records.
    { // Begins the Apply method.
        var sampleBarbers = new[] // Defines the profiles that should exist in the catalogue.
        { // Begins the barber collection.
            new Barber { FirstName = "Kabelo", LastName = "Molefe", Speciality = "Fades & textured hair", Bio = "Precision barber specialising in skin fades, tapers, textured hair and detailed finishing.", ProfileImageUrl = "/images/kabelo.webp", IsActive = true }, // Defines Kabelo's details and portrait path.
            new Barber { FirstName = "Liam", LastName = "Jacobs", Speciality = "Classic & scissor cuts", Bio = "Contemporary classic cuts, scissor work and polished everyday styles.", ProfileImageUrl = "/images/liam.webp", IsActive = true }, // Defines Liam's details and portrait path.
            new Barber { FirstName = "Aiden", LastName = "Naidoo", Speciality = "Beard & precision grooming", Bio = "Beard sculpting, razor detailing, clean line-ups and precise grooming.", ProfileImageUrl = "/images/aiden.webp", IsActive = true }, // Defines Aiden's details and portrait path.
            new Barber { FirstName = "Thando", LastName = "Mkhize", Speciality = "Afro & creative styling", Bio = "Natural textures, afro grooming, modern fades and expressive styles.", ProfileImageUrl = "/images/thando.webp", IsActive = true }, // Defines Thando's details and portrait path.
            new Barber { FirstName = "Miguel", LastName = "Daniels", Speciality = "Modern grooming", Bio = "Modern cutting techniques, fades and beard work for versatile finished looks.", ProfileImageUrl = "/images/miguel.webp", IsActive = true } // Defines Miguel's details and portrait path.
        }; // Ends the barber collection.

        foreach (var barber in sampleBarbers) // Examines each sample barber independently.
        { // Begins the check for the current barber.
            var exists = await db.Barbers.AnyAsync(existing => existing.FirstName == barber.FirstName && existing.LastName == barber.LastName); // Checks whether that full name is already stored.
            if (!exists) db.Barbers.Add(barber); // Queues only a missing barber for insertion.
        } // Ends the check and moves to the next barber.

        var existingServices = await db.Services.OrderBy(service => service.ServiceId).ToListAsync();
        foreach (var group in existingServices.Where(service => service.IsActive)
                     .GroupBy(service => ServiceCatalogue.NameKey(service.ServiceName)))
        {
            // Preserve every record and booking reference; only retire extra active copies.
            foreach (var duplicate in group.Skip(1)) duplicate.IsActive = false;
        }

        var knownNames = existingServices.Select(service => ServiceCatalogue.NameKey(service.ServiceName)).ToHashSet();
        var sampleServices = new[]
            { // Begins the service collection.
                new Service { ServiceName = "Signature cut", Description = "Consultation, precision cut and finish.", Price = 320, DurationMinutes = 45, IsActive = true }, // Defines the signature cut.
                new Service { ServiceName = "Skin fade", Description = "Detailed fade with a crisp finish.", Price = 350, DurationMinutes = 45, IsActive = true }, // Defines the skin fade.
                new Service { ServiceName = "Beard sculpt", Description = "Shape, line and condition your beard.", Price = 190, DurationMinutes = 30, IsActive = true }, // Defines beard grooming.
                new Service { ServiceName = "Cut & beard", Description = "A complete haircut and beard service.", Price = 480, DurationMinutes = 75, IsActive = true }, // Defines the combined service.
                new Service { ServiceName = "Scissor cut", Description = "Tailored scissor work with styling.", Price = 380, DurationMinutes = 60, IsActive = true }, // Defines the scissor cut.
                new Service { ServiceName = "Junior cut", Description = "A considered cut for ages 6–12.", Price = 240, DurationMinutes = 30, IsActive = true }, // Defines the junior cut.
                new Service { ServiceName = "The full ritual", Description = "Cut, beard and finishing treatment.", Price = 650, DurationMinutes = 90, IsActive = true }, // Defines the premium service.
                // PROVISIONAL prices (ZAR) and durations: review before publishing.
                new Service { ServiceName = "Cut and hair colouring", Description = "Precision cut, hair colouring and finish.", Price = 750, DurationMinutes = 120, IsActive = true },
                new Service { ServiceName = "Highlights", Description = "Highlights with a finished style.", Price = 650, DurationMinutes = 120, IsActive = true },
                new Service { ServiceName = "Dread retwist and fade", Description = "Dread retwist with a detailed fade.", Price = 550, DurationMinutes = 120, IsActive = true },
                new Service { ServiceName = "Hair twists and fade", Description = "Hair twists with a detailed fade.", Price = 500, DurationMinutes = 90, IsActive = true }
            };
        foreach (var service in sampleServices)
        {
            // Include inactive names in this check so repeat seeding respects retired services.
            if (knownNames.Add(ServiceCatalogue.NameKey(service.ServiceName))) db.Services.Add(service);
        }

        await db.SaveChangesAsync(); // Writes any newly queued barbers or services to the database.
    } // Ends the Apply method.
} // Ends the DemoSeed class.
