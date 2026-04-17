using Microsoft.EntityFrameworkCore;
using F1Jokers.Data; // Zorgt ervoor dat hij AppDbContext kan vinden

namespace F1Jokers
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // ============================================================
            // DATABASE CONNECTIE TOEVOEGEN
            // ============================================================
            // 1. Haal de connectiestring uit appsettings.json
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            // 2. Registreer de AppDbContext met MySQL (Pomelo)
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
            // ============================================================

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            
            // app.UseAuthentication(); // Zodra we tokens hebben toegevoegd
            app.UseAuthorization();

            app.MapStaticAssets();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Account}/{action=Inloggen}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}