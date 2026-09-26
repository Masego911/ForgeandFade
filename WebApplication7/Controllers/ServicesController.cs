using ForgeAndFade.Api.Data; // Accesses the existing database.
using Microsoft.AspNetCore.Mvc; // Defines HTTP endpoints.
using Microsoft.EntityFrameworkCore; // Executes asynchronous queries.
namespace ForgeAndFade.Api.Controllers; // Groups catalogue routes.
[ApiController, Route("api/services")] public class ServicesController(ApplicationDbContext db) : ControllerBase // Exposes read-only bookable services.
{
    [HttpGet] public async Task<IActionResult> All() => Ok(await db.Services.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.ServiceId).Select(x => new { x.ServiceId, x.ServiceName, x.Description, x.Price, x.DurationMinutes }).ToListAsync()); // Returns active services.
    [HttpGet("{id:int}")] public async Task<IActionResult> One(int id) { var service = await db.Services.AsNoTracking().Where(x => x.ServiceId == id && x.IsActive).Select(x => new { x.ServiceId, x.ServiceName, x.Description, x.Price, x.DurationMinutes }).FirstOrDefaultAsync(); return service is null ? NotFound() : Ok(service); } // Returns one active service.
} // Ends service routes.
