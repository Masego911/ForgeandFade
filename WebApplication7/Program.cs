using ForgeAndFade.Api.Data; // Makes the database context available to the application.
using Microsoft.AspNetCore.Authentication.Cookies; // Uses server-issued, HTTP-only authentication cookies.
using Microsoft.EntityFrameworkCore; // Configures EF Core for SQL Server.
var builder = WebApplication.CreateBuilder(args); // Loads application configuration.
builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles); // Registers API controllers.
builder.Services.AddEndpointsApiExplorer(); // Exposes endpoint metadata.
builder.Services.AddSwaggerGen(); // Provides development API documentation.
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))); // Keeps the supplied SQL Server database.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options => { options.Cookie.HttpOnly = true; options.Cookie.SameSite = SameSiteMode.Lax; options.Cookie.SecurePolicy = CookieSecurePolicy.Always; options.ExpireTimeSpan = TimeSpan.FromDays(7); options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = 401; return Task.CompletedTask; }; options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = 403; return Task.CompletedTask; }; }); // Keeps credentials out of browser-accessible storage.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddAuthorization(); // Enables protected customer actions.
var app = builder.Build(); // Creates the HTTP application.
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); } // Exposes API documentation locally.
app.UseHttpsRedirection(); // Redirects HTTP to HTTPS.
app.UseAuthentication(); // Reads the signed account cookie.
app.UseAuthorization(); // Enforces protected endpoints.
app.UseDefaultFiles(); // Serves the compiled React application's index page.
app.UseStaticFiles(); // Serves compiled assets.
app.MapControllers(); // Enables the API routes.
app.MapFallbackToFile("index.html"); // Allows client-side route refreshes.
{ using var scope = app.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(); await db.Database.MigrateAsync(); await DemoSeed.Apply(db); } // Applies migrations, retires duplicate services and adds missing catalogue names.
app.Run(); // Starts the web server.
