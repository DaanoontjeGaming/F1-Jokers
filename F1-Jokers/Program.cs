using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using F1Jokers.Data;
using F1Jokers.Services;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Controllers en Views toevoegen aan de container
builder.Services.AddControllersWithViews();

// Database verbinding (MySQL) configureren via de AppDbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=localhost;Database=F1-Jokers;User=root;Password=;";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// HttpClient registreren ten behoeve van de externe F1 API
builder.Services.AddHttpClient<F1ApiService>();

// Applicatieservices registreren voor dependency injection
builder.Services.AddScoped<PuntenService>();

// Achtergrondservice (robot) voor automatische deadline-verwerking inschakelen
builder.Services.AddHostedService<VoorspellingKopieerService>();

// Authenticatie-cookie configureren met expliciete claim-overrides tegen exceptions
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Inloggen";
        options.AccessDeniedPath = "/Home/Index";
        options.Cookie.Name = "F1JokersAuthCookie";

        options.Events.OnValidatePrincipal = async context =>
        {
            var principal = context.Principal;
            if (principal?.Identity is ClaimsIdentity identity)
            {
                var roleClaim = principal.FindFirst(ClaimTypes.Role);
                if (roleClaim != null && !identity.HasClaim(c => c.Type == identity.RoleClaimType))
                {
                    identity.AddClaim(new Claim(identity.RoleClaimType, roleClaim.Value));
                }
            }
            await Task.CompletedTask;
        };
    });

var app = builder.Build();

// HTTP request pipeline configureren
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Authenticatie moet ALTIJD vóór Autorisatie staan
app.UseAuthentication();
app.UseAuthorization();

// Standaard MVC-route middleware mapping
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();