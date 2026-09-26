using ForgeAndFade.Api.Data; // Provides the database context.
using Microsoft.AspNetCore.Mvc; // Provides controller responses and route attributes.
using Microsoft.EntityFrameworkCore; // Provides asynchronous EF Core queries.

namespace ForgeAndFade.Api.Controllers; // Groups this controller with the other API controllers.

[ApiController] // Enables API controller behaviour, including request validation.
[Route("api/barbers")] // Sets the URL prefix for barber endpoints.
public class BarbersController(ApplicationDbContext db) : ControllerBase // Receives the database context through dependency injection.
{ // Begins the controller.
    [HttpGet] // Handles GET requests to /api/barbers.
    public async Task<IActionResult> All() // Returns the public list of barbers.
    { // Begins the list action.
        var barbers = await db.Barbers.AsNoTracking() // Reads records without tracking changes because this request does not edit them.
            .Where(x => x.IsActive && x.FirstName != "" && x.LastName != "" && x.ProfileImageUrl != "") // Excludes inactive or incomplete profiles from the public list.
            .OrderBy(x => x.BarberId) // Keeps the list in a consistent order.
            .Select(x => new { x.BarberId, x.FirstName, x.LastName, x.Speciality, x.Bio, x.ProfileImageUrl }) // Sends only fields needed by the frontend.
            .ToListAsync(); // Executes the database query asynchronously.
        return Ok(barbers); // Returns the profiles as JSON with HTTP 200.
    } // Ends the list action.

    [HttpGet("{id:int}")] // Handles GET requests to /api/barbers followed by a numeric ID.
    public async Task<IActionResult> One(int id) // Returns one complete public profile.
    { // Begins the single-profile action.
        var barber = await db.Barbers.AsNoTracking() // Reads the record without tracking changes.
            .Where(x => x.BarberId == id && x.IsActive && x.FirstName != "" && x.LastName != "" && x.ProfileImageUrl != "") // Finds the requested profile only if it is complete and active.
            .Select(x => new { x.BarberId, x.FirstName, x.LastName, x.Speciality, x.Bio, x.ProfileImageUrl }) // Shapes the response for the frontend.
            .FirstOrDefaultAsync(); // Returns the matching profile or null.
        return barber is null ? NotFound() : Ok(barber); // Returns HTTP 404 for an unavailable profile or HTTP 200 for a match.
    } // Ends the single-profile action.
} // Ends the controller.